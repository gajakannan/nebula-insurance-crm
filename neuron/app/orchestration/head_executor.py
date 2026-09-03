"""Shared specialist-head lifecycle for glance and conversation (F0040-S0003)."""

from __future__ import annotations

import asyncio
import time
from typing import TYPE_CHECKING

from ..errors import UpstreamAuthError
from ..telemetry import CompanionTelemetry, build_head_outcome_event
from .zone_heads import HeadContext, ZonePayload, zone_id_for_card

if TYPE_CHECKING:
    from ..persistence.models import Thread
    from ..runtime import NeuronRuntime

_PLAN_ID = "day-at-a-glance"


class HeadExecutor:
    def __init__(self, runtime: "NeuronRuntime") -> None:
        self._rt = runtime

    async def execute(
        self,
        head_card_id: str,
        thread: "Thread",
        token: str,
        owner: str,
        entry_point: str,
    ) -> ZonePayload:
        if entry_point not in {"glance", "conversation"}:
            raise ValueError("entry_point must be glance or conversation")

        rt = self._rt
        registered = rt.agents.get(head_card_id)
        card = registered.card
        plan = rt.plans[_PLAN_ID]
        step = next((candidate for candidate in plan.steps if candidate.agent == head_card_id), None)
        if step is None:
            raise RuntimeError(f"head {head_card_id!r} has no plan step")
        if card.active and step.timeout_ms is None:
            raise RuntimeError(f"active head {head_card_id!r} has no bounded plan step")
        # Inactive heads are local placeholders and never touch Engine. Keep them
        # inside the same lifecycle while giving their immediate stub execution a
        # defensive ceiling that is not part of the active-head contract.
        timeout_ms = step.timeout_ms or 2_000

        run = await rt.task_manager.begin_run(thread, plan, card)
        started = time.monotonic()
        terminal_result = "error"
        try:
            ctx = HeadContext(
                user_token=token,
                owner_user_id=owner,
                thread_id=thread.id,
                tools=rt.tools,
                task_manager=rt.task_manager,
                run=run,
            )
            payload = await asyncio.wait_for(
                registered.handler.build_zone(ctx),
                timeout=timeout_ms / 1000,
            )
            payload.validated()
            expected_zone_id = zone_id_for_card(head_card_id)
            if payload.zone_id != expected_zone_id:
                raise ValueError("head returned a payload for another zone")
            if payload.zone_status == "content":
                if not payload.component or payload.component not in card.components:
                    raise ValueError("head emitted a component it does not own")
                rt.components.validate(payload.component, payload.props or {})

            terminal_result = payload.zone_status if payload.zone_status in {"content", "empty"} else "error"
            await rt.task_manager.complete_run(run, state="completed")
            return payload
        except UpstreamAuthError as exc:
            await rt.task_manager.complete_run(run, state="failed")
            terminal_result = "rejected"
            if exc.status == 401:
                raise
            return self._error_payload(card.card_id, card.name)
        except Exception:
            await rt.task_manager.complete_run(run, state="failed")
            terminal_result = "error"
            return self._error_payload(card.card_id, card.name)
        finally:
            latency_ms = int((time.monotonic() - started) * 1000)
            await CompanionTelemetry(rt.tools).emit(
                token,
                [
                    build_head_outcome_event(
                        owner,
                        thread_id=thread.id,
                        head_run_id=run.id,
                        zone_id=zone_id_for_card(head_card_id),
                        entry_point=entry_point,
                        terminal_result=terminal_result,
                        latency_ms=latency_ms,
                    )
                ],
            )

    @staticmethod
    def _error_payload(card_id: str, title: str) -> ZonePayload:
        broker = zone_id_for_card(card_id) == "broker_activity"
        return ZonePayload(
            zone_id=zone_id_for_card(card_id),
            zone_status="error",
            title=title,
            detail=(
                "Unable to load broker activity."
                if broker
                else "This zone is temporarily unavailable."
            ),
        ).validated()
