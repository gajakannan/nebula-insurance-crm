import asyncio
import os
import sys
import unittest
from dataclasses import replace

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.bootstrap import build_runtime
from app.errors import UpstreamAuthError
from app.orchestration.head_executor import HeadExecutor
from app.orchestration.zone_heads import ZonePayload


class RecordingTelemetryTool:
    name = "engine.telemetry.ingest"

    def __init__(self, exc=None):
        self.exc = exc
        self.batches = []

    async def invoke(self, *, json=None, **_kwargs):
        self.batches.append(json)
        if self.exc is not None:
            raise self.exc


class HeadExecutorTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.rt = build_runtime()
        self.thread = await self.rt.task_manager.open_context("rm-1")
        self.telemetry = RecordingTelemetryTool()
        self.rt.tools._tools["engine.telemetry.ingest"] = self.telemetry
        self.executor = HeadExecutor(self.rt)

    def _handler(self, card_id):
        return self.rt.agents.get(card_id).handler

    def _events(self):
        return [
            event
            for batch in self.telemetry.batches
            for event in (batch or {}).get("events", [])
        ]

    async def _execute(self, card_id="crm.broker_activity.head", entry_point="glance"):
        return await self.executor.execute(
            card_id, self.thread, "jwt.user", "rm-1", entry_point
        )

    async def test_validates_and_returns_owned_component(self):
        async def _content(_ctx):
            return ZonePayload(
                "broker_activity",
                "content",
                component="broker_activity.recent_list",
                props={"items": [{
                    "id": "event-1",
                    "entityType": "Broker",
                    "entityId": "broker-1",
                    "eventType": "BrokerUpdated",
                    "eventDescription": "Stored description",
                    "entityName": "Atlas Brokerage",
                    "actorDisplayName": "Unknown User",
                    "occurredAt": "2026-09-01T12:00:00Z",
                }]},
            ).validated()

        self._handler("crm.broker_activity.head").build_zone = _content
        payload = await self._execute(entry_point="conversation")

        self.assertEqual(payload.zone_status, "content")
        runs = list(self.rt.repository._runs.values())
        self.assertEqual(runs[-1].state, "completed")
        event = self._events()[-1]
        self.assertEqual(event["entry_point"], "conversation")
        self.assertEqual(event["terminal_result"], "content")
        self.assertEqual(event["head_run_id"], runs[-1].id)
        for forbidden in ("Stored description", "Atlas Brokerage", "props", "jwt.user"):
            self.assertNotIn(forbidden, str(event))

    async def test_invalid_props_or_unowned_component_fails_closed(self):
        async def _invalid(_ctx):
            return ZonePayload(
                "broker_activity", "content",
                component="renewals.needs_attention_list",
                props={"items": []},
            ).validated()

        self._handler("crm.broker_activity.head").build_zone = _invalid
        payload = await self._execute()
        self.assertEqual(payload.zone_status, "error")
        self.assertEqual(list(self.rt.repository._runs.values())[-1].state, "failed")

    async def test_timeout_isolated_to_typed_zone_error(self):
        async def _slow(_ctx):
            await asyncio.sleep(0.05)
            return ZonePayload("broker_activity", "empty").validated()

        self._handler("crm.broker_activity.head").build_zone = _slow
        plan = self.rt.plans["day-at-a-glance"]
        steps = tuple(
            replace(step, timeout_ms=1)
            if step.agent == "crm.broker_activity.head"
            else step
            for step in plan.steps
        )
        self.rt.plans["day-at-a-glance"] = replace(plan, steps=steps)

        payload = await self._execute()
        self.assertEqual(payload.zone_status, "error")
        self.assertEqual(payload.detail, "Unable to load broker activity.")
        self.assertEqual(self._events()[-1]["terminal_result"], "error")

    async def test_403_is_rejected_without_data_and_401_reaches_auth_boundary(self):
        async def _forbidden(_ctx):
            raise UpstreamAuthError(403)

        self._handler("crm.broker_activity.head").build_zone = _forbidden
        payload = await self._execute()
        self.assertEqual(payload.zone_status, "error")
        self.assertEqual(self._events()[-1]["terminal_result"], "rejected")

        async def _expired(_ctx):
            raise UpstreamAuthError(401)

        self._handler("crm.broker_activity.head").build_zone = _expired
        with self.assertRaises(UpstreamAuthError):
            await self._execute()
        self.assertEqual(self._events()[-1]["terminal_result"], "rejected")

    async def test_telemetry_failure_never_changes_user_result(self):
        async def _empty(_ctx):
            return ZonePayload(
                "broker_activity", "empty", detail="No recent broker activity."
            ).validated()

        self._handler("crm.broker_activity.head").build_zone = _empty
        self.rt.tools._tools["engine.telemetry.ingest"] = RecordingTelemetryTool(
            RuntimeError("telemetry down")
        )
        payload = await self._execute()
        self.assertEqual(payload.zone_status, "empty")


if __name__ == "__main__":
    unittest.main()
