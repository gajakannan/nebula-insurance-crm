# F0004-S0001: Task List API Endpoint

**Story ID:** F0004-S0001
**Feature:** F0004 — Task Center UI + Manager Assignment
**Title:** Paginated task list API with filters and views
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** Distribution User, Underwriter, Relationship Manager, Program Manager, Distribution Manager, or Admin
**I want** a paginated, filterable API endpoint that returns my tasks (and tasks I assigned for managers)
**So that** the Task Center UI can display a complete, sortable, filterable list

## Context & Background

F0003 provides `GET /my/tasks` (dashboard widget, max 50 items, Open/InProgress only). The Task Center needs a full-featured list endpoint supporting multiple views, all statuses, filtering, sorting, and pagination. The existing `/my/tasks` endpoint is preserved for backward compatibility.

## Acceptance Criteria

**Happy Path — My Work View:**
- **Given** an authenticated internal user
- **When** they call `GET /tasks?view=myWork`
- **Then** the API returns a paginated list of tasks where `AssignedToUserId = authenticated user`, defaulting to page 1, pageSize 20
- **And** each item includes `assignedToDisplayName`, `createdByDisplayName`, `linkedEntityName`, and `isOverdue`

**Happy Path — Assigned By Me View:**
- **Given** an authenticated DistributionManager or Admin
- **When** they call `GET /tasks?view=assignedByMe`
- **Then** the API returns tasks where `CreatedByUserId = authenticated user AND AssignedToUserId ≠ authenticated user`

**Authorization — View Restriction:**
- **Given** an authenticated user who is NOT DistributionManager or Admin
- **When** they call `GET /tasks?view=assignedByMe`
- **Then** the API returns HTTP 403 with ProblemDetails (code: `view_not_authorized`)

**Filtering:**
- **Given** any valid filter combination (status, priority, dueDateFrom, dueDateTo, overdue, assigneeId, linkedEntityType, createdById)
- **When** applied as query parameters
- **Then** results are filtered accordingly (AND composition)

**Sorting:**
- **Given** `sort=dueDate&sortDir=asc` (or priority, createdAt, status)
- **When** the request is made
- **Then** results are sorted accordingly. Null dueDate sorts last for ascending, first for descending.

**Pagination:**
- **Given** `page=2&pageSize=10`
- **When** the request is made
- **Then** the response includes `data`, `page`, `pageSize`, `totalCount`, `totalPages`

**Overdue Filter:**
- **Given** `overdue=true`
- **When** the request is made
- **Then** only tasks where `DueDate < today AND Status ≠ Done AND IsDeleted = false` are returned

**Linked Entity Name Resolution:**
- **Given** a task with `linkedEntityType=Broker` and `linkedEntityId={brokerId}`
- **When** the task appears in results
- **Then** `linkedEntityName` is resolved to the broker's `LegalName`. If the linked entity is soft-deleted, `linkedEntityName` = `"[Deleted]"`. If the linked entity does not exist, `linkedEntityName` = `null`.

**Edge Cases:**
- Empty result: returns `{"data": [], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0}`
- Invalid sort field: returns HTTP 400
- pageSize > 100: clamped to 100
- page beyond total: returns empty data array with correct totalCount
- Default view (no `view` param): defaults to `myWork`

## Data Requirements

**Response schema:** See IMPLEMENTATION-CONTRACT.md §1.1 TaskListItem.

## Role-Based Visibility

- All internal roles: `view=myWork` (own tasks)
- DistributionManager, Admin: `view=myWork` + `view=assignedByMe`
- ExternalUser, BrokerUser: 403

## Non-Functional Expectations

- Performance: 300ms p95 for paginated queries
- Security: ABAC enforced server-side; no cross-user data leakage
- Reliability: ProblemDetails for all error responses

## Dependencies

- F0003 task table and indexes
- UserProfile table (for display name joins)
- New index: `IX_Tasks_CreatedByUserId_AssignedToUserId`

## Implementation Guidance (Architect)

### Query Pattern (My Work)
```sql
SELECT t.*, up_assignee.DisplayName, up_creator.DisplayName,
       COALESCE(b.LegalName, a.Name, ...) AS LinkedEntityName,
       (t.DueDate < CURRENT_DATE AND t.Status != 'Done') AS IsOverdue
FROM Tasks t
  LEFT JOIN UserProfile up_assignee ON up_assignee.UserId = t.AssignedToUserId
  LEFT JOIN UserProfile up_creator ON up_creator.UserId = t.CreatedByUserId
  LEFT JOIN Brokers b ON t.LinkedEntityType = 'Broker' AND t.LinkedEntityId = b.Id
  LEFT JOIN Accounts a ON t.LinkedEntityType = 'Account' AND t.LinkedEntityId = a.Id
  -- ... (Submission, Renewal joins)
WHERE t.AssignedToUserId = @userId AND t.IsDeleted = false
  -- + dynamic filter clauses
ORDER BY ... -- dynamic sort
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
```

### Endpoint
- `app.MapGet("/tasks", ListTasks).WithTags("Tasks").RequireAuthorization()`

### Casbin
- Resource: `task`, Action: `read`
- View-level authorization at application layer (check role for `assignedByMe`)
- Row-level: query already filters to own/created tasks

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Filtering, sorting, and pagination functional
- [ ] Authorization enforced (view restriction, row-level)
- [ ] ProblemDetails for all error responses
- [ ] Linked entity name resolution with soft-delete handling
- [ ] Performance within budget (300ms p95)
- [ ] Tests pass (unit, integration, authorization)
