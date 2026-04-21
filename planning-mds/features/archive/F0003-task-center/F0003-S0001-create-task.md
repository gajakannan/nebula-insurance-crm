# F0003-S0001: Create Task

**Story ID:** F0003-S0001
**Feature:** F0003 — Task Center + Reminders (API-only MVP)
**Title:** Create a task (self-assigned)
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, Distribution Manager, or Admin
**I want** to create a task assigned to myself
**So that** I can track follow-ups that appear on my dashboard

## Context & Background

The dashboard widgets require real tasks to exist. MVP supports self-assigned tasks only to keep scope minimal while enabling full read/write task flow via API. Read endpoints (`GET /my/tasks`, `GET /tasks/{taskId}`) already exist from F0001. This story adds the write path (`POST /tasks`).

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user
- **When** they submit `POST /tasks` with a valid payload containing at minimum `title` and `assignedToUserId`
- **Then** a new task is created with `Status = Open` and `Priority = Normal` (defaults)
- **And** the API returns HTTP 201 with the full Task resource in the response body

**Authorization — Self-Assignment Enforcement:**
- **Given** `assignedToUserId` in the request body does not equal the authenticated user's internal UserId
- **When** the request is submitted
- **Then** the API returns HTTP 403 with a ProblemDetails response (code: `forbidden`)

**Authorization — External User Denied:**
- **Given** the authenticated user has the ExternalUser role
- **When** they attempt to create a task
- **Then** the API returns HTTP 403

**Audit:**
- **Given** a task is successfully created
- **Then** a `TaskCreated` ActivityTimelineEvent is appended with EntityType = `Task` and EntityId = the new task's Id

**Alternative Flows / Edge Cases:**

- **Missing required field (title or assignedToUserId):** API returns HTTP 400 with ProblemDetails listing the missing field(s)
- **Title exceeds 255 characters:** API returns HTTP 400 with ProblemDetails (code: `validation_error`)
- **Description exceeds 2000 characters:** API returns HTTP 400 with ProblemDetails (code: `validation_error`)
- **Invalid LinkedEntityType value:** If `linkedEntityType` is provided, it must be one of: `Broker`, `Account`, `Submission`, `Renewal`. Any other value returns HTTP 400 with ProblemDetails (code: `validation_error`)
- **LinkedEntityType provided without LinkedEntityId (or vice versa):** API returns HTTP 400. Both must be provided together or both omitted.
- **Past DueDate:** Allowed. A task with a past DueDate is immediately eligible for overdue nudge computation per ADR-003.
- **DueDate not provided:** Allowed. Task is created without a due date and excluded from overdue nudge calculations.
- **Invalid Priority value:** If `priority` is provided, it must be one of: `Low`, `Normal`, `High`, `Urgent`. Any other value returns HTTP 400.

## Data Requirements

**Required Fields:**
- `title` (string, max 255): Task title
- `assignedToUserId` (uuid): Must equal the authenticated user's internal UserId per F0005 principal key pattern

**Optional Fields:**
- `description` (string, max 2000): Task detail
- `priority` (string): One of Low, Normal, High, Urgent. Defaults to Normal.
- `dueDate` (date): Optional due date
- `linkedEntityType` (string): One of Broker, Account, Submission, Renewal
- `linkedEntityId` (uuid): ID of the linked entity

**Validation Rules:**
- `title` is required and max 255 characters
- `description` max 2000 characters if provided
- `assignedToUserId` is required and must equal the authenticated user's UserId
- `linkedEntityType` and `linkedEntityId` must both be present or both absent
- `linkedEntityType` must be one of the valid entity types if provided
- All validation errors return ProblemDetails (RFC 7807) with `code` and `traceId`

## Role-Based Visibility

**Roles that can create tasks:**
- DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin — self-assigned only (authorization-matrix.md §2.6)

**Roles denied:**
- ExternalUser — HTTP 403 (Task data is InternalOnly)

**Data Visibility:**
- Task data is InternalOnly. No external access in MVP.

## Non-Functional Expectations

- Security: Enforce self-assignment at the API boundary; server-side validation is authoritative
- Reliability: Return ProblemDetails (RFC 7807) for all validation and authorization errors
- Performance: Task creation completes within 500ms (p95)

## Dependencies

**Depends On:**
- Task entity and indexes (ADR-003)
- ABAC enforcement and Casbin policy coverage (authorization-matrix.md §2.6)
- ActivityTimelineEvent append pattern (BLUEPRINT §1.4)

**Related Stories:**
- F0003-S0002 — Update Task
- F0003-S0003 — Delete Task
- F0001-S0003 — View My Tasks (read endpoint, already implemented)
- F0001-S0005 — View Nudge Cards (consumes overdue tasks from this endpoint)

## Out of Scope

- Assigning tasks to other users (deferred to F0004)
- Task creation from dashboard widget UI (deferred to F0004)
- Automated task creation (future)
- Application-level validation that LinkedEntityId references an existing, non-deleted entity (architect decision)

## Questions & Assumptions

**Assumptions (validated):**
- Past DueDate is allowed at creation time — confirmed. Task immediately appears as overdue for nudge purposes.
- Default Status is always Open on creation; status is not settable at create time
- Default Priority is Normal when not provided

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (validation errors, self-assignment enforcement, invalid LinkedEntityType, missing/partial linked entity fields)
- [ ] Permissions enforced (self-assignment only; ExternalUser denied)
- [ ] Audit/timeline logged (`TaskCreated` event appended)
- [ ] ProblemDetails error responses for all 400/403 cases
- [ ] Tests pass (unit, integration, authorization)
- [ ] Story filename matches `Story ID` prefix (`F0003-S0001-...`)
- [ ] Story index regenerated

## Implementation Guidance (Architect)

**Full technical spec:** [feature-assembly-plan.md](./feature-assembly-plan.md) — Step 1

### New DTO
- `TaskCreateRequestDto` (record): Title, Description?, Priority?, DueDate?, AssignedToUserId, LinkedEntityType?, LinkedEntityId?

### Service Method
- `TaskService.CreateAsync(TaskCreateRequestDto dto, ICurrentUserService user, CancellationToken ct)` → `TaskDto`
- Self-assignment guard: `dto.AssignedToUserId != user.UserId` → 403
- LinkedEntityType must be in `{Broker, Account, Submission, Renewal}` if provided
- LinkedEntityType and LinkedEntityId must both be present or both null
- Defaults: `Status = "Open"`, `Priority = dto.Priority ?? "Normal"`

### Repository Addition
- `ITaskRepository.AddAsync(TaskItem task, CancellationToken ct)` — add to interface and implementation

### Endpoint
- `app.MapPost("/tasks", CreateTask).WithTags("Tasks").RequireAuthorization()`

### Casbin
- Resource: `task`, Action: `create`, Condition: `r.obj.assignee == r.sub.id`
- Hydrate `obj.assignee` from request body `assignedToUserId`, `sub.id` from `user.UserId`

### Timeline Event
- EventType: `TaskCreated`, EntityType: `Task`, BrokerDescription: `null` (InternalOnly)
- Payload: per `activity-event-payloads.schema.json` → TaskCreated definition
- Description: `$"Task \"{task.Title}\" created"` (pre-rendered)

### JSON Schema
- Validated against: `planning-mds/schemas/task-create-request.schema.json`
