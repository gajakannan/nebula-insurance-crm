# Action Context — F0036 run 2026-05-28-077b7b30

## Run Identity

- **Feature:** F0036 — Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)
- **Feature slug:** `dynamic-product-attribute-form-engine`
- **Run ID:** `2026-05-28-077b7b30` (generated once at session start via `secrets.token_hex(4)`; contract format `YYYY-MM-DD-[a-z0-9]{8}`)
- **Action:** `feature` (`agents/actions/feature.md`)
- **Run folder:** `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/`
- **rerun_of:** null (clean first run — no prior `latest-run.json`)
- **PRODUCT_ROOT:** `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`

## Inputs

- `FEATURE_ID=F0036`, `MODE=clean`, `SLICE_ORDER_SOURCE=assembly-plan`, tiers `start=1, max_auto=2`.
- Slice order read from the Primary Spec `feature-assembly-plan.md` (authored this run at G0 Step 0): S0001 → S0008.
- Plan signoff: Phase A (A1) + Phase B (B2) approved, plan run `2026-05-25-51ff2a92`. Operator directed (2026-05-28) to proceed to the feature action without re-running plan-review.
- KG first-pass scope: `lookup.py F0036 --tier 1` (frontend-only; affects dynamic-attribute-panel / dynamic-lob-attributes / validator-equivalence / session-context-restore; governed_by ADR-020/021/022/024; no new canonical nodes).

## Assumptions

- Frontend toolchain (node v24.14.0, pnpm 10.33.0) is the runtime for this feature; no docker containers required (operator-confirmed "frontend toolchain only").
- F0034 Cyber bundle (`planning-mds/lob-schemas/cyber/1.0.0/{data-schema,ui-schema,rules}.json`) and F0035 registry (`useSessionRestorableForm`, `consumeFormSnapshot`, `DirtyFormRegistration`) are consumed as-is; backend validation remains authoritative.
- `runtime_bearing`/`frontend_in_scope` are `false` at G0 because the G0 change set is planning-only; both flip per §7 path classes once `experience/**` code and test files enter `changed_paths` at G2 (the §7 `experience/**/*.test.*` glob forces `runtime_bearing`, satisfied by a frontend-toolchain preflight rather than docker).
- Security Reviewer is required by STATUS (risk-based: snapshot may transiently hold `InternalOnly` fields per ADR-024), independent of the `security_sensitive_scope` boolean.

## Scope Boundaries

- **In:** `experience/**` only — RHF+AJV+widget-registry engine, panel swap (5 screens), shared library-agnostic F0035 registration helper, `useControlledDirtyTracker`, wiring the ~11-component CRUD inventory (S0007).
- **Out:** `engine/**`, `neuron/**`, schema/bundle changes, F0035 behavior changes, CRUD field-state-library rewrite, heavy widgets, new LOBs, deployment/runtime/env changes. No widening beyond F0036.

## Lifecycle Stage

`feature` action, gate **G0 → (G1 … G4.7)**. This document is written at G0; the manifest is `draft` at G0 and flips to `in-progress` after G0 stage validation passes.
