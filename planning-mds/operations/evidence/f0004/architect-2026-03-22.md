# F0004 Architect Review Evidence — 2026-03-22

**Feature:** F0004 — Task Center UI + Manager Assignment
**Reviewer:** Claude (Architect Agent)
**Date:** 2026-03-22
**Verdict:** PASS

## Architectural Assessment

### Authorization Model Extension

The core architectural change is extending the Casbin authorization model to support creator-based access alongside existing assignee-based access.

**Approach:** The `CasbinObject` record was extended from `(type, assignee)` to `(type, assignee, creator)` with a `__no_creator__` sentinel default. This preserves backward compatibility — existing policies with `r.obj.assignee == r.sub.id` conditions are unaffected because the sentinel never matches `r.sub.id`.

**Assessment:** Sound. The sentinel pattern avoids migration risk. OR semantics in policy conditions (`r.obj.assignee == r.sub.id || r.obj.creator == r.sub.id`) correctly grant access to either the assignee or creator without requiring both.

### Layered Authorization

Authorization is enforced at two layers:

1. **Casbin (ABAC):** Grants read/update/delete access based on assignee OR creator match
2. **Application guards (TaskService):** Restrict specific operations within Casbin-granted access:
   - Status changes: assignee only
   - Reassignment: creator only
   - Cross-user assignment on create: manager roles only

**Assessment:** Correct separation of concerns. Casbin handles coarse access control; application logic handles fine-grained business rules. This avoids over-complicating the Casbin model while maintaining security guarantees.

### API Design

| Endpoint | Pattern | Assessment |
|----------|---------|------------|
| GET /tasks?view=... | Query-based view selection | PASS — Clean, RESTful, extensible |
| GET /users?q=... | Search endpoint | PASS — Minimal surface area |
| POST /tasks | Extended with assignee fields | PASS — Backward compatible |
| PUT /tasks/{id} | Extended with reassignment | PASS — If-Match concurrency preserved |
| DELETE /tasks/{id} | Creator-based access | PASS — Consistent with update model |

### Database Design

Two new indexes added:
- `IX_Tasks_CreatedByUserId_AssignedToUserId` — Composite index for `assignedByMe` view query
- `IX_UserProfiles_DisplayName` — Supports user search ILIKE queries

**Assessment:** Appropriate. The composite index directly serves the most complex new query path. Display name index supports the user search endpoint.

### Frontend Architecture

Component hierarchy follows the established feature-first pattern:
```
TaskCenterPage
├── TaskFilterToolbar
├── TaskCenterList (with Tabs)
├── TaskDetailPanel / TaskDetailDrawer
└── TaskCreateModal
    └── AssigneePicker
```

Hooks follow TanStack Query patterns with proper cache invalidation on mutations.

**Assessment:** Consistent with existing codebase patterns. No architectural concerns.

### Backward Compatibility

| Surface | Impact | Assessment |
|---------|--------|------------|
| `/my/tasks` endpoint | Unchanged | PASS |
| Existing Casbin policies | Unchanged (sentinel compat) | PASS |
| Existing task CRUD for non-managers | Unchanged | PASS |
| CasbinObject record | Extended (additive) | PASS |
| TaskDto | Extended with new fields | PASS |

### Performance Considerations

| Concern | Assessment |
|---------|------------|
| Display name N+1 in list query | Batch-resolved via individual lookups; acceptable for page sizes ≤100. Could be optimized with IN-query batch if needed. |
| User search ILIKE | Indexed on DisplayName; Email covered by existing unique index. ILIKE with leading wildcard may not use index efficiently — acceptable for ≤20 result limit. |
| Task list query | Uses composite index for view scoping; additional filters are selective. Pagination keeps result sets bounded. |

## Corrective Fixes Applied

5 defects corrected during closeout (see code-review-2026-03-22.md). All were implementation-level issues, not architectural concerns.

## Residual Risks

1. **Priority sort alphabetical** — Cosmetic. Can be addressed with a priority-to-ordinal mapping in a future pass.
2. **LinkedEntityName null in list** — Architectural trade-off to avoid N+1 joins. Acceptable for Phase 1.
3. **Status toggle rowVersion=0** — Minor optimistic concurrency gap. Low conflict risk given single-user status ownership.

## Verdict

**PASS** — Authorization model extension is sound, backward-compatible, and correctly layered. API design is RESTful and extensible. Database indexes are appropriate. Frontend architecture is consistent with established patterns. No architectural concerns.
