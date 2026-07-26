"""F0039-S0001 — the durable store, against real Postgres.

These are the tests that actually prove the story's acceptance criteria. The
in-memory suite asserts the same *behaviour*, but restart durability, the server
sequence, and idempotent appends are enforced by Postgres constraints — asserting
them against a dict would prove nothing about the shipped store.

Skipped (not failed) when no database is reachable, so the unit suite still runs on a
machine without Docker. CI and the feature run set ``NEURON_TEST_POSTGRES_DSN``.
"""

from __future__ import annotations

import asyncio
import os
import sys
import unittest
import uuid

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.errors import (
    InvalidThreadTitleError,
    PersistenceUnavailableError,
    ThreadNotVisibleError,
)
from app.persistence.migrate import apply_migrations
from app.persistence.postgres import PostgresNeuronRepository

DSN = os.environ.get(
    "NEURON_TEST_POSTGRES_DSN",
    "postgresql://postgres:postgres@127.0.0.1:5433/nebula",
)


def _database_available() -> bool:
    try:
        import psycopg

        with psycopg.connect(DSN, connect_timeout=3):
            return True
    except Exception:
        return False


AVAILABLE = _database_available()
skip_without_db = unittest.skipUnless(
    AVAILABLE, f"no Postgres reachable at NEURON_TEST_POSTGRES_DSN ({DSN.split('@')[-1]})"
)


@skip_without_db
class PostgresStoreTestBase(unittest.IsolatedAsyncioTestCase):
    """Each test gets a unique owner id, so tests never see each other's rows."""

    @classmethod
    def setUpClass(cls):
        asyncio.run(apply_migrations(DSN, verbose=False))

    async def asyncSetUp(self):
        self.repo = PostgresNeuronRepository(DSN, min_size=1, max_size=4)
        await self.repo.startup()
        self.owner = f"uw-{uuid.uuid4()}"
        self.other = f"uw-{uuid.uuid4()}"

    async def asyncTearDown(self):
        await self.repo.shutdown()

    async def _append(self, thread_id, text, **kwargs):
        return await self.repo.add_message(
            thread_id, self.owner, role="user",
            parts=[("text", {"part_type": "text", "text": text})], **kwargs,
        )


class DurabilityTest(PostgresStoreTestBase):
    async def test_thread_and_messages_survive_a_new_connection_pool(self):
        """Restart durability: a *new* repository instance (new pool, as a restarted
        process would have) reads back exactly what the old one wrote."""
        thread = await self.repo.create_thread(self.owner, title="Renewals")
        await self._append(thread.id, "first")
        await self._append(thread.id, "second")

        # Simulate the process restart: drop the pool entirely and open a new one.
        await self.repo.shutdown()
        restarted = PostgresNeuronRepository(DSN, min_size=1, max_size=2)
        await restarted.startup()
        try:
            reloaded = await restarted.get_thread(thread.id, self.owner)
            self.assertIsNotNone(reloaded)
            self.assertEqual(reloaded.title, "Renewals")
            history = await restarted.get_messages(thread.id, self.owner)
            self.assertEqual([m.sequence for m in history], [1, 2])
            self.assertEqual(
                [m.parts[0].content_json["text"] for m in history], ["first", "second"]
            )
        finally:
            await restarted.shutdown()
        await self.repo.startup()  # restore for asyncTearDown

    async def test_message_parts_round_trip_as_jsonb(self):
        thread = await self.repo.create_thread(self.owner)
        message = await self.repo.add_message(
            thread.id, self.owner, role="assistant",
            parts=[
                ("status", {"part_type": "status", "state": "working"}),
                ("text", {"part_type": "text", "text": "done"}),
            ],
        )
        self.assertEqual([p.ordinal for p in message.parts], [0, 1])
        self.assertEqual(message.parts[1].content_json["text"], "done")


