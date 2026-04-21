# F0004-S0003: Cross-User Task Authorization

**Story ID:** F0004-S0003
**Feature:** F0004 — Task Center UI + Manager Assignment
**Title:** Cross-user task authorization for assign, reassign, and creator-based access
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** Distribution Manager or Admin
**I want** to create tasks assigned to other users, reassign tasks I created, and view/edit those tasks
**So that** I can coordinate work across my team

## Context & Background

F0003 enforces strict self-assignment: `AssignedToUserId` must equal the authenticated user for all task operations. F0004 relaxes this constraint for DistributionManager and Admin, adding creator-based access alongside the existing assignee-based access. This story covers the backend authorization changes only — no UI.

## Acceptance Criteria

### Create — Assign to Other

**Happy Path:**
- **Given** an authenticated DistributionManager
- **When** they call `POST /tasks` with `assignedToUserId` set to another active internal user's UserId
- **Then** the task is created with `AssignedToUserId = target user` and `CreatedByUserId = authenticated user`
- **And** HTTP 201 returned with full task resource

**Self-Assignment Still Works:**
- **Given** an authenticated DistributionManager
- **When** they call `POST /tasks` with `assignedToUserId = own UserId`
- **Then** the task is created (self-assigned, same as F0003 behavior)

**Non-Manager Blocked:**
- **Given** an authenticated DistributionUser
- **When** they call `POST /tasks` with `assignedToUserId` set to another user
- **Then** HTTP 403 with ProblemDetails (code: `forbidden`)

**Inactive Assignee Blocked:**
- **Given** `assignedToUserId` references a UserProfile where `IsActive = false`
- **When** DistributionManager creates the task
- **Then** HTTP 422 with ProblemDetails (code: `inactive_assignee`)

**Non-Existent Assignee Blocked:**
- **Given** `assignedToUserId` does not match any UserProfile
- **When** DistributionManager creates the task
- **Then** HTTP 422 with ProblemDetails (code: `invalid_assignee`)

### Read — Creator Access

**Happy Path:**
- **Given** a task where `CreatedByUserId = authenticated DistributionManager` and `AssignedToUserId = another user`
- **When** the manager calls `GET /tasks/{taskId}`
- **Then** HTTP 200 with full task resource

**Non-Creator Non-Assignee Blocked:**
- **Given** a task where neither `AssignedToUserId` nor `CreatedByUserId` matches the authenticated user
- **When** the user calls `GET /tasks/{taskId}`
- **Then** HTTP 403

### Update — Creator Can Edit Fields (Not Status)

**Creator Edits Fields:**
- **Given** a task where `CreatedByUserId = authenticated DistributionManager`
- **When** they call `PUT /tasks/{taskId}` with changes to `title`, `description`, `priority`, or `dueDate`
- **Then** the task is updated and HTTP 200 returned

**Creator Cannot Change Status:**
- **Given** a task where `CreatedByUserId = authenticated DistributionManager` but `AssignedToUserId ≠ authenticated user`
- **When** they call `PUT /tasks/{taskId}` with `status` in the payload
- **Then** HTTP 403 with ProblemDetails (code: `status_change_restricted`)

**Assignee Can Still Change Status:**
- **Given** a task where `AssignedToUserId = authenticated user` (even if created by a manager)
- **When** they change `status` to `InProgress` or `Done`
- **Then** the update succeeds (unchanged from F0003 behavior)

### Reassign — Creator Changes Assignee

**Happy Path:**
- **Given** a task where `CreatedByUserId = authenticated DistributionManager`
- **When** they call `PUT /tasks/{taskId}` with `assignedToUserId = new active user`
- **Then** the task's `AssignedToUserId` is updated
- **And** a `TaskReassigned` timeline event is appended
- **And** a `TaskUpdated` timeline event is NOT appended (reassignment event replaces it)

**Assignee Cannot Reassign:**
- **Given** a task where `AssignedToUserId = authenticated user` but `CreatedByUserId ≠ authenticated user`
- **When** they attempt to change `assignedToUserId`
- **Then** HTTP 403 with ProblemDetails (code: `forbidden`)

**Reassign to Inactive User Blocked:**
- **Given** reassignment to a user where `IsActive = false`
- **Then** HTTP 422 with ProblemDetails (code: `inactive_assignee`)

### Delete — Creator Access

**Creator Can Delete:**
- **Given** a task where `CreatedByUserId = authenticated DistributionManager`
- **When** they call `DELETE /tasks/{taskId}`
- **Then** the task is soft-deleted and HTTP 204 returned
- **And** a `TaskDeleted` timeline event is appended

### Audit

- Task creation with cross-user assignment: `TaskCreated` event includes `createdByUserId`, `createdByDisplayName`, `assignedToUserId`, `assignedToDisplayName`
- Reassignment: `TaskReassigned` event with previous/new assignee and reassigner details
- All other operations: existing event types unchanged, `ActorUserId` = authenticated user

## Role-Based Visibility

| Role | Create to other | Read created | Update created | Reassign | Status change |
|------|----------------|-------------|---------------|----------|--------------|
| DistributionManager | ALLOW | ALLOW | ALLOW (not status) | ALLOW (own created) | Own tasks only |
| Admin | ALLOW | ALLOW | ALLOW (not status) | ALLOW (own created) | Own tasks only |
| All other internal | DENY | DENY | DENY | DENY | Own tasks only |

## Non-Functional Expectations

- Security: No elevation of privilege; creator access is strictly scoped to tasks they created
- Reliability: Assignee validation must check UserProfile existence and active status within the same transaction
- Concurrency: Optimistic concurrency (xmin) enforced on reassignment

## Dependencies

- F0003 task CRUD (implemented)
- F0005 UserProfile table (implemented)
- Casbin policy updates (see IMPLEMENTATION-CONTRACT.md §2)

## Implementation Guidance (Architect)

### Casbin Policy Changes
See IMPLEMENTATION-CONTRACT.md §2.1 for exact policy rows.

### Enforcement Point Changes
The `TaskService` must hydrate both `r.obj.assignee` and `r.obj.creator` before calling the Casbin enforcer. The enforcer evaluates all matching policy rows with OR semantics.

### Status Change Guard
Application-layer check (not in Casbin):
```csharp
if (dto.Status.HasValue && task.AssignedToUserId != currentUser.UserId)
    return Forbid("status_change_restricted");
```

### Assignee Validation Service
```csharp
public async Task<UserProfile?> ValidateAssigneeAsync(Guid userId)
{
    var profile = await _userProfileRepo.GetByIdAsync(userId);
    if (profile == null) throw new InvalidAssigneeException();
    if (!profile.IsActive) throw new InactiveAssigneeException();
    return profile;
}
```

## Definition of Done

- [ ] Acceptance criteria met (all create/read/update/reassign/delete scenarios)
- [ ] Self-assignment still works for all roles (backward compatible)
- [ ] Non-manager roles cannot assign to others
- [ ] Creator cannot change status on tasks assigned to others
- [ ] Assignee validation (exists + active) enforced
- [ ] Reassignment emits TaskReassigned timeline event
- [ ] Casbin policy rows added and tested
- [ ] ProblemDetails for all error codes (forbidden, inactive_assignee, invalid_assignee, status_change_restricted)
- [ ] Tests pass (unit, integration, authorization matrix coverage)
