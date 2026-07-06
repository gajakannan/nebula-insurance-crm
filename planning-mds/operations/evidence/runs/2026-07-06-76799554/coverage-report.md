# Coverage Report - F0037

Result: PASS

## Coverage Scope

Focused G2 coverage exercises the F0037 runtime risk areas:

- Authoritative scope resolution, sibling exclusion, requested-scope fail-closed behavior, and external-role denial.
- Predicate-first projection visibility for operational reporting, search, and broker insights.
- Distribution rollup totals, grouping, and drilldown links.
- Casbin policy parity for `distribution_rollup:read`.
- Frontend rollup rendering and no-leak empty state.

## Test Artifacts

- Backend coverage artifact: `engine/tests/Nebula.Tests/TestResults/f4f5596e-4666-4827-b5fc-a78c89ba68c5/coverage.cobertura.xml`.
- Frontend component test output was console-based; no coverage artifact was generated for the focused Vitest run.

## Gaps

- Full repository coverage was not requested or run at G2.
- Authenticated end-to-end browser coverage is deferred until the code/security review gates approve this implementation for operator testing.
