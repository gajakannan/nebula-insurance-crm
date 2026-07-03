"""The ``NeuronRepository`` interface (ADR-028 §1).

Callers depend on this abstract interface, never on a concrete store, so the
durable home (Postgres ``neuron.*``) can replace the F0038 in-memory impl without
reshaping any caller (F0038-S0001: "clear persistence interface so the storage
owner can change without reshaping callers").

Owner-scoping is a repository invariant: every thread read is scoped to the
authenticated ``owner_user_id`` and returns ``None`` for a non-owner (threads are
private to their creator, ADR-028 §1).
"""

from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Any

from .models import AgentRun, Message, ProvenanceEvent, Thread, ToolCall


class NeuronRepository(ABC):
    # --- threads / messages -------------------------------------------------

    @abstractmethod
    def create_thread(
        self,
        owner_user_id: str,
        *,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
    ) -> Thread: ...

    @abstractmethod
    def get_thread(self, thread_id: str, owner_user_id: str) -> Thread | None:
        """Return the thread only if owned by ``owner_user_id`` (else ``None``)."""

    @abstractmethod
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
        """Append a message + ordered parts. Raises if the thread is not owner-visible."""

    @abstractmethod
    def get_messages(self, thread_id: str, owner_user_id: str) -> list[Message]: ...

    # --- A2A task trace -----------------------------------------------------

    @abstractmethod
    def create_agent_run(self, run: AgentRun) -> AgentRun: ...

    @abstractmethod
    def get_agent_run(self, run_id: str) -> AgentRun | None: ...

    @abstractmethod
    def update_run_state(self, run_id: str, state: str) -> AgentRun: ...

    @abstractmethod
    def attach_engine_ref(
        self, run_id: str, engine_ref_type: str, engine_ref_id: str
    ) -> AgentRun:
        """Idempotently bind the authoritative engine write to this run (ADR-028 §2).

        First call sets the reference; a repeat with the **same** id is a no-op
        (so a cross-store retry cannot double-write). A repeat with a **different**
        id raises — that would mean two engine writes claimed by one run.
        """

    @abstractmethod
    def record_tool_call(self, call: ToolCall) -> ToolCall: ...

    @abstractmethod
    def record_provenance(self, event: ProvenanceEvent) -> ProvenanceEvent: ...

    @abstractmethod
    def list_provenance(self, run_id: str) -> list[ProvenanceEvent]: ...
