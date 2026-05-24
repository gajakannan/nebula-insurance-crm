# Action Context — F0035 plan run 2026-05-23-41109356

## Run Identity

- Action: `plan` (Phase A+B)
- Feature ID: F0035
- Feature slug: `session-continuity-and-token-refresh`
- PLAN_RUN_ID: `2026-05-23-41109356`
- PLAN_RUN_FOLDER: `planning-mds/operations/evidence/2026-05-23-41109356/` (base run path per §8; NOT under a feature evidence root)
- PRODUCT_ROOT (absolute): `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- Run start (local): 2026-05-23T21:00:11-04:00

## Inputs

Declared inputs (operator-provided):

| Input | Value | Source |
|-------|-------|--------|
| `FEATURE_ID` | `F0035` | operator prompt |
| `PHASE` | `A+B` | operator prompt |
| `FEATURE_MODE` | `new` (operator override; effectively `existing` per contract) | operator prompt; see Assumptions below |
| `PRODUCT_ROOT` | default fallback (`../nebula-insurance-crm`) | `NEBULA_PRODUCT_ROOT` unset, no operator override |

Auto-resolved:

| Variable | Value |
|----------|-------|
| `FEATURE_SLUG` | `session-continuity-and-token-refresh` |
| `FEATURE_PATH` | `planning-mds/features/F0035-session-continuity-and-token-refresh` |
| `FEATURE_EVIDENCE_ROOT` | `planning-mds/operations/evidence/F0035-session-continuity-and-token-refresh` (not created during plan) |
| `PLAN_RUN_ID` | `2026-05-23-41109356` |
| `PLAN_RUN_FOLDER` | `planning-mds/operations/evidence/2026-05-23-41109356/` |

## Assumptions

1. **`FEATURE_MODE=new` operator override.** Contract definition says `new` requires the folder to not exist. F0035 folder already contains `PRD.md` + `STATUS.md` skeleton — that matches the `existing` definition exactly. Operator was surfaced this conflict and elected to keep the declared label `new` while treating the existing scaffold as seed material (no destructive recreation). This run honors that choice: Phase A enriches the existing PRD/STATUS in place (append-only provenance), and no folder collision is triggered.
2. **Pre-flight remediation.** Pre-flight `kg/validate.py` initially exited 1 with two errors: F0035 not mapped (the exact gap this plan resolves) + stale `coverage-report.yaml`. Operator authorized a minimal `feature-mappings.yaml` stub (`id`, `path`, `status: planned`) plus `--write-coverage-report` refresh as bootstrap. Both completed; validate now exits 0. Stub is documented as Open Follow-up for Architect Phase B enrichment.
3. **Cache priors.** PRD.md (line 124) explicitly defers stories — Phase A is expected to write the first stories for this feature, not edit existing ones. STATUS.md provenance section is currently empty; Phase A will create the initial Story Provenance table per agent-map.yaml ownership.

## Scope Boundaries

In scope for this plan run:

- Enrich `PRD.md` with personas, acceptance criteria, screen specs (UX-light per current PRD framing), and story breakdown
- Author story files `F0035-S####-{slug}.md`
- Extend `STATUS.md` with story provenance table and required role matrix (PM Phase A skeleton)
- Author `feature-assembly-plan.md` (Architect Phase B)
- Author ADR(s) for session-continuity authentication strategy
- Update OpenAPI / schema artifacts only if F0035 requires new contracts or alters existing 401-handling envelope
- Enrich `feature-mappings.yaml` F0035 entry (Architect Phase B at B1)
- Potentially add canonical nodes (e.g. `capability:session-continuity`) if shared semantics introduced
- Update REGISTRY, ROADMAP, STORY-INDEX, BLUEPRINT Section 3 (if scope warrants)

Out of scope:

- Any implementation under `engine/`, `experience/`, or `neuron/` (plan-only; build action handles)
- Creation of `feature-assembly-plan.md` is split: assembly planning happens at Step 0 of the feature action; for plan A+B per `plan.md` deliverables contract, **the feature-assembly-plan.md is NOT a plan deliverable** (per `agents/actions/plan.md` Deliverables Contract). The Architect Phase B work here produces architectural artifacts (ADRs, API/schema updates, ontology bindings) without the assembly plan.
- Changes to `solution-ontology.yaml` (architect-only; only if ontology vocabulary itself must change)
- Modifications to `canonical-nodes.yaml` outside architect role
- Creation of any feature evidence package at `{FEATURE_EVIDENCE_ROOT}/` (that belongs to feature action)
- Generation of any `g0-*`, `test-*`, `code-review-*` role reports (feature-action territory)

## Lifecycle Stage

Plan action stages:

- A0 PM REQUIREMENTS DRAFT
- A1 PM APPROVAL GATE
- B0 ARCHITECT ARCHITECTURE
- B1 ONTOLOGY SYNC GATE
- B2 ARCHITECT APPROVAL GATE
- Exit validation

This is **plan**, not **feature**: per `plan.md` Exit Validation, `validate-feature-evidence.py` is NOT invoked at plan close. The first stage validation call (`--stage G0`) happens later during the feature action.
