# F0004-S0006: Task Detail Panel + Mobile Detail View

**Story ID:** F0004-S0006
**Feature:** F0004 — Task Center UI + Manager Assignment
**Title:** Task detail side panel (desktop/tablet) and full-page detail (mobile)
**Priority:** High
**Phase:** Phase 1

## User Story

**As an** internal user viewing the Task Center
**I want** to click on a task and see its full details in a side panel (desktop) or full page (mobile)
**So that** I can review, edit, and act on tasks without losing my list context

## Context & Background

The Task Center uses a list + detail pattern. On desktop, the detail panel opens as a right-side drawer alongside the list. On mobile, it replaces the list with a full-page view and back navigation.

## Acceptance Criteria

### Desktop Detail Panel (≥1024px)

- **Given** the user clicks a task row in the list
- **Then** a detail panel slides in from the right, showing:
  - Task title (editable inline for assignee/creator)
  - Status badge with action buttons
  - Priority badge (editable)
  - Due date with date picker (editable)
  - Assignee display (with reassign picker for creator managers)
  - Description (editable textarea)
  - Linked entity link (clickable to navigate to entity)
  - Created by / Created at
  - Updated at
  - Completed at (if Done)
  - Recent timeline events for this task (last 5)

- **Given** the detail panel is open
- **When** the user clicks another task row
- **Then** the panel updates to show the newly selected task

- **Given** the detail panel is open
- **When** the user presses Escape or clicks the close (×) button
- **Then** the panel closes and focus returns to the previously selected list row

### Tablet Detail Panel (768–1023px)

- **Given** a tablet viewport
- **When** the user taps a task row
- **Then** a full-width overlay drawer slides in from the right with a semi-transparent backdrop
- **And** tapping the backdrop or the close button dismisses the drawer

### Mobile Detail View (<768px)

- **Given** a mobile viewport
- **When** the user taps a task row
- **Then** the browser navigates to `/tasks/{taskId}` (full-page detail view)
- **And** a back button/arrow in the header navigates back to `/tasks` with filters preserved

### Task Timeline Section

- **Given** the detail panel is showing a task
- **Then** a "Recent Activity" section shows the last 5 timeline events for `EntityType=Task, EntityId=taskId`
- **And** each event shows: event description, actor display name, relative time ("2 hours ago")
- **And** events are sorted by OccurredAt descending

### Linked Entity Navigation

- **Given** a task is linked to a Broker
- **When** the user clicks the linked entity name
- **Then** they navigate to the broker's 360 view

- **Given** a task's linked entity has been soft-deleted
- **Then** the linked entity shows as "[Deleted]" with no navigation link

### Inline Editing

- **Given** the user is the assignee or creator (for allowed fields)
- **When** they click an editable field (title, description, priority, dueDate)
- **Then** the field becomes an input/picker
- **And** changes are saved on blur or Enter (debounced)
- **And** a save indicator briefly appears ("Saved ✓")

- **Given** the save fails
- **Then** the field reverts to its previous value and an error toast appears

### Edge Cases

- **Given** the task is soft-deleted while the panel is open
- **Then** on next API call, display "This task has been deleted" and disable all actions

- **Given** the task is reassigned while the panel is open (by another user)
- **Then** on next fetch/refetch, if the task is no longer accessible, show "This task is no longer available" and close the panel

## Non-Functional Expectations

- Performance: Detail panel opens within 200ms (data may already be in TanStack Query cache from list)
- Accessibility: Focus trapped in panel when open, Escape closes panel, screen reader announces panel open/close
- Animation: Slide-in/out transition 200ms ease-out

## Dependencies

- F0004-S0004: Task Center List + Filter UI (list provides the shell)
- F0004-S0005: Task Create + Edit UI (inline edit reuses form components)
- Timeline events API (`GET /timeline/events?entityType=Task&entityId={taskId}`)

## Definition of Done

- [ ] Desktop side panel opens on task row click with all detail fields
- [ ] Tablet overlay drawer works with backdrop dismiss
- [ ] Mobile full-page detail view with back navigation
- [ ] Recent timeline section (last 5 events)
- [ ] Linked entity navigation with soft-delete handling
- [ ] Inline editing with save-on-blur and error handling
- [ ] Escape to close, focus management
- [ ] Accessibility: focus trap, ARIA attributes, screen reader announcements
- [ ] Vitest tests for detail panel rendering and interactions
- [ ] Playwright E2E test for detail open/edit/close flow on desktop
