# Action Context — F0027 Plan Run 2026-07-02-e8a31f35

## Inputs

| Field | Value |
|-------|-------|
| `FEATURE_ID` | `F0027` |
| `FEATURE_SLUG` | `coi-acord-and-outbound-document-generation` |
| `PHASE` | `A+B` |
| `FEATURE_MODE` | `existing` |
| `PLAN_RUN_ID` | `2026-07-02-e8a31f35` |
| `PRODUCT_ROOT` | `/Users/wallstreet288/Nebula_pr/nebula-insurance-crm` |
| `FEATURE_PATH` | `/Users/wallstreet288/Nebula_pr/nebula-insurance-crm/planning-mds/features/F0027-coi-acord-and-outbound-document-generation` |
| `PLAN_RUN_FOLDER` | `/Users/wallstreet288/Nebula_pr/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-02-e8a31f35` |

## Harness Contract

- Source prompt: `nandini-nebula-agents/agents/templates/prompts/evidence-contract/plan-operator-friendly.md`
- Action contract: `nandini-nebula-agents/agents/actions/plan.md`
- Required phase order: Product Manager Phase A, then Architect Phase B.
- Required gates: G1 clarification, G2 tracker sync, G3 Phase A approval, G4 ontology sync, G5 Phase B approval.
- Feature evidence package creation is out of scope for this plan run.

## Initial Readiness

- `scripts/kg/validate.py --check-drift` passed before this formal run.
- `scripts/kg/validate.py` failed before this formal run because `coverage-report.yaml` was stale.
- First formal readiness action is to refresh the KG coverage report, then rerun KG validation and drift validation.
