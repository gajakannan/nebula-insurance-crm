# F0003-S0003: Delete Task

**Story ID:** F0003-S0003
**Feature:** F0003 — Task Center + Reminders (API-only MVP)
**Title:** Soft delete a task (self-assigned)
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, Distribution Manager, or Admin
**I want** to delete my own tasks
**So that** I can remove tasks that are no longer relevant

## Context & Background

Tasks are soft-deleted to preserve audit history while removing them from active views. Soft-deleted tasks are excluded from `GET /my/tasks` results and nudge calculations per ADR-003. The `IsDeleted` flag and `DeletedByUserId`/`DeletedAt` fields are set on soft delete, following the project-wide soft-delete pattern.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user who owns the task
- **When** they submit `DELETE /tasks/{taskId}`
- **Then** the task is soft-deleted (IsDeleted = true, DeletedAt = current UTC, DeletedByUserId = authenticated user's UserId)
- **And** the API returns HTTP 204 (no response body)

**Authorization — Ownership Enforcement:**
- **Given** the task's `AssignedToUserId` does not match the authenticated user's UserId
- **When** delete is attempted
- **Then** the API returns HTTP 403 with ProblemDetails (code: `forbidden`)

**Authorization — External User Denied:**
- **Given** the authenticated user has the ExternalUser role
- **When** they attempt to delete a task
- **Then** the API returns HTTP 403

**Audit:**
- **Given** a task is successfully soft-deleted
- **Then** a `TaskDeleted` ActivityTimelineEvent is appended with EntityType = `Task` and EntityId = the deleted task's Id

**Post-Deletion Visibility:**
- **Given** a task has been soft-deleted
- **Then** it no longer appears in `GET /my/tasks` results
- **And** it is excluded from overdue task nudge calculations (`GET /dashboard/nudges`)

**Alternative Flows / Edge Cases:**

- **Task not found:** `DELETE /tasks/{taskId}` where `taskId` does not exist returns HTTP 404 with ProblemDetails
- **Already-deleted task:** `DELETE /tasks/{taskId}` where the task has already been soft-deleted returns HTTP 404 (soft-deleted tasks are not visible to the API)
- **Invalid taskId format:** Non-UUID `taskId` path parameter returns HTTP 400 with ProblemDetails
- **Delete a completed (Done) task:** Allowed. A task in any status (Open, InProgress, Done) can be soft-deleted.

## Data Requirements

**Required Fields:**
- `taskId` (uuid, path parameter): The ID of the task to delete

## Role-Based Visibility

**Roles that can delete tasks:**
- DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin — own tasks only (authorization-matrix.md §2.6)

**Roles denied:**
- ExternalUser — HTTP 403 (Task data is InternalOnly)

**Data Visibility:**
- Task data is InternalOnly. No external access in MVP.
- Soft-deleted tasks are excluded from all active views and queries.

## Non-Functional Expectations

- Security: Enforce task ownership at the API boundary; server-side validation is authoritative
- Reliability: Return ProblemDetails (RFC 7807) for authorization and not-found errors
- Data integrity: Soft delete preserves the task record and full audit trail

## Dependencies

**Depends On:**
- F0003-S0001 — Create Task (task must exist to be deleted)

**Related Stories:**
- F0003-S0002 — Update Task
- F0001-S0003 — View My Tasks (excludes soft-deleted tasks)
- F0001-S0005 — View Nudge Cards (excludes soft-deleted tasks from overdue nudges)

## Out of Scope

- Hard delete (data is always preserved via soft delete)
- Deleting tasks assigned to other users (deferred to F0004)
- Undelete / restore functionality (future)

## Questions & Assumptions

**Assumptions (validated):**
- There is no undo/restore for soft delete in MVP — confirmed, deferred to future.
- Deleting a task in any status (Open, InProgress, Done) is permitted.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (404 not found, already-deleted, invalid taskId format, delete in any status)
- [ ] Permissions enforced (own tasks only; ExternalUser denied)
- [ ] Audit/timeline logged (`TaskDeleted` event appended)
- [ ] ProblemDetails error responses for all 400/403/404 cases
- [ ] Soft-deleted tasks excluded from `GET /my/tasks` and nudge calculations
- [ ] Tests pass (unit, integration, authorization)
- [ ] Story filename matches `Story ID` prefix (`F0003-S0003-...`)
- [ ] Story index regenerated

## Implementation Guidance (Architect)

**Full technical spec:** [feature-assembly-plan.md](./feature-assembly-plan.md) — Step 3

### No New DTOs
Delete takes no request body. taskId comes from path parameter.

### Service Method
- `TaskService.DeleteAsync(Guid taskId, ICurrentUserService user, CancellationToken ct)` → `bool`
- Fetch → 404 if not found (query filter already excludes soft-deleted)
- Ownership guard → 403
- Set: `IsDeleted = true`, `DeletedAt = now`, `DeletedByUserId = user.UserId`
- Set: `UpdatedAt = now`, `UpdatedByUserId = user.UserId`

### Endpoint
- `app.MapDelete("/tasks/{taskId:guid}", DeleteTask).WithTags("Tasks").RequireAuthorization()`

### Casbin
- Resource: `task`, Action: `delete`, Condition: `r.obj.assignee == r.sub.id`
- Hydrate from DB entity (run after fetch)

### Timeline Event
- EventType: `TaskDeleted`, EntityType: `Task`, BrokerDescription: `null` (InternalOnly)
- Payload: empty object (per `activity-event-payloads.schema.json` → TaskDeleted definition)
- Description: `"Task deleted"` (pre-rendered)

### JSON Schema
- No request schema (DELETE has no body)
