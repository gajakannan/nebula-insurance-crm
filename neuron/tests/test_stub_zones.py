import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.orchestration.agent_card import load_cards
from app.orchestration.registries import ToolRegistry
from app.orchestration.zone_heads import HeadContext, StubZoneHead

_STUB_CARDS = ("crm.tasks.head", "crm.pipeline.head", "crm.broker_activity.head")


def _card(card_id):
    return load_cards(load_settings().cards_dir)[card_id]


def _ctx(tools=None):
    return HeadContext(user_token="tok", owner_user_id="u", thread_id="t", tools=tools)


class ExplodingTool:
    """Any invocation is a test failure — proves the stub never reads the engine."""

    name = "engine.renewals.needs_attention"

    async def invoke(self, **kwargs):
        raise AssertionError("a stub zone head must not call the engine")


class StubZoneHeadTest(unittest.IsolatedAsyncioTestCase):
    async def test_returns_typed_inactive_payload_with_no_business_data(self):
        payload = await StubZoneHead(_card("crm.tasks.head")).build_zone(_ctx())
        d = payload.to_dict()
        self.assertEqual(d["zone_status"], "inactive")
        # No component / props / actions -> no business data, nothing clickable (no CTA).
        self.assertNotIn("component", d)
        self.assertNotIn("props", d)

    async def test_makes_no_engine_call(self):
        # Even handed a tool registry, the stub must not invoke it.
        tools = ToolRegistry()
        tools.register(ExplodingTool())
        payload = await StubZoneHead(_card("crm.pipeline.head")).build_zone(_ctx(tools=tools))
        self.assertEqual(payload.to_dict()["zone_status"], "inactive")

    async def test_all_three_stub_zones_present_and_inactive(self):
        for card_id in _STUB_CARDS:
            head = StubZoneHead(_card(card_id))
            payload = await head.build_zone(_ctx())
            self.assertEqual(payload.zone_status if hasattr(payload, "zone_status") else None, "inactive")
            expected_zone = card_id[len("crm.") : -len(".head")]
            self.assertEqual(payload.zone_id, expected_zone)


if __name__ == "__main__":
    unittest.main()
