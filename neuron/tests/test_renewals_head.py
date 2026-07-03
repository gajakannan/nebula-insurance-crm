import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.errors import UpstreamUnavailableError
from app.orchestration.registries import ToolRegistry
from app.orchestration.task_manager import A2ATaskManager
from app.orchestration.zone_heads import HeadContext, RenewalsZoneHead
from app.persistence.in_memory import InMemoryNeuronRepository
from app.persistence.models import AgentRun
from app.config import load_settings
from app.orchestration.agent_card import load_cards


class FakeTool:
    name = "engine.renewals.needs_attention"

    def __init__(self, result=None, exc=None):
        self._result = result
        self._exc = exc
        self.calls = []

    async def invoke(self, *, user_token, path_params=None, json=None, params=None):
        self.calls.append({"user_token": user_token, "params": params})
        if self._exc is not None:
            raise self._exc
        return self._result


def _engine_item(**overrides):
    item = {
        "renewal_id": "11111111-1111-1111-1111-111111111111",
        "account_name": "Acme Mfg",
        "expiry_date": "2026-07-13",
        "days_to_expiry": 12,
        "workflow_state": "Identified",
        "last_broker_contact_at": None,
        "no_contact_flag": True,
        "broker_name": "Atlas Brokerage",
        "can_draft_outreach": False,
    }
    item.update(overrides)
    return item


def _renewals_head():
    card = load_cards(load_settings().cards_dir)["crm.renewals.head"]
    return RenewalsZoneHead(card)


def _ctx(tool, task_manager=None, run=None):
    tools = ToolRegistry()
    tools.register(tool)
    return HeadContext(
        user_token="jwt.tok",
        owner_user_id="uw-42",
        thread_id="t1",
        tools=tools,
        task_manager=task_manager,
        run=run,
    )


class RenewalsZoneHeadTest(unittest.IsolatedAsyncioTestCase):
    async def test_maps_engine_items_to_content_payload(self):
        tool = FakeTool(result={"data": [_engine_item(), _engine_item(days_to_expiry=-3, no_contact_flag=True)]})
        payload = await _renewals_head().build_zone(_ctx(tool))

        d = payload.to_dict()
        self.assertEqual(d["zone_status"], "content")
        self.assertEqual(d["component"], "renewals.needs_attention_list")
        items = d["props"]["items"]
        self.assertEqual(len(items), 2)
        self.assertEqual(items[0]["renewalId"], "11111111-1111-1111-1111-111111111111")
        self.assertEqual(items[0]["accountName"], "Acme Mfg")
        self.assertEqual(items[0]["expiresInDays"], 12)      # from days_to_expiry
        self.assertEqual(items[0]["noBrokerContact30d"], True)  # from no_contact_flag
        self.assertEqual(items[1]["expiresInDays"], -3)      # overdue preserved

    async def test_forwards_user_token_and_window(self):
        tool = FakeTool(result={"data": []})
        await _renewals_head().build_zone(_ctx(tool))
        self.assertEqual(tool.calls[0]["user_token"], "jwt.tok")
        self.assertEqual(tool.calls[0]["params"], {"withinDays": 90})

    async def test_empty_when_nothing_needs_attention(self):
        tool = FakeTool(result={"data": []})
        payload = await _renewals_head().build_zone(_ctx(tool))
        self.assertEqual(payload.to_dict()["zone_status"], "empty")
        self.assertNotIn("component", payload.to_dict())

    async def test_engine_error_propagates_for_zone_isolation(self):
        tool = FakeTool(exc=UpstreamUnavailableError("engine down"))
        with self.assertRaises(UpstreamUnavailableError):
            await _renewals_head().build_zone(_ctx(tool))

    async def test_records_tool_call_in_operation_store(self):
        repo = InMemoryNeuronRepository()
        tm = A2ATaskManager(repo)
        thread = tm.open_context("uw-42")
        run = repo.create_agent_run(
            AgentRun(thread_id=thread.id, plan_id="day-at-a-glance", plan_version="1.0.0",
                     card_id="crm.renewals.head", card_version="1.0.0", card_content_hash="sha256:x")
        )
        tool = FakeTool(result={"data": [_engine_item()]})
        await _renewals_head().build_zone(_ctx(tool, task_manager=tm, run=run))

        tool_calls = list(repo._tool_calls.values())
        self.assertEqual(len(tool_calls), 1)
        self.assertEqual(tool_calls[0].tool_name, "engine.renewals.needs_attention")
        self.assertEqual(tool_calls[0].agent_run_id, run.id)


if __name__ == "__main__":
    unittest.main()
