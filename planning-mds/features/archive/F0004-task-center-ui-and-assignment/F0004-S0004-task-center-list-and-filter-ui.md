# F0004-S0004: Task Center List + Filter UI

**Story ID:** F0004-S0004
**Feature:** F0004 — Task Center UI + Manager Assignment
**Title:** Task Center list view with tabs, filters, sort, and pagination
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, Distribution Manager, or Admin
**I want** a dedicated Task Center page with a filterable, sortable, paginated task list
**So that** I can see all my tasks in one place and find specific tasks quickly

## Context & Background

The dashboard My Tasks widget shows up to 10 Open/InProgress tasks. The Task Center provides the full task management experience with all statuses, rich filters, and multiple views.

## Acceptance Criteria

### Route + Tab Navigation

- **Given** a user navigates to `/tasks`
- **Then** the Task Center page loads with the "My Work" tab selected by default
- **And** the URL reflects the current tab: `/tasks?view=myWork`

- **Given** a DistributionManager or Admin
- **Then** they see both "My Work" and "Assigned By Me" tabs
- **And** clicking "Assigned By Me" loads that view and updates URL to `/tasks?view=assignedByMe`

- **Given** a DistributionUser, Underwriter, RelationshipManager, or ProgramManager
- **Then** they see only the "My Work" tab (no "Assigned By Me" tab shown)

### List Display

- **Given** tasks exist for the current view
- **Then** each row displays: status badge, title, priority indicator, due date (with overdue badge if applicable), assignee name (Assigned By Me only), linked entity name (linked), created date
- **And** columns are sortable by clicking column headers

- **Given** a task is overdue (DueDate < today, status ≠ Done)
- **Then** a red "Overdue" badge with days-overdue count is shown

- **Given** a task's linked entity has been soft-deleted
- **Then** the linked entity name shows as "[Deleted]" with muted styling

### Status Toggle

- **Given** a task in "My Work" where the user is the assignee
- **When** they click the status badge
- **Then** the status cycles: Open → InProgress → Done (following the state machine)
- **And** the toggle is optimistic (updates immediately, reverts on server error)

- **Given** a task in "Assigned By Me" where the user is the creator but not the assignee
- **Then** the status badge is read-only (no click action, tooltip: "Only the assignee can change status")

### Filters

- **Given** the filter toolbar is visible
- **When** the user selects Status = "Open" + Priority = "Urgent"
- **Then** the list updates to show only matching tasks
- **And** the URL query params update to `?view=myWork&status=Open&priority=Urgent`

- **Given** filters are applied
- **When** the user clicks "Clear filters"
- **Then** all filters reset to defaults (Status = Open,InProgress for My Work; all for Assigned By Me)

- **Given** the user applies a date range filter
- **Then** only tasks with DueDate within the range are shown (inclusive both ends)

### Sort

- **Given** the user clicks the "Due Date" column header
- **Then** tasks sort by DueDate ascending (default). Clicking again toggles to descending.
- **And** null DueDates sort last for ascending, first for descending

- **Given** the user clicks "Priority" column header
- **Then** tasks sort by priority: Urgent > High > Normal > Low (descending default)

### Pagination

- **Given** more than 20 tasks match the current view + filters
- **Then** pagination controls show at the bottom: Previous, page numbers, Next
- **And** current page and total count are displayed: "Showing 1-20 of 47"

- **Given** the user navigates to page 2
- **Then** URL updates with `&page=2` and the list updates

### Empty States

- **Given** "My Work" has zero tasks
- **Then** empty state shows: "No tasks yet. Create your first task to get started." with a "Create Task" button

- **Given** "Assigned By Me" has zero tasks
- **Then** empty state shows: "You haven't assigned any tasks to your team yet." with a "Create Task" button

- **Given** filters produce zero results
- **Then** message: "No tasks match your filters." with a "Clear filters" link

### Loading + Error States

- **Given** the page is loading
- **Then** 5 skeleton rows with shimmer animation are displayed

- **Given** the API returns an error
- **Then** an inline error banner appears: "Failed to load tasks. Please try again." with a "Retry" button
- **And** previously loaded data remains visible if available

### Responsive Behavior

- **Desktop (≥1024px):** Side-by-side list + detail panel
- **Tablet (768–1023px):** Full-width list, detail as overlay drawer from right
- **Mobile (<768px):** Full-width list with condensed rows, tap navigates to full-page detail

## Non-Functional Expectations

- Performance: Initial render within 1.5s (FCP), filter changes within 300ms
- Accessibility: All elements keyboard-navigable, ARIA labels, screen reader announcements for filter results and status changes
- URL-based state: Filters, sort, page, and view persisted in URL for bookmarking/sharing

## Dependencies

- F0004-S0001: Task List API Endpoint
- F0015: Frontend quality gates + test infrastructure
- TanStack Query, React Hook Form (existing stack)

## Definition of Done

- [ ] Route `/tasks` renders Task Center page
- [ ] Tab navigation works (My Work / Assigned By Me, role-conditional)
- [ ] List displays all required columns with correct data
- [ ] Status toggle works with optimistic updates
- [ ] All filters functional and URL-synced
- [ ] Sort by all columns works
- [ ] Pagination functional
- [ ] Empty/loading/error states implemented
- [ ] Responsive across desktop/tablet/mobile breakpoints
- [ ] Accessibility: keyboard navigation, ARIA labels, color contrast
- [ ] Vitest component tests for list, filters, pagination
- [ ] Playwright E2E test for happy path (load, filter, sort, paginate)
