# F0039 — Neuron Durable Conversations & Local Phi Intent Resolution — Status

**Overall Status:** Planned — **plan complete & approved** (run `2026-07-21-6eeb172f`). Phase A requirements
committed (G3 `approve-phase-a` ✓) and Phase B architecture authored + approved (G5 `approve-phase-b` ✓),
re-derived from the F0038 as-built seams + the authored design spec
`neuron-phi-intent-security-implementation-spec.md`. All gates G1–G5 PASS; exit validation green. **Ready for
the `feature`/`build` action. No implementation yet.**
**Last Updated:** 2026-07-22

> Phase A (PM requirements) re-derived the provisional skeleton into a committed PRD + 9 stories (S0001–S0009).
> S0009 (Phi contextual adjudicator) is **GATED** — enabled only after S0001–S0008 direct-routing + context
> gates pass (spec §33 Phase 4). Phase B (architecture) authored the feature-assembly-plan, the Phi
> intent-resolution ADR, the `neuron-api.yaml` thread/history contract, and the KG ontology bindings.
> Story Signoff Provenance rows are placeholders until reviews run during the `feature` action (rows are
> append-only audit history).

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0039-S0001 | Durable Neuron conversation store (Postgres, server sequence, idempotency, restart-durable) | Planned |
| F0039-S0002 | Owner-scoped thread & history API (create/list/get/rename/delete + paginated resumable history) | Planned |
| F0039-S0003 | Conversation-first Neuron panel (thread UX, server-rehydrated transcript, Daily Brief envelope) | Planned |
| F0039-S0004 | Structured provider & verified local Phi profile (async structured completion; vLLM/Phi; provenance) | Planned |
| F0039-S0005 | Catalog, prompt registry & composed resolution contract (schemas + deterministic invariants) | Planned |
| F0039-S0006 | Deterministic preflight & one-call direct resolver (fail-closed validation; trusted-head routing) | Planned |
| F0039-S0007 | Dispatcher, persistence & provenance integration (persist-first, resolve-before-dispatch, traceable) | Planned |
| F0039-S0008 | Evaluation, shadow mode & rollout (datasets, gates, tested deterministic rollback, load tests) | Planned |
| F0039-S0009 | Contextual adjudicator (GATED follow-on — bounded second Phi call after S0001–S0008 gates pass) | Planned (gated) |

## Required Role Matrix

_Required signoff roles, set in planning (Phase B). The Story Signoff Provenance table below records the
run-level role verdicts against each story during the `feature` action._

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Acceptance-criteria coverage + persistence/resume validation + intent routing/eval metrics against §30.4 gates. | Architect | 2026-07-21 |
| Code Reviewer | Yes | Independent review of the conversation store, thread API, dispatcher, and deterministic intent-validation logic. | Architect | 2026-07-21 |
| Architect | Yes | Persistence ownership (ADR-028), the Phi intent-resolution ADR, `neuron-api.yaml` + schema/catalog contracts, and A2A run provenance require explicit approval. | Architect | 2026-07-21 |
| AI Engineer | Yes | Structured Phi provider, prompt registry/provenance, intent catalog, resolver, evaluation harness, and shadow→direct→gated-adjudication rollout. | Architect | 2026-07-21 |
| Security | Yes | Prompt-injection / scope-escape handling, deterministic authority boundaries, fail-closed enforcement, owner-scoping/privacy, token handling, and the adversarial false-allow gate. | Architect | 2026-07-21 |

## Story Signoff Provenance

> Placeholder rows — the run-level role verdict is applied per story during the `feature` action
> (Quality Engineer + Code Reviewer shown as the core independent roles; Architect, AI Engineer, and
> Security verdicts are appended per the Required Role Matrix when their reviews run). Rows are append-only
> audit history.

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0039-S0001 | Quality Engineer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0001 | Code Reviewer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0002 | Quality Engineer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0002 | Code Reviewer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0003 | Quality Engineer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0003 | Code Reviewer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0004 | AI Engineer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0005 | AI Engineer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0006 | Security | - | N/A | - | - | Populate during the feature run. |
| F0039-S0007 | Code Reviewer | - | N/A | - | - | Populate during the feature run. |
| F0039-S0008 | Security | - | N/A | - | - | Populate during the feature run. |
| F0039-S0009 | Security | - | N/A | - | - | Populate during the feature run (only if the gate opens adjudication). |

## Notes

- Source of scope: [`neuron-phi-intent-security-implementation-spec.md`](./neuron-phi-intent-security-implementation-spec.md)
  (v1.1.0, local Phi runtime verified 2026-07-21) + [`PRD.md`](./PRD.md). Epic intake:
  [`../F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).
- **G1 product decisions (2026-07-21):** (1) F0039 = all 9 stories S0001–S0009 with S0009 gated; (2) display
  name updated to "Neuron Durable Conversations & Local Phi Intent Resolution" (folder slug unchanged);
  (3) required signoff roles = QE, Code Reviewer, Architect, AI Engineer, Security.
- **ADR-028 authoritative:** Neuron owns and writes `neuron.*` directly (Postgres via the engine DB, not
  through the engine API). Provisional "through the engine" wording corrected in this run.
- KG: F0039 feature-mapping + canonical-node bindings authored by the Architect in Phase B; `code-index.yaml`
  bindings to the as-built `neuron/` source are reconciled at the feature action's KG gate.
- Base evidence for this plan run: `planning-mds/operations/evidence/runs/2026-07-21-6eeb172f/`.
