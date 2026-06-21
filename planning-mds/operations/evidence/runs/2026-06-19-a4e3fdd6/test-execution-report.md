# Test Execution Report — F0023 run 2026-06-19-a4e3fdd6

**Role:** Quality Engineer · **Date:** 2026-06-20 · **Runtime:** host .NET 10 SDK (== Dockerfile `sdk:10.0`) + container build; pnpm 11.7.0

## Commands Executed

| Command | Cwd | Result |
|---------|-----|--------|
| `dotnet build Nebula.slnx -c Debug` | engine | 0 errors, 6 pre-existing warnings |
| `dotnet test Nebula.slnx --filter FullyQualifiedName~SearchReporting` | engine | Passed! 17/17 |
| `dotnet test … --collect:"XPlat Code Coverage"` | engine | coverage cobertura captured |
| `dotnet list package --vulnerable --include-transitive` | engine | 0 vulnerable packages |
| `docker compose build api` / `up -d api` | repo | image built; container healthy |
| `curl /healthz` ; `curl /search-results /saved-views /operational-reports/*` | repo | 200 ; 401 (auth-enforced); 404 control |
| `pnpm build` ; `pnpm lint` ; `pnpm lint:theme` | experience | tsc+vite OK; 0 lint errors; theme guard passed |
| `pnpm exec vitest run src/features/search src/features/reports` | experience | 5/5 passed |
| `pnpm audit --prod` | experience | 9 findings, all pre-existing platform deps |

## Pass/Fail Counts

| Suite | Passed | Failed | Notes |
|-------|--------|--------|-------|
| Backend unit (SearchReporting) | 17 | 0 | SavedViewService 11, SearchService 3, OperationalReportService 3 |
| Frontend component (F0023) | 5 | 0 | SearchResultsList 3, SearchOverlay 2 |
| Frontend full suite | 245 | 1 | the 1 failure is pre-existing `sessionTelemetry.test.ts` (fails in isolation; untouched by F0023) |
| Runtime smoke | 6 | 0 | healthz + 4 routes + control |

## Skipped Tests And Rationale

None skipped. Integration (Testcontainers) + E2E (Playwright) layers not yet authored — deferred follow-ups, not skips.

## Raw Test Artifact Paths

- artifacts/test-results/backend-unit-tests.txt
- artifacts/test-results/frontend-tests.txt
- artifacts/coverage/searchreporting.cobertura.xml
- artifacts/security/dependency-dotnet.txt
- artifacts/security/dependency-pnpm.txt

## Failed / Retried Command History

- Migration first scaffolded a polluted diff (pre-existing stale EF snapshot); resolved by snapshot regen + F0023-only migration (documented in gate-decisions / kg-reconciliation).
- `NewReport(taskId:)` compile error → removed (task id is SourceObjectId). Rebuilt green.

## AC Coverage Result

All S0001–S0007 acceptance criteria have automated unit/component and/or runtime-smoke coverage (see test-plan Story-to-AC mapping). Real-SQL and end-to-end browser coverage deferred.

## Result

`PASS` — all executed automated suites green for F0023; the single full-suite failure is pre-existing and unrelated.
