# Feature Assembly Plan — F0003: Task Center (API-only MVP)

**Created:** 2026-03-19
**Author:** Architect Agent
**Status:** Approved

## Overview

F0003 adds three write endpoints for self-assigned task management: POST, PUT, DELETE. The domain entity (`TaskItem`), EF Core configuration, read endpoints, read DTOs, and repository read methods already exist. This plan covers only the write additions.

## Build Order

| Step | Story | Endpoint | Rationale |
|------|-------|----------|-----------|
| 1 | F0003-S0001 | `POST /tasks` | Foundation — introduces write DTOs, service write methods, repository AddAsync, and timeline event emission pattern for tasks |
| 2 | F0003-S0002 | `PUT /tasks/{taskId}` | Builds on S0001 — adds status state machine, optimistic concurrency, multiple event types |
| 3 | F0003-S0003 | `DELETE /tasks/{taskId}` | Simplest — soft delete using existing BaseEntity pattern |

## Existing Code (Read-Only — No Changes)

| File | Purpose |
|------|---------|
| `Nebula.Domain/Entities/TaskItem.cs` | Domain entity (already has AssignedToUserId as Guid) |
| `Nebula.Domain/Entities/BaseEntity.cs` | Audit fields, soft delete, RowVersion (xmin) |
| `Nebula.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs` | EF Core config with indexes, concurrency token, query filter |
| `Nebula.Application/DTOs/TaskDto.cs` | Response DTO (maps to Task schema) |
| `Nebula.Application/DTOs/TaskSummaryDto.cs` | Dashboard summary DTO |

---

## Step 1 — F0003-S0001: Create Task

### New Files

| File | Layer |
|------|-------|
| `Nebula.Application/DTOs/TaskCreateRequestDto.cs` | Application |

### Modified Files

| File | Change |
|------|--------|
| `Nebula.Application/Interfaces/ITaskRepository.cs` | Add `AddAsync` method |
| `Nebula.Infrastructure/Repositories/TaskRepository.cs` | Implement `AddAsync` |
| `Nebula.Application/Services/TaskService.cs` | Add `CreateAsync` method |
| `Nebula.Api/Endpoints/TaskEndpoints.cs` | Register `POST /tasks` |

### DTO

```csharp
// Nebula.Application/DTOs/TaskCreateRequestDto.cs
public record TaskCreateRequestDto(
    string Title,
    string? Description,
    string? Priority,           // Default: "Normal" in service
    DateTime? DueDate,
    Guid AssignedToUserId,
    string? LinkedEntityType,
    Guid? LinkedEntityId);
```

### Repository Interface Addition

```csharp
// Add to ITaskRepository
Task AddAsync(TaskItem task, CancellationToken ct = default);
```

### Service Method

```
TaskService.CreateAsync(TaskCreateRequestDto dto, ICurrentUserService user, CancellationToken ct)
→ returns TaskDto
```

**Logic flow:**
1. Self-assignment guard: `dto.AssignedToUserId != user.UserId` → throw/return 403
2. Validate LinkedEntityType: if provided, must be in `{"Broker", "Account", "Submission", "Renewal"}`
3. Validate LinkedEntityType/LinkedEntityId pairing: both present or both null
4. Construct `TaskItem` entity:
   - `Status = "Open"`, `Priority = dto.Priority ?? "Normal"`
   - `CreatedAt = UpdatedAt = DateTime.UtcNow`
   - `CreatedByUserId = UpdatedByUserId = user.UserId`
5. `taskRepo.AddAsync(task)`
6. Emit `TaskCreated` timeline event (see Timeline section)
7. `SaveChangesAsync` (via UnitOfWork or DbContext)
8. Return `MapToDto(task)`

### Endpoint Registration

```csharp
app.MapPost("/tasks", CreateTask)
    .WithTags("Tasks").RequireAuthorization();
```

### Casbin Enforcement

- Resource: `task`, Action: `create`
- Hydrate attrs: `{ assignee = dto.AssignedToUserId, subjectId = user.UserId }`
- Policy condition: `r.obj.assignee == r.sub.id`
- Follow existing pattern from `GetTaskById` in TaskEndpoints.cs

