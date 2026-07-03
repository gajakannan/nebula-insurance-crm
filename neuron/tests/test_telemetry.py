import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app import telemetry as tel
from app.actions import ActionDispatcher
from app.auth import persona_from_token
from app.bootstrap import build_runtime
from app.engine_client import EngineResponse
from app.orchestration.glance import GlanceAssembler
from app.orchestration.zone_heads import ZonePayload


class RecordingTelemetryTool:
    name = "engine.telemetry.ingest"

    def __init__(self, fail=False):
        self.batches = []
        self._fail = fail

    async def invoke(self, *, user_token=None, json=None, path_params=None, params=None, headers=None):
        self.batches.append({"user_token": user_token, "json": json})
        if self._fail:
            raise RuntimeError("telemetry sink down")
        return None

    def events(self):
        return [e for b in self.batches for e in (b["json"] or {}).get("events", [])]


class FakeEngineSender:
    """Routes the outreach tool POSTs + the rowVersion GET (telemetry goes to a fake tool)."""

    async def __call__(self, *, method, url, headers, json, params):
        if url.endswith("/outreach-draft"):
            return EngineResponse(201, {"timelineEventId": "evt-1", "renewalId": "r-1"})
        if url.endswith("/outreach-mock-send"):
            return EngineResponse(201, {"transition": {"id": "tr-1", "fromState": "Identified", "toState": "Outreach"}})
        if method == "GET" and url.endswith("/renewals/r-1"):
            return EngineResponse(200, {"id": "r-1", "rowVersion": "0", "currentStatus": "Identified"})
        return EngineResponse(404, None)


# --- build_event shape (PII boundary is structural) -------------------------


class BuildEventTest(unittest.TestCase):
    def test_only_present_correlation_fields_are_included(self):
        e = tel.build_event(tel.MOCK_SENT, "u-1", thread_id="t-1", renewal_id="r-1")
        self.assertEqual(e["event_name"], "mock-sent")
        self.assertEqual(e["event_version"], 1)
        self.assertEqual(e["user_id"], "u-1")
        self.assertIn("timestamp", e)
        self.assertNotIn("persona", e)
        # Only the metric name + correlation ids — nothing that could carry a draft body.
        self.assertEqual(set(e), {"event_name", "event_version", "timestamp", "user_id", "thread_id", "renewal_id"})

    def test_daily_active_carries_persona_only(self):
        e = tel.build_event(tel.COMPANION_DAILY_ACTIVE, "u-1", persona="underwriter")
        self.assertEqual(e["persona"], "underwriter")
        self.assertNotIn("renewal_id", e)
        self.assertNotIn("thread_id", e)


# --- persona (best-effort, telemetry-only) ----------------------------------


class PersonaFromTokenTest(unittest.TestCase):
    @staticmethod
    def _token(claims):
        import base64
        import json

        payload = base64.urlsafe_b64encode(json.dumps(claims).encode()).decode().rstrip("=")
        return f"header.{payload}.sig"

    def test_defaults_to_underwriter_when_unknown(self):
        self.assertEqual(persona_from_token("not-a-jwt"), "underwriter")

    def test_distribution_role_detected(self):
        self.assertEqual(persona_from_token(self._token({"sub": "u", "roles": ["DistributionManager"]})), "distribution")

    def test_underwriter_group(self):
        self.assertEqual(persona_from_token(self._token({"sub": "u", "groups": ["Underwriter"]})), "underwriter")


# --- glance emission (DAU + needs-attention-surfaced per renewal) -----------


class GlanceTelemetryTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.rt = build_runtime()
        self.telemetry = RecordingTelemetryTool()
        self.rt.tools._tools["engine.telemetry.ingest"] = self.telemetry

        async def _content_renewals(ctx):
            return ZonePayload(
                "renewals", "content", title="Renewals",
                component="renewals.needs_attention_list",
                props={"items": [
                    {"renewalId": "r-1", "accountName": "Acme", "expiresInDays": 10,
                     "workflowState": "Identified", "noBrokerContact30d": True, "canDraftOutreach": True},
                    {"renewalId": "r-2", "accountName": "Globex", "expiresInDays": 3,
                     "workflowState": "Outreach", "noBrokerContact30d": False, "canDraftOutreach": True},
                ]},
            ).validated()

        self.rt.agents.get("crm.renewals.head").handler.build_zone = _content_renewals
        self.assembler = GlanceAssembler(self.rt)

    async def test_emits_dau_and_surfaced_per_renewal_correlated(self):
        result = await self.assembler.assemble(user_token="tok", owner_user_id="uw-1")
        events = self.telemetry.events()
        names = [e["event_name"] for e in events]

        self.assertEqual(names.count("companion-daily-active"), 1)
        surfaced = [e for e in events if e["event_name"] == "needs-attention-surfaced"]
        self.assertEqual({e["renewal_id"] for e in surfaced}, {"r-1", "r-2"})
        # Every surfaced event correlates to the glance thread (start of the baseline metric).
        for e in surfaced:
            self.assertEqual(e["thread_id"], result["thread_id"])
            self.assertEqual(e["user_id"], "uw-1")


# --- action emission (draft-ready primary end + secondary counts) -----------


class ActionTelemetryTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.rt = build_runtime()
        self.rt.engine_client._sender = FakeEngineSender()
        self.telemetry = RecordingTelemetryTool()
        self.rt.tools._tools["engine.telemetry.ingest"] = self.telemetry
        self.dispatcher = ActionDispatcher(self.rt)

    async def test_draft_emits_ready_generated_and_actioned_correlated(self):
        message = await self.dispatcher.dispatch(
            action_type="draft_outreach", action_id="a", payload={"renewalId": "r-1", "accountName": "Acme"},
            thread_id=None, user_token="tok", owner_user_id="uw-1",
        )
        thread_id = message["thread_id"]
        events = self.telemetry.events()
        by_name = {e["event_name"]: e for e in events}

        self.assertIn("draft-ready", by_name)
        self.assertIn("draft-generated", by_name)
        self.assertIn("attention-renewal-actioned", by_name)
        # draft-ready (primary end) correlates by the same renewal_id a surfaced event used.
        self.assertEqual(by_name["draft-ready"]["renewal_id"], "r-1")
        self.assertEqual(by_name["draft-ready"]["thread_id"], thread_id)

    async def test_mock_send_emits_mock_sent(self):
        await self.dispatcher.dispatch(
            action_type="mock_send", action_id="a",
            payload={"renewalId": "r-1", "editedBody": "Hi, can we connect about the renewal?"},
            thread_id=None, user_token="tok", owner_user_id="uw-1",
        )
        self.assertTrue(any(e["event_name"] == "mock-sent" and e["renewal_id"] == "r-1"
                            for e in self.telemetry.events()))

    async def test_telemetry_failure_does_not_break_the_draft_flow(self):
        # Fire-and-forget: a failing telemetry sink must not break the user action.
        self.rt.tools._tools["engine.telemetry.ingest"] = RecordingTelemetryTool(fail=True)
        message = await self.dispatcher.dispatch(
            action_type="draft_outreach", action_id="a", payload={"renewalId": "r-1", "accountName": "Acme"},
            thread_id=None, user_token="tok", owner_user_id="uw-1",
        )
        app_parts = [p for p in message["parts"] if p["part_type"] == "app"]
        self.assertEqual(app_parts[0]["component"], "outreach.draft_editor")  # draft still succeeded


if __name__ == "__main__":
    unittest.main()
