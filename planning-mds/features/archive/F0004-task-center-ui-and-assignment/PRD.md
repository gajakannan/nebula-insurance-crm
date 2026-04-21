---
template: feature
version: 1.1
applies_to: product-manager
---

# F0004: Task Center UI + Manager Assignment

**Feature ID:** F0004
**Feature Name:** Task Center UI + Manager Assignment
**Priority:** High
**Phase:** Phase 1

## Feature Statement

**As a** Distribution Manager, Admin, or any internal user
**I want** a dedicated Task Center screen with task list, filters, detail view, and the ability for managers to assign tasks to team members
**So that** I can manage my own follow-ups, see team workload, coordinate assignments, and track completion across insurance workflows

## Business Objective

- **Goal:** Elevate tasks from a dashboard-widget sidebar concern to a first-class operational screen, enabling team-level task management and cross-user assignment.
- **Metric:** Tasks assigned across users and completion rates by team. Reduction in missed follow-ups measured by overdue task count trend.
- **Baseline:** Task creation is self-assigned API-only (F0003 MVP). No UI exists. No cross-user assignment. Dashboard widget shows max 10 own tasks.
- **Target:** Full Task Center UI with list, filters, detail, create/edit. Managers can assign and reassign tasks to any active internal user within their scope. All roles see "My Work" as their default view.

## Problem Statement

- **Current State:** Tasks exist only as API entities with a small dashboard widget. Users cannot create tasks from the UI, cannot see team workload, and managers cannot assign or reassign tasks.
- **Desired State:** A dedicated `/tasks` route with list/filter/detail views. All internal users manage their own tasks through the UI. DistributionManager and Admin can assign tasks to others, see tasks they assigned, and reassign tasks within scope.
- **Impact:** Eliminates spreadsheet-driven task tracking, reduces missed renewal follow-ups and broker callbacks, and gives managers real-time visibility into team workload.

## Scope & Boundaries

**In Scope:**
- Task Center UI — dedicated route `/tasks` with list, filter, and detail panel
- "My Work" default view (tasks assigned to me, Open/InProgress)
- "Assigned By Me" view (tasks I created and assigned to others — manager/admin only)
- Create task from UI (self-assigned for all roles; assign-to-other for manager/admin)
- Edit task from UI (own tasks for all; manager/admin can edit tasks they created)
- Complete, reopen, soft delete tasks from UI
- Assignee lookup/search against UserProfile for manager/admin assignment
- Filter by: status, priority, due date range, overdue flag, assignee, linked entity type, created by
- Sort by: due date, priority, created date, status
- Server-side pagination
- Task detail side panel (drawer) on desktop; full-page on mobile
- Reassignment (manager/admin can change assignee on tasks they created)
- Inline status toggle (Open ↔ InProgress ↔ Done) in list view
- Backward-compatible `/my/tasks` endpoint preserved for dashboard widget
- Audit timeline events for all new operations (assign, reassign)

**Out of Scope:**
- Bulk actions (explicitly deferred to Phase 2 — see Deferred Decisions)
- Out-of-office / backup coverage (explicitly deferred to Phase 2)
- Automated task creation rules engine
- Notification/alert system (push, email, in-app)
- External user (BrokerUser) task creation or mutation
- Kanban/board view (list view only in Phase 1)
- Task comments/notes
- Task attachments
- Recurring tasks

## Product Decisions

### 1. Task Center Mental Model

The Task Center uses a **tabbed list model** with two views:

| Tab | Who Sees It | Contents |
|-----|------------|----------|
| **My Work** (default) | All internal roles | Tasks where `AssignedToUserId = me`. Default filter: Open + InProgress. |
| **Assigned By Me** | DistributionManager, Admin | Tasks where `CreatedByUserId = me AND AssignedToUserId ≠ me`. Shows all statuses. |

