"""A2A task manager (ADR-027 §5, ADR-028 §1/§2).

Maps the A2A-aligned internal profile onto the ``neuron.*`` operation store:

- context  → ``neuron.threads`` (A2A contextId == thread_id, owner-scoped)
- task/subtask → ``neuron.agent_runs`` (referencing plan + card by id/version/hash)
- tool call → ``neuron.tool_calls`` (digest only, no raw args/PII)
- provenance → ``neuron.provenance_events`` (model/ids/hash/cost/latency — no raw prompts)

This is the seam the heads and the outreach drafter (S0003/S0005/S0006) build on.
The cross-store engine reference is bound idempotently on the run id (ADR-028 §2).
"""

from __future__ import annotations

from ..persistence.models import AgentRun, ProvenanceEvent, Thread, ToolCall
from ..persistence.repository import NeuronRepository
from .agent_card import AgentCard
from .plan import OrchestrationPlan


class A2ATaskManager:
    def __init__(self, repo: NeuronRepository) -> None:
        self._repo = repo

    def open_context(
        self,
        owner_user_id: str,
        *,
        thread_id: str | None = None,
        anchor_type: str = "free_form",
        anchor_ref: str | None = None,
        title: str | None = None,
    ) -> Thread:
        """Resume the owner's thread if given + visible, else open a new context."""
        if thread_id is not None:
            existing = self._repo.get_thread(thread_id, owner_user_id)
            if existing is not None:
                return existing
        return self._repo.create_thread(
            owner_user_id, anchor_type=anchor_type, anchor_ref=anchor_ref, title=title
        )

    def begin_run(
        self,
        thread: Thread,
        plan: OrchestrationPlan,
        card: AgentCard,
        *,
        parent_run_id: str | None = None,
        state: str = "working",
    ) -> AgentRun:
        run = AgentRun(
            thread_id=thread.id,
            plan_id=plan.plan_id,
            plan_version=plan.plan_version,
            card_id=card.card_id,
            card_version=card.card_version,
            card_content_hash=card.content_hash,
            state=state,
            parent_run_id=parent_run_id,
        )
        return self._repo.create_agent_run(run)

    def record_tool_call(
        self,
        run: AgentRun,
        tool_name: str,
        *,
        request_digest: str,
        status: str,
        latency_ms: int | None = None,
    ) -> ToolCall:
        return self._repo.record_tool_call(
            ToolCall(
                agent_run_id=run.id,
                tool_name=tool_name,
                request_digest=request_digest,
                status=status,
                latency_ms=latency_ms,
            )
        )

    def emit_provenance(
        self,
        run: AgentRun,
        *,
        model: str,
        content_hash: str,
        prompt_id: str | None = None,
        prompt_version: str | None = None,
        trace_id: str | None = None,
        cost: float | None = None,
        latency_ms: int | None = None,
    ) -> ProvenanceEvent:
        # WHY: ProvenanceEvent structurally cannot hold raw prompts/responses/PII —
        # the redaction guarantee is enforced by the record shape, not by filtering.
        return self._repo.record_provenance(
            ProvenanceEvent(
                agent_run_id=run.id,
                model=model,
                content_hash=content_hash,
                prompt_id=prompt_id,
                prompt_version=prompt_version,
                trace_id=trace_id,
                cost=cost,
                latency_ms=latency_ms,
            )
        )

    def complete_run(
        self,
        run: AgentRun,
        *,
        state: str = "completed",
        engine_ref_type: str | None = None,
        engine_ref_id: str | None = None,
    ) -> AgentRun:
        """Finish a run, optionally binding the authoritative engine write (ADR-028 §2)."""
        if engine_ref_id is not None:
            if engine_ref_type is None:
                raise ValueError("engine_ref_type required when engine_ref_id is set")
            self._repo.attach_engine_ref(run.id, engine_ref_type, engine_ref_id)
        return self._repo.update_run_state(run.id, state)
