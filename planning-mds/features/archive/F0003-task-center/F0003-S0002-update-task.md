# F0003-S0002: Update Task

**Story ID:** F0003-S0002
**Feature:** F0003 — Task Center + Reminders (API-only MVP)
**Title:** Update a task (self-assigned)
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, Distribution Manager, or Admin
**I want** to update my own tasks
**So that** I can keep task details and status accurate

## Context & Background

Tasks evolve as work progresses. MVP supports self-updates to status, due date, and details without enabling cross-user task changes. Status transitions between Open, InProgress, and Done drive timeline events and affect nudge calculations.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user who owns the task
- **When** they submit `PUT /tasks/{taskId}` with at least one updatable field
- **Then** the task is updated and the API returns HTTP 200 with the full updated Task resource

**Authorization — Ownership Enforcement:**
- **Given** the task's `AssignedToUserId` does not match the authenticated user's UserId
- **When** update is attempted
- **Then** the API returns HTTP 403 with ProblemDetails (code: `forbidden`)

**Authorization — External User Denied:**
- **Given** the authenticated user has the ExternalUser role
- **When** they attempt to update a task
- **Then** the API returns HTTP 403

**Status Transition — Complete:**
- **Given** the update changes `status` to `Done`
- **Then** `CompletedAt` is set to the current UTC timestamp
- **And** a `TaskCompleted` ActivityTimelineEvent is appended

**Status Transition — Reopen:**
- **Given** the update changes `status` from `Done` to `Open` or `InProgress`
- **Then** `CompletedAt` is cleared (set to null)
- **And** a `TaskReopened` ActivityTimelineEvent is appended

**Status Transition — Invalid (Open → Done):**
- **Given** the task currently has `status = Open`
- **When** the update attempts to change `status` directly to `Done`
- **Then** the API returns HTTP 409 with ProblemDetails (code: `invalid_status_transition`)
- **Note:** Valid transitions are: Open → InProgress, InProgress → Done, InProgress → Open, Done → Open, Done → InProgress

**General Update (no status change):**
- **Given** a task update that does not change the `status` field
- **Then** a `TaskUpdated` ActivityTimelineEvent is appended

**AssignedToUserId Validation on Update:**
- **Given** `assignedToUserId` is included in the update payload
- **When** its value does not match the authenticated user's UserId
- **Then** the API returns HTTP 403 with ProblemDetails (code: `forbidden`)

**Alternative Flows / Edge Cases:**

- **Task not found:** `PUT /tasks/{taskId}` where `taskId` does not exist or has been soft-deleted returns HTTP 404 with ProblemDetails
- **Empty update payload:** Request body with zero properties returns HTTP 400 with ProblemDetails (code: `validation_error`; at least one field is required)
- **Concurrent update conflict:** If the task has been modified since the client last read it (optimistic concurrency via xmin), the API returns HTTP 409 with ProblemDetails (code: `conflict`)
- **Title exceeds 255 characters:** API returns HTTP 400 with ProblemDetails (code: `validation_error`)
- **Description exceeds 2000 characters:** API returns HTTP 400 with ProblemDetails (code: `validation_error`)
- **Invalid status value:** Status not in [Open, InProgress, Done] returns HTTP 400
- **Invalid status transition (Open → Done):** Direct transition from Open to Done is not allowed. Returns HTTP 409 with ProblemDetails (code: `invalid_status_transition`). Task must transition through InProgress first.
- **Invalid priority value:** Priority not in [Low, Normal, High, Urgent] returns HTTP 400
- **Invalid taskId format:** Non-UUID `taskId` path parameter returns HTTP 400

## Data Requirements

**Updatable Fields:**
- `title` (string, max 255)
- `description` (string, max 2000)
- `status` (string): Open, InProgress, Done
- `priority` (string): Low, Normal, High, Urgent
- `dueDate` (date, nullable — can be set or cleared)
- `assignedToUserId` (uuid): If provided, must equal the authenticated user's UserId

**Validation Rules:**
- Payload must contain at least one updatable field (minProperties: 1)
- `assignedToUserId`, if provided, must equal the authenticated user's UserId
- Field length and enum constraints apply per Data Requirements
- All validation errors return ProblemDetails (RFC 7807) with `code` and `traceId`

