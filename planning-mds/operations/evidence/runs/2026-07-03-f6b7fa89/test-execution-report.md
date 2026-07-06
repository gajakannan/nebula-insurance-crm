# Test Execution Report — F0027 Focused Regression

Scope: `regression`
Date: `2026-07-03`
Mode: `standalone`
Result: `PASS WITH RECOMMENDATIONS`

## Commands Executed

| Layer | Command | Result | Artifact |
|-------|---------|--------|----------|
| Backend build | `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj` | PASS | `commands.log` |
| Backend test build | `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj` | PASS with accepted `Microsoft.OpenApi` warning | `commands.log` |
| Backend unit | `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build --results-directory planning-mds/operations/evidence/runs/2026-07-03-f6b7fa89/artifacts/test-results --logger "trx;LogFileName=f0027-backend-outbound-tests.trx" --collect "XPlat Code Coverage"` | PASS on escalated rerun | `artifacts/test-results/f0027-backend-outbound-tests.trx` |
| Frontend build | `pnpm --dir experience build` | PASS with Vite chunk-size warning | `commands.log` |
| Frontend component | `pnpm --dir experience exec vitest run src/features/documents/tests/ParentDocumentsPanel.test.tsx --reporter=json --outputFile=../planning-mds/operations/evidence/runs/2026-07-03-f6b7fa89/artifacts/test-results/f0027-frontend-parent-documents-panel.json` | PASS | `artifacts/test-results/f0027-frontend-parent-documents-panel.json` |
| Theme regression | `pnpm --dir experience lint:theme` | PASS | `commands.log` |
| KG symbols | `python3 scripts/kg/validate.py --check-symbols` | PASS | `commands.log` |
| KG drift | `python3 scripts/kg/validate.py --check-drift` | PASS | `commands.log` |
| Harness templates | `python3 agents/scripts/validate_templates.py` | PASS | `commands.log` |
| F0027 evidence | `python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet288/Nebula_pr/nebula-insurance-crm --feature F0027 --stage closeout` | PASS | `commands.log` |
| F0027 trackers | `python3 agents/product-manager/scripts/validate-trackers.py --feature F0027 --run-id 2026-07-02-b9316621` | PASS | `commands.log` |
| KG untested | `python3 scripts/kg/lookup.py --untested feature:F0027` | PASS, `untested_count=0` | `commands.log` |

## Pass / Fail Counts

| Suite | Total | Passed | Failed | Skipped |
|-------|-------|--------|--------|---------|
| `OutboundDocumentGenerationServiceTests` | 3 | 3 | 0 | 0 |
| `ParentDocumentsPanel.test.tsx` | 1 | 1 | 0 | 0 |

## Failed / Retried Command History

- Initial backend `dotnet test` attempt failed before test execution because the sandbox denied VSTest socket binding.
- The same backend command was rerun with approved escalation and passed with 3/3 tests.

## Skipped Layers And Rationale

| Layer | Rationale | Owner | Follow-up |
|-------|-----------|-------|-----------|
| Live API integration | F0027 closeout already tracked endpoint integration expansion as nonblocking; no implemented `OutboundDocumentEndpointTests.cs` exists yet. | Quality Engineer | Add API integration tests for preview/issue/regenerate, including auth failures. |
| Browser E2E | No F0027-specific Playwright workflow exists yet. | Quality Engineer | Add preview→issue→document detail provenance E2E once stable seeded templates are available. |
| Accessibility | No F0027-specific axe/Playwright accessibility test exists yet. | Quality Engineer | Add Generate Document Panel accessibility coverage. |
| Full frontend unit suite | Known unrelated shared localStorage/session failures are outside F0027 and were recorded in the feature-review run. | Frontend/QE | Repair shared test baseline separately. |

## Acceptance Criteria Coverage

- Preview before issue: covered by backend service test and frontend component entry-point test.
- Explicit issue: covered by backend service test.
- Generated artifact provenance: covered by backend service test/build and document DTO wiring.
- Regenerate/retrieve: covered by backend service test where implemented.
- Admin/service/distribution permissions: covered through policy/config validation, build, KG routing, and accepted closeout security review; live endpoint auth tests remain a recommended follow-up.

## Result

PASS WITH RECOMMENDATIONS. Focused F0027 regression tests passed with no blocking defects.
