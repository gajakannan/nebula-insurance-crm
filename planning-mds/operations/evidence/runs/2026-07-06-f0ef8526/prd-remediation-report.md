# F0032 PRD Compliance Remediation Report

## Scope

Remediation was performed after screenshot/manual review showed the Admin Configuration workspace did not yet satisfy the operator-facing PRD expectations for catalog population, governed action states, validation/compare visibility, confirmations, rollback selection, and audit inspection.

## Remediated Gaps

| Gap | Resolution | Evidence |
|-----|------------|----------|
| `/admin/*` frontend calls returned the Vite shell instead of API JSON | Added `/admin` to the Vite API proxy path list. | `experience/vite.config.ts` |
| Dev UI could not exercise Admin-only operations with the local dev token | Added `Admin` to the dev auth role set while preserving `DistributionManager`. | `experience/src/services/dev-auth.ts` |
| Domain catalog rendered empty/opaque loading state | Added explicit loading, retry, empty, and error states and populated domain selection from `/admin/configuration-domains`. | `experience/src/features/admin-configuration/components/AdminConfigurationWorkspace.tsx` |
| Draft create/update/publish/rollback lacked reason enforcement in several paths | Added create body contract and endpoint-side nonblank reason checks. Update/publish/rollback already carry reason and now reject blank values. | `engine/src/Nebula.Api/Endpoints/AdminConfigurationEndpoints.cs`, `AdminConfigurationDtos.cs` |
| Validation/compare feedback was not visible enough for operator decision-making | Added validation and compare panel with blocking errors, warnings, change summary, and stale-validation warning. | `AdminConfigurationWorkspace.tsx` |
| Publish/rollback did not require explicit confirmation and rollback target selection | Added publish confirmation and rollback confirmation with historical published version selector. | `AdminConfigurationWorkspace.tsx` |
| Audit panel lacked filters/details | Added action/status/actor/date filters and audit detail dialog with summary JSON. | `AdminConfigurationWorkspace.tsx`, `hooks.ts` |
| Published-set history was unavailable to the UI | Added `publishedSets` to the domain detail DTO and repository/service mapping. | `AdminConfigurationService.cs`, `AdminConfigurationRepository.cs`, `types.ts` |
| Audit summaries were not useful for operator review | Added structured audit summaries for draft/create/update/validation/publish/rollback. | `AdminConfigurationService.cs` |
| API contract drifted from implementation | Updated OpenAPI for create request body, domain detail property names, published history, reason-required update, and `search-report-defaults`. | `planning-mds/api/nebula-api.yaml` |
| Focused regression tests were missing | Added frontend workspace component tests and backend endpoint tests for catalog/reason guard. | `AdminConfigurationWorkspace.test.tsx`, `AdminConfigurationEndpointTests.cs` |

## Validation Commands

| Command | Result |
|---------|--------|
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers -v:minimal` | PASS |
| `corepack pnpm --dir experience build` | PASS |
| `corepack pnpm --dir experience exec vitest run src/features/admin-configuration/components/AdminConfigurationWorkspace.test.tsx` | PASS |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --filter FullyQualifiedName~AdminConfigurationEndpointTests --logger "console;verbosity=minimal"` | PASS |
| `curl -i http://localhost:5113/healthz` | PASS; API returned `200 OK` and `Healthy`. |
| `curl -i http://127.0.0.1:5173/admin/configuration-domains` | PASS; unauthenticated proxy request returned API `401 application/problem+json`, proving `/admin` is proxied to Kestrel rather than served as Vite `index.html`. |
| Headless Playwright live UI check for `http://127.0.0.1:5173/admin/configuration` | PASS; rendered Admin Configuration, all four configured domains, validation/compare panel, and audit filters. |
| Headless Playwright authenticated `/admin/configuration-domains` fetch with dev Admin token | PASS; returned `200 application/json` with four domains through Vite proxy. |

## Residual Follow-Up

- Cross-instance cache invalidation remains the existing non-blocking DevOps follow-up if Nebula is deployed with multiple API instances.
- End-to-end publish/rollback should still be exercised by QA in a seeded test environment before production release packaging.
