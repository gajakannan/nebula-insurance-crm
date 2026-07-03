"""In-memory ``NeuronRepository`` — the F0038 operation store (ADR-028 §1).

Behind the same interface as the durable Postgres ``neuron.*`` schema. It enforces
the store's invariants (owner-scoped threads, idempotent engine reference) so that
swapping in the durable impl is a wiring change, not a behavior change.

Not for production scale/durability — a restart loses state. F0038's statelessness
requirement is about the *service* (no in-process business state assumed across
requests); the operation store's durable home is the Postgres schema in
``migrations/``.
"""

from __future__ import annotations

import threading
from typing import Any

from ..errors import NeuronError
from .models import (
    AgentRun,
    Message,
    MessagePart,
    ProvenanceEvent,
    Thread,
    ToolCall,
    utcnow,
)
from .repository import NeuronRepository


class ThreadNotVisibleError(NeuronError):
    """The thread does not exist or is not owned by the requesting user."""

    status = 404
    title = "Thread not found"


class InMemoryNeuronRepository(NeuronRepository):
    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._threads: dict[str, Thread] = {}
        self._messages: dict[str, Message] = {}
        self._runs: dict[str, AgentRun] = {}
        self._tool_calls: dict[str, ToolCall] = {}
        self._provenance: dict[str, ProvenanceEvent] = {}

    # --- threads / messages -------------------------------------------------

    def create_thread(
        self,
        owner_user_id: str,
        *,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
    ) -> Thread:
        thread = Thread(
            owner_user_id=owner_user_id,
            anchor_type=anchor_type,
            anchor_ref=anchor_ref,
            title=title,
        )
        with self._lock:
            self._threads[thread.id] = thread
        return thread

    def get_thread(self, thread_id: str, owner_user_id: str) -> Thread | None:
        with self._lock:
            thread = self._threads.get(thread_id)
        # WHY: owner-scope is a store invariant — a non-owner gets None, never data.
        if thread is None or thread.deleted_at is not None:
            return None
        if thread.owner_user_id != owner_user_id:
            return None
        return thread

    def add_message(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        role: str,
        parts: list[tuple[str, dict[str, Any]]],
        envelope_version: int = 1,
        in_reply_to_message_id: str | None = None,
    ) -> Message:
        if self.get_thread(thread_id, owner_user_id) is None:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner")
        message = Message(
            thread_id=thread_id,
            role=role,
            envelope_version=envelope_version,
            in_reply_to_message_id=in_reply_to_message_id,
        )
        message.parts = [
            MessagePart(
                message_id=message.id,
                ordinal=ordinal,
                part_type=part_type,
                content_json=content_json,
            )
            for ordinal, (part_type, content_json) in enumerate(parts)
        ]
        with self._lock:
            self._messages[message.id] = message
            self._threads[thread_id].updated_at = utcnow()
        return message

    def get_messages(self, thread_id: str, owner_user_id: str) -> list[Message]:
        if self.get_thread(thread_id, owner_user_id) is None:
            return []
        with self._lock:
            msgs = [m for m in self._messages.values() if m.thread_id == thread_id]
        return sorted(msgs, key=lambda m: m.created_at)

    # --- A2A task trace -----------------------------------------------------

    def create_agent_run(self, run: AgentRun) -> AgentRun:
        with self._lock:
            self._runs[run.id] = run
        return run

    def get_agent_run(self, run_id: str) -> AgentRun | None:
        with self._lock:
            return self._runs.get(run_id)

    def update_run_state(self, run_id: str, state: str) -> AgentRun:
        with self._lock:
            run = self._runs[run_id]
            run.state = state
            run.updated_at = utcnow()
            return run

    def attach_engine_ref(
        self, run_id: str, engine_ref_type: str, engine_ref_id: str
    ) -> AgentRun:
        with self._lock:
            run = self._runs[run_id]
            if run.engine_ref_id is not None:
                # WHY: idempotency key is the run id (ADR-028 §2). A repeat with the
                # same engine id is a safe no-op on cross-store retry; a different id
                # would mean one run claiming two engine writes — a corruption guard.
                if (run.engine_ref_type, run.engine_ref_id) != (
                    engine_ref_type,
                    engine_ref_id,
                ):
                    raise NeuronError(
                        f"run {run_id} already references "
                        f"{run.engine_ref_type}:{run.engine_ref_id}"
                    )
                return run
            run.engine_ref_type = engine_ref_type
            run.engine_ref_id = engine_ref_id
            run.updated_at = utcnow()
            return run

    def record_tool_call(self, call: ToolCall) -> ToolCall:
        with self._lock:
            self._tool_calls[call.id] = call
        return call

    def record_provenance(self, event: ProvenanceEvent) -> ProvenanceEvent:
        with self._lock:
            self._provenance[event.id] = event
        return event

    def list_provenance(self, run_id: str) -> list[ProvenanceEvent]:
        with self._lock:
            events = [p for p in self._provenance.values() if p.agent_run_id == run_id]
        return sorted(events, key=lambda p: p.created_at)
