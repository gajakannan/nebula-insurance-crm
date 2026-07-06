# Test Execution Report

## Summary

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers -v:minimal` | PASS | 0 warnings, 0 errors after endpoint property fix. |
| `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --disable-build-servers -v:minimal` | PASS | Existing nullable warnings in dashboard/workflow/task/search-reporting test code; 0 errors. |
| `corepack pnpm --dir experience build` | PASS | TypeScript and Vite build pass; existing chunk-size warning remains. |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build --filter FullyQualifiedName~SmokeTests --logger console;verbosity=minimal` | PASS | 17 passed, 0 failed, 0 skipped. |
| `corepack pnpm --dir experience exec vitest run src/features/admin-configuration/components/AdminConfigurationWorkspace.test.tsx` | PASS | 2 passed, 0 failed. Covers domain catalog, validation/compare panel, publish/rollback controls, audit empty/retry/detail behavior. |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --filter FullyQualifiedName~AdminConfigurationEndpointTests --logger "console;verbosity=minimal"` | PASS | 2 passed, 0 failed. Covers Admin catalog access and reason-required draft creation guard. |
| `F0032_E2E_BASE_URL=http://127.0.0.1:5174 node planning-mds/operations/evidence/runs/2026-07-06-f0ef8526/e2e-admin-configuration.mjs` | PASS | API lifecycle E2E passed: readiness, Admin/non-Admin authorization, catalog, draft guards, validation, compare, publish, stale-validation rejection, rollback, audit filters, and refresh status. |
| `corepack pnpm --dir experience exec node --input-type=module -e <playwright-ui-check>` | PASS | Live UI E2E passed: page/domain rendering, validation panel, audit filters, invalid JSON warning, and disabled Save draft. |

## Artifacts

- `engine/tests/Nebula.Tests/TestResults/ad9e2d29-aac2-4825-aa25-0fb8f425c759/coverage.cobertura.xml`
- `engine/tests/Nebula.Tests/TestResults/85febe8f-0839-455f-ba72-30a85befad7f/coverage.cobertura.xml`

## Open Test Gaps

- Focused AdminConfiguration endpoint and frontend workspace regression tests were added during PRD compliance remediation.
- Remaining optional hardening: deeper semantic validation cases per source domain and negative authorization matrix tests for non-Admin roles.
- E2E testing completed through the local dev runtime on `2026-07-06`; see `e2e-test-execution.md`.