**Rationale:** Insurance follow-up workflows are personal-queue-first. Distribution users, underwriters, and relationship managers need to see their own tasks immediately. Managers need a secondary view of what they delegated. This maps to Salesforce's "My Open Activities" + "Delegated Activities" pattern and Vertafore's suspense/task split. A flat "All In Scope" or "Team Queue" view is deferred — it requires region-scoped task visibility infrastructure that adds complexity without clear Phase 1 value.

### 2. Assignment Model

| Action | Who Can Do It | Scope |
|--------|--------------|-------|
| Create task assigned to self | All internal roles | Own UserId only |
| Create task assigned to another user | DistributionManager, Admin | Any active internal user |
| Reassign task | DistributionManager, Admin | Tasks they created (`CreatedByUserId = me`) only |
| Edit task fields (title, description, priority, dueDate) | Assignee (own task) | Tasks where `AssignedToUserId = me` |
| Edit task fields | Creator (manager/admin) | Tasks where `CreatedByUserId = me` |
| Change status (complete, reopen) | Assignee only | Tasks where `AssignedToUserId = me` |
| Soft delete | Creator or Assignee | Creator: tasks they created. Assignee: tasks assigned to self. |

**Key rules:**
- Assignees **can** edit manager-assigned tasks: title, description, priority, dueDate. They **cannot** reassign.
- Assignees **are the only ones** who can change status (complete/reopen). Managers cannot complete tasks on behalf of assignees.
- Managers **cannot** reassign tasks they did not create. If a manager needs to redirect a task created by another user, they must create a new task and soft-delete the old one (or ask Admin).
- Admin has the same assignment/reassignment powers as DistributionManager, not broader (no "reassign any task" super-power in Phase 1).

**Rationale:** This balances accountability (only the person doing the work marks it done) with managerial coordination (managers can create, delegate, and redirect their own assignments). It avoids the complexity of scope-based reassignment for tasks created by others, which requires a full "team queue" concept.

### 3. Visibility Rules

| Role | "My Work" | "Assigned By Me" | Task Detail |
|------|-----------|-------------------|-------------|
| DistributionUser | Own tasks | Hidden (tab not shown) | Own tasks only |
| DistributionManager | Own tasks | Tasks they created + assigned to others | Own tasks + tasks they created |
| Underwriter | Own tasks | Hidden | Own tasks only |
| RelationshipManager | Own tasks | Hidden | Own tasks only |
| ProgramManager | Own tasks | Hidden | Own tasks only |
| Admin | Own tasks | Tasks they created + assigned to others | Own tasks + tasks they created |
| ExternalUser | DENY | DENY | DENY |
| BrokerUser | DENY | DENY | DENY |

**Visibility is ownership-based**, not region-based or team-based. A user can see:
1. Tasks assigned to them (`AssignedToUserId = me`)
2. Tasks they created (`CreatedByUserId = me`)

Region-based team visibility (e.g., "see all tasks for my region") is explicitly deferred.

### 4. Assignee Lookup

When a DistributionManager or Admin creates/reassigns a task, they need to search for the target user.

**Endpoint:** `GET /users?q={search}&activeOnly=true` (new)
- Searches `UserProfile.DisplayName` and `UserProfile.Email` (case-insensitive substring)
- Returns only active users (`IsActive = true`)
- Returns: `userId`, `displayName`, `email`, `roles[]`
- Max 20 results
- No role filtering in Phase 1 (managers can assign to any active internal user)

**Display in assignee picker:**
- Show: DisplayName, Email, primary Role badge
- Typeahead with 300ms debounce, minimum 2 characters
- Selected assignee shown as avatar chip (initials + display name)

**Inactive user behavior:**
- A task assigned to a user who is later deactivated remains visible in the deactivated user's "My Work" if they can still authenticate.
- The assignee display shows `DisplayName` normally but with an "(Inactive)" suffix badge if `IsActive = false`.
- Managers cannot assign or reassign tasks to inactive users. The assignee picker excludes inactive users.
- Existing tasks assigned to inactive users are not automatically reassigned. Manual reassignment by the creator (manager/admin) is required.

