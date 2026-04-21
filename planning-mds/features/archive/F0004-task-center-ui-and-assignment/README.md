# F0004 — Task Center UI + Manager Assignment

**Status:** Done (Archived 2026-03-23)
**Priority:** High
**Phase:** Phase 1
**Archived:** 2026-03-23

## Overview

Dedicated Task Center UI at `/tasks` with list, filters, detail panel, and cross-user task assignment for Distribution Managers and Admins. Builds on F0003's self-assigned task CRUD API.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements with all product decisions |
| [IMPLEMENTATION-CONTRACT.md](./IMPLEMENTATION-CONTRACT.md) | API delta, authz delta, schema changes, UI contract |
| [STATUS.md](./STATUS.md) | Completion checklist and signoff tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Developer/agent setup and verification guide |

## Stories

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| [F0004-S0001](./F0004-S0001-task-list-api-endpoint.md) | Paginated task list API with filters and views | Critical | Done |
| [F0004-S0002](./F0004-S0002-user-search-api-endpoint.md) | User search API for assignee picker | High | Done |
| [F0004-S0003](./F0004-S0003-cross-user-task-authorization.md) | Cross-user task authorization (assign, reassign, creator access) | Critical | Done |
| [F0004-S0004](./F0004-S0004-task-center-list-and-filter-ui.md) | Task Center list + filter UI | Critical | Done |
| [F0004-S0005](./F0004-S0005-task-create-edit-ui-with-assignment.md) | Task create + edit UI with assignment | High | Done |
| [F0004-S0006](./F0004-S0006-task-detail-panel-and-mobile-view.md) | Task detail panel + mobile view | High | Done |

**Total Stories:** 6
**Completed:** 6 / 6

## Key Decisions

- **Mental model:** "My Work" + "Assigned By Me" tabs (not team queue or region view)
- **Assignment:** DistributionManager and Admin only; other roles self-assign only
- **Visibility:** Ownership-based (assignee or creator), not region-based
- **Status changes:** Assignee-only (managers cannot complete tasks on behalf of others)
- **Bulk actions:** Deferred to Phase 2
- **Out-of-office:** Deferred to Phase 2

## Dependencies

- F0003 (Task CRUD API) — archived, implemented
- F0005 (authentik + UserProfile) — archived, implemented
- F0015 (Frontend quality gates) — archived, implemented
