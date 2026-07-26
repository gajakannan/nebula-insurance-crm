"""F0039-S0002 — owner-scoped thread & history API.

Covers the service layer against the in-memory store and the HTTP surface through
FastAPI's TestClient. The cross-user tests are the important ones: the story requires
that user B learns *nothing* about user A's thread — not its existence, not its title,
not its contents — and that the failure is indistinguishable from a genuine 404.
"""

from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.errors import InvalidThreadTitleError, ThreadNotVisibleError
from app.persistence.in_memory import InMemoryNeuronRepository
from app.threads import (
    DEFAULT_FREE_FORM_TITLE,
    InvalidAnchorError,
    ThreadService,
    clamp,
    initial_title,
    validate_anchor,
)

OWNER = "uw-A"
OTHER = "uw-B"


class _FakeRuntime:
    """Only the repository matters for the thread service."""

    def __init__(self, repo):
        self.repository = repo


def _service():
    return ThreadService(_FakeRuntime(InMemoryNeuronRepository()))


class InitialTitleTest(unittest.TestCase):
    def test_supplied_title_wins(self):
        self.assertEqual(initial_title("free_form", None, "My chat"), "My chat")

    def test_free_form_gets_the_default(self):
        self.assertEqual(initial_title("free_form", None, None), DEFAULT_FREE_FORM_TITLE)

    def test_domain_anchor_gets_a_stable_label(self):
        self.assertEqual(initial_title("domain", "day-at-a-glance", None), "Day at a Glance")

    def test_unknown_domain_is_still_deterministic(self):
        first = initial_title("domain", "some-zone", None)
        second = initial_title("domain", "some-zone", None)
        self.assertEqual(first, second)
        self.assertEqual(first, "Some Zone")

    def test_record_anchor_references_its_record(self):
        self.assertEqual(initial_title("record", "RN-1", None), "Record RN-1")

    def test_invalid_supplied_title_is_rejected_not_silently_defaulted(self):
        with self.assertRaises(InvalidThreadTitleError):
            initial_title("free_form", None, "   ")


class AnchorValidationTest(unittest.TestCase):
    def test_unknown_anchor_type_rejected(self):
        with self.assertRaises(InvalidAnchorError):
            validate_anchor("galaxy", None)

    def test_domain_anchor_requires_a_ref(self):
        with self.assertRaises(InvalidAnchorError):
            validate_anchor("domain", None)

    def test_free_form_drops_a_stray_ref(self):
        self.assertEqual(validate_anchor("free_form", "ignored"), ("free_form", None))

    def test_over_long_ref_rejected(self):
        with self.assertRaises(InvalidAnchorError):
            validate_anchor("record", "x" * 500)


class PageBoundTest(unittest.TestCase):
    def test_missing_value_uses_the_default(self):
        self.assertEqual(clamp(None, 20, 100), 20)

    def test_garbage_uses_the_default(self):
        self.assertEqual(clamp("abc", 20, 100), 20)

    def test_oversized_request_is_capped(self):
        self.assertEqual(clamp(10_000, 20, 100), 100)

    def test_zero_or_negative_is_floored_to_one(self):
        self.assertEqual(clamp(0, 20, 100), 1)
        self.assertEqual(clamp(-5, 20, 100), 1)


class ThreadServiceTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.svc = _service()

    async def test_create_returns_the_api_shape_without_owner(self):
        thread = await self.svc.create(OWNER, anchor_type="free_form")
        self.assertEqual(
            set(thread),
            {"thread_id", "anchor_type", "anchor_ref", "title", "created_at",
             "updated_at", "last_sequence"},
        )
        # owner_user_id is server-derived and must never be echoed to a client.
        self.assertNotIn("owner_user_id", thread)

    async def test_new_thread_reports_zero_last_sequence(self):
        thread = await self.svc.create(OWNER)
        self.assertEqual(thread["last_sequence"], 0)

    async def test_last_sequence_tracks_appends(self):
        created = await self.svc.create(OWNER)
        for text in ("a", "b"):
            await self.svc._repo.add_message(
                created["thread_id"], OWNER, role="user",
                parts=[("text", {"part_type": "text", "text": text})],
            )
        fetched = await self.svc.get(created["thread_id"], OWNER)
        self.assertEqual(fetched["last_sequence"], 2)

    async def test_rename_changes_only_the_title(self):
        created = await self.svc.create(OWNER, anchor_type="domain", anchor_ref="renewals")
        renamed = await self.svc.rename(created["thread_id"], OWNER, "Q3 renewals")
        self.assertEqual(renamed["title"], "Q3 renewals")
        # The anchor is immutable — rename must not disturb it.
        self.assertEqual(renamed["anchor_type"], created["anchor_type"])
        self.assertEqual(renamed["anchor_ref"], created["anchor_ref"])

    async def test_rename_rejects_empty_and_over_length(self):
        created = await self.svc.create(OWNER)
        with self.assertRaises(InvalidThreadTitleError):
            await self.svc.rename(created["thread_id"], OWNER, "")
        with self.assertRaises(InvalidThreadTitleError):
            await self.svc.rename(created["thread_id"], OWNER, "x" * 200)

    async def test_rename_rejects_control_characters(self):
        created = await self.svc.create(OWNER)
        with self.assertRaises(InvalidThreadTitleError):
            await self.svc.rename(created["thread_id"], OWNER, "bad\x00title")

    async def test_delete_removes_it_from_listing_and_reads(self):
        created = await self.svc.create(OWNER)
        await self.svc.delete(created["thread_id"], OWNER)
        with self.assertRaises(ThreadNotVisibleError):
            await self.svc.get(created["thread_id"], OWNER)
        listing = await self.svc.list(OWNER)
        self.assertNotIn(created["thread_id"], [t["thread_id"] for t in listing["data"]])

    async def test_owner_with_no_threads_gets_an_empty_list(self):
        listing = await self.svc.list("uw-nobody")
        self.assertEqual(listing["data"], [])
        self.assertIsNone(listing["next_cursor"])

    async def test_listing_pages_and_last_page_has_no_cursor(self):
        for i in range(3):
            await self.svc.create(OWNER, title=f"t{i}")
        first = await self.svc.list(OWNER, limit=2)
        self.assertEqual(len(first["data"]), 2)
        self.assertIsNotNone(first["next_cursor"])
        second = await self.svc.list(OWNER, limit=2, cursor=first["next_cursor"])
        self.assertEqual(len(second["data"]), 1)
        self.assertIsNone(second["next_cursor"])


class HistoryTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.svc = _service()
        created = await self.svc.create(OWNER)
        self.thread_id = created["thread_id"]
        for i in range(5):
            await self.svc._repo.add_message(
                self.thread_id, OWNER, role="user",
                parts=[("text", {"part_type": "text", "text": f"m{i}"})],
            )

    async def test_history_returns_versioned_envelopes_not_rows(self):
        page = await self.svc.history(self.thread_id, OWNER)
        first = page["data"][0]
        self.assertEqual(first["envelope_version"], 1)
        self.assertEqual(first["thread_id"], self.thread_id)
        self.assertIn("parts", first)
        # A persistence-row field must not leak through the envelope.
        self.assertNotIn("client_message_key", first)
        self.assertNotIn("sequence", first)

    async def test_history_is_in_sequence_order(self):
        page = await self.svc.history(self.thread_id, OWNER)
        texts = [m["parts"][0]["text"] for m in page["data"]]
        self.assertEqual(texts, ["m0", "m1", "m2", "m3", "m4"])

    async def test_history_resumes_from_the_cursor(self):
        first = await self.svc.history(self.thread_id, OWNER, limit=2)
        self.assertEqual(first["next_after"], 2)
        second = await self.svc.history(
            self.thread_id, OWNER, limit=2, after=first["next_after"]
        )
        texts = [m["parts"][0]["text"] for m in second["data"]]
        self.assertEqual(texts, ["m2", "m3"])

    async def test_exhausted_history_reports_no_cursor(self):
        page = await self.svc.history(self.thread_id, OWNER, limit=50)
        self.assertIsNone(page["next_after"])

    async def test_garbage_cursor_does_not_error(self):
        page = await self.svc.history(self.thread_id, OWNER, after="not-a-number")
        self.assertEqual(len(page["data"]), 5)


