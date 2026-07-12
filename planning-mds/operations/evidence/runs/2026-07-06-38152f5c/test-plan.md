# Test Plan

## Scope

Verify the sidebar exposes the F0037 Operational Reports rollups UI without changing existing route or API behavior.

## Scenarios

- Sidebar renders `Operational Reports`.
- Sidebar link targets `/operational-reports?report=rollups`.
- The route activates the `Distribution rollups` tab.
- Existing F0037 filters remain available.

## Commands

- `corepack pnpm --dir experience build`
- `corepack pnpm -C experience exec vitest run src/components/layout/Sidebar.test.tsx src/features/reports/components/__tests__/DistributionRollupReportView.test.tsx`

## Acceptance

All planned checks passed.
