# F0007 Code Reviewer Evidence — 2026-04-11

**Feature:** F0007 — Renewal Pipeline
**Reviewer:** Codex (Code Reviewer role)
**Date:** 2026-04-11
**Verdict:** PASS

## Scope Reviewed

All modified and new files in the `feat/F0007-renewal-pipeline` change set:

| Layer | Files Reviewed |
|-------|----------------|
| Domain | `Renewal.cs`, `Policy.cs`, `WorkflowSlaThreshold.cs` |
| Application — DTOs | `RenewalDto.cs`, `RenewalListItemDto.cs`, `RenewalListQuery.cs`, `RenewalCreateDto.cs`, `RenewalTransitionRequestDto.cs`, `RenewalAssignmentRequestDto.cs` |
| Application — Validators | `RenewalCreateValidator.cs`, `RenewalTransitionValidator.cs`, `RenewalAssignmentValidator.cs` |
| Application — Interfaces | `IRenewalRepository.cs`, `IReferenceDataRepository.cs`, `IWorkflowSlaThresholdRepository.cs` |
| Application — Services | `RenewalService.cs`, `WorkflowStateMachine.cs` |
| Infrastructure — Repositories | `RenewalRepository.cs`, `ReferenceDataRepository.cs`, `WorkflowSlaThresholdRepository.cs` |
| Infrastructure — Configurations | `RenewalConfiguration.cs`, `WorkflowSlaThresholdConfiguration.cs`, `PolicyConfiguration.cs` |
| Infrastructure — Migrations | `F0007_ReconcileRenewalWorkflowStates`, `F0007_ReconcileRenewalEntityShape`, `F0018_PolicyStubAndF0007RenewalSlaReconcile` |
| Infrastructure — DI | `DependencyInjection.cs` |
| API — Endpoints | `RenewalEndpoints.cs` |
| API — Helpers | `ProblemDetailsHelper.cs` |
| Tests — Integration | `WorkflowEndpointTests.cs`, `NudgePriorityTests.cs` |
| Tests — Unit | `WorkflowServiceTests.cs`, `WorkflowStateMachineTests.cs` |
| Experience | `RenewalsPage.tsx`, `RenewalDetailPage.tsx`, renewals feature module (hooks, components, types), `App.tsx`, `Sidebar.tsx`, mock handlers |

## Review Findings

### Correctness

| Area | Finding | Verdict |
|------|---------|---------|
| State machine | 6-state transition map matches PRD exactly. Role gates enforce Distribution→intake, Underwriter→review stages. `ValidateRenewalTransition` returns `invalid_transition` or `policy_denied` appropriately. Case-insensitive role comparison used. | PASS |
| Service — Create | Policy lookup via `IReferenceDataRepository.GetPolicyByIdAsync`, one-active-per-policy check, assignee resolution with self-assign shortcut, stage-ownership validation, LOB inheritance (explicit > policy > null), SLA-based TargetOutreachDate computation. Atomic commit with transition + timeline. | PASS |
| Service — Transition | Row version precondition check before mutation. Conditional field enforcement (Lost→reasonCode, Completed→boundPolicyId/renewalSubmissionId) with `missing_transition_prerequisite` return. Clears inapplicable fields on non-Lost/non-Completed transitions. `DbUpdateConcurrencyException` caught for optimistic concurrency. | PASS |
| Service — Assign | Terminal state blocked. Assignee validation (exists, active, correct stage role). No-op on same-assignee reassignment. Previous assignee context captured in timeline event. Concurrency exception handled. | PASS |
| Repository — List | ABAC scoping: Admin=unscoped, DistributionUser/Underwriter=assigned only, DistributionManager=region-scoped, RelationshipManager=managed broker, ProgramManager=all visible. Terminal status exclusion by default (`includeTerminal` flag). Due-window filter (45/60/90/overdue). Urgency filter with per-LOB threshold segments using `Concat` for OR composition. | PASS |
| Repository — SLA Threshold | `WorkflowSlaThresholdRepository.GetThresholdAsync` tries exact LOB match first, then falls back to null LOB (default). Used consistently by both `CreateAsync` and `ComputeUrgencyAsync`. | PASS |
| Validators | `RenewalCreateValidator`: PolicyId required, optional LOB validation. `RenewalTransitionValidator`: ToState whitelist, Lost→reasonCode required, Other→reasonDetail required, Completed→boundPolicyId/renewalSubmissionId required. `RenewalAssignmentValidator`: AssignedToUserId not empty. All use FluentValidation conventions. | PASS |
| Endpoints | 6 routes with Casbin enforcement: `renewal:read` (list, detail, timeline), `renewal:create`, `renewal:transition`, `renewal:assign`. If-Match parsing for transition and assignment. Validation error grouping consistent with existing pattern. | PASS |
| Migrations | Status reconciliation maps 15 old states → 6 new states. Entity shape migration adds nullable columns, backfills, then alters to required. Policy stub migration backfills Policies rows for orphaned Renewal.PolicyId values. All three have complete `Down()` methods. | PASS |

### Layering and Architecture

| Area | Finding | Verdict |
|------|---------|---------|
| Clean Architecture | Domain entities have no framework dependencies. Application layer defines interfaces; Infrastructure implements them. API layer only calls service methods and maps results. | PASS |
| Consistency with F0006 | Service rewrite follows the same UoW pattern, timeline event emission, assignee resolution, and ProblemDetails error mapping as SubmissionService. | PASS |
| DTO design | Immutable records throughout. List item DTO carries denormalized display fields. Detail DTO carries computed `Urgency` and `AvailableTransitions`. RowVersion serialized as string. | PASS |

### Maintainability

| Area | Finding | Verdict |
|------|---------|---------|
| Naming | Property names follow established conventions (e.g., `AssignedToUserId`, `PolicyExpirationDate`). DTO field names align with API contract. | PASS |
| Error handling | Consistent error code strings returned from service, mapped to ProblemDetails at endpoint layer. No exception-based control flow except `DbUpdateConcurrencyException` which is the EF Core pattern. | PASS |
| Test stubs | `StubRenewalRepository` and `StubWorkflowSlaThresholdRepository` in unit tests are minimal and focused. Integration tests use `CustomWebApplicationFactory` with real EF Core in-memory provider. | PASS |

### Minor Observations (Non-Blocking)

1. `RenewalService.ResolveAssigneeAsync` constructs a synthetic `UserProfile` when `assigneeId == user.UserId`. This avoids a DB round-trip but creates a UserProfile that isn't persisted. Acceptable for the create/assign fast path, but the synthetic object won't have real `Department` or `Email` — this is fine since only `Id`, `DisplayName`, `IsActive`, and `RolesJson` are consumed.
2. `RenewalRepository.UpdateAsync` returns `Task.CompletedTask` — EF change tracking handles persistence through `UnitOfWork.CommitAsync`. This is consistent with the existing pattern but might surprise a reader unfamiliar with the UoW approach.
3. `WorkflowEndpointTests.PutRenewalAssignment_AsDistributionUser_Returns403` constructs a separate `HttpClient` with DistributionUser-only roles, demonstrating the role-gate enforcement path.

## Verdict

No blocking correctness, layering, or maintainability defects found. The implementation is well-structured, follows project conventions, and is covered by targeted tests.
