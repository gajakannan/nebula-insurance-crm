# Runtime Preflight - F0021-communication-hub-and-activity-capture run 2026-07-01-9cee64f0

## Feature

- Feature ID: F0021
- Run ID: 2026-07-01-9cee64f0
- Date: 2026-07-01
- Owner: DevOps / Feature Orchestrator

## Runtime Services / Containers / Jobs

- PostgreSQL dev database: `nebula-db` via Docker Compose.
- Authentik OIDC dev identity provider: `nebula-authentik-server` and `nebula-authentik-worker`.
- Nebula API: `nebula-api` on port 8080.
- Nebula frontend: Vite dev server under `experience` on port 5173.

## Command Evidence

Command evidence is recorded in `commands.log` for this run.

```text
- docker compose ps authentik-server authentik-worker db api -> Authentik initially unhealthy with blank .env warnings.
- docker compose logs --tail=120 authentik-worker -> worker failed on missing authentik_tenants_tenant.reputation_lower_limit.
- docker compose exec -T db psql ... django_migrations -> Authentik migrations stopped at authentik_core.0055.
- apply_patch create ignored local .env with dev-only Authentik values -> local Compose secrets no longer blank.
- docker compose up -d authentik-server authentik-worker -> restarted Authentik services against existing volume.
- docker compose exec -T db psql ... pg_stat_activity / pg_locks -> found idle transaction blocking Authentik migration.
- docker compose exec -T db psql ... pg_terminate_backend(...) -> terminated stale idle migration-blocking session.
- docker compose exec -T db psql ... django_migrations -> authentik_core.0056 and 0057 applied at 2026-07-01T13:38:30Z.
- docker compose ps authentik-server authentik-worker db api -> db, Authentik server, Authentik worker healthy; API up.
- curl -fsS http://localhost:9000/-/health/live/ -> exit 0.
- curl -fsS http://localhost:8080/healthz -> Healthy.
- pnpm --dir experience dev --host 127.0.0.1 -> Vite ready on http://127.0.0.1:5173/.
- curl -fsS http://127.0.0.1:5173 -> app shell served.
```

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| nebula-db | healthy | Docker Compose healthcheck passed. |
| nebula-authentik-server | healthy | Authentik liveness endpoint exits 0 after clearing stale migration lock. |
| nebula-authentik-worker | healthy | Worker healthy after Authentik migrations advanced. |
| nebula-api | healthy | `/healthz` returns `Healthy`. |
| experience.web | healthy | Vite dev server serves the app shell on `127.0.0.1:5173`. |

## Restore Steps If Unavailable

1. Confirm `.env` exists locally and contains non-blank dev-only Authentik values. `.env` is ignored by git.
2. Restart Authentik services with `docker compose up -d authentik-server authentik-worker`.
3. If Authentik remains at HTTP 502, inspect `django_migrations`, `pg_stat_activity`, and `pg_locks` in the `authentik` database.
4. If a stale `idle in transaction` session is holding `authentik_version_history` and blocking `authentik_core.0056_user_roles`, terminate only that stale backend, then repoll migrations and health.
5. Use `/healthz` for the API health route; `/health` is not mapped in this service.
6. Start the frontend with `pnpm --dir experience dev --host 127.0.0.1`; sandboxed bind attempts may fail with `EPERM`, so run through the approved local runtime path when needed.

## Recommendations

- [low] Document the Authentik migration-lock recovery path in the project README or a runtime runbook so future G1 preflights do not rediscover it from logs — owner: DevOps; follow-up: deferred-no-followup
- [low] Add a short note that the API health endpoint is `/healthz`, not `/health` — owner: DevOps; follow-up: deferred-no-followup

## Result

PASS WITH RECOMMENDATIONS
