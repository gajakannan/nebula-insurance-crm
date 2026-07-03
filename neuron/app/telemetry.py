"""Companion telemetry emission (F0038-S0008).

Fire-and-forget: emission NEVER breaks the instrumented flow. A failed or unreachable
telemetry ingest is swallowed and logged; the glance / draft / mock-send still succeed.
Events are emitted AFTER the instrumented action's authoritative work is done, so a
telemetry failure cannot roll anything back.

PII boundary: events carry only the metric name, the stable user/thread/renewal
correlation ids, and (for DAU) the persona — never a draft body, raw prompt, or
credential. The engine ingest enforces the same closed shape and drops anything else.
"""

from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import TYPE_CHECKING, Any

if TYPE_CHECKING:
    from .orchestration.registries import ToolRegistry

_TELEMETRY_TOOL = "engine.telemetry.ingest"
EVENT_VERSION = 1

# Event names (kebab-case) — mirror neuron-companion-telemetry-event.schema.json and the
# engine registry (NeuronCompanionEventNames).
NEEDS_ATTENTION_SURFACED = "needs-attention-surfaced"  # primary start
DRAFT_READY = "draft-ready"  # primary end
COMPANION_DAILY_ACTIVE = "companion-daily-active"
ATTENTION_RENEWAL_ACTIONED = "attention-renewal-actioned"
DRAFT_GENERATED = "draft-generated"
MOCK_SENT = "mock-sent"

_log = logging.getLogger("neuron.telemetry")


def build_event(
    name: str,
    user_id: str,
    *,
    thread_id: str | None = None,
    renewal_id: str | None = None,
    persona: str | None = None,
) -> dict[str, Any]:
    """Build a schema-shaped telemetry event (correlation ids + metric fields only)."""
    event: dict[str, Any] = {
        "event_name": name,
        "event_version": EVENT_VERSION,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "user_id": user_id,
    }
    if thread_id is not None:
        event["thread_id"] = thread_id
    if renewal_id is not None:
        event["renewal_id"] = renewal_id
    if persona is not None:
        event["persona"] = persona
    return event


class CompanionTelemetry:
    """Best-effort batch emitter over the ``engine.telemetry.ingest`` tool."""

    def __init__(self, tools: "ToolRegistry") -> None:
        self._tools = tools

    async def emit(self, user_token: str, events: list[dict[str, Any]]) -> None:
        if not events or not self._tools.has(_TELEMETRY_TOOL):
            return
        try:
            await self._tools.get(_TELEMETRY_TOOL).invoke(
                user_token=user_token, json={"events": events}
            )
        except Exception as exc:  # noqa: BLE001 — telemetry must never break the user flow
            _log.warning("companion telemetry emit failed: %s", type(exc).__name__)
