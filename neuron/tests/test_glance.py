import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.bootstrap import build_runtime
from app.orchestration.glance import GlanceAssembler
from app.schemas import get_validator

OWNER = "user-A"


class GlanceAssemblyTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.rt = build_runtime()
        self.assembler = GlanceAssembler(self.rt)
        # Keep glance-assembly tests hermetic: the live Renewals head's engine call is
        # covered in test_renewals_head.py; here it deterministically returns empty.
        from app.orchestration.zone_heads import ZonePayload

        async def _empty_renewals(ctx):
            return ZonePayload("renewals", "empty", title="Renewals", detail="No renewals need you.").validated()

        self.rt.agents.get("crm.renewals.head").handler.build_zone = _empty_renewals

        # Stub the telemetry sink (F0038-S0008) so assembly stays hermetic + inspectable.
        class _RecordingTelemetryTool:
            name = "engine.telemetry.ingest"

            def __init__(self):
                self.batches = []

            async def invoke(self, *, user_token=None, json=None, **_kw):
                self.batches.append(json)
                return None

        self.telemetry = _RecordingTelemetryTool()
        self.rt.tools._tools["engine.telemetry.ingest"] = self.telemetry

    async def test_emits_daily_active_telemetry(self):
        await self.assembler.assemble(user_token="tok", owner_user_id=OWNER)
        events = [e for b in self.telemetry.batches for e in (b or {}).get("events", [])]
        self.assertTrue(any(e["event_name"] == "companion-daily-active" for e in events))

    async def test_assembles_four_zones_with_envelope(self):
        result = await self.assembler.assemble(user_token="tok", owner_user_id=OWNER)
        self.assertIn("thread_id", result)
        self.assertEqual(len(result["zones"]), 4)
        by_id = {z["zone_id"]: z for z in result["zones"]}
        self.assertEqual(set(by_id), {"renewals", "tasks", "pipeline", "broker_activity"})
        # Renewals live-slot is empty until S0003; the three stubs are inactive (S0004).
        self.assertEqual(by_id["renewals"]["zone_status"], "empty")
        self.assertEqual(by_id["tasks"]["zone_status"], "inactive")
        self.assertEqual(by_id["pipeline"]["zone_status"], "inactive")
        self.assertEqual(by_id["broker_activity"]["zone_status"], "inactive")

    async def test_every_zone_and_message_is_schema_valid(self):
        result = await self.assembler.assemble(user_token="tok", owner_user_id=OWNER)
        zone_validator = get_validator("zone-payload")
        for zone in result["zones"]:
            zone_validator.validate(zone)
        get_validator("message-envelope").validate(result["message"])
        self.assertEqual(result["message"]["role"], "assistant")
        self.assertEqual(result["message"]["thread_id"], result["thread_id"])

    async def test_thread_and_message_persisted_owner_scoped(self):
        result = await self.assembler.assemble(user_token="tok", owner_user_id=OWNER)
        tid = result["thread_id"]
        self.assertEqual(len(self.rt.repository.get_messages(tid, OWNER)), 1)
        self.assertEqual(self.rt.repository.get_messages(tid, "user-B"), [])

    async def test_resume_existing_thread(self):
        first = await self.assembler.assemble(user_token="tok", owner_user_id=OWNER)
        again = await self.assembler.assemble(user_token="tok", owner_user_id=OWNER, thread_id=first["thread_id"])
        self.assertEqual(again["thread_id"], first["thread_id"])

    async def test_per_zone_error_isolation(self):
        async def _boom(ctx):
            raise RuntimeError("head down")

        self.rt.agents.get("crm.tasks.head").handler.build_zone = _boom
        result = await self.assembler.assemble(user_token="tok", owner_user_id=OWNER)
        by_id = {z["zone_id"]: z for z in result["zones"]}
        self.assertEqual(by_id["tasks"]["zone_status"], "error")
        # A failing zone does not blank the others.
        self.assertEqual(by_id["renewals"]["zone_status"], "empty")
        self.assertEqual(len(result["zones"]), 4)
        # No internal detail leaks in the error slot.
        self.assertNotIn("head down", by_id["tasks"].get("detail", ""))


if __name__ == "__main__":
    unittest.main()
