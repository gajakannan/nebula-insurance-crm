import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.errors import NeuronError
from app.persistence.in_memory import InMemoryNeuronRepository, ThreadNotVisibleError
from app.persistence.models import AgentRun, ProvenanceEvent, ToolCall

OWNER = "user-A"
OTHER = "user-B"


def _run(repo, thread):
    return repo.create_agent_run(
        AgentRun(
            thread_id=thread.id,
            plan_id="day-at-a-glance",
            plan_version="1.0.0",
            card_id="crm.renewals.head",
            card_version="1.0.0",
            card_content_hash="sha256:abc",
        )
    )


class ThreadOwnerScopeTest(unittest.TestCase):
    def setUp(self):
        self.repo = InMemoryNeuronRepository()
        self.thread = self.repo.create_thread(OWNER, title="Day at a Glance")

    def test_owner_can_read_own_thread(self):
        self.assertIsNotNone(self.repo.get_thread(self.thread.id, OWNER))

    def test_non_owner_cannot_read_thread(self):
        self.assertIsNone(self.repo.get_thread(self.thread.id, OTHER))

    def test_messages_are_owner_scoped(self):
        self.repo.add_message(
            self.thread.id, OWNER, role="assistant",
            parts=[("text", {"part_type": "text", "text": "hi"})],
        )
        self.assertEqual(len(self.repo.get_messages(self.thread.id, OWNER)), 1)
        self.assertEqual(self.repo.get_messages(self.thread.id, OTHER), [])

    def test_message_to_foreign_thread_rejected(self):
        with self.assertRaises(ThreadNotVisibleError):
            self.repo.add_message(
                self.thread.id, OTHER, role="user",
                parts=[("text", {"part_type": "text", "text": "x"})],
            )

    def test_parts_are_ordinal_ordered(self):
        msg = self.repo.add_message(
            self.thread.id, OWNER, role="assistant",
            parts=[
                ("status", {"part_type": "status", "state": "working"}),
                ("text", {"part_type": "text", "text": "done"}),
            ],
        )
        self.assertEqual([p.ordinal for p in msg.parts], [0, 1])
        self.assertEqual([p.part_type for p in msg.parts], ["status", "text"])


class EngineRefIdempotencyTest(unittest.TestCase):
    def setUp(self):
        self.repo = InMemoryNeuronRepository()
        self.thread = self.repo.create_thread(OWNER)
        self.run = _run(self.repo, self.thread)

    def test_first_attach_sets_reference(self):
        updated = self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        self.assertEqual(updated.engine_ref_id, "evt-1")

    def test_repeat_same_ref_is_noop(self):
        self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        again = self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        self.assertEqual(again.engine_ref_id, "evt-1")

    def test_conflicting_ref_raises(self):
        self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-1")
        with self.assertRaises(NeuronError):
            self.repo.attach_engine_ref(self.run.id, "timeline_event", "evt-2")


class ProvenanceShapeTest(unittest.TestCase):
    def test_provenance_carries_no_raw_prompt_field(self):
        repo = InMemoryNeuronRepository()
        thread = repo.create_thread(OWNER)
        run = _run(repo, thread)
        repo.record_provenance(
            ProvenanceEvent(agent_run_id=run.id, model="mock-1", content_hash="sha256:x")
        )
        events = repo.list_provenance(run.id)
        self.assertEqual(len(events), 1)
        # Redaction-by-shape: the record type structurally cannot hold raw text/PII.
        forbidden = {"raw_prompt", "prompt", "raw_response", "response", "pii"}
        self.assertFalse(forbidden & set(vars(events[0])))

    def test_tool_call_records_digest_only(self):
        repo = InMemoryNeuronRepository()
        thread = repo.create_thread(OWNER)
        run = _run(repo, thread)
        call = repo.record_tool_call(
            ToolCall(agent_run_id=run.id, tool_name="engine.renewals.needs_attention",
                     request_digest="sha256:req", status="ok", latency_ms=12)
        )
        self.assertEqual(call.tool_name, "engine.renewals.needs_attention")
        self.assertNotIn("args", vars(call))


if __name__ == "__main__":
    unittest.main()
