# G2 Self Review - F0025 run 2026-07-07-9859bad4

## Scope Review

Implemented the F0025 commission revenue vertical slice inside the approved `CommissionRevenue` boundary: expected commission records, schedules, producer split assignments, adjustment requests/decisions, revenue rollups, API endpoints, frontend workspace/detail routes, route navigation, and knowledge-graph bindings.

Out of scope remains unchanged: no ledger, payment, payout, statement, tax, invoice, or accounting reconciliation behavior was added.

## Acceptance Criteria Review

- F0025-S0001: `/commissions` searches expected commission records with status and exception filters, protected by `commission:read`.
- F0025-S0002: detail page and API support creating commission schedules with basis/rate/effective-date/source-note validation.
- F0025-S0003: detail page and API support producer split assignment with 100 percent participant validation.
- F0025-S0004: detail page and API support recalculation and exception state visibility.
- F0025-S0005: detail page and API support adjustment request plus approve/reject decision flow with same-user approval denial in service logic.
- F0025-S0006: `/commissions` displays revenue attribution rollups from read-side projections refreshed on calculation and approved adjustments.

## Implementation Risks

- EF migration was added manually after `dotnet ef migrations add` hung during build; API/application builds pass, but DevOps should run migration apply verification in G6.
- Revenue attribution projection currently uses the primary split participant because the current projection table is one row per expected commission. Multi-participant attribution can be expanded later with a migration if product requires producer-level rows per participant.
- Broader full frontend unit suite currently has unrelated localStorage environment failures; F0025 targeted tests pass.
- Source-record visibility is enforced at the commission Casbin resource/action level in this slice. Fine-grained policy/carrier/producer/territory ABAC should be reviewed in G3 security review before closeout.

## Validation Evidence

- Backend build: `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false` passed.
- Backend focused tests: `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false --filter "FullyQualifiedName~CommissionRevenue|FullyQualifiedName~CasbinAuthorizationServiceTests.CommissionPolicy"` passed, 22 tests.
- Frontend build: `pnpm --dir experience build` passed.
- Frontend lint: `pnpm --dir experience lint` passed with pre-existing warnings outside F0025.
- Frontend theme guard: `pnpm --dir experience lint:theme` passed.
- Frontend targeted tests: `pnpm --dir experience exec vitest run src/features/commissions/tests/CommissionsPage.test.tsx` passed, 2 tests.
- Knowledge graph validation: `.venv/bin/python scripts/kg/validate.py --write-coverage-report` passed.
