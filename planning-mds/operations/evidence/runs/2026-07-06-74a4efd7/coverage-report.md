# Coverage Report

## Verdict

PASS

## Coverage Scope

- Backend integration/unit coverage for search/reporting, broker insights, Casbin, distribution scope, and territory access.
- Frontend build and focused component tests for sidebar, distribution rollup report view, and broker insights workspace.
- Browser E2E coverage for Operational Reports discovery, active rollup tab, filters, scoped empty state, no-leak API behavior, and cross-surface F0037 query propagation.

## Evidence

- Backend: 187 filtered tests passed.
- Frontend: production build passed and 5 focused Vitest tests passed.
- E2E: 4 Playwright tests passed.
- Backend coverage artifact: `engine/tests/Nebula.Tests/TestResults/2d17e331-4e86-4e74-b75d-0e0a8e4f54a5/coverage.cobertura.xml`.

## Waivers

- Seeded local data did not include visible rollup rows for a drilldown screenshot in this run; the E2E captures and validates the safe default empty state and conditionally exercises drilldown when rows are present.
