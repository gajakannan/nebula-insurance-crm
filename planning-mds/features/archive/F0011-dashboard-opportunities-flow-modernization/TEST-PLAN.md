# F0011 — Test Plan

## Scope

Validates the opportunities flow-first modernization vertical slice:
- Connected flow-default opportunities canvas
- Terminal outcomes rail with drilldowns
- Modern visual hierarchy and emphasis cues
- Secondary mini-view behavior
- Responsive and accessibility parity

## Test Types

1. Backend contract tests
2. Frontend component/integration tests
3. End-to-end tests
4. Accessibility verification tests
5. Visual regression checks (theme + breakpoint)

## Happy Path E2E Scenarios

1. Internal user opens dashboard and sees connected flow canvas as opportunities default.
2. User changes period and sees stage and outcome counts update in sync.
3. User opens drilldowns from a stage node and an outcome node.
4. User opens a secondary mini-view detail and returns to primary flow context.
5. User completes the same core action path on desktop, tablet, and phone layouts.

## Error/Edge Scenarios

1. No opportunities data in selected period.
2. One aggregate endpoint fails while others succeed.
3. Unauthorized role attempts opportunities access.
4. Narrow viewport causes potential overflow for flow/outcome components.
5. Keyboard-only navigation through period, stage, outcome, and drilldown controls.

## Coverage Mapping (Story -> Tests)

| Story | Primary Test Coverage |
|-------|------------------------|
| F0011-S0001 | Default connected flow rendering + period synchronization + stage drilldown |
| F0011-S0002 | Outcomes rail metrics + outcome drilldown + empty/error behavior |
| F0011-S0003 | Visual token mapping + emphasis state rendering + contrast checks |
| F0011-S0004 | Mini-view summary and expanded detail behavior |
| F0011-S0005 | Breakpoint parity + keyboard/screen reader flows + non-blocking failures |

## Evidence Requirements

- Backend build/test command logs
- Frontend lint/build/test command logs
- E2E run report artifacts
- Accessibility check output
- MacBook/iPad/iPhone visual snapshots
