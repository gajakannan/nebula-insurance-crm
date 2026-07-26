"""The ``NeuronRepository`` interface (ADR-028 §1).

Callers depend on this abstract interface, never on a concrete store, so the
durable home (Postgres ``neuron.*``) can replace the F0038 in-memory impl without
reshaping any caller (F0038-S0001: "clear persistence interface so the storage
owner can change without reshaping callers").

Owner-scoping is a repository invariant: every thread read is scoped to the
authenticated ``owner_user_id`` and returns ``None`` for a non-owner (threads are
private to their creator, ADR-028 §1).

**The whole surface is ``async`` (F0039-S0001).** Once Postgres backs the store every
method performs I/O, and every caller already runs inside an async request path — a
sync method here would block the event loop, which S0001's non-functional expectation
forbids. The in-memory implementation is async too so both satisfy one interface.
"""

from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Any

from .models import AgentRun, Message, ProvenanceEvent, Thread, ToolCall

# Default page size for thread listing and history replay (S0002).
DEFAULT_PAGE_SIZE = 50
MAX_PAGE_SIZE = 200


@dataclass(frozen=True)
class Page:
    """One page of results plus the cursor that resumes after it.

    ``next_cursor`` is ``None`` on the last page — the caller stops when it sees that,
    rather than probing for an empty page. A short page is not an error (S0002).
    """

    items: list[Any]
    next_cursor: str | None = None


class NeuronRepository(ABC):
    # --- threads ------------------------------------------------------------

    @abstractmethod
    async def create_thread(
        self,
        owner_user_id: str,
        *,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
        idempotency_key: str | None = None,
    ) -> Thread:
        """Open an owner-scoped thread.

        With ``idempotency_key``, a repeat create for the same owner returns the
        original thread instead of a duplicate (F0039-S0001).
        """

    @abstractmethod
    async def get_thread(self, thread_id: str, owner_user_id: str) -> Thread | None:
        """Return the thread only if owned by ``owner_user_id`` (else ``None``)."""

    @abstractmethod
    async def list_threads(
        self,
        owner_user_id: str,
        *,
        limit: int = DEFAULT_PAGE_SIZE,
        cursor: str | None = None,
    ) -> Page:
        """List the caller's own live threads, most-recently-updated first.

        Soft-deleted threads are excluded. An owner with no threads gets an empty
        page — that drives the panel's empty state, not an error (F0039-S0002).
        """

    @abstractmethod
    async def rename_thread(self, thread_id: str, owner_user_id: str, title: str) -> Thread:
        """Rename an owned thread and bump ``updated_at`` transactionally.

        Raises ``ThreadNotVisibleError`` when the thread is not owner-visible — the
        same failure a non-existent thread produces, so a non-owner cannot probe for
        existence (F0039-S0002).
        """

    @abstractmethod
    async def delete_thread(self, thread_id: str, owner_user_id: str) -> None:
        """Soft-delete an owned thread (sets ``deleted_at``); not owner-visible after."""

    # --- messages -----------------------------------------------------------

    @abstractmethod
    async def add_message(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        role: str,
        parts: list[tuple[str, dict[str, Any]]],
        envelope_version: int = 1,
        in_reply_to_message_id: str | None = None,
        client_message_key: str | None = None,
    ) -> Message:
        """Append a message + ordered parts, assigning the server ``sequence``.

        Raises if the thread is not owner-visible. With ``client_message_key``, a
        repeat append returns the **original** row — the append is idempotent and no
        duplicate is written (F0039-S0001).
        """

    @abstractmethod
    async def get_messages(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        limit: int | None = None,
        after_sequence: int | None = None,
    ) -> list[Message]:
        """Replay a thread's history in **server-sequence** order.

        ``after_sequence`` resumes strictly after that sequence, so a client can page
        forward through a long conversation deterministically. A non-owner gets an
        empty list, never another user's history.
        """

    # --- A2A task trace -----------------------------------------------------

    @abstractmethod
    async def create_agent_run(self, run: AgentRun) -> AgentRun: ...

    @abstractmethod
    async def get_agent_run(self, run_id: str) -> AgentRun | None: ...

    @abstractmethod
    async def update_run_state(self, run_id: str, state: str) -> AgentRun: ...

    @abstractmethod
    async def attach_engine_ref(
        self, run_id: str, engine_ref_type: str, engine_ref_id: str
    ) -> AgentRun:
        """Idempotently bind the authoritative engine write to this run (ADR-028 §2).

        First call sets the reference; a repeat with the **same** id is a no-op
        (so a cross-store retry cannot double-write). A repeat with a **different**
        id raises — that would mean two engine writes claimed by one run.
        """

    @abstractmethod
    async def record_tool_call(self, call: ToolCall) -> ToolCall: ...

    @abstractmethod
    async def record_provenance(self, event: ProvenanceEvent) -> ProvenanceEvent: ...

    @abstractmethod
    async def list_provenance(self, run_id: str) -> list[ProvenanceEvent]: ...

    # --- lifecycle ----------------------------------------------------------

    async def startup(self) -> None:
        """Open pools/connections. No-op for stores that need none."""

    async def shutdown(self) -> None:
        """Release pools/connections. No-op for stores that need none."""


def clamp_limit(limit: int | None) -> int:
    """Bound a caller-supplied page size — an unbounded page is a DoS vector."""
    if limit is None:
        return DEFAULT_PAGE_SIZE
    return max(1, min(int(limit), MAX_PAGE_SIZE))
