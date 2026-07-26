"""Conversational message send — POST /v1/messages (F0038-S0007, rebuilt in F0039-S0007).

The turn is ordered deliberately:

1. **Persist first.** The user's message is written to their owner-scoped thread before
   anything else. If that write fails the turn stops there (S0001 business rule 4) —
   routing an unpersisted message would produce work with no record of what asked for it.
2. **Resolve before dispatch.** The direct resolver (S0006) runs next. A head is only
   reached after a *validated* route, and the head id comes from the trusted catalog,
   never from model output.
3. **Dispatch only on a validated route.** Every other outcome — redirect, clarify,
   resolver failure, provider timeout — persists a bounded assistant envelope and makes
   **no engine call**.

Every outcome is persisted as a replayable envelope, so the transcript after a reload is
the turn that actually happened, refusals included.

Authorization is unchanged by any of this: a resolver decision grants nothing. When a head
runs, its tools still call the engine as the user, and the engine authorizes (ADR-027 §8).

The deterministic ``scope_guard`` is retained as the shadow baseline and the rollback path
(spec §33): ``NEURON_INTENT_MODE=deterministic`` restores the F0038 behaviour with no code
change and no deploy.
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

from . import envelope as env
from . import scope_guard
from .errors import NeuronError, PersistenceUnavailableError
from .intent.resolver import IntentResolver, ResolutionOutcome, build_resolver
from .intent.response_policy import UNAVAILABLE_TEXT, reply_text_for
from .orchestration.zone_heads import HeadContext

if TYPE_CHECKING:
    from .runtime import NeuronRuntime

_SCOPE_GUARD_CARD = "crm.scope_guard"
_RESOLVER_CARD = "crm.intent_resolver"
_GLANCE_PLAN_ID = "day-at-a-glance"

# Routing modes (spec §33). `deterministic` is the tested rollback: it restores the F0038
# keyword guard exactly, so reverting is a config change rather than a deploy.
MODE_DETERMINISTIC = "deterministic"
MODE_DIRECT = "direct"
# Shadow: the deterministic guard decides production, the resolver runs recorded-only.
MODE_SHADOW = "shadow"


class EmptyMessageError(NeuronError):
    """The message body carried no text to classify."""

    status = 400
    title = "Empty message"


class MessageDispatcher:
    def __init__(self, runtime: "NeuronRuntime", *, resolver: IntentResolver | None = None) -> None:
        self._rt = runtime
        self._resolver = resolver

    # --- entry point ---------------------------------------------------------

    async def dispatch(
        self,
        *,
        text: str | None,
        thread_id: str | None,
        user_token: str,
        owner_user_id: str,
        client_message_key: str | None = None,
    ) -> dict[str, Any]:
        clean = (text or "").strip()
        if not clean:
            raise EmptyMessageError("message text is required")

        rt = self._rt
        thread = await self._open_thread(thread_id, owner_user_id)

        # 1. PERSIST FIRST — a failure aborts before any resolution or dispatch. The
        #    idempotency key makes a retried send return the original row instead of
        #    duplicating the turn.
        await rt.repository.add_message(
            thread.id,
            owner_user_id,
            role="user",
            parts=[("text", env.text_part(clean))],
            client_message_key=client_message_key,
        )

        mode = self._mode()
        if mode in (MODE_DETERMINISTIC, MODE_SHADOW):
            return await self._dispatch_deterministic(
                clean, thread, user_token, owner_user_id, shadow=(mode == MODE_SHADOW)
            )

        # 2. RESOLVE BEFORE DISPATCH.
        outcome = await self._resolve(clean)
        await self._record_resolution(thread, outcome)

        # 3. DISPATCH ONLY ON A VALIDATED ROUTE.
        if not outcome.should_route:
            return await self._finish(
                thread, owner_user_id, [env.text_part(reply_text_for(outcome.resolution))]
            )

        if outcome.resolution.requires_confirmation:
            # A write-like action is proposed, never auto-committed. The user confirms
            # via the existing /v1/actions path, which the engine then authorizes.
            return await self._finish(
                thread, owner_user_id, [env.text_part(self._confirmation_text(outcome))]
            )

        return await self._route_head(
            outcome.resolution.target_head_card_id, thread, user_token, owner_user_id
        )

    # --- modes ---------------------------------------------------------------

    def _mode(self) -> str:
        return getattr(self._rt.settings, "intent_mode", MODE_DIRECT)

    def _resolver_for(self) -> IntentResolver:
        if self._resolver is None:
            self._resolver = build_resolver(self._rt)
        return self._resolver

    async def _resolve(self, text: str) -> ResolutionOutcome:
        return await self._resolver_for().resolve(text)

    async def _dispatch_deterministic(
        self, clean: str, thread, user_token: str, owner_user_id: str, *, shadow: bool = False
    ) -> dict[str, Any]:
        """The F0038 keyword guard — the rollback path, and production in shadow mode.

        In shadow mode the resolver still runs, but **only to be recorded**: its result
        never selects a route, never reaches a head, and never produces user-visible
        prose. That is what makes shadow safe to enable in production — the observed
        behaviour is byte-for-byte the deterministic one.
        """
        rt = self._rt
        decision = rt.agents.get(_SCOPE_GUARD_CARD).handler.evaluate(clean)
        await self._record_guard_decision(thread, decision)

        if shadow:
            await self._record_shadow(thread, clean, decision)
        if decision.category == scope_guard.ALLOW:
            return await self._route_head(
                decision.target_head_card_id, thread, user_token, owner_user_id
            )
        return await self._finish(
            thread, owner_user_id, [env.text_part(decision.reply_text or "")]
        )

    # --- internals -----------------------------------------------------------

    async def _open_thread(self, thread_id: str | None, owner_user_id: str):
        return await self._rt.task_manager.open_context(
            owner_user_id,
            thread_id=thread_id,
            anchor_type="domain",
            anchor_ref=_GLANCE_PLAN_ID,
            title="Day at a Glance",
        )

    async def _record_resolution(self, thread, outcome: ResolutionOutcome):
        """Record the resolver turn in the operation store — codes and hashes only.

        This A2A run is the parent of any downstream head run, so a completed turn is
        traceable end to end. The tool-call digest carries the bounded decision codes and
        **never** the user's text or the model's raw output.
        """
        rt = self._rt
        registered = rt.agents.get(_RESOLVER_CARD)
        run = await rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], registered.card)
        resolution = outcome.resolution
        await rt.task_manager.record_tool_call(
            run,
            "intent.resolve",
            request_digest=(
                f"scope={resolution.scope.decision};"
                f"reason={resolution.scope.reason_code};"
                f"intent={resolution.intent.decision};"
                f"routed={resolution.should_route}"
            ),
            status="ok" if outcome.provenance else "failed",
            latency_ms=outcome.latency_ms,
        )
        if outcome.provenance is not None:
            # Model/prompt/catalog provenance — ids, versions, hashes, counts. No content.
            await rt.task_manager.emit_provenance(
                run,
                model=outcome.provenance.model,
                content_hash=outcome.provenance.content_hash,
                prompt_id=outcome.prompt_reference,
                prompt_version=outcome.catalog_version,
                latency_ms=outcome.latency_ms,
                cost=outcome.provenance.cost,
            )
        state = "input_required" if resolution.intent.decision == "clarify" else "completed"
        await rt.task_manager.complete_run(run, state=state)
        return run

    async def _record_guard_decision(self, thread, decision: "scope_guard.GuardDecision"):
        """Deterministic-mode trace (F0038 behaviour, digest only)."""
        rt = self._rt
        card = rt.agents.get(_SCOPE_GUARD_CARD).card
        run = await rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], card)
        await rt.task_manager.record_tool_call(
            run,
            "scope_guard.classify",
            request_digest=f"intent={decision.intent};category={decision.category}",
            status="ok",
        )
        state = "input_required" if decision.category == scope_guard.CLARIFY else "completed"
        await rt.task_manager.complete_run(run, state=state)
        return run

    async def _record_shadow(self, thread, clean: str, guard_decision) -> None:
        """Run the resolver for comparison only and record the disagreement, if any.

        Failures here are swallowed on purpose: shadow mode must never be able to break
        or slow a production turn that the deterministic guard already decided. A model
        outage during shadow evaluation is a gap in the data, not an incident.
        """
        try:
            outcome = await self._resolve(clean)
        except Exception:
            return
        shadow_routed = outcome.should_route
        guard_allowed = guard_decision.category == scope_guard.ALLOW
        run = await self._record_resolution(thread, outcome)
        await self._rt.task_manager.record_tool_call(
            run,
            "intent.shadow_compare",
            request_digest=(
                f"guard={guard_decision.category};"
                f"guard_head={guard_decision.target_head_card_id};"
                f"shadow_routed={shadow_routed};"
                f"shadow_head={outcome.resolution.target_head_card_id};"
                f"agree={guard_allowed == shadow_routed}"
            ),
            status="ok",
            latency_ms=outcome.latency_ms,
        )

    @staticmethod
    def _confirmation_text(outcome: ResolutionOutcome) -> str:
        actions = ", ".join(outcome.resolution.intent.actions)
        return f"That would run {actions}, which makes a change. Confirm and I'll go ahead."

    async def _route_head(
        self, head_card_id: str, thread, user_token: str, owner_user_id: str
    ) -> dict[str, Any]:
        """Dispatch to a specialist head. The head's tools call the engine as the user and
        the engine authorizes — nothing here grants access."""
        rt = self._rt
        registered = rt.agents.get(head_card_id)
        run = await rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], registered.card)
        try:
            ctx = HeadContext(
                user_token=user_token,
                owner_user_id=owner_user_id,
                thread_id=thread.id,
                tools=rt.tools,
                task_manager=rt.task_manager,
                run=run,
            )
            payload = await registered.handler.build_zone(ctx)
            payload.validated()
            await rt.task_manager.complete_run(run, state="completed")
        except Exception:
            # Contain a head failure to a bounded reply — never a raw error to the user.
            await rt.task_manager.complete_run(run, state="failed")
            return await self._finish(thread, owner_user_id, [env.text_part(UNAVAILABLE_TEXT)])

        return await self._finish(thread, owner_user_id, self._zone_to_parts(payload))

    @staticmethod
    def _zone_to_parts(payload) -> list[dict[str, Any]]:
        status = payload.zone_status
        if status == "content" and payload.component:
            title = payload.title or "your CRM"
            return [
                env.text_part(f"Here's what needs your attention in {title}."),
                env.app_part(payload.component, payload.props or {}),
            ]
        if status == "empty":
            return [env.text_part(payload.detail or "Nothing needs your attention right now.")]
        if status == "inactive":
            title = payload.title or "That area"
            return [
                env.text_part(
                    f"{title} isn't active in the companion yet — it's coming in a later "
                    "release. I can help you with your renewals today."
                )
            ]
        return [env.text_part(UNAVAILABLE_TEXT)]

    async def _finish(self, thread, owner_user_id, parts) -> dict[str, Any]:
        """Persist the assistant envelope so every outcome replays after a reload.

        If the store cannot record the reply we still return it — losing the answer as
        well as the record helps nobody — but the envelope is marked ``failed`` with a
        plain explanation. Silently returning an unsaved reply would leave the user
        looking at an answer that vanishes on reload, with a user turn and no response
        in the transcript (G3 finding M1).
        """
        try:
            await self._rt.repository.add_message(
                thread.id,
                owner_user_id,
                role="assistant",
                parts=[(p["part_type"], p) for p in parts],
            )
        except PersistenceUnavailableError:
            parts = [
                *parts,
                env.status_part(
                    "failed",
                    detail="This reply could not be saved to your conversation.",
                ),
            ]
        return env.build_envelope(thread.id, role="assistant", parts=parts)
