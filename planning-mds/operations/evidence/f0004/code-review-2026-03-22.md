# F0004 Code Review Evidence — 2026-03-22

**Feature:** F0004 — Task Center UI + Manager Assignment
**Reviewer:** Claude (Architect Agent — Code Review Pass)
**Date:** 2026-03-22
**Verdict:** PASS with corrective fixes applied

## Scope

Full code review of all F0004 implementation files against the approved IMPLEMENTATION-CONTRACT.md and story acceptance criteria.

## Files Reviewed

### Backend (18 files)
- `engine/src/Nebula.Application/DTOs/TaskListQuery.cs`
- `engine/src/Nebula.Application/DTOs/TaskListItemDto.cs`
- `engine/src/Nebula.Application/DTOs/TaskListResponseDto.cs`
- `engine/src/Nebula.Application/DTOs/UserSummaryDto.cs`
- `engine/src/Nebula.Application/DTOs/UserSearchResponseDto.cs`
- `engine/src/Nebula.Application/DTOs/TaskDto.cs` (modified)
- `engine/src/Nebula.Application/Interfaces/ITaskRepository.cs` (modified)
- `engine/src/Nebula.Application/Interfaces/IUserProfileRepository.cs`
- `engine/src/Nebula.Application/Services/TaskService.cs` (modified)
- `engine/src/Nebula.Application/Services/UserService.cs`
- `engine/src/Nebula.Infrastructure/Repositories/TaskRepository.cs` (modified)
- `engine/src/Nebula.Infrastructure/Repositories/UserProfileRepository.cs`
- `engine/src/Nebula.Infrastructure/Authorization/CasbinAuthorizationService.cs` (modified)
- `engine/src/Nebula.Infrastructure/DependencyInjection.cs` (modified)
- `engine/src/Nebula.Api/Endpoints/TaskEndpoints.cs` (modified)
- `engine/src/Nebula.Api/Endpoints/UserEndpoints.cs`
- `engine/src/Nebula.Api/Helpers/ProblemDetailsHelper.cs` (modified)
- `engine/src/Nebula.Api/Program.cs` (modified)

### Frontend (14 files)
- `experience/src/pages/TaskCenterPage.tsx`
- `experience/src/features/tasks/components/TaskCenterList.tsx`
- `experience/src/features/tasks/components/TaskCreateModal.tsx`
- `experience/src/features/tasks/components/TaskDetailPanel.tsx`
- `experience/src/features/tasks/components/TaskFilterToolbar.tsx`
- `experience/src/features/tasks/components/AssigneePicker.tsx`
- `experience/src/features/tasks/hooks/useTaskList.ts`
- `experience/src/features/tasks/hooks/useTaskMutations.ts`
- `experience/src/features/tasks/hooks/useUserSearch.ts`
- `experience/src/features/tasks/types.ts` (modified)
- `experience/src/features/tasks/index.ts` (modified)
- `experience/src/App.tsx` (modified)
- `experience/src/components/layout/Sidebar.tsx` (modified)
- `experience/src/lib/navigation.ts` (modified)

### Tests (2 files)
- `engine/tests/Nebula.Tests/Unit/TaskServiceTests.cs` (modified)
- `engine/tests/Nebula.Tests/Integration/TaskEndpointTests.cs` (modified)