class CrossUserAccessTest(unittest.IsolatedAsyncioTestCase):
    """The story's owner-scoping criteria: user B must learn nothing about A's thread."""

    async def asyncSetUp(self):
        self.svc = _service()
        created = await self.svc.create(OWNER, title="Confidential renewal")
        self.thread_id = created["thread_id"]
        await self.svc._repo.add_message(
            self.thread_id, OWNER, role="user",
            parts=[("text", {"part_type": "text", "text": "secret"})],
        )

    async def test_foreign_get_fails_closed(self):
        with self.assertRaises(ThreadNotVisibleError):
            await self.svc.get(self.thread_id, OTHER)

    async def test_foreign_rename_fails_closed(self):
        with self.assertRaises(ThreadNotVisibleError):
            await self.svc.rename(self.thread_id, OTHER, "hijacked")
        # And the original title is intact.
        mine = await self.svc.get(self.thread_id, OWNER)
        self.assertEqual(mine["title"], "Confidential renewal")

    async def test_foreign_delete_fails_closed(self):
        with self.assertRaises(ThreadNotVisibleError):
            await self.svc.delete(self.thread_id, OTHER)
        self.assertIsNotNone(await self.svc.get(self.thread_id, OWNER))

    async def test_foreign_history_fails_closed(self):
        with self.assertRaises(ThreadNotVisibleError):
            await self.svc.history(self.thread_id, OTHER)

    async def test_foreign_thread_never_appears_in_the_other_users_list(self):
        listing = await self.svc.list(OTHER)
        self.assertEqual(listing["data"], [])

    async def test_existing_and_missing_are_indistinguishable_to_a_non_owner(self):
        import uuid

        with self.assertRaises(ThreadNotVisibleError) as foreign:
            await self.svc.get(self.thread_id, OTHER)
        with self.assertRaises(ThreadNotVisibleError) as missing:
            await self.svc.get(str(uuid.uuid4()), OTHER)
        self.assertEqual(foreign.exception.status, missing.exception.status)
        self.assertEqual(foreign.exception.title, missing.exception.title)


class ThreadHttpSurfaceTest(unittest.TestCase):
    """The HTTP contract: status codes and ProblemDetails, through the real app."""

    @classmethod
    def setUpClass(cls):
        from fastapi.testclient import TestClient

        from app.main import create_app

        os.environ.setdefault("NEURON_PERSISTENCE", "memory")
        cls.client = TestClient(create_app())
        cls.client.__enter__()  # runs startup (opens the store)

    @classmethod
    def tearDownClass(cls):
        cls.client.__exit__(None, None, None)

    def _headers(self, subject="uw-http-a"):
        return {"Authorization": f"Bearer {subject}"}

    def test_create_returns_201_with_the_thread(self):
        response = self.client.post(
            "/v1/threads", json={"anchor_type": "free_form"}, headers=self._headers()
        )
        self.assertEqual(response.status_code, 201)
        self.assertIn("thread_id", response.json())

    def test_missing_token_is_401(self):
        self.assertEqual(self.client.get("/v1/threads").status_code, 401)

    def test_unknown_thread_is_404_problem_details(self):
        import uuid

        response = self.client.get(f"/v1/threads/{uuid.uuid4()}", headers=self._headers())
        self.assertEqual(response.status_code, 404)
        self.assertEqual(response.headers["content-type"], "application/problem+json")
        self.assertEqual(response.json()["status"], 404)

    def test_rename_without_title_is_400(self):
        created = self.client.post(
            "/v1/threads", json={"anchor_type": "free_form"}, headers=self._headers()
        ).json()
        response = self.client.patch(
            f"/v1/threads/{created['thread_id']}", json={}, headers=self._headers()
        )
        self.assertEqual(response.status_code, 400)

    def test_delete_returns_204_and_then_404(self):
        created = self.client.post(
            "/v1/threads", json={"anchor_type": "free_form"}, headers=self._headers()
        ).json()
        tid = created["thread_id"]
        self.assertEqual(
            self.client.delete(f"/v1/threads/{tid}", headers=self._headers()).status_code, 204
        )
        self.assertEqual(
            self.client.get(f"/v1/threads/{tid}", headers=self._headers()).status_code, 404
        )

    def test_another_user_gets_404_not_403(self):
        """A 403 would confirm the thread exists. It must be a 404, like any unknown id."""
        created = self.client.post(
            "/v1/threads", json={"anchor_type": "free_form"}, headers=self._headers("uw-owner")
        ).json()
        response = self.client.get(
            f"/v1/threads/{created['thread_id']}", headers=self._headers("uw-intruder")
        )
        self.assertEqual(response.status_code, 404)

    def test_history_endpoint_returns_envelopes(self):
        created = self.client.post(
            "/v1/threads", json={"anchor_type": "free_form"}, headers=self._headers()
        ).json()
        response = self.client.get(
            f"/v1/threads/{created['thread_id']}/messages", headers=self._headers()
        )
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["data"], [])
        self.assertIsNone(response.json()["next_after"])


if __name__ == "__main__":
    unittest.main()
