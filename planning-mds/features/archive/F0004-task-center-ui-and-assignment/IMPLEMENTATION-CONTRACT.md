# F0004 — Implementation Contract

**Feature:** F0004 — Task Center UI + Manager Assignment
**Version:** 1.0
**Date:** 2026-03-21
**Status:** Implementation-Ready

---

## 1. API Delta from F0003

### 1.1 New Endpoints

#### `GET /tasks` — Paginated Task List

**Purpose:** Powers the Task Center list view (both "My Work" and "Assigned By Me" tabs).

```
GET /tasks?view={myWork|assignedByMe}&status={Open,InProgress,Done}&priority={Low,Normal,High,Urgent}&dueDateFrom={date}&dueDateTo={date}&overdue={true|false}&assigneeId={uuid}&linkedEntityType={Broker,Account,Submission,Renewal}&createdById={uuid}&sort={dueDate|priority|createdAt|status}&sortDir={asc|desc}&page={int}&pageSize={int}
```

**Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| view | string | myWork | `myWork`: tasks where AssignedToUserId = me. `assignedByMe`: tasks where CreatedByUserId = me AND AssignedToUserId ≠ me (requires DistributionManager or Admin role; returns 403 for other roles). |
| status | string[] | Open,InProgress (myWork); all (assignedByMe) | Comma-separated multi-select |
| priority | string[] | — | Comma-separated multi-select |
| dueDateFrom | date | — | Inclusive lower bound |
| dueDateTo | date | — | Inclusive upper bound |
| overdue | boolean | — | If true, only tasks where DueDate < today AND Status ≠ Done |
| assigneeId | uuid | — | Filter by assignee (assignedByMe view only) |
| linkedEntityType | string[] | — | Comma-separated multi-select |
| createdById | uuid | — | Filter by creator |
| sort | string | dueDate | Sort field |
| sortDir | string | asc | Sort direction |
| page | int | 1 | 1-based page number |
| pageSize | int | 20 | Items per page (max 100) |

**Response (200):**
```json
{
  "data": [TaskListItem],
  "page": 1,
  "pageSize": 20,
  "totalCount": 47,
  "totalPages": 3
}
```

**TaskListItem schema** (superset of TaskSummary):
```json
{
  "id": "uuid",
  "title": "string",
  "description": "string | null",
  "status": "Open | InProgress | Done",
  "priority": "Low | Normal | High | Urgent",
  "dueDate": "date | null",
  "assignedToUserId": "uuid",
  "assignedToDisplayName": "string",
  "createdByUserId": "uuid",
  "createdByDisplayName": "string",
  "linkedEntityType": "string | null",
  "linkedEntityId": "uuid | null",
  "linkedEntityName": "string | null",
  "isOverdue": "boolean",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "completedAt": "datetime | null"
}
```

**Authorization:**
- Requires authentication
- `view=myWork`: All internal roles. Returns tasks where `AssignedToUserId = authenticated user`.
- `view=assignedByMe`: DistributionManager and Admin only. Returns tasks where `CreatedByUserId = authenticated user AND AssignedToUserId ≠ authenticated user`. Other roles → 403.

**Errors:**
- 400: Invalid query parameters
- 401: Not authenticated
- 403: Role not authorized for requested view

---

#### `GET /users` — User Search (Assignee Picker)

**Purpose:** Typeahead search for task assignment target.

```
GET /users?q={search}&activeOnly={true|false}&limit={int}
```

**Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| q | string (required) | — | Min 2 chars. Searches DisplayName and Email (case-insensitive substring). |
| activeOnly | boolean | true | If true, only users where IsActive = true |
| limit | int | 20 | Max results (max 50) |

**Response (200):**
```json
{
  "users": [
    {
      "userId": "uuid",
      "displayName": "string",
      "email": "string",
      "roles": ["string"],
      "isActive": true
    }
  ]
}
```