### 5. Task Aging, Overdue, and Prioritization

**Overdue logic** (unchanged from ADR-003):
- A task is overdue if `DueDate < today AND Status ≠ Done AND IsDeleted = false`
- Tasks without a DueDate are never overdue
- Overdue tasks display a red "Overdue" badge with days-overdue count

**Priority levels** (unchanged): Low, Normal, High, Urgent

**Default sort in "My Work":** Overdue first (sorted by days overdue descending), then by DueDate ascending (soonest due first), then by Priority descending (Urgent > High > Normal > Low), then by CreatedAt ascending (oldest first). Null DueDate sorts last.

**Default sort in "Assigned By Me":** Status (Open first, InProgress second, Done last), then by DueDate ascending, then by Priority descending.

### 6. Filters

All filters are query-parameter-based, composable, and persisted in URL for shareability.

| Filter | Type | Values | Available In |
|--------|------|--------|-------------|
| Status | Multi-select | Open, InProgress, Done | Both tabs |
| Priority | Multi-select | Low, Normal, High, Urgent | Both tabs |
| Due Date Range | Date range picker | from/to dates | Both tabs |
| Overdue | Toggle | true/false | Both tabs |
| Assignee | Typeahead select | UserProfile search | Assigned By Me only |
| Linked Entity Type | Multi-select | Broker, Account, Submission, Renewal | Both tabs |
| Created By | Typeahead select | UserProfile search | My Work (useful if Admin) |

**"My Work" default filter:** Status = Open, InProgress (Done hidden by default, toggle to show)

### 7. UI Structure

**Route:** `/tasks`

**Desktop layout (≥1024px):**
- Left: Task list with filters toolbar, sortable column headers, pagination
- Right: Detail side panel (drawer) — opens on row click, shows full task details with edit capability
- Create button in header opens a create form (modal or inline in drawer)

**Tablet layout (768–1023px):**
- Full-width list, detail opens as overlay drawer from right

**Mobile layout (<768px):**
- Full-width list, row tap navigates to full-page detail view
- Back button returns to list
- Create via FAB (floating action button)

**Empty states:**
- "My Work" empty: "No tasks yet. Create your first task to get started." + Create button
- "Assigned By Me" empty: "You haven't assigned any tasks to your team yet." + Create button
- Filtered empty: "No tasks match your filters." + Clear filters link

**Loading state:** Skeleton rows (5 placeholder rows with shimmer animation)

**Error state:** Inline error banner with retry button. Does not block page load — partial data shown if available.

**Pagination:** Server-side, 20 items per page default. Previous/Next + page number indicators.

### 8. Backward Compatibility

- `GET /my/tasks` — **preserved unchanged**. Dashboard widget continues to use this endpoint. Returns Open/InProgress tasks assigned to authenticated user, sorted by DueDate ascending, default limit 10.
- `POST /tasks` — **relaxed self-assignment constraint** for DistributionManager and Admin. Other roles still enforce `assignedToUserId = authenticated user`.
- `PUT /tasks/{taskId}` — **extended visibility**. Assignee can update own tasks (unchanged). Creator (if DistributionManager/Admin) can also update tasks they created.
- `DELETE /tasks/{taskId}` — **extended visibility**. Creator can delete tasks they created. Assignee can delete tasks assigned to them.
- New: `GET /tasks` — full paginated list with filters (new endpoint).
- New: `GET /users?q=...` — user search for assignee picker (new endpoint).

### 9. Audit / Timeline Behavior

| Operation | EventType | Payload | Who Can Trigger |
|-----------|-----------|---------|----------------|
| Create (self-assigned) | TaskCreated | title, assignedTo, dueDate, linkedEntity | All internal roles |
| Create (assigned to other) | TaskCreated | title, assignedTo, createdBy, dueDate, linkedEntity | DistributionManager, Admin |
| Update fields | TaskUpdated | changedFields map | Assignee or Creator |
| Complete | TaskCompleted | completedAt | Assignee only |
| Reopen | TaskReopened | previousCompletedAt | Assignee only |
| Reassign | TaskReassigned | previousAssignee, newAssignee, reassignedBy | Creator (DistributionManager/Admin) |
| Soft delete | TaskDeleted | deletedBy | Assignee or Creator |

