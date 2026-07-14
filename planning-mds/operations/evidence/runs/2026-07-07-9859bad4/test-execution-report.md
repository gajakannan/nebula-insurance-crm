# Test Execution Report - F0025 run 2026-07-07-9859bad4

## Commands Run

| Command | Result | Notes |
|---|---|---|
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false` | PASS | Two pre-existing nullable warnings in `DashboardRepository.cs`. |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false --filter "FullyQualifiedName~CommissionRevenue\|FullyQualifiedName~CasbinAuthorizationServiceTests.CommissionPolicy"` | PASS | 22 passed, 0 failed, 0 skipped after escalated rerun for local test socket permission. |
| `pnpm --dir experience lint` | PASS | 14 pre-existing warnings outside F0025. |
| `pnpm --dir experience lint:theme` | PASS | No raw palette class violations found. |
| `pnpm --dir experience build` | PASS | Vite chunk-size warning only. |
| `pnpm --dir experience exec vitest run src/features/commissions/tests/CommissionsPage.test.tsx` | PASS | 2 passed. |
| `.venv/bin/python scripts/kg/validate.py --write-coverage-report` | PASS | One pre-existing low-confidence inferred-edge warning. |

## Failures And Retries

- Plain `python3 scripts/kg/validate.py` failed because `yaml` was unavailable outside the repo virtualenv; reran via `.venv/bin/python`.
- Package-level `pnpm --dir experience test -- src/features/commissions/tests/CommissionsPage.test.tsx` ran the full suite instead of the target file. F0025 tests passed inside that run; unrelated localStorage environment tests failed. Reran the exact file with `pnpm --dir experience exec vitest run ...`.
- Backend focused tests failed in sandbox due local socket permission; escalated rerun passed.

## Feature-Level Frontend Notes

- F0025 frontend files are feature-local under `experience/src/features/commissions/**` plus route pages `CommissionsPage.tsx` and `CommissionDetailPage.tsx`.
- The frontend test file `experience/src/features/commissions/tests/CommissionsPage.test.tsx` verifies workspace record/rollup rendering and detail recalculation mutation wiring.
- `pnpm --dir experience lint:theme` passed for semantic theme-token compliance.
- No clickable non-interactive wrappers were introduced for F0025 actions; navigation uses `Link` and actions use `button`.

## Result

G2 implementation validation is PASS with the noted G3/G6 follow-ups.