**Authorization:**
- Requires authentication
- DistributionManager and Admin: full search access
- Other internal roles: can search users (needed for display name resolution) but limited to own userId or exact-match by userId
- ExternalUser / BrokerUser: 403

**Errors:**
- 400: Query too short (< 2 chars) or invalid parameters
- 401: Not authenticated
- 403: Not authorized

---

### 1.2 Modified Endpoints

#### `POST /tasks` — Create Task (relaxed assignment)

**Change:** Remove self-assignment enforcement for DistributionManager and Admin.

| Role | Allowed assignedToUserId |
|------|------------------------|
| DistributionUser, Underwriter, RelationshipManager, ProgramManager | Must equal authenticated user's UserId (unchanged) |
| DistributionManager, Admin | Any active internal user's UserId |

**New validation (DistributionManager/Admin):**
- `assignedToUserId` must reference an existing, active UserProfile
- If target user is inactive → 422 with code `inactive_assignee`
- If target user does not exist → 422 with code `invalid_assignee`

**New fields in TaskCreateRequest:**
- No schema changes. `assignedToUserId` already exists. The change is purely authorization logic.

**New audit behavior:**
- When `assignedToUserId ≠ createdByUserId`, the TaskCreated event payload includes `createdByUserId` and `createdByDisplayName` to distinguish delegated creation.

---

#### `PUT /tasks/{taskId}` — Update Task (extended visibility)

**Change:** Allow creator (DistributionManager/Admin) to update tasks they created, even if assigned to someone else.

**Authorization matrix for update:**

| Caller | Task relationship | Allowed fields |
|--------|------------------|---------------|
| Any role | AssignedToUserId = me | title, description, priority, dueDate, status |
| DistributionManager/Admin | CreatedByUserId = me | title, description, priority, dueDate, assignedToUserId |
| DistributionManager/Admin | CreatedByUserId = me | NOT status (only assignee can change status) |

**Reassignment via update:**
- When `assignedToUserId` is changed and caller is CreatedByUserId (DistributionManager/Admin):
  - New assignee must be active internal user (same validation as create)
  - Emit `TaskReassigned` timeline event (see §3)
  - Previous assignee loses access to the task
  - New assignee sees task in their "My Work"

**Status change restriction:**
- Only the current assignee (`AssignedToUserId = authenticated user`) can change status
- If creator (non-assignee) attempts to change status → 403 with code `status_change_restricted`

---

#### `DELETE /tasks/{taskId}` — Delete Task (extended visibility)

**Change:** Allow creator (DistributionManager/Admin) to delete tasks they created.

| Caller | Condition | Allowed |
|--------|-----------|---------|
| Assignee | AssignedToUserId = me | Yes (unchanged) |
| Creator | CreatedByUserId = me (DistributionManager/Admin) | Yes (new) |
| Other | Neither assignee nor creator | 403 |

---

### 1.3 Unchanged Endpoints

- `GET /my/tasks` — No changes. Dashboard widget backward compatibility preserved.
- `GET /tasks/{taskId}` — Authorization extended: visible to assignee OR creator.

---

## 2. Authorization / Policy Delta

### 2.1 New Casbin Policy Rules

Current F0003 policy uses `r.obj.assignee == r.sub.id` for all task actions. F0004 introduces creator-based access:

**New condition vocabulary:**
- `r.obj.assignee == r.sub.id` — unchanged (assignee match)
- `r.obj.creator == r.sub.id` — new (creator match)

**New policy rows (additions to policy.csv):**

