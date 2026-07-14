# Post-Closeout Validation - F0025 run 2026-07-07-9859bad4

Date: 2026-07-07
Harness: `nebula-agents`

## Summary

F0025 remains closed out and tracker-valid. Core backend, frontend, KG, and template validations passed. Security scanner hardening was partially blocked by local tool availability, and dependency audit found frontend advisories requiring follow-up.

## Runtime Preflight

- `docker compose ps`: PASS with `api`, `db`, `authentik-server`, and `authentik-worker` running; DB/Auth services were healthy.
- `.NET SDK`: available.
- `pnpm`: available.
- `experience/node_modules`: present.

## Evidence And Tracker Validation

- `validate-feature-evidence.py --feature F0025 --stage closeout`: PASS.
- `validate-trackers.py --feature F0025 --run-id 2026-07-07-9859bad4`: PASS with 0 errors and 0 warnings.

## Backend Validation

- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -m:1 -v:minimal -nr:false -p:UseSharedCompilation=false`: PASS with 0 warnings and 0 errors.
- Focused backend tests:
  - First sandbox run was runtime-blocked by testhost socket permission.
  - Corrected elevated rerun with filter `FullyQualifiedName~CommissionRevenue|FullyQualifiedName~CasbinAuthorizationServiceTests.CommissionPolicy`: PASS, 22 passed, 0 failed, 0 skipped.
  - Coverage artifact: `engine/tests/Nebula.Tests/TestResults/56c77996-b2c0-489e-bfa9-f7adec376ead/coverage.cobertura.xml`.

## Frontend Validation

- `pnpm --dir experience lint`: PASS with 0 errors and 14 existing warnings outside F0025.
- `pnpm --dir experience lint:theme`: PASS.
- `pnpm --dir experience build`: PASS; Vite emitted the existing large chunk warning.
- `pnpm --dir experience exec vitest run src/features/commissions/tests/CommissionsPage.test.tsx`: PASS, 2 tests.

## KG And Template Validation

- `.venv/bin/python scripts/kg/validate.py --check-drift`: PASS.
- `.venv/bin/python scripts/kg/validate.py --write-coverage-report`: PASS.
- `/Users/msig4/Documents/NEBULA/nebula-insurance-crm/.venv/bin/python agents/scripts/validate_templates.py`: PASS.

## Security Scanner Hardening

- Dependency scan:
  - Initial sandbox run was blocked by npm advisory DNS/network resolution and backend vulnerability lookup hang; the stuck scan was terminated.
  - Elevated rerun completed.
  - Backend NuGet vulnerability scan: PASS, 0 vulnerable packages reported.
  - Frontend `pnpm audit`: FAIL with 61 advisories: 3 low, 28 moderate, 29 high, 1 critical.
  - Notable frontend advisory classes include `vitest`, `minimatch`, `flatted`, `picomatch`, `axios`, `ws`, `form-data`, and `vite`, mostly through test/build/tooling dependency paths.
- Secrets scan: BLOCKED. `gitleaks` is not installed in PATH.
- SAST scan: BLOCKED. `semgrep` is not installed in PATH.
- DAST scan: BLOCKED. Docker attempted `owasp/zap2docker-stable`, but image pull/access failed.

## Follow-Ups

- Treat frontend dependency advisories as a security hardening item before production release.
- Install or provide `gitleaks` and `semgrep`, then rerun secrets and SAST wrappers.
- Provide an available OWASP ZAP runner/image or staging security job, then rerun DAST.
- Preserve existing closeout follow-ups from `pm-closeout.md`, especially source-scope regression tests and projection-granularity confirmation.
