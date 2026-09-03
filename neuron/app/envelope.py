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

from .components import COMPONENTS, InvalidComponentPropsError, UnknownComponentError
from .errors import NeuronError
from .schemas import get_validator

ENVELOPE_VERSION = 1

# Backward-compatible public view used by existing tests/callers. Validation is owned
# by ComponentContractRegistry rather than a flat allow-list as of F0040.
REGISTERED_COMPONENTS = frozenset(COMPONENTS.names())

# Registered, allow-listed action types echoed back to /v1/actions (envelope schema).
REGISTERED_ACTIONS = frozenset(
    {"draft_outreach", "mock_send", "drill_renewal", "scope_redirect_ack"}
)


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
    COMPONENTS.validate(component, props)
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