class ServerSequenceTest(PostgresStoreTestBase):
    async def test_sequence_is_server_assigned_and_gapless(self):
        thread = await self.repo.create_thread(self.owner)
        seqs = [(await self._append(thread.id, str(i))).sequence for i in range(5)]
        self.assertEqual(seqs, [1, 2, 3, 4, 5])

    async def test_concurrent_appends_get_unique_sequences(self):
        """The allocator is an UPDATE ... RETURNING under a row lock, so racing
        appends serialize instead of colliding on the unique index."""
        thread = await self.repo.create_thread(self.owner)
        results = await asyncio.gather(
            *(self._append(thread.id, f"m{i}") for i in range(8))
        )
        sequences = sorted(m.sequence for m in results)
        self.assertEqual(sequences, list(range(1, 9)))
        self.assertEqual(len(set(sequences)), 8)

    async def test_history_orders_by_sequence_not_timestamp(self):
        thread = await self.repo.create_thread(self.owner)
        for i in range(4):
            await self._append(thread.id, f"m{i}")
        history = await self.repo.get_messages(thread.id, self.owner)
        self.assertEqual([m.sequence for m in history], [1, 2, 3, 4])

    async def test_history_resumes_after_a_cursor(self):
        thread = await self.repo.create_thread(self.owner)
        for i in range(5):
            await self._append(thread.id, f"m{i}")
        page = await self.repo.get_messages(thread.id, self.owner, after_sequence=3)
        self.assertEqual([m.sequence for m in page], [4, 5])

    async def test_last_page_is_short_without_error(self):
        thread = await self.repo.create_thread(self.owner)
        for i in range(3):
            await self._append(thread.id, f"m{i}")
        page = await self.repo.get_messages(thread.id, self.owner, limit=10)
        self.assertEqual(len(page), 3)


class IdempotentAppendTest(PostgresStoreTestBase):
    async def test_duplicate_client_key_returns_the_original_row(self):
        thread = await self.repo.create_thread(self.owner)
        first = await self._append(thread.id, "hello", client_message_key="k-1")
        repeat = await self._append(thread.id, "hello", client_message_key="k-1")
        self.assertEqual(first.id, repeat.id)
        history = await self.repo.get_messages(thread.id, self.owner)
        self.assertEqual(len(history), 1)

    async def test_concurrent_duplicate_appends_write_one_row(self):
        """The scoped partial unique index — not application logic — is what makes
        a double-submit safe."""
        thread = await self.repo.create_thread(self.owner)
        await asyncio.gather(
            *(self._append(thread.id, "same", client_message_key="dup") for _ in range(5)),
            return_exceptions=True,
        )
        history = await self.repo.get_messages(thread.id, self.owner)
        self.assertEqual(len(history), 1)

    async def test_daily_brief_key_is_not_duplicated_across_rerenders(self):
        thread = await self.repo.create_thread(self.owner)
        key = "daily-brief-2026-07-25"
        await self.repo.add_message(
            thread.id, self.owner, role="assistant",
            parts=[("text", {"part_type": "text", "text": "Here's your day."})],
            client_message_key=key,
        )
        await self.repo.add_message(
            thread.id, self.owner, role="assistant",
            parts=[("text", {"part_type": "text", "text": "Here's your day."})],
            client_message_key=key,
        )
        history = await self.repo.get_messages(thread.id, self.owner)
        self.assertEqual(len(history), 1)

    async def test_thread_creation_is_idempotent_by_key(self):
        key = f"brief-{uuid.uuid4()}"
        first = await self.repo.create_thread(self.owner, idempotency_key=key)
        repeat = await self.repo.create_thread(self.owner, idempotency_key=key)
        self.assertEqual(first.id, repeat.id)

    async def test_same_idempotency_key_is_scoped_per_owner(self):
        key = f"brief-{uuid.uuid4()}"
        mine = await self.repo.create_thread(self.owner, idempotency_key=key)
        theirs = await self.repo.create_thread(self.other, idempotency_key=key)
        self.assertNotEqual(mine.id, theirs.id)


class OwnerScopingTest(PostgresStoreTestBase):
    async def test_non_owner_cannot_read_thread(self):
        thread = await self.repo.create_thread(self.owner)
        self.assertIsNone(await self.repo.get_thread(thread.id, self.other))

    async def test_non_owner_gets_empty_history_not_another_users_messages(self):
        thread = await self.repo.create_thread(self.owner)
        await self._append(thread.id, "private")
        self.assertEqual(await self.repo.get_messages(thread.id, self.other), [])

    async def test_non_owner_cannot_append(self):
        thread = await self.repo.create_thread(self.owner)
        with self.assertRaises(ThreadNotVisibleError):
            await self.repo.add_message(
                thread.id, self.other, role="user",
                parts=[("text", {"part_type": "text", "text": "x"})],
            )

    async def test_non_owner_rename_and_delete_fail_closed_like_a_404(self):
        thread = await self.repo.create_thread(self.owner)
        with self.assertRaises(ThreadNotVisibleError):
            await self.repo.rename_thread(thread.id, self.other, "hijacked")
        with self.assertRaises(ThreadNotVisibleError):
            await self.repo.delete_thread(thread.id, self.other)
        # And the owner's thread is untouched by the attempts.
        still = await self.repo.get_thread(thread.id, self.owner)
        self.assertIsNotNone(still)

    async def test_unknown_thread_is_indistinguishable_from_a_foreign_one(self):
        """A non-owner must not be able to probe for thread existence: both cases
        raise the identical error."""
        owned = await self.repo.create_thread(self.owner)
        missing = str(uuid.uuid4())
        with self.assertRaises(ThreadNotVisibleError) as foreign:
            await self.repo.rename_thread(owned.id, self.other, "x")
        with self.assertRaises(ThreadNotVisibleError) as absent:
            await self.repo.rename_thread(missing, self.other, "x")
        self.assertEqual(type(foreign.exception), type(absent.exception))
        self.assertEqual(foreign.exception.status, absent.exception.status)

    async def test_malformed_thread_id_is_not_found_not_a_crash(self):
        self.assertIsNone(await self.repo.get_thread("not-a-uuid", self.owner))


