"""Dataclasses mirroring the ``neuron.*`` operation store (ADR-028 §1).

These are the six durable record shapes: threads, messages, message_parts,
agent_runs, tool_calls, provenance_events. They intentionally hold only companion
*operation* state — never CRM/product business data, which stays engine-owned
(F0038-S0001 business rule 1). Provenance never stores raw prompts, raw LLM
responses, or customer PII (ADR-027 security notes).
"""

from __future__ import annotations

import uuid
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any

# --- enumerations (kept as literals validated at the boundary) --------------

ANCHOR_TYPES = ("domain", "record", "free_form")
MESSAGE_ROLES = ("user", "assistant")
PART_TYPES = ("text", "app", "status", "sources", "actions")
# A2A task states (neuron-message-envelope status aligns to a subset for display).
AGENT_RUN_STATES = (
    "submitted",
    "working",
    "input_required",
    "completed",
    "failed",
    "canceled",
)


def new_id() -> str:
    return str(uuid.uuid4())


def utcnow() -> datetime:
    return datetime.now(timezone.utc)


@dataclass
class Thread:
    """A conversation thread — owner-scoped, private to its creator. Maps to A2A contextId."""

    owner_user_id: str
    anchor_type: str = "free_form"
    anchor_ref: str | None = None
    title: str | None = None
    id: str = field(default_factory=new_id)
    created_at: datetime = field(default_factory=utcnow)
    updated_at: datetime = field(default_factory=utcnow)
    deleted_at: datetime | None = None


@dataclass
class MessagePart:
    """One ordered part of a message envelope (neuron-message-envelope.schema.json)."""

    message_id: str
    ordinal: int
    part_type: str
    content_json: dict[str, Any]
    id: str = field(default_factory=new_id)
    created_at: datetime = field(default_factory=utcnow)


@dataclass
class Message:
    """A single chat message, replayed through the versioned envelope."""

    thread_id: str
    role: str
    envelope_version: int = 1
    in_reply_to_message_id: str | None = None
    id: str = field(default_factory=new_id)
    created_at: datetime = field(default_factory=utcnow)
    updated_at: datetime = field(default_factory=utcnow)
    parts: list[MessagePart] = field(default_factory=list)


@dataclass
class AgentRun:
    """An A2A task / subtask trace.

    References the active plan + Agent Card by id + version + content hash — the
    definitions themselves stay checked-in source assets (ADR-027 §9). ``engine_ref_*``
    references the authoritative engine write for a cross-store gesture (ADR-028 §2).
    """

    thread_id: str
    plan_id: str
    plan_version: str
    card_id: str
    card_version: str
    card_content_hash: str
    state: str = "submitted"
    parent_run_id: str | None = None
    engine_ref_type: str | None = None
    engine_ref_id: str | None = None
    id: str = field(default_factory=new_id)
    created_at: datetime = field(default_factory=utcnow)
    updated_at: datetime = field(default_factory=utcnow)


@dataclass
class ToolCall:
    """An MCP/tool invocation under a task. Carries no raw args/PII — only a digest."""

    agent_run_id: str
    tool_name: str
    request_digest: str
    status: str
    latency_ms: int | None = None
    id: str = field(default_factory=new_id)
    created_at: datetime = field(default_factory=utcnow)


@dataclass
class ProvenanceEvent:
    """Run/draft provenance — ids, versions, hashes, model, cost, latency, trace.

    Never contains raw prompts, raw LLM responses, or customer PII (ADR-028 §1).
    """

    agent_run_id: str
    model: str
    content_hash: str
    prompt_id: str | None = None
    prompt_version: str | None = None
    trace_id: str | None = None
    cost: float | None = None
    latency_ms: int | None = None
    id: str = field(default_factory=new_id)
    created_at: datetime = field(default_factory=utcnow)
