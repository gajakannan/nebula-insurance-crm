# F0024 Reference Notes

Feature: F0024 - Claims & Service Case Tracking
Product repo: `/Users/msig2/Desktop/work_space/nebula-insurance-crm`
Harness repo: `/Users/msig2/Desktop/work_space/nebula-agents`
Most recent harness run: `2026-07-03-72f49d29`
Prior archived baseline run: `2026-07-03-ba011af8`
Date: 2026-07-03

## Purpose

This note records the F0024 work performed through the Nebula agents harness, from dependency/runtime setup prompts through implementation, archive reconciliation, and validation. It is intended as a reference ledger for product, engineering, QA, and future closeout review.

## Operator Prompt Timeline

1. Install required packages and dependencies for both `nebula-agents` and `nebula-insurance-crm`.
2. Run `nebula-agents`.
3. Switch to Product Manager role.
4. Review `planning-mds/features/ROADMAP.md` for F0024 readiness and blockers.
5. List not-yet-built features that were good to go.
6. Move F0024 from Later to Now.
7. Confirm strict use of `nebula-agents` harness.
8. Produce a structured implementation plan using the harness.
9. Continue after operator approval.
10. Inject the evidence contract prompt from `/Users/msig2/Desktop/work_space/nebula-agents/agents/templates/prompts/evidence-contract/plan-operator-friendly.md`.
11. Confirm whether F0024 was complete.
12. Run `nebula-insurance-crm` and provide frontend/backend localhost links.
13. Fix frontend authentication configuration issue.
14. Fix "Unable to load service cases" issue.
15. Review F0024 PRD completeness from `planning-mds/features/archive/F0024-claims-and-service-case-tracking/PRD.md`.
16. Produce and implement the plan needed for F0024 to meet PRD requirements.
17. Run F0024 smoke tests.
18. Explain and resolve F0024 active/archive duplication.
19. Validate F0024 completely and create this reference notes file.

## Dependencies And Runtime Notes

- `nebula-agents` harness was used from `/Users/msig2/Desktop/work_space/nebula-agents`.
- `nebula-insurance-crm` product work was performed in `/Users/msig2/Desktop/work_space/nebula-insurance-crm`.
- Frontend dependency execution used `npm`; `pnpm` was not available in the shell.
- Existing installed/restored dependencies were used for the F0024 validation pass. No new package dependency was added specifically for this final validation step.
- Backend validation used .NET test execution for `engine/tests/Nebula.Tests/Nebula.Tests.csproj`.
- A known dependency warning remained visible during .NET tests: `Microsoft.OpenApi` 2.0.0 high-severity advisory `GHSA-v5pm-xwqc-g5wc`.
- The managed sandbox blocked MSBuild named-pipe creation; the focused .NET test was rerun with approved escalated execution and passed.

## Harness Agents And Roles

| Role | Harness Responsibility | F0024 Activity |
|------|------------------------|----------------|
| Product Manager | Roadmap, readiness, PRD alignment, lifecycle tracking, evidence governance | Reconciled F0024 to archive-only lifecycle state and maintained tracker evidence |
| Architect | Assembly plan, dependencies, KG bindings, API/schema/authorization plan | G0 assembly validation and drift-reconciliation framing |
| Feature Implementation | Product code changes and feature behavior | Backend and frontend PRD drift gaps were implemented in prior steps |
| Quality Engineer | Test plan, focused tests, smoke coverage, validation report | Backend, frontend unit, frontend build, and Playwright smoke validations |
| DevOps | Runtime preflight and deployability | Runtime preflight/deployability evidence and localhost checks |
| Code Reviewer | Independent implementation review | Required by role matrix; current active drift run has not yet produced a fresh G3+ code-review report |
| Security Reviewer | Authorization and sensitive-data behavior review | Required by role matrix; current active drift run has not yet produced a fresh G3+ security-review report |

## Actions Performed

### Planning And Lifecycle

- Moved F0024 back to active "Now" work for drift reconciliation during the earlier run.
- After operator request, removed the duplicate non-archive F0024 folder and made the archive folder canonical.
- Preserved the final F0024 planning package under: `planning-mds/features/archive/F0024-claims-and-service-case-tracking`.
- Updated `REGISTRY.md`, `ROADMAP.md`, `STORY-INDEX.md`, `BLUEPRINT.md`, and KG mappings to point F0024 references at archive.
- Removed the KG excluded-coverage workaround for the historical baseline because there is now only one canonical F0024 feature folder.

### Backend Implementation Scope

