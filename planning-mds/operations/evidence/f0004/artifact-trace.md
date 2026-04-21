# F0004 Artifact Trace

**Feature:** F0004 — Task Center UI + Manager Assignment
**Last Updated:** 2026-03-22

## Planning Artifacts

| Artifact | Path | Status |
|----------|------|--------|
| PRD | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/PRD.md` | Final |
| STATUS | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/STATUS.md` | Updated 2026-03-22 |
| IMPLEMENTATION-CONTRACT | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/IMPLEMENTATION-CONTRACT.md` | Final |
| GETTING-STARTED | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/GETTING-STARTED.md` | Final |
| F0004-S0001 | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/F0004-S0001-task-list-api-endpoint.md` | Final |
| F0004-S0002 | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/F0004-S0002-user-search-api-endpoint.md` | Final |
| F0004-S0003 | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/F0004-S0003-cross-user-task-authorization.md` | Final |
| F0004-S0004 | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/F0004-S0004-task-center-list-and-filter-ui.md` | Final |
| F0004-S0005 | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/F0004-S0005-task-create-edit-ui-with-assignment.md` | Final |
| F0004-S0006 | `planning-mds/features/archive/F0004-task-center-ui-and-assignment/F0004-S0006-task-detail-panel-and-mobile-view.md` | Final |

## Security Artifacts

| Artifact | Path | Status |
|----------|------|--------|
| Authorization Matrix (§2.6a, §2.6b) | `planning-mds/security/authorization-matrix.md` | Updated for F0004 |
| Casbin policy.csv | `planning-mds/security/policies/policy.csv` | Updated for F0004 |
| Casbin model.conf | `planning-mds/security/policies/model.conf` | Unchanged |

## API Artifacts

| Artifact | Path | Status |
|----------|------|--------|
| OpenAPI spec v0.4.0 | `planning-mds/api/nebula-api.yaml` | Updated for F0004 |
| Task schema | `planning-mds/schemas/task.schema.json` | Updated for F0004 |
| Task create request schema | `planning-mds/schemas/task-create-request.schema.json` | Final |
| Task update request schema | `planning-mds/schemas/task-update-request.schema.json` | Final |

## Evidence Artifacts

| Artifact | Path | Date |
|----------|------|------|
| Implementation plan | `planning-mds/operations/evidence/f0004/plan-2026-03-21.md` | 2026-03-21 |
| Implementation evidence | `planning-mds/operations/evidence/f0004/implementation-2026-03-22.md` | 2026-03-22 |
| Code review | `planning-mds/operations/evidence/f0004/code-review-2026-03-22.md` | 2026-03-22 |
| QE evidence | `planning-mds/operations/evidence/f0004/qe-2026-03-22.md` | 2026-03-22 |
| Security review | `planning-mds/operations/evidence/f0004/security-2026-03-22.md` | 2026-03-22 |
| Architect review | `planning-mds/operations/evidence/f0004/architect-2026-03-22.md` | 2026-03-22 |
| PM closeout | `planning-mds/operations/evidence/f0004/pm-closeout-2026-03-23.md` | 2026-03-23 |
| Artifact trace | `planning-mds/operations/evidence/f0004/artifact-trace.md` | 2026-03-23 |

## Implementation Files — Backend

### New Files
| File | Story |
|------|-------|
| `engine/src/Nebula.Application/DTOs/TaskListQuery.cs` | S0001 |
| `engine/src/Nebula.Application/DTOs/TaskListItemDto.cs` | S0001 |
| `engine/src/Nebula.Application/DTOs/TaskListResponseDto.cs` | S0001 |
| `engine/src/Nebula.Application/DTOs/UserSummaryDto.cs` | S0002 |
| `engine/src/Nebula.Application/DTOs/UserSearchResponseDto.cs` | S0002 |
| `engine/src/Nebula.Application/Interfaces/IUserProfileRepository.cs` | S0002, S0003 |
| `engine/src/Nebula.Infrastructure/Repositories/UserProfileRepository.cs` | S0002 |
| `engine/src/Nebula.Application/Services/UserService.cs` | S0002 |
| `engine/src/Nebula.Api/Endpoints/UserEndpoints.cs` | S0002 |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260322184705_F0004_AddTaskAndUserProfileIndexes.cs` | S0001 |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260322184705_F0004_AddTaskAndUserProfileIndexes.Designer.cs` | S0001 |

### Modified Files
| File | Story | Change |
|------|-------|--------|
| `engine/src/Nebula.Application/DTOs/TaskDto.cs` | S0003 | Added display name fields |
| `engine/src/Nebula.Application/Interfaces/ITaskRepository.cs` | S0001 | Added GetTaskListAsync |
| `engine/src/Nebula.Infrastructure/Repositories/TaskRepository.cs` | S0001 | Implemented GetTaskListAsync, assignedByMe filter fix |
| `engine/src/Nebula.Application/Services/TaskService.cs` | S0001, S0003 | Major: list API, creator access, status/reassign guards |
| `engine/src/Nebula.Infrastructure/Authorization/CasbinAuthorizationService.cs` | S0003 | CasbinObject creator field + sentinel |
| `engine/src/Nebula.Api/Endpoints/TaskEndpoints.cs` | S0001 | GET /tasks handler, error code handling |
| `engine/src/Nebula.Api/Helpers/ProblemDetailsHelper.cs` | S0003 | 4 new error factories |
| `engine/src/Nebula.Api/Program.cs` | S0002 | UserService DI + endpoints |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | S0002 | IUserProfileRepository registration |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs` | S0001 | Index configuration |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/UserProfileConfiguration.cs` | S0002 | Index configuration |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` | S0001 | Snapshot update |

## Implementation Files — Frontend

### New Files
| File | Story |
|------|-------|
| `experience/src/pages/TaskCenterPage.tsx` | S0004 |
| `experience/src/features/tasks/components/TaskCenterList.tsx` | S0004 |
| `experience/src/features/tasks/components/TaskFilterToolbar.tsx` | S0004 |
| `experience/src/features/tasks/components/TaskCreateModal.tsx` | S0005 |
| `experience/src/features/tasks/components/TaskDetailPanel.tsx` | S0006 |
| `experience/src/features/tasks/components/AssigneePicker.tsx` | S0005 |
| `experience/src/features/tasks/hooks/useTaskList.ts` | S0004 |
| `experience/src/features/tasks/hooks/useTaskMutations.ts` | S0005 |
| `experience/src/features/tasks/hooks/useUserSearch.ts` | S0005 |

### Modified Files
| File | Story | Change |
|------|-------|--------|
| `experience/src/features/tasks/types.ts` | S0004 | 10 new types, linkedEntityName fix |
| `experience/src/features/tasks/index.ts` | S0004 | Barrel exports |
| `experience/src/App.tsx` | S0004 | /tasks routes |
| `experience/src/components/layout/Sidebar.tsx` | S0004 | Tasks nav item |
| `experience/src/lib/navigation.ts` | S0004 | Route registration |

## Test Files

| File | Change |
|------|--------|
| `engine/tests/Nebula.Tests/Unit/TaskServiceTests.cs` | 13 new F0004 tests, stub updates |
| `engine/tests/Nebula.Tests/Integration/TaskEndpointTests.cs` | 16 new F0004 tests, multi-user helpers |
