"""Component action callbacks — POST /v1/actions (F0038).

Executes an allow-listed action echoed from an envelope ``actions`` part. The handler
re-authorizes server-side by calling the engine with the forwarded user token — the
action payload never carries authorization claims. F0038-S0003 implements
``drill_renewal``; F0038-S0005/S0006 implement ``draft_outreach``/``mock_send``
(outreach drafter, engine-first + idempotent Neuron record per ADR-028 §2);
``scope_redirect_ack`` (F0038-S0007) is a benign client acknowledgment of a scope
redirect — no engine call, no data access, no state change.
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

from . import envelope as env
from . import telemetry as tel
from .errors import NeuronError, UnknownActionError
from .telemetry import CompanionTelemetry
from .outreach_drafter import (
    DRAFT_COMPONENT,
    DRAFTER_CARD_ID,
    PROMPT_ID,
    PROMPT_VERSION,
    content_hash,
    content_violation,
    generate_draft_body,
)

if TYPE_CHECKING:
    from .runtime import NeuronRuntime

_COMPANION_CONTEXT_TOOL = "engine.renewals.companion_context"
_COMPANION_CONTEXT_COMPONENT = "renewals.companion_context"
_OUTREACH_DRAFT_TOOL = "engine.renewals.outreach_draft"
_OUTREACH_MOCK_SEND_TOOL = "engine.renewals.outreach_mock_send"
_GLANCE_PLAN_ID = "day-at-a-glance"


class ContentConstraintError(NeuronError):
    """The draft/edited content violated the no-premium/quote/terms/binding constraint."""

    status = 422
    title = "Draft content not allowed"


class ActionDispatcher:
    def __init__(self, runtime: "NeuronRuntime") -> None:
        self._rt = runtime

    async def dispatch(
        self,
        *,
        action_type: str,
        action_id: str | None,
        payload: dict[str, Any] | None,
        thread_id: str | None,
        user_token: str,
        owner_user_id: str,
    ) -> dict[str, Any]:
        if action_type == "drill_renewal":
            return await self._drill_renewal(payload, thread_id, user_token, owner_user_id)
        if action_type == "draft_outreach":
            return await self._draft_outreach(payload, thread_id, user_token, owner_user_id)
        if action_type == "mock_send":
            return await self._mock_send(payload, thread_id, user_token, owner_user_id)
        if action_type == "scope_redirect_ack":
            return await self._scope_redirect_ack(thread_id, owner_user_id)
        raise UnknownActionError(f"unknown action_type {action_type!r}")

    def _open_thread(self, thread_id: str | None, owner_user_id: str):
        return self._rt.task_manager.open_context(
            owner_user_id,
            thread_id=thread_id,
            anchor_type="domain",
            anchor_ref=_GLANCE_PLAN_ID,
            title="Day at a Glance",
        )

    async def _drill_renewal(self, payload, thread_id, user_token, owner_user_id) -> dict[str, Any]:
        renewal_id = (payload or {}).get("renewalId")
        if not renewal_id:
            raise UnknownActionError("drill_renewal requires payload.renewalId")

        rt = self._rt
        thread = self._open_thread(thread_id, owner_user_id)
        tool = rt.tools.get(_COMPANION_CONTEXT_TOOL)
        context = await tool.invoke(user_token=user_token, path_params={"renewalId": renewal_id})

        card = rt.agents.get("crm.renewals.head").card
        run = rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], card)
        rt.task_manager.record_tool_call(
            run, _COMPANION_CONTEXT_TOOL, request_digest=f"companion_context:{renewal_id}", status="ok"
        )
        rt.task_manager.complete_run(run, state="completed")

        account_name = (context or {}).get("accountName", "this renewal")
        parts = [
            env.text_part(f"Here's the latest on {account_name}."),
            env.app_part(_COMPANION_CONTEXT_COMPONENT, context or {}),
        ]
        return self._finish(thread, owner_user_id, parts)

    async def _draft_outreach(self, payload, thread_id, user_token, owner_user_id) -> dict[str, Any]:
        renewal_id = (payload or {}).get("renewalId")
        if not renewal_id:
            raise UnknownActionError("draft_outreach requires payload.renewalId")
        account_name = (payload or {}).get("accountName") or "your client"

        rt = self._rt
        thread = self._open_thread(thread_id, owner_user_id)
        card = rt.agents.get(DRAFTER_CARD_ID).card
        run = rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], card)

        body = generate_draft_body(account_name)
        violation = content_violation(body)
        if violation is not None:
            rt.task_manager.complete_run(run, state="failed")
            raise ContentConstraintError(violation)

        model = rt.model_router.default
        digest = content_hash(body)

        # Engine-first (ADR-028 §2): persist the draft, then get the authoritative event id.
        engine_result = await rt.tools.get(_OUTREACH_DRAFT_TOOL).invoke(
            user_token=user_token,
            path_params={"renewalId": renewal_id},
            json={
                "draft_body": body,
                "internal_only": True,
                "label": "AI-generated draft",
                "provenance": self._provenance(owner_user_id, model, digest, run.id),
            },
        )
        timeline_event_id = (engine_result or {}).get("timelineEventId")

        # Idempotent Neuron record referencing the engine id.
        rt.task_manager.record_tool_call(
            run, _OUTREACH_DRAFT_TOOL, request_digest=f"outreach_draft:{renewal_id}", status="ok"
        )
        rt.task_manager.emit_provenance(
            run, model=model, content_hash=digest, prompt_id=PROMPT_ID, prompt_version=PROMPT_VERSION
        )
        rt.task_manager.complete_run(
            run, state="completed", engine_ref_type="timeline_event", engine_ref_id=timeline_event_id
        )

        # F0038-S0008: draft-ready (primary end) + secondary drafts-generated / actioned.
        # Fire-and-forget, after the authoritative engine write — never breaks the flow.
        await CompanionTelemetry(rt.tools).emit(user_token, [
            tel.build_event(tel.DRAFT_READY, owner_user_id, thread_id=thread.id, renewal_id=renewal_id),
            tel.build_event(tel.DRAFT_GENERATED, owner_user_id, thread_id=thread.id, renewal_id=renewal_id),
            tel.build_event(tel.ATTENTION_RENEWAL_ACTIONED, owner_user_id, thread_id=thread.id, renewal_id=renewal_id),
        ])

        parts = [
            env.text_part(f"Here's a draft outreach for {account_name}. Edit it, then send when ready."),
            env.app_part(
                DRAFT_COMPONENT,
                {
                    "renewalId": renewal_id,
                    "accountName": account_name,
                    "draftBody": body,
                    "timelineEventId": timeline_event_id,
                    "internalOnly": True,
                    "label": "AI-generated draft",
                },
            ),
        ]
        return self._finish(thread, owner_user_id, parts)

    async def _mock_send(self, payload, thread_id, user_token, owner_user_id) -> dict[str, Any]:
        renewal_id = (payload or {}).get("renewalId")
        edited_body = (payload or {}).get("editedBody") or (payload or {}).get("draftBody")
        if not renewal_id or not edited_body:
            raise UnknownActionError("mock_send requires payload.renewalId and payload.editedBody")

        violation = content_violation(edited_body)
        if violation is not None:
            raise ContentConstraintError(violation)

        rt = self._rt
        thread = self._open_thread(thread_id, owner_user_id)
        card = rt.agents.get(DRAFTER_CARD_ID).card
        run = rt.task_manager.begin_run(thread, rt.plans[_GLANCE_PLAN_ID], card)

        # Just-in-time rowVersion for the engine's optimistic-concurrency If-Match.
        renewal = await rt.engine_client.call("GET", f"/renewals/{renewal_id}", user_token=user_token)
        row_version = str((renewal or {}).get("rowVersion", ""))

        model = rt.model_router.default
        digest = content_hash(edited_body)

        # Engine-first: commit the mock-send (atomic Identified->Outreach; no SMTP).
        engine_result = await rt.tools.get(_OUTREACH_MOCK_SEND_TOOL).invoke(
            user_token=user_token,
            path_params={"renewalId": renewal_id},
            headers={"If-Match": f'"{row_version}"'},
            json={
                "final_draft_body": edited_body,
                "simulate_delivery": True,
                "draft_timeline_event_id": (payload or {}).get("draftTimelineEventId"),
                "provenance": self._provenance(owner_user_id, model, digest, run.id),
            },
        )
        transition = (engine_result or {}).get("transition") or {}

        rt.task_manager.record_tool_call(
            run, _OUTREACH_MOCK_SEND_TOOL, request_digest=f"mock_send:{renewal_id}", status="ok"
        )
        rt.task_manager.emit_provenance(
            run, model=model, content_hash=digest, prompt_id=PROMPT_ID, prompt_version=PROMPT_VERSION
        )
        rt.task_manager.complete_run(
            run, state="completed", engine_ref_type="workflow_transition", engine_ref_id=transition.get("id")
        )

        # F0038-S0008: mock-sent count. Fire-and-forget, after the atomic transition.
        await CompanionTelemetry(rt.tools).emit(user_token, [
            tel.build_event(tel.MOCK_SENT, owner_user_id, thread_id=thread.id, renewal_id=renewal_id),
        ])

        account_name = (payload or {}).get("accountName") or "The renewal"
        parts = [
            env.status_part("completed", detail="Sent (simulated)"),
            env.text_part(f"Sent (simulated). {account_name} moved to Outreach — no email was dispatched."),
        ]
        return self._finish(thread, owner_user_id, parts)

    async def _scope_redirect_ack(self, thread_id, owner_user_id) -> dict[str, Any]:
        # Benign acknowledgment (F0038-S0007): the client confirming a scope redirect
        # was shown. No engine call, no data access, no state change — just a typed ack.
        thread = self._open_thread(thread_id, owner_user_id)
        return env.build_envelope(
            thread.id, role="assistant", parts=[env.status_part("completed", detail="acknowledged")]
        )

    @staticmethod
    def _provenance(owner_user_id, model, digest, agent_run_id) -> dict[str, Any]:
        # actor_user_id echoes the caller subject; the engine records its own authenticated
        # actor. No raw prompt/response is sent — only model/prompt/version/hash/run id.
        return {
            "actor_user_id": owner_user_id,
            "model": model,
            "prompt_id": PROMPT_ID,
            "prompt_version": PROMPT_VERSION,
            "content_hash": digest,
            "agent_run_id": agent_run_id,
        }

    def _finish(self, thread, owner_user_id, parts) -> dict[str, Any]:
        message = env.build_envelope(thread.id, role="assistant", parts=parts)
        self._rt.repository.add_message(
            thread.id, owner_user_id, role="assistant", parts=[(p["part_type"], p) for p in parts]
        )
        return message
