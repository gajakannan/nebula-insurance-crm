# F0039 — Neuron Durable Conversations & Local Phi Intent Resolution — Status

**Overall Status:** **Done** — delivered by feature run `2026-07-25-273d5672` (gates G0–G8 PASS).
Plan was completed and approved beforehand (run `2026-07-21-6eeb172f`).
**Delivered: S0001–S0008.** The gated ninth story was **promoted to F0041** (Neuron Contextual
Intent Adjudicator) at closeout — its gate opens only after F0039's direct-routing and context
gates pass, so it could not be built in the run that produces those gate results.
**Shipped in shadow mode:** the §30.4 rollout gate is red on routing *accuracy* (all security
gates green), so direct Phi routing is withheld and `NEURON_INTENT_MODE` defaults to `shadow`,
which is observationally identical to the F0038 deterministic guard.
**Last Updated:** 2026-07-25

> Phase A (PM requirements) re-derived the provisional skeleton into a committed PRD + 9 stories (S0001–S0009).
> S0009 (Phi contextual adjudicator) is **GATED** — enabled only after S0001–S0008 direct-routing + context
> gates pass (spec §33 Phase 4). Phase B (architecture) authored the feature-assembly-plan, the Phi
> intent-resolution ADR, the `neuron-api.yaml` thread/history contract, and the KG ontology bindings.
> Story Signoff Provenance rows are placeholders until reviews run during the `feature` action (rows are
> append-only audit history).

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0039-S0001 | Durable Neuron conversation store (Postgres, server sequence, idempotency, restart-durable) | Done |
| F0039-S0002 | Owner-scoped thread & history API (create/list/get/rename/delete + paginated resumable history) | Done |
| F0039-S0003 | Conversation-first Neuron panel (thread UX, server-rehydrated transcript, Daily Brief envelope) | Done |
| F0039-S0004 | Structured provider & verified local Phi profile (async structured completion; vLLM/Phi; provenance) | Done |
| F0039-S0005 | Catalog, prompt registry & composed resolution contract (schemas + deterministic invariants) | Done |
| F0039-S0006 | Deterministic preflight & one-call direct resolver (fail-closed validation; trusted-head routing) | Done |
| F0039-S0007 | Dispatcher, persistence & provenance integration (persist-first, resolve-before-dispatch, traceable) | Done |
| F0039-S0008 | Evaluation, shadow mode & rollout (datasets, gates, tested deterministic rollback, load tests) | Done |

> The feature's ninth story (contextual adjudicator) was **promoted to F0041-S0001** at closeout and is tracked there.
> It was gated and never built here, so it is not carried as an incomplete story of a Done feature.

## Story × Role Progress

_Live per-story × per-role progress for the feature run `2026-07-25-273d5672` (advisory, not
validator-enforced). Implementation cells flip at G2 as each slice is built and its tests/deployability pass;
the Code Review and Security cells resolve at G3. **Overall** is Done only when every required cell is
Done/PASS. `not-in-scope` cells follow the manifest scope booleans and the slice's own scope._

Cell values: `not-started` · `in-progress` · `done` · `not-in-scope` — review columns: `PASS` / `FAIL`.

| Story | Backend | Frontend | AI | QA | DevOps | Code Review | Security | Overall |
|-------|---------|----------|----|----|--------|-------------|----------|---------|
| F0039-S0001 | done | not-in-scope | not-in-scope | done | done | PASS | PASS | done |
| F0039-S0002 | done | not-in-scope | not-in-scope | done | not-in-scope | PASS | PASS | done |
| F0039-S0003 | not-in-scope | done | not-in-scope | done | not-in-scope | PASS | PASS | done |
| F0039-S0004 | done | not-in-scope | done | done | done | PASS | PASS | done |
| F0039-S0005 | not-in-scope | not-in-scope | done | done | not-in-scope | PASS | PASS | done |
| F0039-S0006 | not-in-scope | not-in-scope | done | done | not-in-scope | PASS | PASS | done |
| F0039-S0007 | done | not-in-scope | done | done | not-in-scope | PASS | PASS | done |
| F0039-S0008 | not-in-scope | not-in-scope | done | done | done | PASS | PASS | done |

## Required Role Matrix

