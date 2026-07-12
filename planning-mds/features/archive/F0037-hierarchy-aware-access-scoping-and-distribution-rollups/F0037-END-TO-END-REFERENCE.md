# F0037 End-to-End Implementation Reference

Feature: F0037 - Hierarchy-Aware Access Scoping and Distribution Rollups  
Product: `nebula-insurance-crm`  
Harness: `nebula-agents`  
Final status: Approved / Done  
Latest approved evidence run: `2026-07-06-2e7e606d`  
Last updated: 2026-07-06

## Executive Summary

F0037 was implemented end to end using the `nebula-agents` governed lifecycle. The feature adds hierarchy-aware access scoping and distribution rollup reporting across backend APIs, projection visibility, search/reporting surfaces, broker insights, frontend filters/views, security policy documentation, knowledge-graph bindings, and E2E evidence.

The final user-facing entry point is:

`/operational-reports?report=rollups`

The final local validation URL used during testing was:

`http://127.0.0.1:4173/operational-reports?report=rollups`

## Harness Runs

| Run ID | Purpose | Status | Notes |
|--------|---------|--------|-------|
| `2026-07-06-6e3851ab` | Plan action | Completed | Phase A/B planning, story breakdown, architecture direction, gate preparation. |
| `2026-07-06-76799554` | Main feature implementation | Superseded by later evidence | Backend, frontend, policy, schema, KG, and initial validation implementation. |
| `2026-07-06-38152f5c` | Sidebar discoverability follow-up | Superseded by final E2E rerun | Added left-sidebar access to Operational Reports -> Distribution rollups. |
| `2026-07-06-74a4efd7` | Final E2E testing rerun | Superseded by PRD alignment rerun | Browser/API E2E, final fixes, KG/tracker validation, closeout. |
| `2026-07-06-2e7e606d` | PRD alignment rerun | Approved | Corrected Distribution rollups filter surface to match the PRD screenshot expectation. |

Latest run pointer:

`planning-mds/operations/evidence/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/latest-run.json`

Latest evidence package:

`planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/`

## Setup and Dependency Work

The work started by preparing both repositories:

- `nebula-agents`: used as the governing harness for plan, feature, validation, and closeout actions.
- `nebula-insurance-crm`: used as the product root for implementation, runtime, tests, tracker updates, and evidence.

Dependency/runtime setup covered:

- Node/pnpm tooling through `corepack pnpm`.
- Frontend package/build validation in `experience`.
- .NET backend test/build validation through the CRM engine test project.
- Docker Postgres runtime through the CRM compose stack.
- Local API and Vite frontend runtime health checks.

Runtime endpoints used during the final E2E run:

- API: `http://127.0.0.1:5114/healthz`
- Frontend: `http://127.0.0.1:4173/`

## Agent Flow

The work followed the `nebula-agents` lifecycle rather than ad hoc development.

| Agent / Role | Participation |
|--------------|---------------|
| Product Manager | Moved F0037 into Now, refined feature scope, produced story breakdown, tracked approvals, and closed the feature. |
| Architect | Reviewed Phase B architecture, service/API/schema/security direction, KG bindings, and assembly plan. |
| Feature Orchestrator | Managed feature action evidence, gates, candidate validation, tracker sync, and run closeout. |
| Quality Engineer | Defined and executed backend, frontend, and browser E2E validation. |
| Code Reviewer | Reviewed implementation deltas, tests, and final narrow fixes. |
| Security Reviewer | Reviewed access-control, no-leak behavior, policy alignment, and security-sensitive surfaces. |
| DevOps | Confirmed runtime health/build/deployability; no new runtime topology was introduced. |
| KG / Ontology Validation | Validated feature mappings, symbol bindings, coverage, and drift checks. |

## Gates Followed

The process used the planned `plan` then `feature` flow.

### Plan Action

Plan action scope:

- Phase A Product Manager planning.
- G1 clarification.
- G2 tracker sync.
- G3 requirements approval.
- Phase B Architect planning.
- G4 ontology/KG validation.
- G5 architecture approval.

Important planning outputs:

- F0037 story breakdown.
- Architecture direction.
- Security and authorization constraints.
- API/schema expectations.
- KG and tracker alignment.

### Feature Action

Feature action scope:

- G0 assembly-plan validation.
- G1 runtime preflight.
- G2 self-review and test execution.
- G3 code/security review.
- G5 role signoff.
- G6 candidate validation and tracker validation.
- G7 KG reconciliation.
- G8 PM closeout.

