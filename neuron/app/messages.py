"""Conversational message send — POST /v1/messages (F0038-S0007).

Runs the CRM scope guard on **every** inbound message before any handler executes
(intake L8: a CRM-scoped assistant, not a general chatbot). In-scope CRM intents route
to the matching specialist head/zone; out-of-scope or prompt-injection messages get a
polite CRM redirect; ambiguous messages get a CRM-framed clarifying question; a
classifier failure fails safe to the redirect. The guard grants no authorization — an
in-scope read still calls the engine as the user, which authorizes it.

The guard decision is recorded in the operation store (a ``scope_guard`` ``agent_run``
plus a digest-only ``tool_call`` carrying the bounded intent label — never the user's
raw text) for observability (S0007 validation rule). The user's own message is stored
in their private, owner-scoped thread like any other chat turn.
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

from . import envelope as env
from . import scope_guard
from .errors import NeuronError
from .orchestration.zone_heads import HeadContext

if TYPE_CHECKING:
    from .runtime import NeuronRuntime

_SCOPE_GUARD_CARD = "crm.scope_guard"
_GLANCE_PLAN_ID = "day-at-a-glance"
_UNAVAILABLE_TEXT = (
    "That part of your CRM is temporarily unavailable — please try again in a moment."
)


class EmptyMessageError(NeuronError):
    """The message body carried no text to classify."""

    status = 400
    title = "Empty message"


class MessageDispatcher:
    def __init__(self, runtime: "NeuronRuntime") -> None:
        self._rt = runtime

    async def dispatch(
        self,
        *,
        text: str | None,
        thread_id: str | None,
        user_token: str,
        owner_user_id: str,
    ) -> dict[str, Any]:
        clean = (text or "").strip()
        if not clean:
            raise EmptyMessageError("message text is required")

        rt = self._rt
        thread = self._open_thread(thread_id, owner_user_id)
        # Persist the user's own turn in their private, owner-scoped thread.
        rt.repository.add_message(
            thread.id, owner_user_id, role="user", parts=[("text", env.text_part(clean))]
        )

        # Guard runs BEFORE any handler — a non-CRM message never reaches a data handler.
        decision = rt.agents.get(_SCOPE_GUARD_CARD).handler.evaluate(clean)
        self._record_guard_decision(thread, decision)

        if decision.category == scope_guard.ALLOW:
            return await self._route_in_scope(decision, thread, user_token, owner_user_id)

        # REDIRECT / CLARIFY — a bounded text reply; no engine call, no data access.
        return self._finish(thread, owner_user_id, [env.text_part(decision.reply_text or "")])

    # --- internals ----------------------------------------------------------

    def _open_thread(self, thread_id: str | None, owner_user_id: str):
        return self._rt.task_manager.open_context(
            owner_user_id,
            thread_id=thread_id,
            anchor_type="domain",
            anchor_ref=_GLANCE_PLAN_ID,
            title="Day at a Glance",
        )

    def _record_guard_decision(self, thread, decision: "scope_guard.GuardDecision"):
        """Record the guard decision in the operation store (observability, S0007).

        Digest only — the bounded intent label + policy category, never the user's raw
        message (no PII, no injection payload persisted here). A clarify is
        ``input_required`` (we asked the user for more); everything else ``completed``.
        """
        rt = self._rt
        card = rt.agents.get(_SCOPE_GUARD_CARD).card
        run = rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], card)
        rt.task_manager.record_tool_call(
            run,
            "scope_guard.classify",
            request_digest=f"intent={decision.intent};category={decision.category}",
            status="ok",
        )
        state = "input_required" if decision.category == scope_guard.CLARIFY else "completed"
        rt.task_manager.complete_run(run, state=state)
        return run

    async def _route_in_scope(self, decision, thread, user_token, owner_user_id) -> dict[str, Any]:
        rt = self._rt
        registered = rt.agents.get(decision.target_head_card_id)
        run = rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], registered.card)
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
            rt.task_manager.complete_run(run, state="completed")
        except Exception:
            # Contain a head failure to a bounded reply — never a raw error to the user.
            rt.task_manager.complete_run(run, state="failed")
            return self._finish(thread, owner_user_id, [env.text_part(_UNAVAILABLE_TEXT)])

        return self._finish(thread, owner_user_id, self._zone_to_parts(payload))

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
        return [env.text_part(_UNAVAILABLE_TEXT)]

    def _finish(self, thread, owner_user_id, parts) -> dict[str, Any]:
        message = env.build_envelope(thread.id, role="assistant", parts=parts)
        self._rt.repository.add_message(
            thread.id, owner_user_id, role="assistant", parts=[(p["part_type"], p) for p in parts]
        )
        return message
