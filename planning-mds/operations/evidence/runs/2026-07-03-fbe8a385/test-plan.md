# Test Plan - F0008 Post-Closeout Runtime Verification

Result: PASS

## Story-to-Test Mapping

| Story | Acceptance Criteria | Test Type | Test Case |
|-------|---------------------|-----------|-----------|
| F0008-S0001 | Scorecard shows broker metrics, denominator, period, refresh context | Backend unit, frontend component, API smoke | Service scorecard grouping test; workspace render test; `/broker-insights/scorecards` authenticated smoke |
| F0008-S0002 | Trend drilldown preserves broker/metric/window and authorized source state | Backend unit, frontend component, API smoke | Trend bucket/partial test; trend panel render; `/broker-insights/{id}/trends` smoke |
| F0008-S0003 | Benchmark suppresses rank/percentile below visible peer threshold | Backend unit, frontend component, API smoke | Benchmark peer suppression test; benchmark panel render; `/broker-insights/{id}/benchmarks` smoke |
| F0008-S0004 | Review snapshot renders highlights, risks, summaries, source links | Frontend component, API smoke | Workspace snapshot render; `/broker-insights/{id}/snapshot` smoke |
| F0008-S0005 | Aggregates are permission-safe and require `broker_insight/read` | Backend unit, API smoke | Visibility filter service test; unauthenticated/authorized API smoke |

## Test Types in Scope

- [x] Unit tests: F0008 service aggregation and visibility behavior.
- [x] Component tests: F0008 workspace rendering and filter behavior.
- [x] API tests: rebuilt runtime health and broker insight endpoint smoke.
- [x] Integration/runtime tests: Docker API rebuild/restart and migration startup.
- [ ] E2E browser tests: not executed unless a frontend runtime is started separately.
- [ ] Performance tests: not executed; no F0008-specific NFR threshold beyond bounded query expectation.
- [ ] Accessibility tests: not executed in this run; no browser runtime container exists.

## Coverage Targets

- Backend F0008 service tests: all existing F0008 unit tests pass.
- Frontend F0008 component tests: all existing F0008 Vitest tests pass.
- Runtime API smoke: health and endpoint auth paths return expected status classes.
- Coverage target: artifact-backed coverage where tooling emits it; otherwise layer waiver with owner/date/follow-up.

## Evidence Artifacts

- Backend test result: `artifacts/test-results/f0008-postcloseout-backend.trx`
- Backend coverage: `artifacts/coverage/f0008-postcloseout-backend-coverage.cobertura.xml`
- Frontend test output: `artifacts/test-results/f0008-postcloseout-frontend-vitest.txt`
- Frontend build output: `artifacts/test-results/f0008-postcloseout-frontend-build.txt`
- Runtime output: `artifacts/runtime/`

## Test Infrastructure

- Docker Compose API runtime is required for runtime smoke.
- Dev JWT is generated using the same issuer/subject/roles pattern as `experience/src/services/dev-auth.ts`.
- Frontend tests use host Node because the product has no frontend Dockerfile or Compose service.
- No production code edits are planned for this test run.
