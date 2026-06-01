# Action Context

## Run Identity

- feature_id: F0035
- feature_slug: session-continuity-and-token-refresh
- run_id: 2026-05-24-c92b16b6
- mode: clean
- run_date: 2026-05-24

## Inputs

- FEATURE_ID=F0035
- MODE=clean
- SLICE_ORDER_SOURCE=assembly-plan
- PRODUCT_ROOT=/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm
- PRIMARY_SPEC=planning-mds/features/F0035-session-continuity-and-token-refresh/feature-assembly-plan.md

## Assumptions

- The feature action runs from the architect-authored assembly plan created for F0035.
- Drift discovered after G0 blocks final approval until reconciled in this run.

## Scope Boundaries

The run is limited to F0035 session continuity and token refresh evidence, implementation, validation, and tracker closeout. Cross-feature behavior may be referenced where F0035 depends on F0005, F0009, or F0033, but ownership and status changes stay within F0035 unless a required contract repair is discovered.

## Lifecycle Stage

G0 assembly plan validation.