**New event type: `TaskReassigned`** — appended when `AssignedToUserId` changes on an existing task. Captures both old and new assignee.

### 10. Bulk Actions — Deferred Decision

Bulk actions (multi-select + batch status change, batch reassign, batch delete) are **explicitly deferred to Phase 2**.

**Rationale:** Phase 1 establishes the core assignment model and validates it with real usage. Bulk operations add complexity in partial-failure handling, audit granularity, and UX around selection state. Insurance task volumes per manager (typically 20-50 active) do not require bulk operations for initial viability.

### 11. Out-of-Office / Backup Coverage — Deferred Decision

Out-of-office/backup coverage is **explicitly deferred to Phase 2**.

**Rationale:** Vertafore AMS360 provides an "Out of Office Assistant" that temporarily redirects renewal tasks to a backup user. This is valuable but requires: (a) a coverage period model on UserProfile, (b) automatic reassignment logic, (c) restoration logic when the user returns. This is a significant feature in its own right and is not required for Phase 1 viability. Manual reassignment by managers covers the immediate need.

## Success Criteria

1. All internal users can create, view, update, complete, and delete their own tasks from the Task Center UI.
2. DistributionManager and Admin can create tasks assigned to other active internal users.
3. DistributionManager and Admin can reassign tasks they created.
4. Task Center displays correctly on desktop, tablet, and mobile.
5. Filters, sort, and pagination work correctly.
6. Audit timeline captures all task operations including assignment and reassignment.
7. Dashboard widget (`/my/tasks`) continues to function unchanged.
8. All ABAC policies enforce ownership-based access.

## Risks & Assumptions

- **Risk:** Managers may expect region-scoped team visibility. **Mitigation:** Document this as Phase 2 and provide the "Assigned By Me" tab as the Phase 1 alternative.
- **Risk:** Inactive-user tasks may accumulate. **Mitigation:** "Assigned By Me" view lets managers see stale assignments; manual reassignment is available.
- **Assumption:** UserProfile table already contains all active internal users via claims normalization (F0005). No separate user provisioning is needed.
- **Assumption:** F0003 task CRUD endpoints are implemented and functional.
- **Assumption:** Task status state machine (Open → InProgress → Done, with reopen) is unchanged from F0003.

## Dependencies

- F0003 Task CRUD endpoints (implemented, archived)
- F0005 authentik + UserProfile + UserId (implemented, archived)
- F0015 Frontend quality gates + test infrastructure (implemented, archived)
- UserProfile table with DisplayName, Email, IsActive, Roles (exists per ADR-006)

## Related User Stories

- F0004-S0001 — Task Center List + Filter UI
- F0004-S0002 — Task Create + Edit UI with Assignment
- F0004-S0003 — Task List API Endpoint
- F0004-S0004 — User Search API Endpoint
- F0004-S0005 — Cross-User Task Authorization (Assign + Reassign)
- F0004-S0006 — Task Detail Panel + Mobile Detail View

## Deferred Decisions (Phase 2)

| Decision | Reason for Deferral |
|----------|-------------------|
| Bulk actions | Validate single-task workflow first; insurance task volumes manageable |
| Out-of-office / backup coverage | Requires coverage period model, auto-reassignment, restoration logic |
| Region-scoped team view ("All In Scope") | Requires region-based task visibility queries; validate need with usage data |
| Kanban/board view | List view sufficient for Phase 1; board requires drag-drop status changes |
| Task comments/notes | Task description field sufficient for Phase 1 |
| Notification system | Email/push for task assignment; requires notification infrastructure |
