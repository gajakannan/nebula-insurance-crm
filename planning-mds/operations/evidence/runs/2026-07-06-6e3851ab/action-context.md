# Action Context — F0037 Plan Run

## Inputs

- `FEATURE_ID=F0037`
- `PHASE=A+B`
- `FEATURE_MODE=existing`
- `PLAN_RUN_ID=2026-07-06-6e3851ab`
- `PRODUCT_ROOT=/Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm`

## Resolved Paths

- `FEATURE_SLUG=hierarchy-aware-access-scoping-and-distribution-rollups`
- `FEATURE_PATH=planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups`
- `PLAN_RUN_FOLDER=planning-mds/operations/evidence/runs/2026-07-06-6e3851ab`

## Harness Constraints

- Run the nebula-agents `plan` action before any `feature` build action.
- Do not create a feature evidence package during plan.
- Do not call `validate-feature-evidence.py` during plan.
- Do not proceed past G3 or G5 without explicit user approval.
- Product Manager owns Phase A artifacts and tracker sync.
- Architect owns Phase B architecture, API/schema/security deltas, and ontology sync.

