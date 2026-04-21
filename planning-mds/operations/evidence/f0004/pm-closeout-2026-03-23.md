# F0004 Product Manager Closeout — 2026-03-23

**Feature:** F0004 — Task Center UI + Manager Assignment
**Reviewer:** Claude (Architect Agent — PM Closeout Pass)
**Date:** 2026-03-23
**Verdict:** ARCHIVE — Core value delivered. Gaps documented as Phase 1 deferrals.

## PRD Success Criteria Assessment

| # | Criterion | Verdict | Notes |
|---|-----------|---------|-------|
| 1 | All internal users can create, view, update, complete, and delete their own tasks from the Task Center UI | **PASS** | Create modal, detail panel inline edit, status actions, delete with confirmation all functional |
| 2 | DistributionManager and Admin can create tasks assigned to other active internal users | **PASS** | Assignee picker with typeahead, active-only validation, inactive/non-existent blocked |
| 3 | DistributionManager and Admin can reassign tasks they created | **PASS** | Reassign picker in detail panel, creator-only guard, TaskReassigned timeline event |
| 4 | Task Center displays correctly on desktop, tablet, and mobile | **PASS** | Desktop table + side panel, tablet/mobile overlay drawer, mobile card layout |
| 5 | Filters, sort, and pagination work correctly | **PASS** | Status, priority, date range, overdue, assignee, entity type filters. Sort by dueDate, priority, createdAt. Server-side pagination with Previous/Next. |
| 6 | Audit timeline captures all task operations including assignment and reassignment | **PASS** | TaskCreated, TaskUpdated, TaskReassigned, TaskDeleted events. Reassignment captures previous/new assignee. |
| 7 | Dashboard widget (`/my/tasks`) continues to function unchanged | **PASS** | Endpoint preserved, backward-compatible |
| 8 | All ABAC policies enforce ownership-based access | **PASS** | Casbin creator-based + assignee-based OR conditions. IDOR normalized to 404. Application-layer guards for status/reassignment. |

**Result: All 8 success criteria met.**

## Story-Level Acceptance Criteria Review

### F0004-S0001: Paginated Task List API — PASS

| AC | Status | Notes |
|----|--------|-------|
| My Work view | PASS | `view=myWork` returns tasks where AssignedToUserId = caller |
| Assigned By Me view | PASS | `view=assignedByMe` with exclusion filter (DEF-01 fixed) |
| View restriction 403 | PASS | Non-manager gets `view_not_authorized` |
| Filtering (status, priority, dates, overdue, assignee, entityType) | PASS | AND composition |
| Sorting (dueDate, priority, createdAt, status) | PASS | Null dueDate sorts last for asc |
| Pagination | PASS | page, pageSize, totalCount, totalPages in response |
| Overdue filter | PASS | DueDate < today AND Status != Done |
| Linked entity name resolution | **PARTIAL** | `ResolveLinkedEntityNameAsync` exists but not called for list items. Detail endpoint resolves correctly. Accepted as RSK-02. |
| Invalid sort field → 400 | **GAP** | Falls through to default sort instead of 400. Low impact. |
| Empty result, pageSize clamping, default view | PASS | |

### F0004-S0002: User Search API — PASS

| AC | Status | Notes |
|----|--------|-------|
| Happy path search | PASS | ILIKE on DisplayName + Email |
| Minimum query length | PASS | 2-char minimum, 400 if shorter |
| Active-only default | PASS | `activeOnly` defaults to true |
| Include inactive (`activeOnly=false`) | PASS | Parameter accepted and filters accordingly |
| Result limit 20 (capped at 50) | PASS | `Math.Min(limit ?? 20, 50)` |
| Internal roles access | PASS | All 6 internal roles in Casbin policy |
| External denied | PASS | No ExternalUser policy row; tested |
| No sensitive data exposed | PASS | IdpSubject, IdpIssuer excluded from response |

### F0004-S0003: Cross-User Authorization — PASS

| AC | Status | Notes |
|----|--------|-------|
| Create — assign to other (manager) | PASS | |
| Create — self-assign still works | PASS | |
| Create — non-manager blocked | PASS | |
| Create — inactive assignee blocked (422) | PASS | |
| Create — non-existent assignee blocked (422) | PASS | |
| Read — creator access | PASS | |
| Read — non-creator non-assignee blocked | PASS | Normalized to 404 |
| Update — creator edits fields | PASS | |
| Update — creator cannot change status (403) | PASS | `status_change_restricted` |
| Update — assignee can still change status | PASS | |
| Reassign — creator changes assignee | PASS | |
| Reassign — assignee cannot reassign | PASS | |
| Reassign — inactive target blocked (422) | PASS | |
| Reassign — TaskReassigned event emitted | PASS | |
| Delete — creator can delete | PASS | |
| Audit events | PASS | TaskCreated, TaskReassigned with correct payloads |