- Service-case DTO/query shape was extended for PRD-aligned workspace filtering and display.
- Service-case service logic was updated for search/filter behavior, contextual fields, validation guardrails, and owner profile lookup.
- Repository query behavior was updated for service-case workspace use cases.
- API endpoint behavior was aligned with updated DTO/query handling.
- Focused backend tests were updated and passed.

### Frontend Implementation Scope

- Service Cases workspace supports list rendering, search/filter behavior, and create modal rendering.
- Detail page supports work-management fields, status controls, communication link affordance, and history rendering.
- Service-case page routing/rendering was repaired after the earlier load failure.
- Playwright smoke coverage was added for list/filter/create-modal and detail/history paths.

## Artifacts Generated Or Updated

### Product Code And Tests

- `engine/src/Nebula.Application/DTOs/ServiceCaseDtos.cs`
- `engine/src/Nebula.Application/Services/ServiceCaseService.cs`
- `engine/src/Nebula.Application/Validators/ServiceCaseValidators.cs`
- `engine/src/Nebula.Infrastructure/Repositories/ServiceCaseRepository.cs`
- `engine/src/Nebula.Api/Endpoints/ServiceCaseEndpoints.cs`
- `engine/tests/Nebula.Tests/Unit/ServiceCaseServiceTests.cs`
- `experience/src/features/service-cases/**`
- `experience/src/pages/ServiceCasesPage.tsx`
- `experience/src/pages/ServiceCaseDetailPage.tsx`
- `experience/playwright.f0024.config.ts`
- `experience/tests/visual/f0024-smoke.spec.ts`

### Planning And Tracker Artifacts

- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/features/archive/F0024-claims-and-service-case-tracking/**`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`

### Harness Evidence Artifacts

- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/README.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/action-context.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/commands.log`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/evidence-manifest.json`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/g0-assembly-plan-validation.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/g1-runtime-preflight.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/g2-self-review.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/test-plan.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/test-execution-report.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/coverage-report.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/deployability-check.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/smoke-test-report.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/lifecycle-tracker-reconciliation.md`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/lifecycle-gates.log`
- `planning-mds/operations/evidence/runs/2026-07-03-72f49d29/gate-decisions.md`

## Validation Run Summary

| Validation | Command | Result | Notes |
|------------|---------|--------|-------|
| KG validation | `python3 scripts/kg/validate.py --write-coverage-report` | PASS | 30 mapped features, 12 excluded, 0 uncovered; warning remains for low-confidence inferred F0028/F0018 edge |
| Evidence validation | `python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/msig2/Desktop/work_space/nebula-insurance-crm --feature F0024 --run-id 2026-07-03-72f49d29 --stage G2` | PASS | G2 evidence contract validates |
| Tracker validation | `python3 agents/product-manager/scripts/validate-trackers.py --product-root /Users/msig2/Desktop/work_space/nebula-insurance-crm --feature F0024` | PASS | Passed after archived baseline story files were de-indexed and BLUEPRINT links were corrected |
| Backend focused tests | `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter ServiceCaseServiceTests --no-restore` | PASS | 9 passed; rerun escalated due sandbox MSBuild named-pipe restriction |
| Frontend build | `npm run build` | PASS | Vite build completed; chunk-size warning is non-blocking |
| Frontend unit test | `npm test -- ServiceCaseListPanel.test.tsx` | PASS | 1 file, 2 tests passed |
| Browser smoke | `npm exec playwright -- test tests/visual/f0024-smoke.spec.ts --config=playwright.f0024.config.ts` | PASS | 2 smoke tests passed |

## Current Validation Status

F0024 is validated through the harness G2 drift-reconciliation layer with passing tracker, KG, focused backend, frontend build/unit, and Playwright smoke validation.

The feature behavior is materially aligned with the F0024 PRD areas exercised in this pass: service-case workspace visibility, filters/search, contextual creation affordance, ownership/follow-up fields, status controls, claim/reference context, communication link affordance, and history rendering.

F0024 now exists only in the archive folder. Code Reviewer and Security Reviewer reports remain sourced from the prior archived baseline run, while the latest G2 validation evidence is sourced from run `2026-07-03-72f49d29`.

## Remaining Closeout Steps

1. Produce fresh Code Reviewer report if F0024 is reopened for another hardening pass.
2. Produce fresh Security Reviewer report if F0024 is reopened for another hardening pass.
3. Keep all future F0024 planning references pointed at `planning-mds/features/archive/F0024-claims-and-service-case-tracking`.
