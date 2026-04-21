# F0004-S0005: Task Create + Edit UI with Assignment

**Story ID:** F0004-S0005
**Feature:** F0004 — Task Center UI + Manager Assignment
**Title:** Task create and edit UI with assignee picker for managers
**Priority:** High
**Phase:** Phase 1

## User Story

**As a** Distribution Manager or Admin
**I want** to create tasks assigned to other users and edit tasks I created, including reassignment
**So that** I can delegate follow-ups and redirect work as priorities change

**As a** Distribution User, Underwriter, Relationship Manager, or Program Manager
**I want** to create tasks for myself and edit my own tasks from the Task Center UI
**So that** I don't need to use the API directly

## Context & Background

F0003 provided API-only task creation (self-assigned). This story adds the UI create/edit forms, with the assignee picker component enabling managers to assign tasks to others.

## Acceptance Criteria

### Create Task — Self-Assigned (All Roles)

- **Given** any authenticated internal user on the Task Center page
- **When** they click "Create Task"
- **Then** a create modal/drawer opens with fields: Title, Description, Priority, Due Date, Linked Entity
- **And** the Assignee field is pre-filled with the current user and read-only (for non-manager roles)
- **And** submitting creates the task and shows it in "My Work"

### Create Task — Assign to Other (Manager/Admin)

- **Given** a DistributionManager or Admin on the Task Center page
- **When** they click "Create Task"
- **Then** the Assignee field is an editable typeahead search
- **And** typing 2+ characters queries `GET /users?q=...` with 300ms debounce
- **And** results show: DisplayName, Email, Role badge
- **And** selecting a user sets `assignedToUserId` to their UserId
- **And** the creator can still select themselves (self-assign)

### Assignee Picker

- **Given** the manager types "lis" in the assignee picker
- **When** 300ms debounce completes
- **Then** a dropdown shows matching active users (e.g., "Lisa Wong — lisa.wong@nebula.com — DistributionUser")

- **Given** the search returns no results
- **Then** the dropdown shows "No matching users found"

- **Given** a user is selected
- **Then** the picker shows an avatar chip: initials circle + display name
- **And** a clear (×) button to reset the selection

### Linked Entity Picker

- **Given** the user wants to link a task to an entity
- **When** they select a Linked Entity Type (Broker, Account, Submission, Renewal)
- **Then** a search/select for that entity type appears
- **And** selecting an entity populates both `linkedEntityType` and `linkedEntityId`

- **Given** the user does not want to link an entity
- **Then** both fields remain empty (optional)

### Edit Task — Assignee (Own Task)

- **Given** a task where the user is the assignee
- **When** they click on the task to open the detail panel
- **Then** they can edit: Title, Description, Priority, Due Date
- **And** the Assignee field is read-only (assignees cannot reassign)
- **And** saving calls `PUT /tasks/{taskId}` and updates the list

### Edit Task — Creator (Manager/Admin)

- **Given** a task where the user is the creator (DistributionManager/Admin)
- **When** they open the task detail
- **Then** they can edit: Title, Description, Priority, Due Date, Assignee
- **And** changing the Assignee uses the same typeahead picker
- **And** saving calls `PUT /tasks/{taskId}` with the new `assignedToUserId`
- **And** the task moves from the old assignee's "My Work" to the new assignee's "My Work"

### Status Actions

- **Given** the user is the assignee
- **Then** they see action buttons: "Start" (Open→InProgress), "Complete" (InProgress→Done), "Reopen" (Done→Open)

- **Given** the user is the creator but not the assignee
- **Then** status action buttons are hidden, with a note: "Only the assignee can update status"

### Delete

- **Given** the user is the assignee or creator
- **When** they click "Delete" on a task
- **Then** a confirmation dialog appears: "Delete this task? This action can't be undone."
- **And** confirming calls `DELETE /tasks/{taskId}` and removes the task from the list

### Validation

- **Given** the user submits the create form with an empty title
- **Then** an inline validation error appears: "Title is required"

- **Given** the title exceeds 255 characters
- **Then** inline error: "Title must be 255 characters or less"

- **Given** server returns 422 `inactive_assignee`
- **Then** the assignee picker shows: "This user is no longer active. Please select another assignee."

- **Given** server returns 409 `concurrency_conflict`
- **Then** a toast notification: "This task was modified by someone else. Please refresh and try again."

### Edge Cases

- Creating a task with a past due date: allowed (task immediately shows as overdue)
- Creating a task with no due date: allowed
- Editing LinkedEntityType/LinkedEntityId: not allowed (immutable after creation, fields disabled in edit mode)

## Non-Functional Expectations

- Performance: Create/edit form opens within 200ms. Assignee search results within 500ms (includes 300ms debounce + 200ms API).
- Accessibility: Form labels associated with inputs, error messages linked via aria-describedby, focus management in modal/drawer
- UX: Form preserves unsaved state if user accidentally closes (confirmation prompt)

## Dependencies

- F0004-S0002: User Search API Endpoint (for assignee picker)
- F0004-S0003: Cross-User Task Authorization (backend)
- F0004-S0004: Task Center List + Filter UI (page shell)

## Definition of Done

- [ ] Create modal/drawer functional for self-assigned tasks (all roles)
- [ ] Assignee picker with typeahead search for DistributionManager/Admin
- [ ] Edit form functional for own tasks (all roles) and created tasks (manager/admin)
- [ ] Reassignment via edit works with timeline event
- [ ] Status action buttons role-aware
- [ ] Delete with confirmation dialog
- [ ] Inline validation for all fields
- [ ] Server error handling (inactive_assignee, concurrency_conflict, etc.)
- [ ] Accessibility: labels, focus management, error association
- [ ] Vitest component tests for form, picker, validation
- [ ] Playwright E2E test for create + edit + assign flow
