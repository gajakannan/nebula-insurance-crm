# F0010 — Test Plan

## Scope

Validates the Opportunities refactor vertical slice:
- Pipeline Board default
- Heatmap view
- Treemap view
- Sunburst view
- Drilldown consistency, responsive behavior, accessibility, and authorization boundaries

## Test Types

1. Backend contract tests
2. Frontend component/integration tests
3. End-to-end tests
4. Visual regression checks (theme + breakpoint)

## Happy Path E2E Scenarios

1. Internal user opens dashboard and sees Pipeline Board as default.
2. User changes period window and sees refreshed counts in all views.
3. User opens drilldown from each view and sees scoped mini-cards.
4. User switches between Submissions and Renewals with state preserved.

## Error/Edge Scenarios

1. No opportunities data for selected period.
2. Partial endpoint failure (insights endpoint fails but summary succeeds).
3. Unauthorized role attempts opportunities access.
4. iPhone viewport overflow and popover edge positioning.
5. Keyboard-only navigation through all view controls.

## Coverage Mapping (Story -> Tests)

| Story | Primary Test Coverage |
|-------|------------------------|
| F0010-S0001 | Dashboard default view + period controls + stage drilldown |
| F0010-S0002 | Heatmap render + bucket tooltip + empty state |
| F0010-S0003 | Treemap hierarchy and selection behavior |
| F0010-S0004 | Sunburst hierarchy and compact fallback behavior |
| F0010-S0005 | Breakpoint interaction parity + accessibility flows |

## Evidence Requirements

- Backend test output logs
- Frontend unit/component test output logs
- E2E run report artifacts
- Visual snapshots for MacBook/iPad/iPhone breakpoints