Final closeout result:

- `validate-feature-evidence.py --feature F0037 --stage closeout`: PASS

## Stories Implemented

| Story | Scope |
|-------|-------|
| F0037-S0001 | Resolve current user distribution scope from hierarchy, territory, ownership, role, and as-of date. |
| F0037-S0002 | Enforce hierarchy-aware read scoping for distribution structure and source-record visibility. |
| F0037-S0003 | Apply visibility to search, saved views, broker insights, and operational reports. |
| F0037-S0004 | Add distribution rollup reporting by hierarchy, territory, and producer. |
| F0037-S0005 | Add UI filters, rollup panels, drilldowns, and no-leak empty/no-access states. |
| F0037-S0006 | Add audit/security evidence, permission matrix parity, and rollup reconciliation checks. |

## Implementation Summary

### Backend

Implemented hierarchy-aware distribution scope resolution and applied it before response materialization.

Key backend changes:

- Added distribution scope service/repository contracts and implementation.
- Extended operational report query DTOs and validators with F0037 scope filters.
- Added distribution rollup reporting contracts and service behavior.
- Applied scope-aware filtering to operational reports, search, broker insights, and projection repositories.
- Updated distribution and territory endpoints with no-leak direct-read behavior.
- Ensured hidden/out-of-scope records are omitted from rows, counts, totals, facets, suggestions, drilldowns, and rollups.

Representative files:

- `engine/src/Nebula.Application/Interfaces/IDistributionScopeService.cs`
- `engine/src/Nebula.Application/Services/DistributionScopeService.cs`
- `engine/src/Nebula.Infrastructure/Repositories/DistributionScopeRepository.cs`
- `engine/src/Nebula.Application/Services/OperationalReportService.cs`
- `engine/src/Nebula.Application/Services/SearchService.cs`
- `engine/src/Nebula.Application/Services/BrokerInsightService.cs`
- `engine/src/Nebula.Infrastructure/Repositories/OperationalReportProjectionRepository.cs`
- `engine/src/Nebula.Infrastructure/Repositories/SearchDocumentRepository.cs`
- `engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs`

Final E2E rerun also fixed a discovered backend edge case:

- Inactive root distribution nodes can still be resolved as the requested root while inactive descendants remain filtered.
- Unknown territory/member lookups preserve no-leak behavior while visible members with no assignment return a safe null assignment response.

### API and Schema

Added/updated API and schema documentation:

- `GET /operational-reports/distribution-rollups`
- Hierarchy/scope query filters:
  - `rootNodeId`
  - `territoryId`
  - `producerUserId`
  - `asOf`
- Rollup response schema with grouped rows, totals, generated timestamp, as-of date, and drilldown links.

Representative files:

- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/distribution-rollup-report.schema.json`
- `engine/src/Nebula.Application/DTOs/OperationalReportDtos.cs`
- `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs`

### Security and Policy

Security scope included role enforcement and hidden-record no-leak behavior.

Implemented/updated:

- `distribution_rollup:read` authorization surface.
- Permission matrix and policy CSV updates.
- BrokerUser/ExternalUser denied unless explicitly in policy.
- No-leak behavior for direct hidden/out-of-scope record access.

Representative files:

- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `engine/tests/Nebula.Tests/Unit/CasbinAuthorizationServiceTests.cs`

### Frontend

Added the F0037 rollup UI inside Operational Reports and then exposed it in the sidebar.

Implemented:

- Distribution rollup tab in Operational Reports.
- Rollup filters for root node, territory, producer, as-of date, group-by, and metric family.
- Safe empty/no-visible-rollup state.
- Drilldown links to workload reports when rollup rows exist.
- Search page scope filters.
- Sidebar navigation item:
  - Label: `Operational Reports`
  - URL: `/operational-reports?report=rollups`

Representative files:

- `experience/src/pages/OperationalReportsPage.tsx`
- `experience/src/features/reports/components/DistributionRollupReportView.tsx`
- `experience/src/features/reports/components/ReportControls.tsx`
- `experience/src/features/reports/hooks.ts`
- `experience/src/features/reports/types.ts`
- `experience/src/features/search/components/SearchFilters.tsx`
- `experience/src/pages/SearchResultsPage.tsx`
- `experience/src/components/layout/Sidebar.tsx`

## Artifacts Generated

Final approved evidence run:

`planning-mds/operations/evidence/runs/2026-07-06-74a4efd7/`

Key evidence files:

- `README.md`
- `action-context.md`
- `artifact-trace.md`
- `gate-decisions.md`
- `commands.log`
- `lifecycle-gates.log`
- `evidence-manifest.json`
- `g0-assembly-plan-validation.md`
- `g1-runtime-preflight.md`
- `g2-self-review.md`
- `test-plan.md`
- `test-execution-report.md`
- `coverage-report.md`
- `deployability-check.md`
- `code-review-report.md`
- `security-review-report.md`
- `signoff-ledger.md`
- `feature-action-execution.md`
- `kg-reconciliation.md`
- `pm-closeout.md`

Test artifacts:

- `artifacts/test-results/f0037-playwright.txt`
- `artifacts/test-results/f0037-playwright-results.json`
- `artifacts/screenshots/f0037-sidebar-rollups.png`
- `artifacts/screenshots/f0037-scoped-empty.png`
- `artifacts/screenshots/f0037-rollups-default.png`

Diff artifact:

- `artifacts/diffs/changed-files.txt`

## Validations Run

### Backend

Command:

```bash
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter "FullyQualifiedName~SearchReporting|FullyQualifiedName~BrokerInsights|FullyQualifiedName~Casbin|FullyQualifiedName~Distribution|FullyQualifiedName~Territory"
```

Result:

- PASS
- 187 tests passed

### Frontend Build

Command:

```bash
corepack pnpm --dir experience build
```

Result:

- PASS

### Frontend Focused Tests

Command:

```bash
corepack pnpm -C experience exec vitest run src/components/layout/Sidebar.test.tsx src/features/reports/components/__tests__/DistributionRollupReportView.test.tsx src/features/broker-insights/tests/BrokerInsightsWorkspace.test.tsx
```

Result:

- PASS
- 5 tests passed

### Browser E2E

Command:

```bash
corepack pnpm -C experience exec playwright test f0037-distribution-rollups.spec.ts --config=playwright.f0037.config.ts
```

Result:

- PASS
- 4 tests passed

E2E coverage:

- Sidebar opens Operational Reports.
- Sidebar link lands on `/operational-reports?report=rollups`.
- Distribution rollups tab is active.
- F0037 filters are visible and update query state.
- Group-by and metric-family switching works.
- Scoped-away filters show a no-leak empty state.
- External user search behavior returns no-leak outcomes.
- Search and broker-insights preserve F0037 query filters.

### Harness Validators

Commands:

```bash
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7 --stage G1
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7 --stage G2
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7 --stage G3
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7 --stage G5
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7 --stage G6
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7 --stage G7
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --stage closeout
```

Result:

- PASS for all stages
- Only warning: `commands_log_absolute_cwd_warns`

### Tracker Validation

Command:

```bash
python3 agents/product-manager/scripts/validate-trackers.py --product-root /Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm --feature F0037 --run-id 2026-07-06-74a4efd7
```

Result:

- PASS

## Knowledge Graph Work

KG files were updated during the implementation run and revalidated during the final E2E run.

Representative KG/planning files:

- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/symbol-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/knowledge-graph/unbound-but-referenced.yaml`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/STORY-INDEX.md`

KG validation commands:

```bash
python3 scripts/kg/validate.py --check-symbols
python3 scripts/kg/validate.py --check-drift
python3 scripts/kg/validate.py --write-coverage-report
```

Final KG validation result:

- PASS
- 34 mapped features
- 170 mapped stories
- 34 mapped, 6 excluded, 0 uncovered feature coverage
- 220 code bindings
- 1467 symbols
- Existing warning only: low-confidence inferred edge on F0028 in F0018, unrelated to F0037

## Final Runtime Verification

Final runtime checks:

- API health: `http://127.0.0.1:5114/healthz` returned `200`
- Frontend: `http://127.0.0.1:4173/` returned `200`
- F0037 UI: `http://127.0.0.1:4173/operational-reports?report=rollups`

## Known Caveat

The local seed data used during the final E2E run did not include visible distribution rollup rows. Because of that, the browser E2E validated the safe no-leak empty state and conditionally exercises drilldown behavior when rows exist.

This was recorded in the final evidence manifest as an omission, not a blocker.

## Final Decision

F0037 is implemented end to end and approved through the `nebula-agents` harness. The final PRD-aligned screen shows `As of`, `Root node`, `Territory`, `Producer`, `Group by`, and `Metric family` on the Distribution rollups tab, and does not show the generic workload filters there.

Final approved run:

`2026-07-06-2e7e606d`

Final evidence package:

`planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/`