```csv
# §2.6a Task — Manager Assignment (F0004 delta)
# DistributionManager and Admin can create tasks assigned to others
p, DistributionManager, task, create, true
p, Admin,               task, create, true

# DistributionManager and Admin can read tasks they created
p, DistributionManager, task, read, r.obj.creator == r.sub.id
p, Admin,               task, read, r.obj.creator == r.sub.id

# DistributionManager and Admin can update tasks they created
p, DistributionManager, task, update, r.obj.creator == r.sub.id
p, Admin,               task, update, r.obj.creator == r.sub.id

# DistributionManager and Admin can delete tasks they created
p, DistributionManager, task, delete, r.obj.creator == r.sub.id
p, Admin,               task, delete, r.obj.creator == r.sub.id

# New resource: user (for assignee search)
p, DistributionManager, user, search, true
p, Admin,               user, search, true
p, DistributionUser,    user, search, true
p, Underwriter,         user, search, true
p, RelationshipManager, user, search, true
p, ProgramManager,      user, search, true
```

**Existing rows (unchanged):** All `r.obj.assignee == r.sub.id` rows for task CRUD remain. The enforcer evaluates multiple matching policy rows with OR semantics — a request is allowed if **any** matching row's condition is satisfied.

### 2.2 Casbin Enforcement Logic

The enforcement point must hydrate **both** `r.obj.assignee` and `r.obj.creator` before calling the enforcer:

```
For task create:
  r.obj.assignee = request.body.assignedToUserId
  r.obj.creator  = authenticated.userId  (always the creator on create)

For task read/update/delete:
  r.obj.assignee = task.AssignedToUserId  (from DB)
  r.obj.creator  = task.CreatedByUserId   (from DB)
```

**Status change guard (application layer, not Casbin):**
Status changes on update are enforced at the application layer:
- If `status` field is present in update payload AND `task.AssignedToUserId ≠ authenticated.userId` → 403 (`status_change_restricted`)

### 2.3 Authorization Matrix Addition

New section `§2.6a Task — Manager Assignment (F0004)` to be added to `authorization-matrix.md`.

---

## 3. Audit / Timeline Event Delta

### 3.1 New Event Type: TaskReassigned

| Field | Value |
|-------|-------|
| EventType | `TaskReassigned` |
| EntityType | `Task` |
| EntityId | task.Id |
| descriptionTemplate | `"Task \"{title}\" reassigned from {previousAssigneeDisplayName} to {newAssigneeDisplayName}"` |
| BrokerDescription | `null` (InternalOnly) |

**Payload schema:**
```json
{
  "previousAssigneeUserId": "uuid",
  "previousAssigneeDisplayName": "string",
  "newAssigneeUserId": "uuid",
  "newAssigneeDisplayName": "string",
  "reassignedByUserId": "uuid",
  "reassignedByDisplayName": "string"
}
```

### 3.2 Modified Event: TaskCreated

Add optional fields for delegated creation:
```json
{
  "title": "string",
  "assignedToUserId": "uuid",
  "assignedToDisplayName": "string",
  "createdByUserId": "uuid",
  "createdByDisplayName": "string",
  "dueDate": "date | null",
  "linkedEntityType": "string | null",
  "linkedEntityId": "uuid | null"
}
```

When `assignedToUserId = createdByUserId` (self-assigned), `createdByUserId`/`createdByDisplayName` may be omitted for backward compatibility.

### 3.3 Unchanged Events

TaskUpdated, TaskCompleted, TaskReopened, TaskDeleted — no payload changes. The `ActorUserId` on the timeline event always reflects the authenticated user who performed the action.

---

## 4. Schema Delta

### 4.1 task.schema.json — Add createdByUserId

Add to required: `createdByUserId`

Add to properties:
```json
"createdByUserId": {
  "type": "string",
  "format": "uuid",
  "description": "Internal UserId (uuid) of task creator. Equals assignedToUserId for self-assigned tasks."
},
"createdByDisplayName": {
  "type": "string",
  "description": "Display name of creator. Resolved from UserProfile."
},
"assignedToDisplayName": {
  "type": "string",
  "description": "Display name of assignee. Resolved from UserProfile."
}
```

### 4.2 task-create-request.schema.json — No Changes

