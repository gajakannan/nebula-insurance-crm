# F0004 — Task Center UI + Manager Assignment — Getting Started

## Prerequisites

- [x] F0003 Task CRUD APIs implemented and functional
- [x] F0005 authentik + UserProfile + UserId principal key pattern implemented
- [x] F0015 Frontend quality gates + test infrastructure active
- [ ] Docker environment running (`docker-compose up`)
- [ ] Dev users seeded in authentik (lisa.wong, john.miller, broker001, akadmin)

## Key Files to Read Before Implementation

| File | What It Contains |
|------|-----------------|
| [PRD.md](./PRD.md) | All product decisions — mental model, assignment rules, visibility, filters |
| [IMPLEMENTATION-CONTRACT.md](./IMPLEMENTATION-CONTRACT.md) | API delta, Casbin policy changes, schema updates, UI component tree |
| [authorization-matrix.md](../../../security/authorization-matrix.md) §2.6a | F0004 authorization rules |
| [policy.csv](../../../security/policies/policy.csv) §2.6a, §2.6b | New Casbin policy rows |
| [nebula-api.yaml](../../../api/nebula-api.yaml) | Updated OpenAPI spec (v0.4.0) |

## Implementation Order

### Backend (can parallelize S0001–S0003)

1. **F0004-S0003: Cross-user task authorization**
   - Add new Casbin policy rows to `policy.csv`
   - Update enforcement point to hydrate `r.obj.creator` alongside `r.obj.assignee`
   - Add status-change guard (application layer)
   - Add assignee validation service (exists + active check)
   - Add `TaskReassigned` timeline event type
   - Test: authorization matrix coverage for all role × action × condition combinations

2. **F0004-S0001: Task list API endpoint**
   - Add `GET /tasks` endpoint with view, filter, sort, pagination parameters
   - Add `IX_Tasks_CreatedByUserId_AssignedToUserId` index migration
   - Join UserProfile for display names, linked entities for entity names
   - Test: pagination, filter combinations, view authorization

3. **F0004-S0002: User search API endpoint**
   - Add `GET /users` endpoint
   - Add `IX_UserProfile_DisplayName` index
   - Return safe fields only (no IdP details)
   - Test: search accuracy, active-only filter, authorization

### Frontend (depends on backend)

4. **F0004-S0004: Task Center list + filter UI**
   - Route `/tasks` with TaskCenterPage component
   - Tab bar (My Work / Assigned By Me — role-conditional)
   - Filter toolbar, sortable columns, pagination
   - TanStack Query integration, URL-synced state
   - Responsive breakpoints

5. **F0004-S0005: Task create + edit UI**
   - Create modal with assignee picker (typeahead for managers, read-only for others)
   - Edit form reusing create components
   - Linked entity picker
   - Validation and error handling

6. **F0004-S0006: Task detail panel + mobile view**
   - Side panel (desktop), overlay drawer (tablet), full-page (mobile)
   - Inline editing with save-on-blur
   - Timeline section (recent 5 events)
   - Status action buttons (role-aware)

## How to Verify

### Backend Verification

```bash
# 1. Start environment
./scripts/dev-reset.sh

# 2. Get tokens for test users
LISA_TOKEN=$(curl -s -X POST http://localhost:9000/application/o/token/ \
  -d "grant_type=password&client_id=nebula&username=lisa.wong&password=nebula-dev-token&scope=openid profile email nebula_roles" \
  | jq -r '.access_token')

ADMIN_TOKEN=$(curl -s -X POST http://localhost:9000/application/o/token/ \
  -d "grant_type=password&client_id=nebula&username=akadmin&password=nebula-dev-token&scope=openid profile email nebula_roles" \
  | jq -r '.access_token')

# 3. Test user search (Admin)
curl -s http://localhost:5000/users?q=lisa \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

# 4. Admin creates task assigned to lisa.wong
LISA_USERID="<lisa's UserId from user search>"
curl -s -X POST http://localhost:5000/tasks \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Follow up with Acme broker\",\"assignedToUserId\":\"$LISA_USERID\",\"priority\":\"High\"}" | jq

# 5. Verify lisa sees it in her My Work
curl -s "http://localhost:5000/tasks?view=myWork" \
  -H "Authorization: Bearer $LISA_TOKEN" | jq

# 6. Verify admin sees it in Assigned By Me
curl -s "http://localhost:5000/tasks?view=assignedByMe" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

# 7. Verify lisa (non-manager) cannot access assignedByMe view
curl -s "http://localhost:5000/tasks?view=assignedByMe" \
  -H "Authorization: Bearer $LISA_TOKEN"
# Expected: 403

# 8. Verify lisa cannot assign tasks to others
curl -s -X POST http://localhost:5000/tasks \
  -H "Authorization: Bearer $LISA_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Test\",\"assignedToUserId\":\"$ADMIN_USERID\"}"
# Expected: 403
```

### Frontend Verification

1. Navigate to `http://localhost:5173/tasks`
2. Verify "My Work" tab shows own tasks
3. As Admin: verify "Assigned By Me" tab is visible and functional
4. As DistributionUser: verify only "My Work" tab is visible
5. Create a task — self-assigned (all roles) and assigned to others (Admin)
6. Filter by status, priority, overdue — verify list updates
7. Click a task — verify detail panel opens with correct data
8. Resize to mobile — verify full-page detail view

### Test Commands

```bash
# Backend tests
cd engine && dotnet test --filter "Category=F0004"

# Frontend tests
cd experience && pnpm test -- --grep "TaskCenter"
cd experience && pnpm test:e2e -- --grep "task-center"
```
