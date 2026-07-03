import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.actions import ActionDispatcher, ContentConstraintError
from app.bootstrap import build_runtime
from app.engine_client import EngineResponse
from app.errors import UnknownActionError
from app.schemas import get_validator

_CONTEXT = {
    "renewalId": "r-1", "accountName": "Acme Mfg", "brokerName": "Atlas Brokerage",
    "workflowState": "Identified", "expiryDate": "2026-07-13", "canDraftOutreach": True, "timeline": [],
}


class FakeTool:
    name = "engine.renewals.companion_context"

    def __init__(self, result):
        self._result = result
        self.calls = []

    async def invoke(self, *, user_token, path_params=None, json=None, params=None, headers=None):
        self.calls.append({"user_token": user_token, "path_params": path_params})
        return self._result


class FakeEngineSender:
    """Routes engine calls (tool POSTs + the rowVersion GET) at the transport layer."""

    def __init__(self):
        self.calls = []

    async def __call__(self, *, method, url, headers, json, params):
        self.calls.append({"method": method, "url": url, "headers": headers, "json": json})
        if url.endswith("/outreach-draft"):
            return EngineResponse(201, {"timelineEventId": "evt-1", "renewalId": "r-1", "internalOnly": True})
        if url.endswith("/outreach-mock-send"):
            return EngineResponse(201, {"transition": {"id": "tr-1", "fromState": "Identified", "toState": "Outreach"}, "simulatedSendEventId": "evt-2"})
        if method == "GET" and url.endswith("/renewals/r-1"):
            return EngineResponse(200, {"id": "r-1", "rowVersion": "0", "currentStatus": "Identified"})
        return EngineResponse(404, None)


class ActionDispatchTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.rt = build_runtime()
        self.rt.tools._tools["engine.renewals.companion_context"] = FakeTool(_CONTEXT)
        self.sender = FakeEngineSender()
        self.rt.engine_client._sender = self.sender  # routes the outreach tools + rowVersion GET
        self.dispatcher = ActionDispatcher(self.rt)

    def _runs(self):
        return list(self.rt.repository._runs.values())

    # --- drill (F0038-S0003) ---

    async def test_drill_returns_companion_context_envelope(self):
        message = await self.dispatcher.dispatch(
            action_type="drill_renewal", action_id="a1", payload={"renewalId": "r-1"},
            thread_id=None, user_token="jwt.tok", owner_user_id="uw-42",
        )
        get_validator("message-envelope").validate(message)
        app_parts = [p for p in message["parts"] if p["part_type"] == "app"]
        self.assertEqual(app_parts[0]["component"], "renewals.companion_context")

    async def test_drill_requires_renewal_id(self):
        with self.assertRaises(UnknownActionError):
            await self.dispatcher.dispatch(action_type="drill_renewal", action_id="a", payload={}, thread_id=None, user_token="t", owner_user_id="u")

    # --- draft (F0038-S0005) ---

    async def test_draft_persists_engine_first_and_returns_editor(self):
        message = await self.dispatcher.dispatch(
            action_type="draft_outreach", action_id="a", payload={"renewalId": "r-1", "accountName": "Acme Mfg"},
            thread_id=None, user_token="jwt.tok", owner_user_id="uw-42",
        )
        get_validator("message-envelope").validate(message)
        app = [p for p in message["parts"] if p["part_type"] == "app"][0]
        self.assertEqual(app["component"], "outreach.draft_editor")
        self.assertEqual(app["props"]["renewalId"], "r-1")
        self.assertEqual(app["props"]["timelineEventId"], "evt-1")
        self.assertTrue(app["props"]["internalOnly"])
        self.assertIn("Acme Mfg", app["props"]["draftBody"])

        draft_call = next(c for c in self.sender.calls if c["url"].endswith("/outreach-draft"))
        self.assertTrue(draft_call["json"]["internal_only"])
        self.assertIn("agent_run_id", draft_call["json"]["provenance"])
        # Idempotent Neuron record references the engine timeline-event id (ADR-028 §2).
        self.assertTrue(any(r.engine_ref_id == "evt-1" and r.engine_ref_type == "timeline_event" for r in self._runs()))

    # --- mock-send (F0038-S0006) ---

    async def test_mock_send_uses_if_match_and_records_transition(self):
        message = await self.dispatcher.dispatch(
            action_type="mock_send", action_id="a",
            payload={"renewalId": "r-1", "editedBody": "Hi, can we connect about the renewal?", "accountName": "Acme Mfg"},
            thread_id=None, user_token="jwt.tok", owner_user_id="uw-42",
        )
        get_validator("message-envelope").validate(message)
        send_call = next(c for c in self.sender.calls if c["url"].endswith("/outreach-mock-send"))
        self.assertEqual(send_call["headers"].get("If-Match"), '"0"')  # just-in-time rowVersion
        self.assertTrue(send_call["json"]["simulate_delivery"])
        self.assertTrue(any(r.engine_ref_id == "tr-1" and r.engine_ref_type == "workflow_transition" for r in self._runs()))

    async def test_mock_send_rejects_forbidden_content(self):
        with self.assertRaises(ContentConstraintError):
            await self.dispatcher.dispatch(
                action_type="mock_send", action_id="a",
                payload={"renewalId": "r-1", "editedBody": "Your premium is $5000 this year."},
                thread_id=None, user_token="t", owner_user_id="u",
            )

    async def test_mock_send_requires_edited_body(self):
        with self.assertRaises(UnknownActionError):
            await self.dispatcher.dispatch(action_type="mock_send", action_id="a", payload={"renewalId": "r-1"}, thread_id=None, user_token="t", owner_user_id="u")

    # --- scope redirect ack (F0038-S0007) + unknown ---

    async def test_scope_redirect_ack_is_benign_noop(self):
        # Acknowledging a scope redirect makes no engine call and changes no state.
        message = await self.dispatcher.dispatch(
            action_type="scope_redirect_ack", action_id="a", payload={},
            thread_id=None, user_token="t", owner_user_id="u",
        )
        get_validator("message-envelope").validate(message)
        self.assertEqual(message["parts"][0]["part_type"], "status")
        self.assertEqual(message["parts"][0]["state"], "completed")
        self.assertEqual(self.sender.calls, [])  # no engine call

    async def test_unknown_action_rejected(self):
        with self.assertRaises(UnknownActionError):
            await self.dispatcher.dispatch(action_type="rm_rf", action_id="a", payload={}, thread_id=None, user_token="t", owner_user_id="u")


if __name__ == "__main__":
    unittest.main()
