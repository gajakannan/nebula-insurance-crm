# Runtime Preflight — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-07-771a5ef6

> Required at G1 because `runtime_bearing = true` (F0017 adds `engine/` code + an EF Core migration).

## Feature

- Feature ID: F0017
- Run ID: 2026-06-07-771a5ef6
- Date: 2026-06-07
- Owner: DevOps / Feature Orchestrator

## Runtime Services / Containers / Jobs

F0017 backend executes against these application runtime surfaces:

- `nebula-api` — .NET API (compiles + serves F0017 endpoints; runs EF migration on startup)
- `nebula-db` — PostgreSQL 16 (target for the F0017 migration + filtered indexes)
- `nebula-authentik-server` / `-worker` — identity (auth for `RequireAuthorization()` endpoints)
- `nebula-temporal` / `-ui` — workflow runtime (unaffected by F0017; up for parity)

Frontend (`experience`) dev server is **not** brought up locally — the experience toolchain cannot run on the `/mnt/c` WSL mount; frontend validation is deferred to CI (recorded in `coverage-report.md`).

## Command Evidence

```text
- docker compose up -d --build           → all services created/started (commands.log)
- docker compose ps                      → see artifacts/test-results/g1-docker-ps.txt
- curl http://localhost:8080/openapi/v1.json → HTTP 200 (API app serving)
- docker compose exec -T db pg_isready   → "/var/run/postgresql:5432 - accepting connections"
```

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| nebula-api | healthy | Up 18m; `/openapi/v1.json` → 200 (no `/health` route — 404 expected; openapi 200 confirms app up) |
| nebula-db | healthy | `pg_isready` accepting connections; container health=healthy |
| nebula-authentik-server | healthy | container health=healthy |
| nebula-authentik-worker | healthy | container health=healthy |
| nebula-temporal | up | running (not exercised by F0017) |
| nebula-temporal-ui | up | running |
| experience.web | n/a (deferred) | toolchain cannot run on `/mnt/c` WSL mount; frontend validation in CI |

## Restore Steps If Unavailable

`docker compose up -d --build` from `{PRODUCT_ROOT}`; wait for `nebula-db` healthy then `nebula-api`. If migration fails on startup, inspect `docker compose logs api`, fix the migration, and re-run preflight before any code validation.

## Result

**PASS** — backend runtime surfaces healthy; frontend deferred to CI (documented, non-blocking).
