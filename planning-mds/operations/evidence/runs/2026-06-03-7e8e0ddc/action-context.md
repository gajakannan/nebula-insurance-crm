# Action Context

## Run Identity

- Feature: F0019
- Feature slug: submission-quoting-proposal-and-approval
- Run ID: 2026-06-03-7e8e0ddc
- Mode: clean
- Product root: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- Feature path: `planning-mds/features/F0019-submission-quoting-proposal-and-approval`
- Prior run: null
- Contract effective date: 2026-05-19

## Inputs

- `FEATURE_ID=F0019`
- `MODE=clean`
- `SLICE_ORDER_SOURCE=assembly-plan`
- `PRIMARY_SPEC=planning-mds/features/F0019-submission-quoting-proposal-and-approval/feature-assembly-plan.md`
- Tier defaults: clean start tier 1, max auto tier 2

## Assumptions

- Phase A and Phase B planning are approved per `STATUS.md` and ADR-025.
- Dependencies F0006, F0018, F0020, F0034, and F0036 are completed and archived.
- F0019 remains a CRM workflow feature and must not introduce rating, pricing, scoring, or carrier rating integration.

## Scope Boundaries

- In scope: submission downstream states, packet, approval, bind handoff, terminal outcomes, archive/reactivate, downstream list and timeline.
- Out of scope: underwriting workbench, document rendering/template engine, physical purge, billing/issuance, broker self-service quoting, AI/neuron changes.

## Lifecycle Stage

G0 draft: Architect assembly plan authoring and validation.