### Timeline Event

- EventType: `TaskCreated`
- EntityType: `Task`, EntityId: `task.Id`
- EventDescription: `$"Task \"{task.Title}\" created"` (pre-rendered)
- BrokerDescription: `null` (task events are InternalOnly)
- EventPayloadJson: per `activity-event-payloads.schema.json` → TaskCreated definition
- ActorUserId: `user.UserId`, ActorDisplayName: `user.DisplayName`

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `TaskDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 401 | — | Unauthenticated |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or self-assignment violation |

---

## Step 2 — F0003-S0002: Update Task

### New Files

| File | Layer |
|------|-------|
| `Nebula.Application/DTOs/TaskUpdateRequestDto.cs` | Application |

### Modified Files

| File | Change |
|------|--------|
| `Nebula.Application/Services/TaskService.cs` | Add `UpdateAsync` method |
| `Nebula.Api/Endpoints/TaskEndpoints.cs` | Register `PUT /tasks/{taskId}` |

### DTO

```csharp
// Nebula.Application/DTOs/TaskUpdateRequestDto.cs
public record TaskUpdateRequestDto(
    string? Title,
    string? Description,
    string? Status,
    string? Priority,
    DateTime? DueDate,
    Guid? AssignedToUserId);
```

**DueDate nullable handling:** JSON `null` means "clear DueDate". JSON absence means "no change". The endpoint handler must distinguish these — use `JsonDocument` or a presence-tracking wrapper at the API layer. The service method should receive a flag or `Optional<DateTime?>` to differentiate.

### Status Transition State Machine

```
Valid transitions:
  Open → InProgress     (start work)
  InProgress → Open     (pause / return to backlog)
  InProgress → Done     (complete)
  Done → Open           (reopen)
  Done → InProgress     (reopen to active)

Invalid:
  Open → Done           → HTTP 409 (invalid_status_transition)

Same-state (e.g. Open → Open):
  Treated as no status change → TaskUpdated event
```

### Service Method

```
TaskService.UpdateAsync(Guid taskId, TaskUpdateRequestDto dto, ICurrentUserService user, CancellationToken ct)
→ returns TaskDto? (null = 404)
```

**Logic flow:**
1. Fetch task: `taskRepo.GetByIdAsync(taskId)` → null = 404
2. Ownership guard: `task.AssignedToUserId != user.UserId` → 403
3. If `dto.AssignedToUserId` provided and `!= user.UserId` → 403
4. If `dto.Status` provided and different from current:
   - Validate transition against state machine → invalid = 409 (`invalid_status_transition`)
   - If transitioning to Done: `task.CompletedAt = DateTime.UtcNow`
   - If transitioning from Done: `task.CompletedAt = null`
5. Apply non-null DTO fields to entity
6. `task.UpdatedAt = DateTime.UtcNow`, `task.UpdatedByUserId = user.UserId`
7. Emit timeline event (see below)
8. `SaveChangesAsync` — catch `DbUpdateConcurrencyException` → 409 (`concurrency_conflict`)
9. Return `MapToDto(task)`

### Timeline Events (conditional)

| Condition | EventType | Payload |
|-----------|-----------|---------|
| Status changed to Done | `TaskCompleted` | `{ completedAt }` |
| Status changed from Done | `TaskReopened` | `{ previousCompletedAt }` |
| No status change (or same status) | `TaskUpdated` | `{ changedFields: { field: { from, to } } }` |

All use: EntityType=`Task`, EntityId=`task.Id`, BrokerDescription=`null`

### Endpoint Registration

```csharp
app.MapPut("/tasks/{taskId:guid}", UpdateTask)
    .WithTags("Tasks").RequireAuthorization();
```

### Casbin Enforcement

- Resource: `task`, Action: `update`
- Hydrate from DB: `{ assignee = task.AssignedToUserId, subjectId = user.UserId }`
- Run AFTER fetching the task (need entity for attrs)

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `TaskDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Empty payload, invalid field values |
| 401 | — | Unauthenticated |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or ownership violation |
| 404 | ProblemDetails (`not_found`) | Task doesn't exist or soft-deleted |
| 409 | ProblemDetails (`invalid_status_transition`) | Forbidden transition (Open → Done) |
| 409 | ProblemDetails (`concurrency_conflict`) | Optimistic concurrency failure (xmin) |

