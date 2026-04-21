# Story to Suite Mapping

## F0015-S0001 — Frontend Tooling, Harness, and Commands

| Layer | Command / Suite | Evidence |
|-------|------------------|----------|
| Component / unit | `pnpm test` | `planning-mds/operations/evidence/f0015/component.log` |
| Integration harness | `pnpm test:integration` | `planning-mds/operations/evidence/f0015/integration.log` |
| Accessibility harness | `pnpm test:accessibility` | `planning-mds/operations/evidence/f0015/accessibility.log` |
| Coverage emission | `pnpm test:coverage` | `planning-mds/operations/evidence/f0015/coverage.log`, `experience/coverage/lcov.info`, `experience/coverage/coverage-summary.json` |
| Shared support | `experience/src/mocks/*`, `experience/src/test-utils/render-app.tsx`, `experience/src/test-setup.ts` | implementation diff plus the logs above |

## F0015-S0002 — Solution Lifecycle + Evidence Enforcement

| Layer | Command / Suite | Evidence |
|-------|------------------|----------|
| Gate manifest validation | `python3 planning-mds/testing/validate-frontend-quality-gate.py planning-mds/operations/evidence/frontend-quality/latest-run.json` | `planning-mds/operations/evidence/f0015/lifecycle-gates.log` |
| Lifecycle stage enforcement | `python3 agents/scripts/run-lifecycle-gates.py` | `planning-mds/operations/evidence/f0015/lifecycle-gates.log` |
| Planning / tracker consistency | tracker validation commands | `planning-mds/operations/evidence/f0015/devops-2026-03-21.md` |

## F0015-S0003 — Critical-Slice Backfill + Full Validation Run

| Validation Layer | Critical Slice | Suites | Evidence |
|------------------|----------------|--------|----------|
| Component / unit | Auth bootstrap and guarded navigation | `src/features/auth/tests/useCurrentUser.test.tsx`, `src/features/auth/tests/ProtectedRoute.test.tsx`, `src/features/auth/tests/useSessionTeardown.test.tsx`, `src/features/auth/tests/LoginPage.test.tsx` | `planning-mds/operations/evidence/f0015/component.log` |
| Component / unit | Dashboard summary surface | `src/features/opportunities/tests/OpportunitiesSummary.test.tsx` | `planning-mds/operations/evidence/f0015/component.log` |
| Integration | Dashboard route data loading | `src/pages/tests/DashboardPage.integration.test.tsx` | `planning-mds/operations/evidence/f0015/integration.log` |
| Integration | Broker directory route data loading and filtering | `src/features/brokers/tests/BrokerListPage.integration.test.tsx` | `planning-mds/operations/evidence/f0015/integration.log` |
| Accessibility | Login route | `src/features/auth/tests/LoginPage.a11y.test.tsx` | `planning-mds/operations/evidence/f0015/accessibility.log` |
| Accessibility | Broker directory route | `src/features/brokers/tests/BrokerListPage.a11y.test.tsx` | `planning-mds/operations/evidence/f0015/accessibility.log` |
| Coverage | Full non-visual suite | `pnpm test:coverage` | `planning-mds/operations/evidence/f0015/coverage.log`, `experience/coverage/lcov.info`, `experience/coverage/coverage-summary.json`, `experience/coverage/index.html` |
| Visual support | Theme smoke on dashboard and brokers | `tests/visual/theme-smoke.spec.ts` | `planning-mds/operations/evidence/f0015/visual.log`, `planning-mds/operations/evidence/f0015/artifacts/playwright-report/index.html`, `planning-mds/operations/evidence/f0015/artifacts/visual-test-results/.last-run.json` |
