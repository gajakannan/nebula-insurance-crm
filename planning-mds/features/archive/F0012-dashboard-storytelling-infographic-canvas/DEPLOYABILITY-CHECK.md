# F0012 — Deployability Check Summary

## Objective

Validate that dashboard storytelling refactor can be shipped without breaking existing dashboard runtime behavior, collapsible rails, or role-scoped aggregate access.

## Runtime/Deployability Checklist

- [x] Backend story-canvas contracts versioned and documented
- [x] Frontend build consumes compatible contract fields
- [x] Rail collapse state handling does not break routing or persistent shell behavior
- [x] Feature flags/toggles (if used) documented (none introduced)
- [x] Env var requirements unchanged or documented
- [ ] Container startup smoke checks pass (blocked in this environment)
- [ ] Dashboard route smoke checks pass post-deploy (blocked in this environment)

## Evidence Paths

- Backend contract/tests:
  - `engine/src/Nebula.Api/Endpoints/DashboardEndpoints.cs`
  - `engine/src/Nebula.Infrastructure/Repositories/DashboardRepository.cs`
  - `engine/src/Nebula.Application/Services/OpportunityFlowNodeEmphasisCalculator.cs`
  - `engine/tests/Nebula.Tests/Unit/Dashboard/OpportunityFlowNodeEmphasisCalculatorTests.cs`
  - `engine/tests/Nebula.Tests/Unit/Dashboard/DashboardRepositoryKpiTests.cs`
  - `engine/tests/Nebula.Tests/Integration/DashboardReadAuthorizationTests.cs`
  - `engine/tests/Nebula.Tests/Integration/DashboardPartialFailureTests.cs`
- Frontend implementation/tests:
  - `experience/src/pages/DashboardPage.tsx`
  - `experience/src/features/opportunities/components/StoryCanvas.tsx`
  - `experience/tests/visual/f0012-dashboard-canvas.spec.ts`
  - `planning-mds/operations/evidence/frontend-ux/ux-audit-2026-03-14.md`
- Command evidence (this run):
  - `pnpm --dir experience lint` -> pass
  - `pnpm --dir experience lint:theme` -> pass
  - `pnpm --dir experience build` -> pass
  - `pnpm --dir experience test` -> fails in pre-existing auth suites (outside F0012)
  - `pnpm --dir experience exec vitest run src/features/opportunities/tests/OpportunitiesSummary.test.tsx` -> pass
  - `pnpm --dir experience exec playwright test tests/visual/f0012-dashboard-canvas.spec.ts` -> blocked (`libnspr4.so` missing)
  - `pnpm --dir experience test:visual:theme` -> blocked (`libnspr4.so` missing)

## Deployability Assessment

Status: **Conditional pass for code deployability; runtime visual smoke is environment-blocked**

## Notes

- No new runtime services, ports, or environment-variable contracts were introduced by F0012.
- No `docker-compose*.yml`, Dockerfile, or runtime contract changes were required.
- Playwright browser runtime cannot launch in this host because required shared libraries (`libnspr4`, `libnss3`) are unavailable and `playwright install --with-deps` needs privileged `sudo`.