---

## Step 3 — F0003-S0003: Delete Task

### No New Files

### Modified Files

| File | Change |
|------|--------|
| `Nebula.Application/Services/TaskService.cs` | Add `DeleteAsync` method |
| `Nebula.Api/Endpoints/TaskEndpoints.cs` | Register `DELETE /tasks/{taskId}` |

### Service Method

```
TaskService.DeleteAsync(Guid taskId, ICurrentUserService user, CancellationToken ct)
→ returns bool (false = 404)
```

**Logic flow:**
1. Fetch task: `taskRepo.GetByIdAsync(taskId)` → null = 404 (query filter already excludes soft-deleted)
2. Ownership guard: `task.AssignedToUserId != user.UserId` → 403
3. Set soft delete fields:
   - `task.IsDeleted = true`
   - `task.DeletedAt = DateTime.UtcNow`
   - `task.DeletedByUserId = user.UserId`
   - `task.UpdatedAt = DateTime.UtcNow`
   - `task.UpdatedByUserId = user.UserId`
4. Emit `TaskDeleted` timeline event (empty payload, description: "Task deleted")
5. `SaveChangesAsync`
6. Return true

### Endpoint Registration

```csharp
app.MapDelete("/tasks/{taskId:guid}", DeleteTask)
    .WithTags("Tasks").RequireAuthorization();
```

### Casbin Enforcement

- Resource: `task`, Action: `delete`
- Hydrate from DB: `{ assignee = task.AssignedToUserId, subjectId = user.UserId }`

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 204 No Content | — | Success |
| 401 | — | Unauthenticated |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or ownership violation |
| 404 | ProblemDetails (`not_found`) | Task doesn't exist or already soft-deleted |

---

## Integration Checkpoints

### After S0001 (Create)
- [ ] `POST /tasks` returns 201 with valid TaskDto
- [ ] Self-assignment violation returns 403
- [ ] ExternalUser returns 403 (no Casbin policy)
- [ ] Invalid LinkedEntityType returns 400
- [ ] `TaskCreated` timeline event appears in `GET /timeline/events?entityType=Task`
- [ ] Created task appears in `GET /my/tasks`

### After S0002 (Update)
- [ ] `PUT /tasks/{id}` returns 200 with updated TaskDto
- [ ] Status Open→InProgress→Done cycle works with correct CompletedAt handling
- [ ] Open→Done returns 409 (`invalid_status_transition`)
- [ ] Concurrent update returns 409 (`concurrency_conflict`)
- [ ] Empty payload returns 400
- [ ] Correct timeline event type emitted per status change

### After S0003 (Delete)
- [ ] `DELETE /tasks/{id}` returns 204
- [ ] Deleted task excluded from `GET /my/tasks`
- [ ] Deleted task excluded from nudge calculations
- [ ] `TaskDeleted` timeline event emitted
- [ ] Second delete of same task returns 404

### Cross-Story Verification
- [ ] Full lifecycle: Create → Update status → Complete → Reopen → Delete
- [ ] All Casbin policies enforced (all 6 internal roles + ExternalUser denied)
- [ ] Timeline events for full lifecycle are correct and ordered
- [ ] Optimistic concurrency prevents lost updates
- [ ] ProblemDetails format consistent with existing endpoints (code + traceId)

## JSON Serialization Convention

C# `AssignedToUserId` (Guid) serializes to JSON `assignedToUserId` (string, uuid format) via System.Text.Json camelCase naming policy. This is consistent with existing DTOs (TaskDto already uses `Guid AssignedToUserId`).

## No ADR Required

ADR-003 already covers the Task entity design, indexes, audit requirements, and nudge engine integration. No new architectural decisions are introduced by F0003 — the write endpoints follow established SOLUTION-PATTERNS.md conventions.

## No Migration Required

The Tasks table, indexes, and query filter already exist. No schema changes needed for F0003.
