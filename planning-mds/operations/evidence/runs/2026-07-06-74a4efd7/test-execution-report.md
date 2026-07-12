# Test Execution Report

## Verdict

PASS

## Scope

F0037 backend, frontend, and browser E2E validation for Operational Reports -> Distribution rollups, hierarchy-aware scope filters, no-leak behavior, and cross-surface filter propagation.

## Commands

- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~SearchReporting|FullyQualifiedName~BrokerInsights|FullyQualifiedName~Casbin|FullyQualifiedName~Distribution|FullyQualifiedName~Territory"`: PASS, 187 passed.
- `corepack pnpm --dir experience build`: PASS.
- `corepack pnpm -C experience exec vitest run src/components/layout/Sidebar.test.tsx src/features/reports/components/__tests__/DistributionRollupReportView.test.tsx src/features/broker-insights/tests/BrokerInsightsWorkspace.test.tsx`: PASS, 5 tests passed.
- `corepack pnpm -C experience exec playwright test f0037-distribution-rollups.spec.ts --config=playwright.f0037.config.ts --reporter=list`: PASS, 4 tests passed.

## Browser E2E Results

- Sidebar renders `Operational Reports` and opens `/operational-reports?report=rollups`.
- `Distribution rollups` tab is active and F0037 filters are visible.
- Group by and metric family controls update query state for Hierarchy, Territory, Producer, Production, Workflow, and Activity.
- Scoped-away root node state shows no visible rollup rows and does not leak hidden counts.
- Drilldown behavior is exercised when seeded rollup rows are available; otherwise the default empty state screenshot is captured.
- External user search behavior returns allowed no-leak outcomes only.
- Search and broker-insights cross-surface requests preserve F0037 scope filters.

## Artifacts

- planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/artifacts/test-results/f0037-playwright.txt
- planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/artifacts/test-results/f0037-playwright-results.json
- planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/artifacts/screenshots/f0037-sidebar-rollups.png
- planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/artifacts/screenshots/f0037-scoped-empty.png
- planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/artifacts/screenshots/f0037-rollups-default.png
