"""In-memory ``NeuronRepository`` — the F0038 operation store (ADR-028 §1).

Behind the same interface as the durable Postgres ``neuron.*`` schema. It enforces
the store's invariants (owner-scoped threads, server-assigned message sequence,
idempotent appends, idempotent engine reference) so that swapping in the durable impl
is a wiring change, not a behavior change.

Not for production scale/durability — a restart loses state. F0038's statelessness
requirement is about the *service* (no in-process business state assumed across
requests); the operation store's durable home is the Postgres schema in
``migrations/``.

The surface is ``async`` to match the interface (F0039-S0001). Nothing here actually
awaits — the methods are async so callers are written once against one contract and
the Postgres store can be swapped in without touching a call site.
"""

from __future__ import annotations

import threading
from typing import Any

from ..errors import (
    InvalidThreadTitleError,
    NeuronError,
    ThreadNotVisibleError,
)
from .models import (
    AgentRun,
    Message,
    MessagePart,
    ProvenanceEvent,
    Thread,
    ToolCall,
    utcnow,
)
from .repository import DEFAULT_PAGE_SIZE, NeuronRepository, Page, clamp_limit

# Re-exported: callers and tests imported this from here before it moved to errors.py
# (one definition, two import paths — no behavior change).
__all__ = ["InMemoryNeuronRepository", "ThreadNotVisibleError", "MAX_TITLE_LENGTH"]

# Bound from neuron-api.yaml (NeuronThread.title maxLength) — the contract is the
# source of truth, not this constant.
MAX_TITLE_LENGTH = 120


def validate_title(title: str) -> str:
    """Normalize and validate a thread title (F0039-S0002 / neuron-api.yaml).

    Rejects empty/whitespace-only, over-length, and control-character titles. Control
    characters are refused rather than stripped: a title is user-authored text that
    gets rendered, so silently rewriting it would hide what was actually submitted.
    """
    cleaned = (title or "").strip()
    if not cleaned:
        raise InvalidThreadTitleError("thread title must not be empty")
    if len(cleaned) > MAX_TITLE_LENGTH:
        raise InvalidThreadTitleError(
            f"thread title must be at most {MAX_TITLE_LENGTH} characters"
        )
    if any(ord(ch) < 0x20 or ord(ch) == 0x7F for ch in cleaned):
        raise InvalidThreadTitleError("thread title must not contain control characters")
    return cleaned


