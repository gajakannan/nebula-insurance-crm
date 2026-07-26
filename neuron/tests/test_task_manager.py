import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.config import load_settings
from app.orchestration.agent_card import load_cards
from app.orchestration.plan import load_plans
from app.orchestration.registries import AgentRegistry, ToolRegistry
from app.orchestration.heads import BootstrapHandler
from app.orchestration.task_manager import A2ATaskManager
from app.engine_client import EngineClient
from app.tools.engine_tools import build_engine_tools
from app.persistence.in_memory import InMemoryNeuronRepository

OWNER = "user-A"
OTHER = "user-B"


def _plan_and_card():
    settings = load_settings()
    cards = load_cards(settings.cards_dir)
    agents = AgentRegistry()
    for c in cards.values():
        agents.register(c, BootstrapHandler(c, "test"))
    tools = ToolRegistry()
    tools.register_all(build_engine_tools(EngineClient("http://engine")))
    plan = load_plans(settings.plans_dir, agents, tools)["day-at-a-glance"]
    return plan, cards["crm.renewals.head"]


class TaskManagerTest(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.repo = InMemoryNeuronRepository()
        self.tm = A2ATaskManager(self.repo)
        self.plan, self.card = _plan_and_card()

    async def test_open_context_creates_thread(self):
        thread = await self.tm.open_context(OWNER, title="Day at a Glance")
        self.assertEqual(thread.owner_user_id, OWNER)

    async def test_open_context_resumes_owned_thread(self):
        first = await self.tm.open_context(OWNER)
        resumed = await self.tm.open_context(OWNER, thread_id=first.id)
        self.assertEqual(resumed.id, first.id)

    async def test_open_context_ignores_foreign_thread_id(self):
        first = await self.tm.open_context(OWNER)
        # A different user passing someone else's thread_id gets a fresh thread, not theirs.
        other = await self.tm.open_context(OTHER, thread_id=first.id)
        self.assertNotEqual(other.id, first.id)

    async def test_begin_run_references_plan_and_card(self):
        thread = await self.tm.open_context(OWNER)
        run = await self.tm.begin_run(thread, self.plan, self.card)
        self.assertEqual(run.plan_id, "day-at-a-glance")
        self.assertEqual(run.card_id, "crm.renewals.head")
        self.assertEqual(run.card_content_hash, self.card.content_hash)
        self.assertEqual(run.state, "working")

    async def test_complete_run_binds_engine_ref_idempotently(self):
        thread = await self.tm.open_context(OWNER)
        run = await self.tm.begin_run(thread, self.plan, self.card)
        await self.tm.complete_run(run, state="completed", engine_ref_type="timeline_event", engine_ref_id="evt-9")
        # A retry of the same cross-store completion must not double-write.
        done = await self.tm.complete_run(run, state="completed", engine_ref_type="timeline_event", engine_ref_id="evt-9")
        self.assertEqual(done.state, "completed")
        self.assertEqual(done.engine_ref_id, "evt-9")

    async def test_emit_provenance_records_metadata(self):
        thread = await self.tm.open_context(OWNER)
        run = await self.tm.begin_run(thread, self.plan, self.card)
        await self.tm.emit_provenance(run, model="mock-1", content_hash="sha256:z", cost=0.0, latency_ms=3)
        events = await self.repo.list_provenance(run.id)
        self.assertEqual(len(events), 1)
        self.assertEqual(events[0].model, "mock-1")


if __name__ == "__main__":
    unittest.main()
