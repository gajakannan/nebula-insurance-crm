"""Zone-producing heads for Day-at-a-Glance dispatch (F0038-S0002 + S0003 + S0004 stubs).

Each registered specialist head resolves to a handler that returns a
``NeuronZonePayload`` for its slot. The live Renewals head (F0038-S0003) calls the
engine's ``needs-attention`` read **as the user** (forwarded token; the engine
authorizes) and maps the result into the ``renewals.needs_attention_list`` component.
Stub heads return the typed ``inactive`` payload (F0038-S0004): they read nothing and
expose no CTA — the F0040 extension point that flips a stub to live.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING, Any, Protocol, runtime_checkable

from ..schemas import get_validator
from .agent_card import AgentCard

if TYPE_CHECKING:
    from ..persistence.models import AgentRun
    from .registries import ToolRegistry
    from .task_manager import A2ATaskManager

# Live Renewals wiring (F0038-S0003).
_NEEDS_ATTENTION_TOOL = "engine.renewals.needs_attention"
_RENEWALS_COMPONENT = "renewals.needs_attention_list"
_DEFAULT_WINDOW_DAYS = 90


def zone_id_for_card(card_id: str) -> str:
    """crm.renewals.head -> renewals; crm.broker_activity.head -> broker_activity."""
    core = card_id
    if core.startswith("crm."):
        core = core[len("crm.") :]
    if core.endswith(".head"):
        core = core[: -len(".head")]
    return core


@dataclass
class ZonePayload:
    zone_id: str
    zone_status: str
    title: str | None = None
    component: str | None = None
    props: dict[str, Any] | None = None
    detail: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"zone_id": self.zone_id, "zone_status": self.zone_status}
        for key in ("title", "component", "props", "detail"):
            value = getattr(self, key)
            if value is not None:
                out[key] = value
        return out

    def validated(self) -> "ZonePayload":
        get_validator("zone-payload").validate(self.to_dict())
        return self


@dataclass(frozen=True)
class HeadContext:
    """What a head needs to build its zone.

    ``tools`` lets a live head invoke an engine tool as the user; ``task_manager`` +
    ``run`` let it record the engine tool call into the A2A trace (operation store).
    """

    user_token: str
    owner_user_id: str
    thread_id: str
    tools: "ToolRegistry | None" = None
    task_manager: "A2ATaskManager | None" = None
    run: "AgentRun | None" = None


@runtime_checkable
class ZoneHead(Protocol):
    card: AgentCard
    zone_id: str

    async def build_zone(self, ctx: HeadContext) -> ZonePayload: ...


class StubZoneHead:
    """Inert stub head — returns a typed ``inactive`` payload (F0038-S0004)."""

    def __init__(self, card: AgentCard) -> None:
        self.card = card
        self.zone_id = zone_id_for_card(card.card_id)

    async def build_zone(self, ctx: HeadContext) -> ZonePayload:
        return ZonePayload(
            zone_id=self.zone_id,
            zone_status="inactive",
            title=self.card.name,
            detail="Not yet active.",
        ).validated()


class RenewalsZoneHead:
    """Live Renewals head (F0038-S0003).

    Calls the engine needs-attention read as the user, then maps the (snake_case)
    engine items into the ``renewals.needs_attention_list`` component props. Returns
    ``content`` when renewals need attention, ``empty`` when none. An engine failure
    propagates as a typed error — the orchestrator isolates it to this slot.
    """

    def __init__(self, card: AgentCard) -> None:
        self.card = card
        self.zone_id = "renewals"

    async def build_zone(self, ctx: HeadContext) -> ZonePayload:
        if ctx.tools is None or not ctx.tools.has(_NEEDS_ATTENTION_TOOL):
            raise RuntimeError(f"renewals head requires the {_NEEDS_ATTENTION_TOOL!r} tool")

        tool = ctx.tools.get(_NEEDS_ATTENTION_TOOL)
        response = await tool.invoke(
            user_token=ctx.user_token, params={"withinDays": _DEFAULT_WINDOW_DAYS}
        )
        raw_items = (response or {}).get("data") or []
        items = [self._map_item(item) for item in raw_items]

        if ctx.task_manager is not None and ctx.run is not None:
            # Record the engine call in the A2A trace — digest only, no PII.
            ctx.task_manager.record_tool_call(
                ctx.run,
                _NEEDS_ATTENTION_TOOL,
                request_digest=f"needs_attention:withinDays={_DEFAULT_WINDOW_DAYS}",
                status="ok",
            )

        if not items:
            return ZonePayload(
                zone_id="renewals",
                zone_status="empty",
                title="Renewals",
                detail="No renewals need your attention in the next 90 days.",
            ).validated()

        return ZonePayload(
            zone_id="renewals",
            zone_status="content",
            title="Renewals",
            component=_RENEWALS_COMPONENT,
            props={"items": items},
        ).validated()

    @staticmethod
    def _map_item(item: dict[str, Any]) -> dict[str, Any]:
        # Engine snake_case -> the registered component's camelCase props.
        return {
            "renewalId": item["renewal_id"],
            "accountName": item["account_name"],
            "expiresInDays": item["days_to_expiry"],
            "workflowState": item["workflow_state"],
            "noBrokerContact30d": item.get("no_contact_flag", False),
            "brokerName": item.get("broker_name"),
            "canDraftOutreach": item.get("can_draft_outreach", False),
        }
