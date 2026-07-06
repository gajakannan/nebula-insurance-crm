# Test Execution Report — F0028

## Verdict

PASS WITH RECOMMENDATIONS

## Executed Commands

| Area | Command | Result |
|------|---------|--------|
| Backend API build | `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -m:1 /p:UseSharedCompilation=false` | PASS |
| Backend test build | `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false` | PASS |
| Focused backend tests | `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build --filter "FullyQualifiedName~CarrierMarketEndpointTests|FullyQualifiedName~CasbinAuthorizationServiceTests"` | PASS, 111 tests |
| Frontend build | `npm run build` | PASS |
| Frontend route tests | `NODE_OPTIONS=--localstorage-file=/private/tmp/nebula-vitest-localstorage npm run test -- App.test.tsx` | PASS, 15 tests |
| Runtime health | `curl -sS -i http://127.0.0.1:8080/healthz` | PASS, `200 Healthy` |
| Runtime auth smoke | `curl -sS -i http://127.0.0.1:8080/carrier-markets` | PASS, `401 Unauthorized` |

## Notes

- Initial unprivileged `dotnet test` was blocked by local sandbox socket permissions; rerun with approved escalation passed.
- Frontend route tests require the Node localStorage option shown above in this environment.
- Full regression suites were not run; recommendation is to run them before a larger release merge.

## Recommendations

- [medium] Run the complete backend and frontend regression suites before merging F0028 into a release branch - owner: Quality Engineer; follow-up: Release validation checklist.
- [low] Add a reusable test environment variable for Vitest localStorage setup - owner: Frontend; follow-up: Test harness hardening.