class InMemoryNeuronRepository(NeuronRepository):
    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._threads: dict[str, Thread] = {}
        self._messages: dict[str, Message] = {}
        self._runs: dict[str, AgentRun] = {}
        self._tool_calls: dict[str, ToolCall] = {}
        self._provenance: dict[str, ProvenanceEvent] = {}
        # Mirrors neuron.threads.next_sequence — the per-thread server allocator.
        self._next_sequence: dict[str, int] = {}

    # --- threads ------------------------------------------------------------

    async def create_thread(
        self,
        owner_user_id: str,
        *,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
        idempotency_key: str | None = None,
    ) -> Thread:
        with self._lock:
            if idempotency_key is not None:
                # Mirrors ux_neuron_threads_owner_idem: a retried create returns the
                # original thread rather than opening a duplicate conversation.
                for existing in self._threads.values():
                    if (
                        existing.owner_user_id == owner_user_id
                        and existing.thread_idempotency_key == idempotency_key
                        and existing.deleted_at is None
                    ):
                        return existing
            thread = Thread(
                owner_user_id=owner_user_id,
                anchor_type=anchor_type,
                anchor_ref=anchor_ref,
                title=title,
                thread_idempotency_key=idempotency_key,
            )
            self._threads[thread.id] = thread
            self._next_sequence[thread.id] = 1
        return thread

    async def get_thread(self, thread_id: str, owner_user_id: str) -> Thread | None:
        with self._lock:
            thread = self._threads.get(thread_id)
        # WHY: owner-scope is a store invariant — a non-owner gets None, never data.
        if thread is None or thread.deleted_at is not None:
            return None
        if thread.owner_user_id != owner_user_id:
            return None
        return thread

    async def list_threads(
        self,
        owner_user_id: str,
        *,
        limit: int = DEFAULT_PAGE_SIZE,
        cursor: str | None = None,
    ) -> Page:
        size = clamp_limit(limit)
        with self._lock:
            owned = [
                t
                for t in self._threads.values()
                if t.owner_user_id == owner_user_id and t.deleted_at is None
            ]
        # Most-recently-updated first; id breaks ties so the cursor is stable.
        owned.sort(key=lambda t: (t.updated_at, t.id), reverse=True)
        start = 0
        if cursor:
            # The cursor is the last thread id of the previous page. An unknown cursor
            # (e.g. the thread was deleted between pages) restarts rather than errors.
            for index, thread in enumerate(owned):
                if thread.id == cursor:
                    start = index + 1
                    break
        window = owned[start : start + size]
        next_cursor = window[-1].id if len(owned) > start + size and window else None
        return Page(items=list(window), next_cursor=next_cursor)

    async def rename_thread(self, thread_id: str, owner_user_id: str, title: str) -> Thread:
        cleaned = validate_title(title)
        thread = await self.get_thread(thread_id, owner_user_id)
        if thread is None:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner")
        with self._lock:
            thread.title = cleaned
            thread.updated_at = utcnow()
        return thread

    async def delete_thread(self, thread_id: str, owner_user_id: str) -> None:
        thread = await self.get_thread(thread_id, owner_user_id)
        if thread is None:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner")
        with self._lock:
            # Soft delete — the row stays for retention/audit, but it is no longer
            # owner-visible and drops out of listings after reload (F0039-S0002).
            thread.deleted_at = utcnow()
            thread.updated_at = thread.deleted_at

    # --- messages -----------------------------------------------------------

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
        if await self.get_thread(thread_id, owner_user_id) is None:
            raise ThreadNotVisibleError(f"thread {thread_id} not visible to owner")
        with self._lock:
            if client_message_key is not None:
                # Mirrors ux_neuron_messages_thread_client_key: the repeat append
                # returns the original row — no duplicate, no new sequence burned.
                for existing in self._messages.values():
                    if (
                        existing.thread_id == thread_id
                        and existing.client_message_key == client_message_key
                    ):
                        return existing
            sequence = self._next_sequence.get(thread_id, 1)
            self._next_sequence[thread_id] = sequence + 1
            message = Message(
                thread_id=thread_id,
                role=role,
                envelope_version=envelope_version,
                in_reply_to_message_id=in_reply_to_message_id,
                sequence=sequence,
                client_message_key=client_message_key,
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
            self._messages[message.id] = message
            self._threads[thread_id].last_sequence = sequence
            # Transactional with the append: the thread's updated_at always reflects
            # its latest activity, which is what list ordering keys on.
            self._threads[thread_id].updated_at = utcnow()
        return message

    async def get_messages(
        self,
        thread_id: str,
        owner_user_id: str,
        *,
        limit: int | None = None,
        after_sequence: int | None = None,
    ) -> list[Message]:
        if await self.get_thread(thread_id, owner_user_id) is None:
            return []
        with self._lock:
            msgs = [m for m in self._messages.values() if m.thread_id == thread_id]
        # Server sequence is the ordering key — never created_at (timestamps collide).
        msgs.sort(key=lambda m: m.sequence)
        if after_sequence is not None:
            msgs = [m for m in msgs if m.sequence > after_sequence]
        if limit is not None:
            msgs = msgs[: clamp_limit(limit)]
        return msgs

    # --- A2A task trace -----------------------------------------------------

    async def create_agent_run(self, run: AgentRun) -> AgentRun:
        with self._lock:
            self._runs[run.id] = run
        return run

    async def get_agent_run(self, run_id: str) -> AgentRun | None:
        with self._lock:
            return self._runs.get(run_id)

    async def update_run_state(self, run_id: str, state: str) -> AgentRun:
        with self._lock:
            run = self._runs[run_id]
            run.state = state
            run.updated_at = utcnow()
            return run

    async def attach_engine_ref(
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

    async def record_tool_call(self, call: ToolCall) -> ToolCall:
        with self._lock:
            self._tool_calls[call.id] = call
        return call

    async def record_provenance(self, event: ProvenanceEvent) -> ProvenanceEvent:
        with self._lock:
            self._provenance[event.id] = event
        return event

    async def list_provenance(self, run_id: str) -> list[ProvenanceEvent]:
        with self._lock:
            events = [p for p in self._provenance.values() if p.agent_run_id == run_id]
        return sorted(events, key=lambda p: p.created_at)
