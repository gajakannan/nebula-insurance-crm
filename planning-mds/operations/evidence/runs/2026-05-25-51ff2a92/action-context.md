# Action Context — F0036 plan run 2026-05-25-51ff2a92

## Run Identity

- Action: `plan` (Phase A+B)
- Feature ID: F0036
- Feature slug: `dynamic-product-attribute-form-engine`
- PLAN_RUN_ID: `2026-05-25-51ff2a92`
- PLAN_RUN_FOLDER: `planning-mds/operations/evidence/runs/2026-05-25-51ff2a92/` (base run path per §8; NOT under a feature evidence root)
- PRODUCT_ROOT (absolute): `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- Run start (local): 2026-05-25T19:25:00-04:00

## Inputs

Declared inputs (operator-provided):

| Input | Value | Source |
|-------|-------|--------|
| `FEATURE_ID` | `F0036` | operator prompt |
| `PHASE` | `A+B` | operator prompt |
| `FEATURE_MODE` | `existing` | operator prompt; confirmed below |
| `PRODUCT_ROOT` | default fallback (`../nebula-insurance-crm`) | `NEBULA_PRODUCT_ROOT` unset, no operator override |

Auto-resolved:

| Variable | Value |
|----------|-------|
| `FEATURE_SLUG` | `dynamic-product-attribute-form-engine` |
| `FEATURE_PATH` | `planning-mds/features/F0036-dynamic-product-attribute-form-engine` |
| `FEATURE_INDEX_ROOT` | `planning-mds/operations/evidence/features/F0036-dynamic-product-attribute-form-engine` (NOT created during plan) |
| `PLAN_RUN_ID` | `2026-05-25-51ff2a92` |
| `PLAN_RUN_FOLDER` | `planning-mds/operations/evidence/runs/2026-05-25-51ff2a92/` |

## Assumptions

1. **`FEATURE_MODE=existing` confirmed.** The folder `planning-mds/features/F0036-dynamic-product-attribute-form-engine/` already contains `PRD.md`, `README.md`, and a `STATUS.md` skeleton — matching the contract's `existing` definition exactly. No operator override needed (unlike the F0035 plan run). Phase A enriches the existing PRD/STATUS in place; any `STATUS.md` story-provenance rows are append-only and must not be mutated. The Story Provenance table is currently absent (the skeleton has a "Story Checklist (proposed)" only), so Phase A creates the initial empty provenance table — this is a creation, not a mutation of prior rows.
2. **Pre-flight `kg/validate.py` passed without remediation.** Initial `python3 scripts/kg/validate.py` exited 0 (24 features mapped, coverage report fresh). F0036 currently sits in `feature-mappings.yaml` `coverage.excluded_features[]` with reason "Phase A draft feature pending approval; ontology mapping backfill has not started." — this is a valid covered-as-excluded state, so validate passes. The single pre-existing warning (low-confidence inferred edge 0.4 on `feature:F0028` in `feature:F0018.depends_on`) predates F0036 and is carried forward (out of scope for this run).
3. **Folder slug stability.** The folder slug remains `F0036-dynamic-product-attribute-form-engine` even though the feature title was broadened on 2026-05-25 to "Form Engine and Form-State Preservation". This is an intentional decision recorded in the existing PRD/README for link stability; this run preserves it.
4. **Scope was broadened by the operator on 2026-05-25 (today)** from product-attributes-only to two workstreams: A (dynamic product-attribute engine, full ADR-021) and B (CRUD form RHF migration + F0035 preservation). The existing PRD already reflects this broadening; Phase A finalizes the story breakdown (S0001–S0008) and Phase A clarification, and Phase B reconciles ADR-021 with the shipped code.
5. **Grounding verification performed up front** (read-only, see `artifact-trace.md`): F0035 form-preservation API (`useSessionRestorableForm`, `consumeFormSnapshot`, `DirtyFormRegistration`), the Cyber `cyber/1.0.0` bundle (`data-schema.json`, `ui-schema.json`, `rules.json`), `DynamicAttributePanel.tsx`, and the six Workstream B CRUD form components. These confirm the PRD's current-state anchors and surface two concrete Phase B reconciliations (recorded in `gate-decisions.md` B0).

## Scope Boundaries

In scope for this plan run:

- Enrich `PRD.md` (already substantially developed; finalize stories, clarification, and confirm Workstream B inventory)
- Author story files `F0036-S0001..S0008-{slug}.md` (none exist yet)
- Author `acceptance-criteria-checklist.md` for the feature
- Extend `STATUS.md` with the Required Role Matrix (PM skeleton; Architect confirms `Required` values at Phase B) and an empty Story Provenance table
- Reconcile/amend **ADR-021** to match the shipped F0034 reality and decide whether a companion ADR is needed for the F0035 form-preservation integration contract (Architect Phase B)
- Complete the `feature-mappings.yaml` F0036 binding (move from `excluded_features` to `features[]` with `affects`/`governed_by`/`uses_schema`/`depends_on`) (Architect Phase B at B1)
- Add canonical nodes in `canonical-nodes.yaml` only if Phase B introduces reusable shared semantics (e.g. a `capability:dynamic-form-engine`)
- Update REGISTRY, ROADMAP, STORY-INDEX, BLUEPRINT Section 3/4 as scope warrants

Out of scope:

- Any implementation under `engine/`, `experience/`, or `neuron/` (plan-only; the feature/build action implements)
- `feature-assembly-plan.md` — per `agents/actions/plan.md` Deliverables Contract this is NOT a plan deliverable; it is owned by the feature action at Step 0
- Backend changes to the schema-bundle registry, `LobSchemaBundle`, `lob-schema-bundle.schema.json`, or the published `cyber/1.0.0` bundle (consumed as-is; backend validation remains authoritative)
- Changes to F0035 session-continuity behavior (F0036 only consumes its registry/restore API)
- Modifying `solution-ontology.yaml` unless the ontology vocabulary itself must change (architect-only)
- Editing `canonical-nodes.yaml` outside the Architect phase
- Creating any feature completion run package under `planning-mds/operations/evidence/runs/{RUN_ID}/` (feature-action territory)
- Generating any `g0-*`, `test-*`, `code-review-*` role reports (feature-action territory)

## Lifecycle Stage

Plan action stages (operator prompt labels, mapped to `agents/actions/plan.md` Gate Contract):

- A0 PM REQUIREMENTS DRAFT  (≅ Step 1 PM Phase A + `G1 CLARIFICATION`)
- G2 TRACKER SYNC (A)       (≅ `G2 TRACKER SYNC (A)`)
- A1 PM APPROVAL GATE       (≅ `G3 PHASE A APPROVAL`)
- B0 ARCHITECT ARCHITECTURE (≅ Step 3 Architect Phase B)
- B1 ONTOLOGY SYNC GATE     (≅ `G4 ONTOLOGY SYNC (B)`)
- B2 ARCHITECT APPROVAL GATE(≅ `G5 PHASE B APPROVAL`)
- Exit validation

This is **plan**, not **feature**: per `plan.md` Exit Validation, `validate-feature-evidence.py` is NOT invoked at plan close. The first stage validation call (`--stage G0`) happens later during the feature action.
