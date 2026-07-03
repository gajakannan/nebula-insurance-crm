"""Versioned multi-part message envelope (F0038-S0002, ADR-027 §6).

A companion response is an ordered list of typed parts (text | app | status |
sources | actions) keyed by ``thread_id`` so persisted threads replay as the app-part
schema evolves. Neuron emits only **registered** component identifiers with validated
props — never executable markup or model-emitted numbers (intake L1). The envelope is
validated against the vendored ``neuron-message-envelope.schema.json``.
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone
from typing import Any

from .errors import NeuronError
from .schemas import get_validator

ENVELOPE_VERSION = 1

# Server-side component registry: the allow-list of component identifiers Neuron may
# place in an `app` part. The frontend enforces its own registry too; both sides refuse
# anything unregistered so no model-generated markup can reach the DOM (F0038-S0002).
REGISTERED_COMPONENTS = frozenset(
    {
        "renewals.needs_attention_list",  # F0038-S0003 Renewals zone content
        "renewals.companion_context",  # F0038-S0003 per-renewal drill context
        "outreach.draft_editor",  # F0038-S0005 in-chat draft editor
    }
)

# Registered, allow-listed action types echoed back to /v1/actions (envelope schema).
REGISTERED_ACTIONS = frozenset(
    {"draft_outreach", "mock_send", "drill_renewal", "scope_redirect_ack"}
)


class UnknownComponentError(NeuronError):
    """Neuron attempted to emit a component identifier that is not registered."""

    status = 500
    title = "Unregistered component"


class UnknownActionError(NeuronError):
    status = 500
    title = "Unregistered action"


def text_part(text: str) -> dict[str, Any]:
    return {"part_type": "text", "text": text}


def status_part(state: str, detail: str | None = None) -> dict[str, Any]:
    part: dict[str, Any] = {"part_type": "status", "state": state}
    if detail is not None:
        part["detail"] = detail
    return part


def app_part(component: str, props: dict[str, Any]) -> dict[str, Any]:
    if component not in REGISTERED_COMPONENTS:
        raise UnknownComponentError(f"component {component!r} is not registered")
    return {"part_type": "app", "component": component, "props": props}


def sources_part(sources: list[dict[str, Any]]) -> dict[str, Any]:
    return {"part_type": "sources", "sources": sources}


def actions_part(actions: list[dict[str, Any]]) -> dict[str, Any]:
    for action in actions:
        if action.get("action_type") not in REGISTERED_ACTIONS:
            raise UnknownActionError(f"action_type {action.get('action_type')!r} is not registered")
    return {"part_type": "actions", "actions": actions}


def build_envelope(
    thread_id: str,
    *,
    role: str,
    parts: list[dict[str, Any]],
    message_id: str | None = None,
    in_reply_to_message_id: str | None = None,
    created_at: str | None = None,
) -> dict[str, Any]:
    """Assemble + schema-validate a message envelope. Raises on an invalid shape."""
    envelope: dict[str, Any] = {
        "envelope_version": ENVELOPE_VERSION,
        "thread_id": thread_id,
        "message_id": message_id or str(uuid.uuid4()),
        "role": role,
        "created_at": created_at or datetime.now(timezone.utc).isoformat(),
        "parts": parts,
    }
    if in_reply_to_message_id is not None:
        envelope["in_reply_to_message_id"] = in_reply_to_message_id
    get_validator("message-envelope").validate(envelope)
    return envelope
