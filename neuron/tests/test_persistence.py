import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.errors import NeuronError, ThreadNotVisibleError
from app.persistence.in_memory import InMemoryNeuronRepository
from app.persistence.models import AgentRun, ProvenanceEvent, ToolCall

OWNER = "user-A"
OTHER = "user-B"


async def _run(repo, thread):
    return await repo.create_agent_run(
        AgentRun(
            thread_id=thread.id,
            plan_id="day-at-a-glance",
            plan_version="1.0.0",
            card_id="crm.renewals.head",
            card_version="1.0.0",
            card_content_hash="sha256:abc",
        )
    )


class ThreadOwnerScopeTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.repo = InMemoryNeuronRepository()
        self.thread = await self.repo.create_thread(OWNER, title="Day at a Glance")

    async def test_owner_can_read_own_thread(self):
        self.assertIsNotNone(await self.repo.get_thread(self.thread.id, OWNER))

    async def test_non_owner_cannot_read_thread(self):
        self.assertIsNone(await self.repo.get_thread(self.thread.id, OTHER))

    async def test_messages_are_owner_scoped(self):
        await self.repo.add_message(
            self.thread.id, OWNER, role="assistant",
            parts=[("text", {"part_type": "text", "text": "hi"})],
        )
        self.assertEqual(len(await self.repo.get_messages(self.thread.id, OWNER)), 1)
        self.assertEqual(await self.repo.get_messages(self.thread.id, OTHER), [])

    async def test_message_to_foreign_thread_rejected(self):
        with self.assertRaises(ThreadNotVisibleError):
            await self.repo.add_message(
                self.thread.id, OTHER, role="user",
                parts=[("text", {"part_type": "text", "text": "x"})],
            )

    async def test_parts_are_ordinal_ordered(self):
        msg = await self.repo.add_message(
            self.thread.id, OWNER, role="assistant",
            parts=[
                ("status", {"part_type": "status", "state": "working"}),
                ("text", {"part_type": "text", "text": "done"}),
            ],
        )
        self.assertEqual([p.ordinal for p in msg.parts], [0, 1])
        self.assertEqual([p.part_type for p in msg.parts], ["status", "text"])


class EngineRefIdempotencyTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.repo = InMemoryNeuronRepository()
        self.thread = await self.repo.create_thread(OWNER)
        self.run = await _run(self.repo, self.thread)

    async def test_first_attach_sets_reference(self):
        updated = await self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        self.assertEqual(updated.engine_ref_id, "evt-1")

    async def test_repeat_same_ref_is_noop(self):
        await self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        again = await self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        self.assertEqual(again.engine_ref_id, "evt-1")

    async def test_conflicting_ref_raises(self):
        await self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        with self.assertRaises(NeuronError):
            await self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-2")


class ProvenanceShapeTest(unittest.IsolatedAsyncioTestCase):
    async def test_provenance_carries_no_raw_prompt_field(self):
        repo = InMemoryNeuronRepository()
        thread = await repo.create_thread(OWNER)
        run = await _run(repo, thread)
        await repo.record_provenance(
            ProvenanceEvent(agent_run_id=run.id, model="mock-1", content_hash="sha256:x")
        )
        events = await repo.list_provenance(run.id)
        self.assertEqual(len(events), 1)
        # Redaction-by-shape: the record type structurally cannot hold raw text/PII.
        forbidden = {"raw_prompt", "prompt", "raw_response", "response", "pii"}
        self.assertFalse(forbidden & set(vars(events[0])))

    async def test_tool_call_records_digest_only(self):
        repo = InMemoryNeuronRepository()
        thread = await repo.create_thread(OWNER)
        run = await _run(repo, thread)
        call = await repo.record_tool_call(
            ToolCall(agent_run_id=run.id, tool_name="engine.renewals.needs_attention",
                     request_digest="sha256:req", status="ok", latency_ms=12)
        )
        self.assertEqual(call.tool_name, "engine.renewals.needs_attention")
        self.assertNotIn("args", vars(call))


# --------------------------------------------------------------------------- #
# F0039-S0001 — server-owned ordering + idempotent appends.
# These run against the in-memory store; test_postgres_store.py asserts the same
# invariants against real Postgres, which is where they are actually enforced.
# --------------------------------------------------------------------------- #


class ServerSequenceTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.repo = InMemoryNeuronRepository()
        self.thread = await self.repo.create_thread(OWNER)

    async def _append(self, text):
        return await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": text})],
        )

    async def test_sequences_are_server_assigned_and_monotonic(self):
        first = await self._append("one")
        second = await self._append("two")
        third = await self._append("three")
        self.assertEqual([first.sequence, second.sequence, third.sequence], [1, 2, 3])

    async def test_history_replays_in_sequence_order(self):
        for text in ("a", "b", "c"):
            await self._append(text)
        history = await self.repo.get_messages(self.thread.id, OWNER)
        self.assertEqual([m.sequence for m in history], [1, 2, 3])

    async def test_sequences_are_per_thread_not_global(self):
        other_thread = await self.repo.create_thread(OWNER)
        await self._append("a")
        second = await self.repo.add_message(
            other_thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "a"})],
        )
        # A second thread starts its own numbering at 1.
        self.assertEqual(second.sequence, 1)

    async def test_after_sequence_resumes_strictly_after(self):
        for text in ("a", "b", "c", "d"):
            await self._append(text)
        page = await self.repo.get_messages(self.thread.id, OWNER, after_sequence=2)
        self.assertEqual([m.sequence for m in page], [3, 4])

    async def test_limit_bounds_the_page(self):
        for text in ("a", "b", "c", "d"):
            await self._append(text)
        page = await self.repo.get_messages(self.thread.id, OWNER, limit=2)
        self.assertEqual([m.sequence for m in page], [1, 2])


class AppendIdempotencyTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.repo = InMemoryNeuronRepository()
        self.thread = await self.repo.create_thread(OWNER)

    async def test_duplicate_client_key_returns_original_row(self):
        first = await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "hello"})],
            client_message_key="key-1",
        )
        repeat = await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "hello"})],
            client_message_key="key-1",
        )
        self.assertEqual(repeat.id, first.id)
        self.assertEqual(len(await self.repo.get_messages(self.thread.id, OWNER)), 1)

    async def test_duplicate_append_does_not_burn_a_sequence(self):
        await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "a"})],
            client_message_key="key-1",
        )
        await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "a"})],
            client_message_key="key-1",
        )
        following = await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "b"})],
        )
        # The retry must not leave a gap in the conversation's ordering.
        self.assertEqual(following.sequence, 2)

    async def test_distinct_keys_create_distinct_rows(self):
        a = await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "a"})], client_message_key="k1",
        )
        b = await self.repo.add_message(
            self.thread.id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "b"})], client_message_key="k2",
        )
        self.assertNotEqual(a.id, b.id)

    async def test_thread_creation_is_idempotent_by_key(self):
        first = await self.repo.create_thread(OWNER, idempotency_key="daily-brief-2026-07-25")
        repeat = await self.repo.create_thread(OWNER, idempotency_key="daily-brief-2026-07-25")
        self.assertEqual(first.id, repeat.id)

    async def test_same_key_for_different_owners_is_not_shared(self):
        mine = await self.repo.create_thread(OWNER, idempotency_key="daily-brief")
        theirs = await self.repo.create_thread(OTHER, idempotency_key="daily-brief")
        self.assertNotEqual(mine.id, theirs.id)


if __name__ == "__main__":
    unittest.main()
