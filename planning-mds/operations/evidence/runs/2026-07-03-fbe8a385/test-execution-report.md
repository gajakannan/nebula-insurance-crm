# Test Execution Report - F0008 Broker Insights

## Verdict

PASS_AFTER_REPAIR

## Scope

- Parent feature run: `2026-07-03-fd732693`
- Standalone test run: `2026-07-03-fbe8a385`
- Harness action: `test`
- Role: Quality Engineer

## Repair Loop

Runtime smoke initially failed on the authenticated scorecards endpoint with HTTP 500. Logs showed EF queried `BrokerInsightProjections`, but the table did not exist in a fresh database. The migration file existed, but lacked EF migration metadata normally supplied by the generated designer partial. Minimal repair applied:

- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260703185200_F0008BrokerInsights.cs`
- Added `[DbContext(typeof(AppDbContext))]`
- Added `[Migration("20260703185200_F0008BrokerInsights")]`

Images and affected tests were rebuilt/rerun after repair.

## Results

| Layer | Command | Result | Evidence |
| --- | --- | --- | --- |
| API image rebuild | `docker compose build api` | PASS | `commands.log` |
| Backend focused tests | `dotnet test --filter BrokerInsightServiceTests` in SDK-stage container | PASS: 3 passed, 0 failed, 0 skipped | `artifacts/test-results/f0008-postcloseout-backend-after-repair.trx` |
| Backend coverage | `dotnet test --collect "XPlat Code Coverage"` | PASS | `artifacts/test-results/7699d9ee-3196-487b-aced-1578a6fc406e/coverage.cobertura.xml` |
| Runtime health | isolated Docker API + fresh Postgres | PASS: HTTP 200 Healthy | `commands.log` |
| Migration table check | `to_regclass('"BrokerInsightProjections"')` | PASS | `artifacts/runtime/broker-insight-table-check.txt` |
| Scorecards no-auth | `/broker-insights/scorecards` | PASS: HTTP 401 | `artifacts/runtime/scorecards-noauth-after-repair.http` |
| Scorecards auth | `/broker-insights/scorecards` | PASS: HTTP 200 | `artifacts/runtime/scorecards-auth-after-repair.http` |
| Trends auth | nonexistent seeded broker id | PASS: HTTP 404, no 500 | `artifacts/runtime/trends-auth-after-repair.http` |
| Benchmarks auth | nonexistent seeded broker id | PASS: HTTP 404, no 500 | `artifacts/runtime/benchmarks-auth-after-repair.http` |
| Snapshot auth | nonexistent seeded broker id | PASS: HTTP 404, no 500 | `artifacts/runtime/snapshot-auth-after-repair.http` |
| Frontend component | `pnpm vitest run src/features/broker-insights/tests/BrokerInsightsWorkspace.test.tsx` | PASS: 2 passed, 0 failed, 0 skipped | `artifacts/test-results/f0008-postcloseout-frontend-vitest.json` |
| Frontend build | `pnpm build` | PASS with existing chunk-size warning | `artifacts/test-results/f0008-postcloseout-frontend-build.txt` |

## Notes

- Regular `docker compose up -d api` was blocked by drift in the user's persistent local `nebula-db` volume: EF migration history and existing tables were inconsistent around `SavedViews`. To avoid wiping developer data, runtime smoke used test-only Docker containers on `nebula-f0008-net`.
- The broker-specific endpoint smokes used a deliberately nonexistent broker id and are expected to return 404 in an empty/fresh seeded database. The gate assertion is that authenticated routes do not fail with migration/runtime errors.
- Frontend tests ran on the host Node toolchain because the repository has no frontend Dockerfile or Compose service.
