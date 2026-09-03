import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.errors import UpstreamUnavailableError
from app.orchestration.agent_card import load_cards
from app.orchestration.registries import ToolRegistry
from app.orchestration.task_manager import A2ATaskManager
from app.orchestration.zone_heads import BrokerActivityZoneHead, HeadContext
from app.persistence.in_memory import InMemoryNeuronRepository
from app.persistence.models import AgentRun


class FakeBrokerActivityTool:
    name = "engine.timeline.list_broker_activity"

    def __init__(self, result=None, exc=None):
        self.result = result
        self.exc = exc
        self.calls = []

    async def invoke(self, *, user_token=None, params=None, **_kwargs):
        self.calls.append({"user_token": user_token, "params": params})
        if self.exc is not None:
            raise self.exc
        return self.result


def _event(index=0, **overrides):
    item = {
        "id": f"event-{index}",
        "entityType": "Broker",
        "entityId": f"broker-{index}",
        "eventType": "BrokerUpdated",
        "eventDescription": f"Stored description {index}",
        "entityName": f"Broker {index}",
        "actorDisplayName": "Unknown User" if index == 0 else "Casey Smith",
        "occurredAt": f"2026-08-{31 - index:02d}T12:00:00Z",
    }
    item.update(overrides)
    return item


def _head():
    card = load_cards(load_settings().cards_dir)["crm.broker_activity.head"]
    return BrokerActivityZoneHead(card)


def _ctx(tool, task_manager=None, run=None):
    tools = ToolRegistry()
    tools.register(tool)
    return HeadContext(
        user_token="jwt.user",
        owner_user_id="rm-1",
        thread_id="thread-1",
        tools=tools,
        task_manager=task_manager,
        run=run,
    )


class BrokerActivityZoneHeadTest(unittest.IsolatedAsyncioTestCase):
    async def test_maps_exact_engine_fields_to_registered_content(self):
        tool = FakeBrokerActivityTool({"data": [_event()]})
        payload = await _head().build_zone(_ctx(tool))

        value = payload.to_dict()
        self.assertEqual(value["zone_status"], "content")
        self.assertEqual(value["component"], "broker_activity.recent_list")
        self.assertEqual(value["props"]["items"], [_event()])
        self.assertEqual(value["props"]["items"][0]["actorDisplayName"], "Unknown User")

    async def test_forwards_token_and_fixed_scoped_query(self):
        tool = FakeBrokerActivityTool({"data": []})
        await _head().build_zone(_ctx(tool))
        self.assertEqual(
            tool.calls,
            [{
                "user_token": "jwt.user",
                "params": {
                    "entityType": "Broker",
                    "page": 1,
                    "pageSize": 20,
                    "internalOnly": True,
                },
            }],
        )

    async def test_empty_is_explicit_and_has_no_component(self):
        payload = await _head().build_zone(
            _ctx(FakeBrokerActivityTool({"data": []}))
        )
        value = payload.to_dict()
        self.assertEqual(value["zone_status"], "empty")
        self.assertEqual(value["detail"], "No recent broker activity.")
        self.assertNotIn("component", value)

    async def test_defensively_caps_response_at_twenty(self):
        payload = await _head().build_zone(
            _ctx(FakeBrokerActivityTool({"data": [_event(i) for i in range(25)]}))
        )
        self.assertEqual(len(payload.props["items"]), 20)

    async def test_rejects_non_broker_or_malformed_rows(self):
        for row in (_event(entityType="Policy"), {"id": "missing"}):
            with self.subTest(row=row):
                with self.assertRaises(ValueError):
                    await _head().build_zone(
                        _ctx(FakeBrokerActivityTool({"data": [row]}))
                    )

    async def test_engine_error_propagates_to_shared_isolation_boundary(self):
        with self.assertRaises(UpstreamUnavailableError):
            await _head().build_zone(
                _ctx(FakeBrokerActivityTool(exc=UpstreamUnavailableError("down")))
            )

    async def test_records_only_bounded_tool_provenance(self):
        repo = InMemoryNeuronRepository()
        task_manager = A2ATaskManager(repo)
        thread = await task_manager.open_context("rm-1")
        run = await repo.create_agent_run(
            AgentRun(
                thread_id=thread.id,
                plan_id="day-at-a-glance",
                plan_version="1.1.0",
                card_id="crm.broker_activity.head",
                card_version="1.1.0",
                card_content_hash="sha256:test",
            )
        )

        await _head().build_zone(
            _ctx(
                FakeBrokerActivityTool({"data": [_event()]}),
                task_manager=task_manager,
                run=run,
            )
        )

        calls = list(repo._tool_calls.values())
        self.assertEqual(len(calls), 1)
        self.assertEqual(calls[0].tool_name, "engine.timeline.list_broker_activity")
        self.assertNotIn("Broker 0", calls[0].request_digest)
        self.assertNotIn("Stored description", calls[0].request_digest)


if __name__ == "__main__":
    unittest.main()
