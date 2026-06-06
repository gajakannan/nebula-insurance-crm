# Code Review Report - F0019-submission-quoting-proposal-and-approval run 2026-06-03-7e8e0ddc

## Reviewed Files

Reviewed the canonical changed-file set in artifacts/diffs/changed-files.txt, with focused review on:

- engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs
- engine/src/Nebula.Application/Services/SubmissionService.cs
- engine/src/Nebula.Infrastructure/Repositories/SubmissionRepository.cs
- engine/src/Nebula.Infrastructure/Persistence/Migrations/20260603220000_F0019_SubmissionQuotingApproval.cs
- engine/tests/Nebula.Tests/Unit/WorkflowServiceTests.cs
- engine/tests/Nebula.Tests/Unit/WorkflowStateMachineTests.cs
- experience/src/pages/SubmissionDetailPage.tsx
- experience/src/pages/SubmissionsPage.tsx
- experience/src/features/submissions/hooks/useSubmissionQuotePacket.ts
- experience/src/features/submissions/hooks/useSubmissionApproval.ts
- experience/src/features/submissions/hooks/useBindSubmission.ts
- experience/src/features/submissions/hooks/useArchiveSubmission.ts
- planning-mds/api/nebula-api.yaml
- planning-mds/schemas/activity-event-payloads.schema.json

## Validation Artifacts

- artifacts/test-results/backend-build-after-g3-filter-fix.txt
- artifacts/test-results/backend-workflow-tests-after-g3-filter-fix.txt
- artifacts/test-results/frontend-build.txt
- artifacts/test-results/frontend-submissions-integration.txt
- artifacts/test-results/frontend-unit.txt
- artifacts/coverage/backend-workflow-tests-after-g3-filter-fix-coverage.cobertura.xml
- artifacts/diffs/f0019-migration-scoped.sql

## Severity-Ranked Findings

- [medium] Archive/reactivate accepted blank audit reasons, leaving terminal lifecycle audit events without the required rationale. Fixed in SubmissionService.ArchiveAsync and ReactivateAsync by returning missing_reason before state mutation; covered by SubmissionService_ArchiveAndReactivateAsync_BlankReason_ReturnsMissingReason. Status: fixed before approval.
- [medium] The approvalPending list filter over-included Quoted submissions without a ready packet and submissions with any prior approval decision. Fixed in SubmissionRepository.ApplyFilters to require a ReadyForApproval packet and no approval decisions. Backend build and targeted workflow tests remained green. Status: fixed before approval.

## Non-Blocking Recommendations With Owner/Follow-up

- None.

## Vertical-Slice Completeness

The slice covers data, API, UI, contracts, and tests:

- Data: new quote packet, approval decision, bind handoff tables plus archive columns.
- API: quote-packet, approval, bind request/confirmation, archive, reactivate, and list filter endpoints/params.
- UI: submission detail downstream panel and submission list status/filter signals.
- Contracts: OpenAPI route/schema additions and activity payload schema additions.
- Tests: backend workflow/service tests and submissions frontend integration tests.

## AC / Test Adequacy

The highest-risk workflow ACs have direct backend behavior coverage. Frontend list/detail changes have integration coverage and production build validation. API endpoint behavior does not have a live HTTP integration suite in this run, but endpoint compilation, service tests, OpenAPI sync, and UI integration coverage are sufficient for this feature gate.

## Architecture Compliance

The implementation follows the G0 assembly plan and ADR-025 boundary:

- Quote packet values are recorded CRM coordination data, not computed rating/pricing.
- Bind handoff is an in-process idempotent record, not a new async infrastructure dependency.
- Downstream submission workflow remains on the existing submission resource and state-machine vocabulary.
- Shared contracts and activity payload schema were updated with the as-built behavior.

## Coverage Verification

coverage-report.md references the final backend Cobertura artifact and accurately states that the Cobertura line-rate is project-wide for a targeted test filter. The report does not claim frontend lcov coverage.

## Result

Result: APPROVED

