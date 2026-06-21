# Runtime Preflight — F0023 run 2026-06-19-a4e3fdd6

> Required at G1 because `runtime_bearing = true`.

## Feature

- Feature ID: F0023
- Run ID: 2026-06-19-a4e3fdd6
- Date: 2026-06-19
- Owner: DevOps / Feature Orchestrator

## Runtime Services / Containers / Jobs

F0023 is backend-runtime-bearing (`engine/` API + EF/Postgres + projection backfill) and frontend-bearing (`experience/`). Implementation, compile, migration, and test commands must run against the application runtime containers. Surfaces touched:

- `nebula-api` (`engine/` ASP.NET API, port 8080) — search/saved-view/report endpoints + DI + Casbin runtime policy.
- `nebula-db` (postgres:16, port 5433→5432) — new SearchReporting tables + EF migration `F0023_SearchSavedViewsOperationalReporting` + projection backfill.
- `nebula-authentik-server` / `worker` — identity/auth (authorization-sensitive scope).
- `nebula-temporal` / `temporal-ui` — workflow runtime (aging/backlog projections read workflow state).
- `experience` web build (frontend) — runs in node toolchain (pnpm); not a long-running container in this compose set.

## Command Evidence

Preflight commands (see `commands.log`):

```text
- docker compose ps                          → all services Up (db/authentik healthy)
- curl :8080/healthz                         → 200 (API healthy)
- curl :8080/health, /api/health, /          → 404 (not the health route; /healthz is canonical)
- docker compose exec -T db pg_isready -U postgres → /var/run/postgresql:5432 - accepting connections
```

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| nebula-api (engine) | healthy | `/healthz` → 200 |
| nebula-db (postgres) | healthy | `pg_isready` accepting connections; compose health `(healthy)` |
| nebula-authentik-server | healthy | compose health `(healthy)` |
| nebula-authentik-worker | healthy | compose health `(healthy)` |
| nebula-temporal | up | running 4 days (no compose healthcheck defined) |
| nebula-temporal-ui | up | running 4 days |
| experience (frontend build) | n/a | node/pnpm toolchain; validated at build/test time in G2 |

## Restore Steps If Unavailable

If a runtime surface is down: `docker compose up -d <service>`; for the API after code changes, rebuild image (`docker compose build api && docker compose up -d api`) then re-probe `/healthz`. For DB schema, apply EF migrations inside the api container. Re-run this preflight before any compile/test/scan command. Runbook: `agents/docs/MANUAL-ORCHESTRATION-RUNBOOK.md`.

## Result

`PASS` — all required runtime surfaces (API, Postgres) healthy at G1. No recommendations.
