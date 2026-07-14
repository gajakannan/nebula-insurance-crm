# Action Context - F0025 run 2026-07-07-9859bad4

## Run Identity

- Action: `feature`
- Feature: `F0025`
- Feature slug: `commission-producer-splits-and-revenue-tracking`
- Run ID: `2026-07-07-9859bad4`
- Product root: `/Users/msig4/Documents/NEBULA/nebula-insurance-crm`
- Framework root: `/Users/msig4/Documents/NEBULA/nebula-agents`
- Harness contract: `agents/actions/feature.md`
- Current gate: `G0 ARCHITECT ASSEMBLY PLAN VALIDATION`

## Inputs

- Operator approved proceeding with the feature action and required strict `nebula-agents` harness use.
- Plan run `2026-07-07-8a9b2629` completed Phase A+B and recorded G5 approval.
- ADR-032, F0025 stories S0001-S0006, API contracts, JSON Schemas, data model, authorization matrix, policy rows, and KG mappings are the governing inputs.

## Assumptions

- F0025 remains internal-only for the first release.
- AI Engineer is not required unless later implementation touches `neuron/`, LLM, MCP, prompts, or model/tool orchestration.
- Security Reviewer and DevOps are required because F0025 handles internal economic data and introduces persisted entities/migration/deployability concerns.

## Scope Boundaries

- In scope: commission search/detail, effective-dated schedules, effective-dated producer splits, expected commission review/calculation, adjustment request/decision, and revenue attribution rollups.
- Out of scope: accounting ledger, billing, payments, reconciliation, tax, statements, GL export, producer payout execution, and external compensation portals.
- No implementation begins until G0 validation passes and G1 runtime preflight is recorded.

## Lifecycle Stage

G0 in progress: Architect assembly plan and feature evidence package initialization.