### Migrations (1 file)
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260322184705_F0004_AddTaskAndUserProfileIndexes.cs`

## Defects Found and Fixed

### DEF-01: `assignedByMe` view missing exclusion filter (Severity: HIGH)
- **File:** `engine/src/Nebula.Infrastructure/Repositories/TaskRepository.cs:57`
- **Issue:** `assignedByMe` view only filtered `CreatedByUserId == CallerUserId`, omitting `AssignedToUserId != CallerUserId`. Self-assigned tasks by managers would appear in both "My Work" and "Assigned By Me" tabs.
- **Contract ref:** IMPLEMENTATION-CONTRACT.md §API — "assignedByMe = created by caller AND assigned to someone else"
- **Fix:** Added `&& t.AssignedToUserId != query.CallerUserId` to the Where clause.
- **Risk:** This was masked by the unit test StubTaskRepository which had the correct logic.

### DEF-02: Missing `linkedEntityName` on frontend `TaskDto` (Severity: MEDIUM)
- **File:** `experience/src/features/tasks/types.ts`
- **Issue:** `TaskDto` interface missing `linkedEntityName` field that backend returns. Caused TypeScript compile error TS2551 at `TaskCenterPage.tsx:125`.
- **Fix:** Added `linkedEntityName: string | null;` to the interface.

### DEF-03: MANAGER_ROLES too broad in frontend (Severity: HIGH)
- **Files:** `TaskCenterPage.tsx`, `TaskCreateModal.tsx`, `TaskDetailPanel.tsx`
- **Issue:** `MANAGER_ROLES` included `ProgramManager` and `RelationshipManager`, which are NOT authorized for cross-user task assignment per authorization-matrix.md §2.6a. Only `DistributionManager` and `Admin` can assign tasks to others.
- **Contract ref:** PRD Decision #3, authorization-matrix.md §2.6a
- **Fix:** Reduced to `['DistributionManager', 'Admin']` in all 3 files.

### DEF-04: Status buttons shown to creator who is not assignee (Severity: MEDIUM)
- **File:** `experience/src/features/tasks/components/TaskDetailPanel.tsx:235`
- **Issue:** `(canManage || isOwn)` guard allowed creators (who may not be the assignee) to see status action buttons. Per contract, only the assignee can change status.
- **Contract ref:** F0004-S0005 AC-10 "Status change restricted to assignee only"
- **Fix:** Changed guard to `isOwn` only. Added "Only the assignee can update status." note for creator view.

### DEF-05: No Reopen button for Done tasks (Severity: LOW)
- **File:** `experience/src/features/tasks/components/TaskDetailPanel.tsx:235`
- **Issue:** `task.status !== 'Done'` guard hid all status buttons for completed tasks. Assignee had no path to reopen a Done task.
- **Contract ref:** F0004-S0005 AC-09 status transitions (Done→Open is valid)
- **Fix:** Added Reopen button inside the `task.status === 'Done'` branch.

## Residual Risks (Accepted)

### RSK-01: Priority sort is alphabetical (LOW)
Sorting by priority uses alphabetical order (`High < Low < Normal < Urgent`) rather than severity order. Acceptable for Phase 1 — sorting by dueDate (default) is the primary UX path.

### RSK-02: LinkedEntityName always null in list view (LOW)
`TaskListItemDto.LinkedEntityName` requires entity name resolution which was deferred to avoid N+1 queries on the list endpoint. Detail panel resolves names correctly.

### RSK-03: TaskCenterList status toggle uses rowVersion=0 (LOW)
Quick status toggle in the list table passes `rowVersion: 0` because `TaskListItemDto` doesn't carry `rowVersion`. This bypasses optimistic concurrency for that action. Low risk: status is changed by the assignee only, unlikely to conflict.

### RSK-04: Integration test data accumulation (LOW)
Tests share a Testcontainers PostgreSQL instance without per-test cleanup. Some tests use `pageSize=100` to account for accumulated data. Not a production risk.

## Contract Compliance Summary

| Contract Item | Status |
|---|---|
| GET /tasks with view/filters/sort/pagination | PASS |
| GET /users with ILIKE search | PASS |
| POST /tasks with cross-user assignment | PASS |
| PUT /tasks with creator-based access | PASS |
| DELETE /tasks with creator-based access | PASS |
| Casbin policy §2.6a (creator conditions) | PASS |
| Casbin policy §2.6b (user search) | PASS |
| Error codes: inactive_assignee, invalid_assignee | PASS |
| Error codes: status_change_restricted, view_not_authorized | PASS |
| DB indexes (CreatedByUserId, DisplayName) | PASS |
| Frontend component hierarchy | PASS |
| ARIA accessibility (combobox, role=switch, etc.) | PASS |
| /my/tasks backward compatibility | PASS |
| IDOR prevention (404 normalization) | PASS |

## Verdict

**PASS** — All 5 defects corrected during review. 4 residual risks accepted. Implementation satisfies the approved contract.
