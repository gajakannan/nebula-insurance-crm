# F0012 — Test Plan

## Scope

Validates dashboard storytelling infographic canvas as a full vertical slice:
- Flat infographic canvas with no panel borders, card wrappers, or divider lines
- Nudge bar integrated as top canvas section flowing into story controls
- Unified KPI + connected opportunities flow with terminal outcome branches
- Chapter-based in-canvas analytics overlays (Flow/Friction/Outcomes/Aging/Mix)
- Activity and My Tasks as flat canvas sections below story content
- Collapsible left nav and right Neuron rail with adaptive canvas width
- Responsive, accessibility, and performance parity

## Test Types

1. Backend contract tests
2. Frontend component/integration tests
3. End-to-end workflow tests
4. Accessibility verification tests
5. Visual regression checks (theme + breakpoint + rail-state + no-panel-border validation)

## Happy Path E2E Scenarios

1. Internal user opens Dashboard and sees continuous flat infographic canvas with nudge bar flowing into story controls, embedded KPI band, and connected opportunity flow — no panel borders visible.
2. User dismisses a nudge item; nudge zone collapses gracefully without breaking canvas flow.
3. User switches period and all canvas data (KPI, stage flow, terminal outcomes) updates in sync.
4. User switches chapters (`Flow`, `Friction`, `Outcomes`, `Aging`, `Mix`) without leaving the canvas context.
5. User collapses and expands left nav and right Neuron rail; canvas width adapts smoothly.
6. User scrolls to Activity and My Tasks sections below story content; sections render as flat canvas zones with no borders.
7. User opens a follow-up action from My Tasks section and navigates to the linked workflow item.

## Error/Edge Scenarios

1. No nudge items — nudge zone collapses; KPI band and flow remain intact.
2. No opportunities data in selected period — canvas shows empty narrative state.
3. One overlay dataset fails while base flow still loads — fallback text shown for affected chapter.
4. Unauthorized role attempts infographic canvas access — denied per ABAC policy.
5. Minimum-width layout with both rails expanded and long labels — compact mode activates.
6. Keyboard-only navigation across nudge items, chapter controls, stage/outcome nodes, and canvas sections.
7. Rapid chapter switching — latest selection wins with no stale data flash.

## Coverage Mapping (Story -> Tests)

| Story | Primary Test Coverage |
|-------|------------------------|
| F0012-S0001 | Flat canvas shell + nudge bar integration + embedded KPI + connected flow + terminal outcomes + period synchronization + no-panel-border validation |
| F0012-S0002 | Chapter controls + in-canvas overlay behavior + chapter-to-data mapping verification + no mode-switch context loss |
| F0012-S0003 | Activity/tasks flat canvas sections + spacing/typography differentiation + action handoff behavior |
| F0012-S0004 | Left/right rail collapse-expand behavior + adaptive canvas width across all rail-state combinations |
| F0012-S0005 | Breakpoint parity + keyboard/screen reader flows + performance budget checks + no-panel-border validation across devices |

## Evidence Requirements

- Backend build/test command logs
- Frontend lint/build/test command logs
- E2E run report artifacts
- Accessibility check output (axe-core or equivalent)
- MacBook/iPad/iPhone visual snapshots with rail state variants
- Visual regression check confirming zero panel borders, card wrappers, or divider lines in rendered output

## Runtime Preflight (Mandatory Before Test Commands)

1. Verify required runtime services/containers are up and healthy before running test commands.
2. Record the preflight command and result in execution evidence.
3. If a test command fails with runtime symptoms (service unavailable, DNS/network errors, missing container, browser dependency not present):
   - Stop code edits.
   - Classify the failure as environment/runtime-blocked.
   - Restore runtime prerequisites.
   - Re-run the same test command unchanged before making any code change.

## Implemented Automation (2026-03-14)

| Step | Requirement | Artifact | Status |
|------|-------------|----------|--------|
| 20 | Story-to-test mapping across narrative + operational layers | This document + coverage mapping table | Complete |
| 21 | Flat canvas render E2E (nudge, KPI band, connected flow, terminal outcomes, no panel wrappers) | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`flat canvas renders...`) | PASS (containerized Playwright run) |
| 22 | Period switch synchronization across KPI + flow + outcomes | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`period switching...`) | PASS (containerized Playwright run) |
| 23 | Chapter switching, lazy-load behavior, overlay fallback | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`chapter overlays...`) | PASS (containerized Playwright run) |
| 24 | Rail collapse combinations (4 states), adaptive width | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`canvas width adapts...`) | PASS (containerized Playwright run) |
| 25 | Activity/My Tasks stacked below canvas + handoff link | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`activity and tasks...`) | PASS (containerized Playwright run) |
| 26 | Keyboard/screen-reader + reduced-motion validation | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`keyboard and screen-reader...`) | PASS (containerized Playwright run) |
| 27 | Visual regression (MacBook/iPad/iPhone, no-panel-border snapshots) | `experience/tests/visual/f0012-dashboard-canvas.spec.ts` (`responsive breakpoints...`) | PASS (containerized Playwright run) |

## Execution Log (2026-03-14)

- Runtime preflight:
  - `docker compose ps`: application runtime containers healthy (`nebula-db`, `nebula-authentik-*`, `nebula-temporal*` up).
- `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~DashboardRepositoryKpiTests|FullyQualifiedName~OpportunityFlowNodeEmphasisCalculatorTests|FullyQualifiedName~DashboardEndpointTests|FullyQualifiedName~DashboardReadAuthorizationTests|FullyQualifiedName~DashboardPartialFailureTests"`: PASS (`31 passed, 0 failed`).
- `pnpm --dir experience lint`: pass.
- `pnpm --dir experience lint:theme`: pass.
- `pnpm --dir experience build`: pass.
- `pnpm --dir experience test`: fails in pre-existing auth suites (`LoginPage`, `ProtectedRoute`, `useSessionTeardown`), not in F0012 dashboard-story scope.
- `pnpm --dir experience exec vitest run src/features/opportunities/tests/OpportunitiesSummary.test.tsx`: pass (5 passed, 0 failed).
- `docker run ... mcr.microsoft.com/playwright:v1.58.2-noble ... VITE_AUTH_MODE=dev pnpm --dir experience exec playwright test tests/visual/f0012-dashboard-canvas.spec.ts`: PASS (`7 passed, 0 failed`) with visual snapshot artifacts for MacBook/iPad/iPhone breakpoints.
- Process correction captured: runtime/environment preflight is now mandatory in `agents/actions/feature.md` to prevent code churn when failures are caused by runtime/container outages.

## Residual Quality Notes

- Host Playwright runtime remains dependency-blocked (`libnspr4`/`libnss3`), but F0012 visual/E2E validation is fully covered by containerized Playwright execution evidence.
- Full frontend unit suite still has pre-existing auth failures outside F0012 scope; feature-targeted dashboard tests are passing.
