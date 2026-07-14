# F0025 — Commission, Producer Splits & Revenue Tracking — Getting Started

## Prerequisites

- [x] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [x] Review hierarchy, policy, and market relationship dependencies first
- [x] Refine this feature into Phase A stories before coding
- [x] Complete Phase B architecture draft before any feature/build action
- [x] Receive G5 Phase B approval before any feature/build action

## Dependency Context

- F0017 supplies effective-dated producer ownership and hierarchy context.
- F0018 supplies policy lifecycle and policy premium context.
- F0028 supplies carrier and market relationship context.

## How to Verify Phase A

1. Confirm F0025 remains operational revenue visibility and attribution, not full accounting.
2. Confirm each story has acceptance criteria, role visibility, data requirements, and out-of-scope boundaries.
3. Confirm mutation stories include entry point, editable state, save result, persistence evidence, role constraints, validation behavior, and audit expectation.
4. Run story and tracker validation from the `nebula-agents` harness.

## Phase B Handoff Notes

- ADR-032 defines the `CommissionRevenue` module boundary, persisted expected commission review records, effective-dated schedules, effective-dated producer split assignments, single-step adjustment approval, source-authorized rollups, and timeline audit requirements.
- Implementation must keep expected commission as CRM review/attribution data only; no ledger, payment, invoicing, reconciliation, statement, tax, or payout behavior.
- G5 approval was recorded on 2026-07-07 in plan run `2026-07-07-8a9b2629`.
- Feature action may start next only through the `nebula-agents` `feature` harness entrypoint.

## Feature Action Notes

- Active feature run: `2026-07-07-9859bad4`.
- G0 feature assembly plan: [feature-assembly-plan.md](./feature-assembly-plan.md).
- G1 runtime preflight required a local ignored `.env` restored from `.env.example`; do not commit `.env`.
- Backend entry points added:
  - `engine/src/Nebula.Api/Endpoints/CommissionEndpoints.cs`
  - `engine/src/Nebula.Application/Services/CommissionRevenueService.cs`
  - `engine/src/Nebula.Application/DTOs/CommissionDtos.cs`
  - `engine/src/Nebula.Infrastructure/Repositories/CommissionRepository.cs`
  - `engine/src/Nebula.Infrastructure/Repositories/RevenueAttributionRepository.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Configurations/CommissionConfiguration.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260707150000_F0025_CommissionRevenue.cs`
- Backend verification used single-node/no-shared-compilation dotnet flags to avoid local build-server stalls.