_Required signoff roles, set in planning (Phase B). The Story Signoff Provenance table below records the
run-level role verdicts against each story during the `feature` action._

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Acceptance-criteria coverage + persistence/resume validation + intent routing/eval metrics against §30.4 gates. | Architect | 2026-07-21 |
| Code Reviewer | Yes | Independent review of the conversation store, thread API, dispatcher, and deterministic intent-validation logic. | Architect | 2026-07-21 |
| Architect | Yes | Persistence ownership (ADR-028), the Phi intent-resolution ADR, `neuron-api.yaml` + schema/catalog contracts, and A2A run provenance require explicit approval. | Architect | 2026-07-21 |
| AI Engineer | Yes | Structured Phi provider, prompt registry/provenance, intent catalog, resolver, evaluation harness, and shadow→direct→gated-adjudication rollout. | Architect | 2026-07-21 |
| Security Reviewer | Yes | Prompt-injection / scope-escape handling, deterministic authority boundaries, fail-closed enforcement, owner-scoping/privacy, token handling, and the adversarial false-allow gate. | Architect | 2026-07-21 |
| DevOps | Yes | The slice changes runtime/deployment configuration — `neuron/config/models.yaml` + Phi endpoint settings, the `0002` forward migration, and the local vLLM runtime dependency — so `deployment_config_changed` is true for this run. | Architect (G0, feature run `2026-07-25-273d5672`) | 2026-07-25 |

> **G0 role-name reconciliation (2026-07-25):** the plan-run row `Security` is recorded here under the
> contract's canonical role name **`Security Reviewer`** (the evidence validator matches role names exactly),
> and **DevOps** is added as a scope-forced required role. Same intent as the plan-run G1 decision — no role
> was removed.

## Story Signoff Provenance

> **F0039-S0009 carries no signoff rows by design** — it is gated-deferred and was not
> delivered in run `2026-07-25-273d5672`, so there is nothing to sign off. Its rows are
> added when the gate opens and the story is built.
>
> Rows below are the recorded run-level role verdicts applied per story during the `feature` action
> (Quality Engineer + Code Reviewer shown as the core independent roles; Architect, AI Engineer, and
> Security verdicts are appended per the Required Role Matrix when their reviews run). Rows are append-only
> audit history.

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0039-S0001 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0001 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0001 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0001 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0001 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0001 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0002 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0002 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0002 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0002 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0002 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0002 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0003 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0003 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0003 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0003 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0003 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0003 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0004 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0004 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0004 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0004 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0004 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0004 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0005 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0005 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0005 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0005 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0005 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0005 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0006 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0006 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0006 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0006 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0006 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0006 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0007 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0007 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0007 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0007 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0007 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0007 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0008 | Quality Engineer | Quality Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/test-execution-report.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0008 | Code Reviewer | Code Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/code-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0008 | Security Reviewer | Security Reviewer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/security-review-report.md | 2026-07-25 | **PASS WITH RECOMMENDATIONS** — non-blocking; see the linked report and pm-closeout.md acceptance lines. |
| F0039-S0008 | Architect | Architect (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0008 | DevOps | DevOps (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/deployability-check.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |
| F0039-S0008 | AI Engineer | AI Engineer (F0039 run 2026-07-25-273d5672) | PASS | planning-mds/operations/evidence/runs/2026-07-25-273d5672/g2-self-review.md | 2026-07-25 | Recorded at G5 from the G2/G3 role reports. |

## Notes

- Source of scope: [`neuron-phi-intent-security-implementation-spec.md`](./neuron-phi-intent-security-implementation-spec.md)
  (v1.1.0, local Phi runtime verified 2026-07-21) + [`PRD.md`](./PRD.md). Epic intake:
  [`intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).
- **G1 product decisions (2026-07-21):** (1) F0039 = all 9 stories S0001–S0009 with S0009 gated; (2) display
  name updated to "Neuron Durable Conversations & Local Phi Intent Resolution" (folder slug unchanged);
  (3) required signoff roles = QE, Code Reviewer, Architect, AI Engineer, Security.
- **ADR-028 authoritative:** Neuron owns and writes `neuron.*` directly (Postgres via the engine DB, not
  through the engine API). Provisional "through the engine" wording corrected in this run.
- KG: F0039 feature-mapping + canonical-node bindings authored by the Architect in Phase B; `code-index.yaml`
  bindings to the as-built `neuron/` source are reconciled at the feature action's KG gate.
- Base evidence for this plan run: `planning-mds/operations/evidence/runs/2026-07-21-6eeb172f/`.
