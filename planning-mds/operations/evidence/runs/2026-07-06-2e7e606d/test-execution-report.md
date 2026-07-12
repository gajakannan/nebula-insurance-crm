# Test Execution Report

## Verdict

PASS

## Scope

F0037 PRD-alignment validation for Operational Reports -> Distribution rollups.

## Feature-Level Frontend Notes

This frontend rerun changes the visible filter surface for the F0037 Distribution rollups tab only. The PRD-aligned screen now shows `As of`, `Root node`, `Territory`, `Producer`, `Group by`, and `Metric family`, and the E2E test confirms `Region`, `Line of business`, and `Workflow type` are absent on the rollups tab.

## Commands

- `corepack pnpm --dir experience build`: PASS.
- `corepack pnpm -C experience exec vitest run src/features/reports/components/__tests__/DistributionRollupReportView.test.tsx`: PASS, 2 tests passed.
- `corepack pnpm -C experience exec playwright test f0037-distribution-rollups.spec.ts --config=playwright.f0037.config.ts`: PASS, 4 tests passed.

## Browser E2E Results

- Sidebar renders `Operational Reports` and opens `/operational-reports?report=rollups`.
- `Distribution rollups` tab is active.
- PRD-required filters are visible: `As of`, `Root node`, `Territory`, `Producer`, `Group by`, `Metric family`.
- Generic non-rollup filters are not present on the rollups tab: `Region`, `Line of business`, `Workflow type`.
- Grouping, metric family, scoped empty state, no-leak API behavior, search, and broker-insights scope checks still pass.

## Artifacts

- planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/artifacts/test-results/f0037-playwright-results.json
- planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/artifacts/screenshots/f0037-sidebar-rollups.png
- planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/artifacts/screenshots/f0037-scoped-empty.png
- planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/artifacts/screenshots/f0037-rollups-default.png
