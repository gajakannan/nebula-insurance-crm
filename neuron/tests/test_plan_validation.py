import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.engine_client import EngineClient
from app.errors import PlanValidationError, UnknownReferenceError
from app.orchestration.agent_card import load_cards
from app.orchestration.heads import BootstrapHandler
from app.orchestration.plan import load_plans, validate_plan
from app.orchestration.registries import AgentRegistry, ToolRegistry
from app.tools.engine_tools import build_engine_tools


def _registries():
    settings = load_settings()
    agents = AgentRegistry()
    for card in load_cards(settings.cards_dir).values():
        agents.register(card, BootstrapHandler(card, "test"))
    tools = ToolRegistry()
    tools.register_all(build_engine_tools(EngineClient("http://engine")))
    return agents, tools


def _base_plan():
    # Minimal valid plan: a single orchestrator step to a terminal state.
    return {
        "plan_id": "t",
        "plan_version": "1.0.0",
        "entrypoint": "a",
        "terminal_states": ["done"],
        "steps": [{"step_id": "a", "agent": "neuron.orchestrator", "on_success": "done"}],
    }


class ShippedPlanTest(unittest.TestCase):
    def test_day_at_a_glance_plan_validates(self):
        agents, tools = _registries()
        plans = load_plans(load_settings().plans_dir, agents, tools)
        self.assertIn("day-at-a-glance", plans)
        plan = plans["day-at-a-glance"]
        self.assertEqual(plan.entrypoint, "dispatch")
        self.assertIn("assembled", plan.terminal_states)


class InvalidPlanTest(unittest.TestCase):
    def setUp(self):
        self.agents, self.tools = _registries()

    def _validate(self, plan):
        return validate_plan(plan, self.agents, self.tools)

    def test_base_plan_is_valid(self):
        self.assertIsNotNone(self._validate(_base_plan()))

    def test_unknown_agent_rejected(self):
        plan = _base_plan()
        plan["steps"][0]["agent"] = "crm.ghost.head"
        with self.assertRaises(UnknownReferenceError):
            self._validate(plan)

    def test_unknown_tool_rejected(self):
        plan = _base_plan()
        plan["steps"][0]["agent"] = "crm.renewals.head"
        plan["steps"][0]["tools"] = ["engine.renewals.nope"]
        plan["steps"][0]["on_failure"] = "done"
        with self.assertRaises(UnknownReferenceError):
            self._validate(plan)

    def test_missing_terminal_states_rejected(self):
        plan = _base_plan()
        del plan["terminal_states"]
        with self.assertRaises(PlanValidationError):
            self._validate(plan)

    def test_dangling_transition_target_rejected(self):
        plan = _base_plan()
        plan["steps"][0]["on_success"] = "nowhere"
        with self.assertRaises(PlanValidationError):
            self._validate(plan)

    def test_entrypoint_not_a_step_rejected(self):
        plan = _base_plan()
        plan["entrypoint"] = "zzz"
        with self.assertRaises(PlanValidationError):
            self._validate(plan)

    def test_engine_step_without_on_failure_rejected(self):
        # A specialist-head step calls the engine → must declare a fallback path.
        plan = _base_plan()
        plan["steps"][0]["agent"] = "crm.renewals.head"  # no on_failure
        with self.assertRaises(PlanValidationError):
            self._validate(plan)


if __name__ == "__main__":
    unittest.main()