class ThreadLifecycleTest(PostgresStoreTestBase):
    async def test_rename_persists_and_bumps_updated_at(self):
        thread = await self.repo.create_thread(self.owner, title="Old")
        renamed = await self.repo.rename_thread(thread.id, self.owner, "New")
        self.assertEqual(renamed.title, "New")
        self.assertGreaterEqual(renamed.updated_at, thread.updated_at)

    async def test_rename_rejects_empty_and_over_length_titles(self):
        thread = await self.repo.create_thread(self.owner)
        with self.assertRaises(InvalidThreadTitleError):
            await self.repo.rename_thread(thread.id, self.owner, "   ")
        with self.assertRaises(InvalidThreadTitleError):
            await self.repo.rename_thread(thread.id, self.owner, "x" * 5000)

    async def test_soft_deleted_thread_disappears_from_owner_views(self):
        thread = await self.repo.create_thread(self.owner)
        await self.repo.delete_thread(thread.id, self.owner)
        self.assertIsNone(await self.repo.get_thread(thread.id, self.owner))
        page = await self.repo.list_threads(self.owner)
        self.assertNotIn(thread.id, [t.id for t in page.items])

    async def test_appending_bumps_thread_updated_at_transactionally(self):
        thread = await self.repo.create_thread(self.owner)
        await self._append(thread.id, "hi")
        refreshed = await self.repo.get_thread(thread.id, self.owner)
        self.assertGreaterEqual(refreshed.updated_at, thread.updated_at)

    async def test_empty_owner_lists_nothing(self):
        page = await self.repo.list_threads(f"uw-{uuid.uuid4()}")
        self.assertEqual(page.items, [])
        self.assertIsNone(page.next_cursor)

    async def test_listing_pages_with_a_stable_cursor(self):
        created = [await self.repo.create_thread(self.owner, title=f"t{i}") for i in range(5)]
        first = await self.repo.list_threads(self.owner, limit=2)
        self.assertEqual(len(first.items), 2)
        self.assertIsNotNone(first.next_cursor)
        second = await self.repo.list_threads(self.owner, limit=2, cursor=first.next_cursor)
        self.assertEqual(len(second.items), 2)
        # No thread appears on two pages.
        self.assertFalse({t.id for t in first.items} & {t.id for t in second.items})
        self.assertLessEqual(
            len({t.id for t in first.items} | {t.id for t in second.items}), len(created)
        )

    async def test_last_page_reports_no_next_cursor(self):
        for i in range(3):
            await self.repo.create_thread(self.owner, title=f"t{i}")
        page = await self.repo.list_threads(self.owner, limit=10)
        self.assertIsNone(page.next_cursor)


class FailSafeTest(PostgresStoreTestBase):
    async def test_writes_fail_closed_when_the_pool_is_not_open(self):
        """Persistence failure must surface as a typed unavailable error so the caller
        aborts the turn *before* routing (S0001 business rule 4) rather than dispatching
        an unpersisted message."""
        closed = PostgresNeuronRepository(DSN)
        with self.assertRaises(PersistenceUnavailableError):
            await closed.create_thread(self.owner)

    async def test_unavailable_error_carries_no_raw_message_text(self):
        closed = PostgresNeuronRepository(DSN)
        secret = "policy 12345 for Jane Doe"
        try:
            await closed.add_message(
                str(uuid.uuid4()), self.owner, role="user",
                parts=[("text", {"part_type": "text", "text": secret})],
            )
        except PersistenceUnavailableError as exc:
            self.assertNotIn(secret, str(exc))
            self.assertNotIn("Jane", str(exc))
        else:  # pragma: no cover
            self.fail("expected PersistenceUnavailableError")


if __name__ == "__main__":
    unittest.main()
