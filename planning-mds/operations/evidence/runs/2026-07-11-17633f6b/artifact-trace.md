# Artifact Trace — F0037 standalone review run 2026-07-11-17633f6b

## Produced by this run

| Artifact | Path | Owner |
|----------|------|-------|
| Base run README | `README.md` | review orchestrator |
| Action context | `action-context.md` | review orchestrator |
| Gate decisions | `gate-decisions.md` | review orchestrator |
| Artifact trace | `artifact-trace.md` | review orchestrator |
| Commands log | `commands.log` | review orchestrator |
| Lifecycle gates log | `lifecycle-gates.log` | review orchestrator |
| Code review report | `code-review-report.md` | Code Reviewer |
| Security review report | `security-review-report.md` | Security Reviewer |

## Inputs consumed (evidence basis)

**Framework (`nebula-agents/`):**
- `CONSUMER-CONTRACT.md` §8/§11/§13/§14 · `agents/actions/review.md` · `agents/code-reviewer/SKILL.md` ·
  `agents/security/SKILL.md` · `agents/templates/prompts/evidence-contract/review-operator-friendly.md`

**PR #56 source (read from `pr-56` @ `27a5162`; diff base `e2f78be`):**
- Scope resolution: `IDistributionScopeService.cs`, `Services/DistributionScopeService.cs`,
  `Repositories/DistributionScopeRepository.cs`, `Services/ProjectionVisibilityResolver.cs`
- Enforcement points: `Services/OperationalReportService.cs`, `Services/SearchService.cs`,
  `Services/BrokerInsightService.cs`
- Data access: `Repositories/OperationalReportProjectionRepository.cs`,
  `Repositories/BrokerInsightProjectionRepository.cs`, `Repositories/SearchDocumentRepository.cs`
- DTOs / validators: `DTOs/OperationalReportDtos.cs`, `DTOs/SearchDtos.cs`,
  `Validators/OperationalReportQueryValidator.cs`
- API: `Endpoints/OperationalReportEndpoints.cs`, `Endpoints/TerritoryEndpoints.cs`,
  `Endpoints/DistributionEndpoints.cs`, `Endpoints/SearchEndpoints.cs`
- Authorization: `planning-mds/security/policies/policy.csv`,
  `Infrastructure/Authorization/CasbinAuthorizationService.cs`, `Nebula.Infrastructure.csproj`
- Frontend: `features/reports/components/DistributionRollupReportView.tsx`, `ReportControls.tsx`,
  `features/reports/{types.ts,hooks.ts}`, `pages/OperationalReportsPage.tsx`, `components/layout/Sidebar.tsx`
- Tests: `Unit/SearchReporting/{DistributionScopeServiceTests,OperationalReportServiceTests,SearchServiceTests}.cs`,
  `Unit/BrokerInsights/BrokerInsightServiceTests.cs`, `Unit/CasbinAuthorizationServiceTests.cs`,
  `Integration/TerritoryEndpointTests.cs`, `experience/tests/e2e/f0037-distribution-rollups.spec.ts`
- Requirements: `planning-mds/features/archive/F0037-.../PRD.md`, `F0037-S0002-enforce-hierarchy-aware-read-scoping.md`

## Cross-reference

- F0037 feature-run `2026-07-06-2e7e606d` `code-review-report.md` / `security-review-report.md` were a
  **frontend-only** PRD-alignment follow-up (scope: `OperationalReportsPage.tsx` + e2e). This standalone
  run adds independent backend-enforcement coverage and does not contradict that run's frontend PASS.
