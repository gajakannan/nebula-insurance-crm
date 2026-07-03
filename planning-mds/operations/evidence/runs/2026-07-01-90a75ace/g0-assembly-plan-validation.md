# F0038 G0 Assembly Plan Validation

**Gate:** G0 (Architect assembly-plan authoring + validation)
**Run:** 2026-07-01-90a75ace
**Validator:** Architect Agent (second-pass checklist per `feature.md` Step 0.5)
**Date:** 2026-07-01

## Result

**PASS**

## Artifact Under Validation

- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/feature-assembly-plan.md` — authored at G0 Step 0 of this run from the 8 stories, PRD, intake brief, ADR-027/028, the Neuron + engine API contracts, and the JSON schemas (clean first run; plan did not exist prior).

## Step 0.5 Checklist

- [x] **Scope split matches feature story requirements** — Build order maps every story to a layer: S0001/S0007→neuron runtime+guard; S0002→shell+envelope (neuron+FE); S0003→engine read + neuron head + FE zone; S0004→neuron stubs + FE slots; S0005/S0006→engine mutations + neuron drafter + FE draft/mock-send; S0008→engine telemetry ingest + neuron emission.
- [x] **Dependencies between agents identified** — Dependency Order fixes engine-first (source of truth + ADR-028 §2 cross-store order) → neuron runtime → neuron heads → frontend → QE/DevOps, with named checkpoints.
- [x] **Integration checkpoints feasible** — checkpoints after Step 1 (engine contracts), Step 3 (glance assembly + engine-first writes + scope guard), Step 4 (registry-only rendering + draft reload), plus cross-story lifecycle verification.
- [x] **No missing or conflicting artifact ownership** — Backend owns `engine/**` + Casbin; AI owns `neuron/**` + `neuron.*` persistence; Frontend owns `experience/**`; QE owns tests/coverage/scans; DevOps owns the new neuron service runtime; Architect owns ADRs/contracts/state-machine exception design + KG binding. Matches ADR-027/028 Consequences and the ownership contract.
- [x] **Mutation traceability present** — S0005 (outreach-draft) and S0006 (mock-send) have full traceability tables (endpoint, service method, entity/carrier, authorization, validation failure, audit/timeline, test expectation).
- [x] **Casbin / authorization design captured** — new least-privilege `renewal:draft_outreach` (Underwriter/Admin), path-scoped WorkflowStateMachine `Identified→Outreach` exception (mock-send only), policy.csv embedded-resource sync noted (ADR-028 §3).
- [x] **Security surface flagged** — forwarded token (on-behalf-of), prompt-injection guard, registry-only rendering (no model markup), provenance-without-PII — Security Reviewer required.
- [x] **KG Binding Plan present** — intended `code-index.yaml` glob delta (`neuron/app/**`, `neuron/crm_agents/**`, `experience/src/features/neuron/**`) + candidate canonical nodes (`renewal:draft_outreach`, neuron operation store) recorded as the G7 baseline.
- [x] **Required Signoff Roles matrix initialized in STATUS.md** — QE, Code Reviewer, Security Reviewer, AI Engineer, DevOps, Architect all Required=Yes (set in planning, affirmed at G0 for this run).

## Reconciliations

- No plan-vs-story conflicts requiring override at G0 (clean first run; plan derived directly from stories + ratified ADRs).
- LLM mocked per operator decision (2026-07-01): the outreach drafter runs against a deterministic provider behind the model-router seam; disclosed as an assumption in `action-context.md` and a low risk in the plan.

## Umbrella Reference

- `planning-mds/architecture/feature-assembly-plan.md` updated with an F0038 section referencing this feature-local plan (cross-feature sequencing view).