## Role-Based Visibility

**Roles that can update tasks:**
- DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin — own tasks only (authorization-matrix.md §2.6)

**Roles denied:**
- ExternalUser — HTTP 403 (Task data is InternalOnly)

**Data Visibility:**
- Task data is InternalOnly. No external access in MVP.

## Non-Functional Expectations

- Security: Enforce task ownership at the API boundary; server-side validation is authoritative
- Reliability: Return ProblemDetails (RFC 7807) for validation, authorization, and conflict errors
- Concurrency: Support optimistic concurrency to prevent silent overwrites

## Dependencies

**Depends On:**
- F0003-S0001 — Create Task (task must exist to be updated)

**Related Stories:**
- F0003-S0003 — Delete Task
- F0001-S0003 — View My Tasks (dashboard reflects updated task state)
- F0001-S0005 — View Nudge Cards (status/DueDate changes affect nudge eligibility)

## Out of Scope

- Reassigning tasks to other users (deferred to F0004)
- Bulk task updates
- Updating LinkedEntityType/LinkedEntityId (immutable after creation in MVP; see Assumptions)

## Questions & Assumptions

**Assumptions (validated):**
- Status transitions follow a defined state machine: Open → InProgress → Done, with reopening allowed (Done → Open, Done → InProgress) and pausing allowed (InProgress → Open). Direct Open → Done is rejected with HTTP 409 — confirmed.
- LinkedEntityType and LinkedEntityId are immutable after creation in MVP (not included in update payload per current OpenAPI contract)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (empty payload, 404 not found, concurrent conflict, validation errors, invalid taskId)
- [ ] Permissions enforced (own tasks only; ExternalUser denied)
- [ ] Audit/timeline logged (`TaskUpdated`, `TaskCompleted`, `TaskReopened` events as appropriate)
- [ ] ProblemDetails error responses for all 400/403/404/409 cases
- [ ] Optimistic concurrency enforced
- [ ] Tests pass (unit, integration, authorization, concurrency)
- [ ] Story filename matches `Story ID` prefix (`F0003-S0002-...`)
- [ ] Story index regenerated

## Implementation Guidance (Architect)

**Full technical spec:** [feature-assembly-plan.md](./feature-assembly-plan.md) — Step 2

### New DTO
- `TaskUpdateRequestDto` (record): Title?, Description?, Status?, Priority?, DueDate?, AssignedToUserId?
- **DueDate nullable handling:** JSON `null` = clear DueDate. JSON absence = no change. Use presence-tracking wrapper at API layer.

### Service Method
- `TaskService.UpdateAsync(Guid taskId, TaskUpdateRequestDto dto, ICurrentUserService user, CancellationToken ct)` → `TaskDto?`
- Fetch → 404 if not found
- Ownership guard → 403
- Status transition validation against state machine → 409 (`invalid_status_transition`) for Open→Done
- CompletedAt set/cleared on Done transitions
- Catch `DbUpdateConcurrencyException` → 409 (`concurrency_conflict`)

### Status State Machine
```
Open → InProgress    ✓ (start)
InProgress → Open    ✓ (pause)
InProgress → Done    ✓ (complete) → set CompletedAt
Done → Open          ✓ (reopen)  → clear CompletedAt
Done → InProgress    ✓ (reopen)  → clear CompletedAt
Open → Done          ✗ REJECTED  → 409
```

### Endpoint
- `app.MapPut("/tasks/{taskId:guid}", UpdateTask).WithTags("Tasks").RequireAuthorization()`

### Casbin
- Resource: `task`, Action: `update`, Condition: `r.obj.assignee == r.sub.id`
- Hydrate from DB entity (run after fetch)

### Timeline Events (conditional)
| Condition | EventType | Payload Schema |
|-----------|-----------|----------------|
| Status → Done | `TaskCompleted` | `{ completedAt }` |
| Status from Done → Open/InProgress | `TaskReopened` | `{ previousCompletedAt }` |
| No status change | `TaskUpdated` | `{ changedFields: { field: { from, to } } }` |

### JSON Schema
- Validated against: `planning-mds/schemas/task-update-request.schema.json`
