# F0032 E2E Test Execution

## Summary

**Result:** PASS

F0032 was tested end to end through the local Vite `/admin` proxy and Kestrel API using the nebula-agents evidence run `2026-07-06-f0ef8526`.

## Runtime

| Check | Result | Notes |
|-------|--------|-------|
| Docker Postgres | PASS | `docker compose up -d db`; container was already running. |
| API health | PASS | `GET http://localhost:5113/healthz` returned `200 Healthy`. |
| Vite frontend | PASS | Dev server started on `http://127.0.0.1:5174` because `5173` was occupied. |
| `/admin` Vite proxy | PASS | Unauthenticated `/admin/configuration-domains` returned API `401 application/problem+json`, not Vite HTML. |

## API Lifecycle E2E

Command:

```bash
F0032_E2E_BASE_URL=http://127.0.0.1:5174 node planning-mds/operations/evidence/runs/2026-07-06-f0ef8526/e2e-admin-configuration.mjs
```

Final result: PASS

| Scenario | Result | Evidence |
|----------|--------|----------|
| API health | PASS | `200 Healthy` |
| Unauthenticated admin proxy | PASS | `401 application/problem+json` |
| Non-Admin catalog access | PASS | `403` |
| Admin catalog access | PASS | 4 domains returned |
| Catalog contains `queue-routing` | PASS | Present |
| Catalog contains `workflow-sla-thresholds` | PASS | Present |
| Catalog contains `search-report-defaults` | PASS | Present |
| Catalog contains `template-metadata` | PASS | Present |
| Domain detail | PASS | `template-metadata` detail loaded |
| Draft create/reuse | PASS | Existing active draft reused, then subsequent drafts created |
| Blank reason guard | PASS | Draft update with blank reason returned `400` |
| Missing `If-Match` guard | PASS | Draft update without `If-Match` returned `428` |
| Draft update | PASS | Updates returned `200` with row version flow |
| Validation | PASS | Validation returned `Passed` |
| Compare | PASS | Compare returned change summary |
| Publish | PASS | Published versions `3`, `4`, and `5` during the final run |
| Stale validation protection | PASS | Publish after mutation without revalidation returned `409 validation_required` |
| Revalidation after stale mutation | PASS | Revalidation then publish succeeded |
| Rollback | PASS | Rollback to version `3` created new published version `6` |
| Append-only rollback | PASS | Rollback version `6` is greater than prior published version `4` |
| Audit filters | PASS | `DraftCreated`, `DraftUpdated`, `ValidationPassed`, `Published`, and `RollbackPublished` filters returned events |
| Refresh status | PASS | Final detail included `Refreshed` status |

## UI E2E

Command:

```bash
corepack pnpm --dir experience exec node --input-type=module -e <playwright-ui-check>
```

Result: PASS

| Scenario | Result |
|----------|--------|
| Admin Configuration page renders | PASS |
| All four domains visible | PASS |
| Validation and compare panel visible | PASS |
| Audit filters visible | PASS |
| Invalid JSON payload warning visible | PASS |
| Save draft disabled for invalid JSON | PASS |

## Regression Validation

| Command | Result |
|---------|--------|
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers -v:minimal` | PASS |
| `corepack pnpm --dir experience build` | PASS |
| `corepack pnpm --dir experience exec vitest run src/features/admin-configuration/components/AdminConfigurationWorkspace.test.tsx` | PASS; 2 tests passed |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --filter FullyQualifiedName~AdminConfigurationEndpointTests --logger "console;verbosity=minimal"` | PASS; 2 tests passed |

## Runner Correction

The first E2E runner attempt failed while testing stale validation because the script used the pre-validation row version after validation had updated the draft. The runner was corrected to refetch the active draft after validation before mutating it. The final rerun passed.

## Residual Risk

- E2E execution used the local dev database and intentionally appended published configuration versions for `template-metadata`.
- Broader full-suite regression and production deployment validation remain outside this E2E pass.
