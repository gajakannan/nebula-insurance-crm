"""Day-at-a-Glance zone assembly (F0038-S0002) — assembly, not composition.

The orchestrator enumerates the registered specialist heads and dispatches each into
its own zone slot, in parallel, with **per-zone error isolation**: a head that fails or
times out yields a typed ``error`` payload for its slot while every other zone still
renders (one zone failing never blanks the shell). No zone reads or ranks another zone.
"""

from __future__ import annotations

import asyncio
from typing import TYPE_CHECKING, Any

from .. import envelope as env
from .. import telemetry as tel
from ..auth import persona_from_token
from ..telemetry import CompanionTelemetry
from .zone_heads import HeadContext, ZonePayload, zone_id_for_card

if TYPE_CHECKING:
    from ..runtime import NeuronRuntime

_GLANCE_PLAN_ID = "day-at-a-glance"


class GlanceAssembler:
    def __init__(self, runtime: "NeuronRuntime") -> None:
        self._rt = runtime

    async def assemble(
        self, *, user_token: str, owner_user_id: str, thread_id: str | None = None
    ) -> dict[str, Any]:
        rt = self._rt
        thread = rt.task_manager.open_context(
            owner_user_id,
            thread_id=thread_id,
            anchor_type="domain",
            anchor_ref=_GLANCE_PLAN_ID,
            title="Day at a Glance",
        )
        plan = rt.plans[_GLANCE_PLAN_ID]
        head_ids = rt.agents.heads()
        zones = await asyncio.gather(
            *(self._dispatch(cid, plan, thread, user_token, owner_user_id) for cid in head_ids)
        )

        # F0038-S0008: emit the DAU signal + one needs-attention-surfaced (primary start)
        # per surfaced renewal. Fire-and-forget — never blocks or breaks the glance.
        await self._emit_telemetry(user_token, owner_user_id, thread.id, zones)

        parts = [
            env.status_part("completed"),
            env.text_part("Here's your day at a glance."),
        ]
        message = env.build_envelope(thread.id, role="assistant", parts=parts)
        rt.repository.add_message(
            thread.id,
            owner_user_id,
            role="assistant",
            parts=[(p["part_type"], p) for p in parts],
        )
        return {"thread_id": thread.id, "zones": list(zones), "message": message}

    async def _emit_telemetry(self, user_token, owner_user_id, thread_id, zones) -> None:
        events = [
            tel.build_event(
                tel.COMPANION_DAILY_ACTIVE, owner_user_id, persona=persona_from_token(user_token)
            )
        ]
        for zone in zones:
            if zone.get("zone_id") != "renewals" or zone.get("zone_status") != "content":
                continue
            for item in (zone.get("props") or {}).get("items", []):
                renewal_id = item.get("renewalId")
                if renewal_id:
                    events.append(
                        tel.build_event(
                            tel.NEEDS_ATTENTION_SURFACED,
                            owner_user_id,
                            thread_id=thread_id,
                            renewal_id=renewal_id,
                        )
                    )
        await CompanionTelemetry(self._rt.tools).emit(user_token, events)

    async def _dispatch(self, card_id, plan, thread, user_token, owner_user_id) -> dict[str, Any]:
        rt = self._rt
        registered = rt.agents.get(card_id)
        card = registered.card
        head = registered.handler
        run = rt.task_manager.begin_run(thread, plan, card)
        try:
            ctx = HeadContext(
                user_token=user_token,
                owner_user_id=owner_user_id,
                thread_id=thread.id,
                tools=rt.tools,
                task_manager=rt.task_manager,
                run=run,
            )
            payload = await head.build_zone(ctx)
            payload.validated()
            rt.task_manager.complete_run(run, state="completed")
            return payload.to_dict()
        except Exception:
            # WHY: per-zone error isolation — a failing head is contained to its own slot;
            # the other zones still render (F0038-S0002 reliability). No detail leak.
            rt.task_manager.complete_run(run, state="failed")
            return ZonePayload(
                zone_id=zone_id_for_card(card_id),
                zone_status="error",
                title=card.name,
                detail="This zone is temporarily unavailable.",
            ).to_dict()
