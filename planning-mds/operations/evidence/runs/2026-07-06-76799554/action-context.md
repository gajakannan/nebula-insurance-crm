# Action Context - F0037 Feature Run 2026-07-06-76799554

## Run Identity

- Action: `feature`
- Feature: `F0037`
- Feature slug: `hierarchy-aware-access-scoping-and-distribution-rollups`
- Run ID: `2026-07-06-76799554`
- Product root: `/Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm`
- Harness root: `/Users/srinivasubezawada/Desktop/nebula3/nebula-agents`
- Plan run approved for feature action: `2026-07-06-6e3851ab`

## Inputs

- `FEATURE_ID=F0037`
- `MODE=clean`
- `SLICE_ORDER_SOURCE=assembly-plan`
- `PRODUCT_ROOT=/Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm`
- `RUN_ID=2026-07-06-76799554`

## Assumptions

- G5 approval from the plan action is recorded before implementation begins.
- F0037 remains a full feature covering access scoping and distribution rollups.
- Existing F0017 hierarchy data and F0023/F0008 projection/reporting substrate remain the implementation foundation.
- DevOps signoff is conditional and not required unless deployment configuration, runtime topology, migrations, or materialized jobs are introduced.

## Scope Boundaries

- In scope: F0037 backend, frontend, tests, security evidence, role signoff, KG reconciliation, and PM closeout.
- Out of scope: replacing F0023 reporting/search infrastructure, building external broker portal behavior, commission economics, and new reporting infrastructure not approved by Phase B.

## Lifecycle Stage

G0 - Architect assembly plan validation.
