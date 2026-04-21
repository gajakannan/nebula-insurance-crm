# F0003 — Task Center + Reminders (API-only MVP) — Getting Started

## Prerequisites

- [ ] Docker and docker-compose installed
- [ ] .NET 10 SDK installed
- [ ] Backend API running (`engine/src/Nebula.Api`)
- [ ] authentik IdP configured and running (per ADR-006 / F0005)
- [ ] PostgreSQL database with migrations applied

## Services to Run

```bash
docker compose up -d postgres authentik-server authentik-worker
dotnet run --project engine/src/Nebula.Api
```

## Authentication Setup

F0003 endpoints require a valid JWT from authentik.

- **authentik Admin UI:** http://localhost:9000/
- **Token endpoint:** `POST http://localhost:9000/application/o/nebula/token/`
- **Client ID:** `nebula`
- **Audience:** `nebula`

**Get a dev token (password grant):**
```bash
TOKEN=$(curl -s -X POST http://localhost:9000/application/o/nebula/token/ \
  -d "grant_type=password&client_id=nebula&username=admin&password=<password>" \
  | jq -r '.access_token')
```

## How to Verify

1. **Create a task:**
   ```bash
   curl -s -X POST http://localhost:5000/tasks \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"title": "Follow up with broker", "assignedToUserId": "<your-user-id>"}' \
     | jq .
   ```
   Expect: HTTP 201 with the created Task resource.

2. **Update a task:**
   ```bash
   curl -s -X PUT http://localhost:5000/tasks/<taskId> \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"status": "Done"}' \
     | jq .
   ```
   Expect: HTTP 200 with `status: "Done"` and `completedAt` set.

3. **Delete a task:**
   ```bash
   curl -s -X DELETE http://localhost:5000/tasks/<taskId> \
     -H "Authorization: Bearer $TOKEN" -w "\n%{http_code}"
   ```
   Expect: HTTP 204.

4. **Verify task excluded from my tasks:**
   ```bash
   curl -s http://localhost:5000/my/tasks \
     -H "Authorization: Bearer $TOKEN" | jq .
   ```
   Deleted task must not appear in results.

## Key References

- [ADR-003: Task Entity and Nudge Engine Design](../../architecture/decisions/ADR-003-task-entity-nudge-engine.md)
- [ADR-006: IdP Migration — authentik](../../architecture/decisions/ADR-006-idp-migration.md)
- [Authorization Matrix §2.6: Task](../../security/authorization-matrix.md)
- [OpenAPI Spec — Task endpoints](../../api/nebula-api.yaml)