`assignedToUserId` already exists. The self-assignment constraint relaxation is authorization logic, not schema.

### 4.3 task-update-request.schema.json — No Changes

`assignedToUserId` already exists in the update schema. The reassignment authorization is handled at the application layer.

---

## 5. Database Delta

### 5.1 New Index

| Table | Index Name | Columns | Purpose |
|-------|-----------|---------|---------|
| Tasks | `IX_Tasks_CreatedByUserId_AssignedToUserId` | (CreatedByUserId, AssignedToUserId) WHERE IsDeleted = false | "Assigned By Me" query |

### 5.2 No New Tables

The existing `Tasks` table already has `CreatedByUserId` (inherited from BaseEntity). No new columns are required.

### 5.3 No Migration Required for Task Table

`CreatedByUserId` already exists on all task rows. The new index is the only migration.

---

## 6. Frontend Implementation Contract

### 6.1 New Route

| Route | Component | Description |
|-------|-----------|-------------|
| `/tasks` | TaskCenterPage | Main task center with tab bar, filter toolbar, list, detail panel |

### 6.2 Component Hierarchy

```
TaskCenterPage
├── TaskTabBar (My Work | Assigned By Me)
├── TaskFilterToolbar
│   ├── StatusMultiSelect
│   ├── PriorityMultiSelect
│   ├── DateRangePicker
│   ├── OverdueToggle
│   ├── AssigneeSearch (Assigned By Me tab only)
│   ├── LinkedEntityTypeMultiSelect
│   └── ClearFiltersButton
├── TaskList
│   ├── TaskListHeader (sortable columns)
│   ├── TaskListRow (repeating)
│   │   ├── StatusBadge (clickable toggle)
│   │   ├── PriorityIndicator
│   │   ├── OverdueBadge (conditional)
│   │   ├── AssigneeBadge
│   │   └── LinkedEntityLink
│   └── Pagination
├── TaskDetailPanel (drawer/side panel)
│   ├── TaskDetailHeader
│   ├── TaskEditForm
│   ├── AssigneeSection (with reassign for manager/admin creator)
│   ├── LinkedEntitySection
│   ├── TaskTimeline (recent events for this task)
│   └── ActionButtons (Complete, Reopen, Delete)
└── TaskCreateModal
    ├── TitleInput
    ├── DescriptionTextarea
    ├── PrioritySelect
    ├── DueDatePicker
    ├── AssigneePicker (self for regular users; search for manager/admin)
    ├── LinkedEntityPicker
    └── SubmitButton
```

### 6.3 State Management

- TanStack Query for all API calls
- Query keys: `['tasks', view, filters, sort, page]`, `['users', searchQuery]`, `['task', taskId]`
- Optimistic updates for status toggle (revert on error)
- URL-synced filters via `useSearchParams`

### 6.4 Accessibility Requirements

- All interactive elements keyboard-navigable
- ARIA labels on filter controls, sort headers, status toggles
- Focus management: detail panel trap focus when open; return focus on close
- Screen reader announcements for status changes, filter results count
- Color contrast meets WCAG 2.1 AA
- Overdue/priority indicators use icon + text (not color alone)

---

## 7. Error Codes (New)

| Code | HTTP Status | Trigger |
|------|-------------|---------|
| `inactive_assignee` | 422 | Assign/reassign to inactive user |
| `invalid_assignee` | 422 | Assign/reassign to non-existent user |
| `status_change_restricted` | 403 | Non-assignee attempts status change |
| `view_not_authorized` | 403 | Non-manager requests assignedByMe view |

---

## 8. Performance Budget

| Operation | Target (p95) |
|-----------|-------------|
| GET /tasks (paginated) | 300ms |
| GET /users?q=... | 200ms |
| POST /tasks (create) | 500ms |
| PUT /tasks/{taskId} (update) | 500ms |
| Task Center initial page load (FCP) | 1.5s |
| Filter change (re-render) | 300ms |
