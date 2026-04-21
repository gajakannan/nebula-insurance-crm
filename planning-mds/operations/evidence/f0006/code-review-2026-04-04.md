# F0006 Code Review Evidence — 2026-04-04

**Feature:** F0006 — Submission Intake Workflow
**Reviewer:** Codex (Code Reviewer role)
**Date:** 2026-04-04
**Verdict:** PASS

## Reviewed Files

- `engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs`
- `engine/src/Nebula.Application/Services/SubmissionService.cs`
- `engine/src/Nebula.Infrastructure/Repositories/SubmissionRepository.cs`
- `engine/src/Nebula.Infrastructure/Services/UnavailableSubmissionDocumentChecklistReader.cs`
- `experience/src/pages/CreateSubmissionPage.tsx`
- `experience/src/pages/SubmissionDetailPage.tsx`
- `experience/src/pages/SubmissionsPage.tsx`
- `experience/src/features/submissions/hooks/useAssignSubmission.ts`
- `experience/src/features/submissions/hooks/useTransitionSubmission.ts`
- `experience/src/features/submissions/hooks/useUpdateSubmission.ts`
- `engine/tests/Nebula.Tests/Integration/WorkflowEndpointTests.cs`
- `engine/tests/Nebula.Tests/Unit/WorkflowServiceTests.cs`
- `engine/tests/Nebula.Tests/Unit/WorkflowStateMachineTests.cs`
- `experience/src/pages/tests/SubmissionsPage.integration.test.tsx`
- `experience/src/pages/tests/CreateSubmissionPage.integration.test.tsx`
- `experience/src/pages/tests/SubmissionDetailPage.integration.test.tsx`
- `experience/src/pages/tests/DashboardPage.integration.test.tsx`

## Review Summary

- No blocking correctness, layering, or maintainability defects were found in the F0006 implementation slice.
- The API endpoints stay thin and delegate business rules to `SubmissionService`, which keeps the controller layer aligned with the solution's application-service pattern.
- The repository owns scoped query composition, filter application, sort validation support, and stale-flag calculation without leaking database concerns into the API layer.
- The frontend submission routes use dedicated hooks for update, assignment, and transition mutations and preserve the route-level separation expected by the existing feature structure.

## Story Mapping

| Story | Key Code Paths | Review Verdict |
|-------|----------------|----------------|
| F0006-S0001 | `SubmissionEndpoints.ListSubmissions`, `SubmissionRepository.ListAsync`, `SubmissionsPage.tsx` | PASS |
| F0006-S0002 | `SubmissionEndpoints.CreateSubmission`, `SubmissionService.CreateAsync`, `CreateSubmissionPage.tsx` | PASS |
| F0006-S0003 | `SubmissionService.MapToDtoAsync`, `SubmissionEndpoints.UpdateSubmission`, `SubmissionDetailPage.tsx` | PASS |
| F0006-S0004 | `SubmissionEndpoints.PostTransition`, `SubmissionService.TransitionAsync`, `WorkflowStateMachine` | PASS |
| F0006-S0005 | `SubmissionService.EvaluateCompletenessAsync`, `UnavailableSubmissionDocumentChecklistReader` | PASS |
| F0006-S0006 | `SubmissionEndpoints.AssignSubmission`, `SubmissionService.AssignAsync`, `useAssignSubmission.ts` | PASS |
| F0006-S0007 | `SubmissionService.UpdateAsync`, `SubmissionService.TransitionAsync`, `SubmissionTimelineSection` integration path | PASS |
| F0006-S0008 | `SubmissionRepository.BuildStaleFlagsAsync`, stale UI rendering on submissions and dashboard pages | PASS |

## Test Adequacy

- Backend integration rerun: 10/10 passed.
- Backend unit / workflow rerun: 31/31 passed.
- Frontend route smoke rerun: 12/12 passed.
- Frontend page integration rerun: 8/8 passed.

## Non-Blocking Recommendations

- `WorkflowSlaThreshold.WarningDays` is still seeded but unused in the current stale-flag computation path. That is acceptable for closeout, but it remains low-priority cleanup or future UX work.
- The dashboard integration path emitted a nested-anchor warning from the task widget markup. That warning is outside the F0006 submission implementation and does not change this feature's code-review verdict.
