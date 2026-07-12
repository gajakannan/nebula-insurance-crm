# G2 Self Review - F0037 Hierarchy-Aware Access Scoping and Distribution Rollups

Result: PASS

## Scope Review

Implemented the approved F0037 feature-action slice using the existing F0023/F0008 projection substrate rather than adding a new reporting store.

- Backend distribution scope contracts, resolver service, EF-backed scope repository, and dependency injection are in place.
- Search, operational reporting, and broker insights now resolve F0037 scope and apply projection visibility before rows, counts, facets, drilldowns, or rollups materialize.
- New `GET /operational-reports/distribution-rollups` endpoint, query validator, DTO contract, and `distribution_rollup:read` policy gate are wired.
- Frontend operational reports now expose hierarchy, territory, producer, and as-of filters plus a distribution rollup tab with grouped metrics and no-leak empty states.
- Search results and saved-view reapplication carry F0037 hierarchy filters.
- Runtime policy and authorization matrix already include `distribution_rollup:read` parity.

## Acceptance Criteria Review

- F0037-S0001: Current user scope is resolved from role plus requested hierarchy, territory, producer, and as-of context; external roles fail closed.
- F0037-S0002: Projection repositories apply source visibility predicates before result materialization.
- F0037-S0003: Global search, saved views, broker insights, and operational reports consume the F0037 visibility shape.
- F0037-S0004: Distribution rollup reporting returns grouped rows, totals, as-of date, generated timestamp, and drilldown links.
- F0037-S0005: UI filters, rollup panel, drilldowns, and no-visible-row state are implemented.
- F0037-S0006: Casbin parity test, local secrets scan, authorization-focused SAST review, and no-leak aggregation tests are recorded.

## Implementation Risks

- Manager subtree membership currently depends on available hierarchy/territory/producer inputs and existing projection correlation keys; deeper authority-claim reconciliation should be reviewed at G3/G4.
- DAST was not run against a rebuilt live API container in this G2 pass.
- Full dependency vulnerability audit was waived because external registry access is restricted in this execution environment.

## Validation Evidence

- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -v:minimal` passed.
- `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -v:minimal` passed with existing nullable warnings.
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build --filter "...F0037..." -v:minimal` passed: 19 tests.
- Post-review predicate hardening was applied so requested hierarchy/territory/producer filters narrow owner/region/admin scope instead of granting new scope; focused backend tests still passed.
- `corepack pnpm --dir experience exec vitest run src/features/reports/components/__tests__/DistributionRollupReportView.test.tsx` passed: 2 tests.
- `corepack pnpm --dir experience build` passed with the existing Vite chunk-size advisory.
- Story validation passed for all six F0037 stories.
- KG drift validation passed with one pre-existing low-confidence warning unrelated to F0037.
