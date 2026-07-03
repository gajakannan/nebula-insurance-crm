import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.engine_client import EngineClient
from app.errors import ConfigError
from app.orchestration.agent_card import load_cards
from app.orchestration.heads import BootstrapHandler
from app.orchestration.registries import AgentRegistry, ToolRegistry
from app.tools.engine_tools import build_engine_tools


def _agent_registry() -> AgentRegistry:
    reg = AgentRegistry()
    for card in load_cards(load_settings().cards_dir).values():
        reg.register(card, BootstrapHandler(card, "test"))
    return reg


class AgentRegistryTest(unittest.TestCase):
    def setUp(self):
        self.reg = _agent_registry()

    def test_resolves_registered_agents(self):
        self.assertTrue(self.reg.has("crm.renewals.head"))
        self.assertFalse(self.reg.has("crm.unknown.head"))

    def test_heads_are_specialist_heads_only(self):
        self.assertEqual(
            self.reg.heads(),
            ["crm.broker_activity.head", "crm.pipeline.head", "crm.renewals.head", "crm.tasks.head"],
        )

    def test_active_heads_excludes_stubs(self):
        self.assertEqual(self.reg.active_heads(), ["crm.renewals.head"])

    def test_duplicate_registration_rejected(self):
        card = self.reg.get("crm.renewals.head").card
        with self.assertRaises(ConfigError):
            self.reg.register(card, BootstrapHandler(card, "dup"))


class ToolRegistryTest(unittest.TestCase):
    def setUp(self):
        self.reg = ToolRegistry()
        self.reg.register_all(build_engine_tools(EngineClient("http://engine")))

    def test_five_engine_tools_registered(self):
        self.assertEqual(
            self.reg.names(),
            [
                "engine.renewals.companion_context",
                "engine.renewals.needs_attention",
                "engine.renewals.outreach_draft",
                "engine.renewals.outreach_mock_send",
                "engine.telemetry.ingest",
            ],
        )

    def test_resolution(self):
        self.assertTrue(self.reg.has("engine.renewals.needs_attention"))
        self.assertFalse(self.reg.has("engine.renewals.nope"))

    def test_duplicate_tool_rejected(self):
        tool = self.reg.get("engine.telemetry.ingest")
        with self.assertRaises(ConfigError):
            self.reg.register(tool)


if __name__ == "__main__":
    unittest.main()
