# Test Execution Report - F0037

Result: PASS

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet restore engine/tests/Nebula.Tests/Nebula.Tests.csproj --disable-parallel` | PASS | Restored test project assets. |
| `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -v:minimal` | PASS | API build passed with 0 warnings/errors. |
| `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -v:minimal` | PASS | Test build passed; existing nullable warnings remain in unrelated tests plus one report-test warning. |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build --filter "FullyQualifiedName~SearchReporting.OperationalReportServiceTests|FullyQualifiedName~SearchReporting.SearchServiceTests|FullyQualifiedName~BrokerInsights.BrokerInsightServiceTests|FullyQualifiedName~CasbinAuthorizationServiceTests.DistributionRollupRead" -v:minimal` | PASS | 19 passed, 0 failed, 0 skipped. |
| `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build --filter "FullyQualifiedName~SearchReporting.OperationalReportServiceTests|FullyQualifiedName~SearchReporting.SearchServiceTests|FullyQualifiedName~SearchReporting.DistributionScopeServiceTests|FullyQualifiedName~BrokerInsights.BrokerInsightServiceTests|FullyQualifiedName~CasbinAuthorizationServiceTests.DistributionRollupRead" -v:minimal` | PASS | G3 remediation regression: 23 passed, 0 failed, 0 skipped. |
| `corepack pnpm --dir experience exec vitest run src/features/reports/components/__tests__/DistributionRollupReportView.test.tsx` | PASS | 2 passed, 0 failed. |
| `corepack pnpm --dir experience build` | PASS | TypeScript and Vite build passed; existing chunk-size advisory only. |
| `.venv/bin/python agents/product-manager/scripts/validate-stories.py .../F0037-hierarchy-aware-access-scoping-and-distribution-rollups` | PASS | All six F0037 stories passed validation. |
| `.venv/bin/python agents/product-manager/scripts/generate-story-index.py .../planning-mds/features/` | PASS | Regenerated `STORY-INDEX.md`; found 201 story files. |
| `python3 scripts/kg/validate.py --check-drift` | PASS | KG integrity passed with one pre-existing low-confidence F0028/F0018 warning. |

## Result Summary

- Backend focused tests: 19 passed.
- Backend focused tests after visibility predicate hardening: 19 passed.
- Backend focused tests after G3 remediation: 23 passed.
- Frontend focused tests: 2 passed.
- API build: passed.
- Frontend production build: passed.
- Story validation: passed.
- KG validation: passed.

## Known Warnings

- `Nebula.Tests` build reports existing nullable warnings in `WorkflowEndpointTests.cs` and `TaskServiceTests.cs`.
- `OperationalReportServiceTests.cs` has one nullable warning on a `ShouldContain` assertion; behavior test still passes.
- Vite reports an existing large chunk advisory.