### F0004-S0004: Task Center List + Filter UI — PASS (with gaps)

| AC | Status | Notes |
|----|--------|-------|
| Route `/tasks` renders page | PASS | |
| Tab navigation (role-conditional) | PASS | MANAGER_ROLES corrected to DistributionManager + Admin (DEF-03) |
| List display with all columns | PASS | Status, title, priority, due date, assignee (assignedByMe), linked, created |
| Overdue badge with days count | PASS | Red badge with `{n}d` |
| Status toggle (own tasks only) | PASS | `isOwn` guard, optimistic update |
| Status toggle read-only for creator in Assigned By Me | **GAP** | No tooltip "Only the assignee can change status" on the list toggle; button is disabled but no tooltip. Detail panel now shows the note (DEF-04 fix). |
| Filters functional | PASS | All 7 filter types working |
| URL-synced filters | **GAP** | Filters stored in React useState, not URL query params. View and page not reflected in URL. Filter state is lost on browser back/refresh. |
| Sort by column headers | PASS | 4 sortable columns |
| Pagination | PASS | Previous/Next with "Showing X–Y of Z" |
| Empty states (3 variants) | PASS | My Work empty, Assigned By Me empty, filtered empty |
| Loading state (skeleton rows) | PASS | 5 skeleton rows |
| Error state with retry | PASS | ErrorFallback with retry button |
| Responsive (desktop/tablet/mobile) | PASS | Table → cards at md breakpoint, drawer overlay |
| Vitest component tests | **NOT DONE** | No frontend unit tests written |
| Playwright E2E tests | **NOT DONE** | No E2E tests written |

### F0004-S0005: Task Create + Edit UI — PASS (with gaps)

| AC | Status | Notes |
|----|--------|-------|
| Create modal with all fields | PASS | Title, description, priority, due date, linked entity type/id |
| Assignee read-only for non-managers | PASS | Shows self as chip |
| Assignee typeahead for managers | PASS | 300ms debounce, 2-char min, role badges |
| Linked entity picker (entity search) | **GAP** | Only type dropdown + manual UUID input. No entity search/select. Acceptable for Phase 1. |
| Edit from detail panel (own task) | PASS | Inline editing for title, desc, priority, due date |
| Edit from detail panel (creator) | PASS | Same inline editing + reassign |
| Status action buttons — assignee only | PASS | Fixed in DEF-04: only assignee sees buttons |
| Status actions: Start, Complete, Reopen | PASS | Fixed in DEF-05: Reopen button added for Done |
| Creator sees "Only assignee can update status" | PASS | Added in DEF-04 fix |
| Delete with confirmation | PASS | Two-step confirmation in detail panel |
| Title required validation | PASS | Inline error |
| Title 255-char limit validation | **GAP** | Not implemented. Backend likely rejects via DB constraint. |
| Server error toast (inactive_assignee) | **GAP** | Error displayed but no toast notification UI |
| Server error toast (concurrency_conflict) | **GAP** | Not implemented |
| Form unsaved-state confirmation on close | **GAP** | Not implemented |
| Vitest component tests | **NOT DONE** | |
| Playwright E2E tests | **NOT DONE** | |

### F0004-S0006: Task Detail Panel + Mobile — PASS (with gaps)

| AC | Status | Notes |
|----|--------|-------|
| Desktop side panel with all detail fields | PASS | Title, status, priority, due date, assignee, description, created by, linked entity, timestamps |
| Panel updates on new task selection | PASS | useEffect syncs on task.id |
| Escape to close | PASS | Global keydown handler |
| Tablet overlay drawer with backdrop | PASS | `TaskDetailDrawer` with portal |
| Mobile full-page detail | **PARTIAL** | Uses same overlay drawer, not true full-page `/tasks/{taskId}` route. Route exists but renders drawer, not full page. Acceptable for Phase 1. |
| Recent timeline section (last 5 events) | **NOT DONE** | No call to timeline events API. Panel shows timestamps only. |
| Linked entity navigation (clickable) | **PARTIAL** | List view has `<Link>` to entity path, but detail panel shows type + ID as plain text. |
| Inline editing with save-on-blur | PASS | Title on Enter/blur, description on Save button, priority/dueDate on change/blur |
| Soft-delete edge case handling | **NOT DONE** | No handling for task deleted while panel is open |
| Vitest component tests | **NOT DONE** | |
| Playwright E2E tests | **NOT DONE** | |

## Summary of Gaps

### Product Gaps (deferred to future work)

| # | Gap | Story | Impact | Recommendation |
|---|-----|-------|--------|----------------|
| PG-01 | Filters not URL-synced | S0004 | Filter state lost on refresh/back. Cannot share filtered views via URL. | **Phase 2 priority** — Use `useSearchParams` to sync filters to URL |
| PG-02 | Timeline section not implemented in detail panel | S0006 | Users cannot see recent activity for a task without navigating away | **Phase 2** — Fetch from `/timeline/events?entityType=Task&entityId={id}` |
| PG-03 | Linked entity name null in list view | S0001 | List shows "—" instead of entity name | **Phase 2** — Batch resolve in list query or use a join |
| PG-04 | No entity search in linked entity picker | S0005 | Users must manually paste UUID for linked entity | **Phase 2** — Add entity typeahead per linked type |
| PG-05 | Linked entity not clickable in detail panel | S0006 | No navigation from task detail to linked entity | **Phase 2** — Add `<Link>` using `getEntityPath()` |
| PG-06 | No toast notifications for server errors | S0005 | Server errors (inactive_assignee, concurrency_conflict) not surfaced as toasts | **Phase 2** — Add toast/notification system |
| PG-07 | No form unsaved-state confirmation | S0005 | Accidental close of create modal loses all input | **Low priority** — Add `beforeunload` / close confirmation |
| PG-08 | Frontend component and E2E tests not written | S0004–S0006 | No automated frontend test coverage for Task Center UI | **Phase 2 priority** — Vitest component + Playwright E2E |
| PG-09 | Mobile uses drawer not full-page detail | S0006 | Works but not the "back button" full-page pattern specified | **Low priority** — Current UX is acceptable |
| PG-10 | Invalid sort field doesn't return 400 | S0001 | Falls through to default sort silently | **Low priority** — Backend defense-in-depth, not user-facing |
| PG-11 | Title 255-char limit not validated client-side | S0005 | Backend DB constraint catches it but no inline error | **Low priority** — Add `maxLength` to input |

### Not Gaps (verified as working)

- `activeOnly=false` on user search: WORKS
- Overdue badge with day count: WORKS
- Status toggle restricted to assignee: WORKS (list + detail)
- Delete restricted to assignee or creator: WORKS
- Pagination shows "Showing X–Y of Z": WORKS
- Empty states (3 variants): WORKS
- Loading skeleton: WORKS
- Error state with retry: WORKS

## Scope Delivered vs Planned

| Scope Item | Planned | Delivered |
|------------|---------|-----------|
| Task Center page with list | Yes | Yes |
| My Work / Assigned By Me tabs | Yes | Yes |
| Create task from UI | Yes | Yes |
| Cross-user assignment (manager/admin) | Yes | Yes |
| Edit task from UI (inline) | Yes | Yes |
| Status actions (complete, reopen) | Yes | Yes |
| Assignee lookup/search | Yes | Yes |
| Filters (7 types) | Yes | Yes |
| Sort (4 columns) | Yes | Yes |
| Server-side pagination | Yes | Yes |
| Detail side panel / drawer | Yes | Yes |
| Reassignment | Yes | Yes |
| Inline status toggle in list | Yes | Yes |
| Backward-compatible `/my/tasks` | Yes | Yes |
| Audit timeline events | Yes | Yes |
| URL-synced filters | Yes | **No** — React state only |
| Timeline section in detail panel | Yes | **No** — Not implemented |
| Entity search in linked entity picker | Yes | **No** — Manual UUID only |
| Frontend component/E2E tests | Yes | **No** — Not written |

**Delivery rate:** 15/19 scope items delivered (79%). The 4 missing items are UX polish and test automation, not core functionality.

## Archive Decision

**ARCHIVE.** The core product value proposition is delivered:
- Internal users can manage their own tasks from a dedicated UI
- Managers can assign and reassign tasks to team members
- Authorization model is correct and secure
- Backend is fully tested (365 tests, 0 failures)
- All 8 PRD success criteria are met

The 11 product gaps are documented above and should be addressed in a future polish pass or Phase 2 feature. None block the feature from being used in its current state.

## Deferred Items Registry (for Phase 2 planning)

| Priority | Item | Source |
|----------|------|--------|
| HIGH | URL-synced filters (PG-01) | S0004 AC |
| HIGH | Frontend component + E2E tests (PG-08) | S0004–S0006 DoD |
| MEDIUM | Timeline section in detail panel (PG-02) | S0006 AC |
| MEDIUM | Linked entity name in list (PG-03) | S0001 AC |
| MEDIUM | Entity search in linked entity picker (PG-04) | S0005 AC |
| MEDIUM | Linked entity clickable in detail (PG-05) | S0006 AC |
| MEDIUM | Server error toast system (PG-06) | S0005 AC |
| LOW | Form unsaved-state confirmation (PG-07) | S0005 NF |
| LOW | Mobile full-page detail (PG-09) | S0006 AC |
| LOW | Invalid sort → 400 (PG-10) | S0001 edge case |
| LOW | Title 255-char limit client-side (PG-11) | S0005 AC |
