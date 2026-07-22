# Runtime Preflight — F0026-billing-invoicing-and-reconciliation run 2026-07-19-86ad3248

## Feature

- Feature ID: F0026
- Run ID: `2026-07-19-86ad3248`
- Date: 2026-07-19
- Owner: DevOps

## Runtime Services / Containers / Jobs

- `db` — PostgreSQL 16 persistence and migration target.
- `authentik-server` / `authentik-worker` — authentication and role-claim source.
- `api` — .NET API runtime for the backend slice and integration tests.
- `temporal` / `temporal-ui` — existing platform dependencies; F0026 introduces no Temporal workflow.
- Frontend build/component/E2E validation jobs will use the repository-defined Node/Playwright runtime during G2; no always-on frontend Compose service is declared.
- `neuron` is not part of F0026 scope and was not started.

## Command Evidence

- `docker compose ps` showed database and authentik containers healthy and Temporal services running.
- `docker compose config --services` confirmed the declared `api` runtime.
- `docker compose up -d api` restored the API without changing source or configuration.
- `docker compose ps api` confirmed the API container running on port 8080.
- The first generic `/health` probe returned 404. After the mandatory KG hint and a scoped source check, `Program.cs` identified `/healthz` as the declared readiness endpoint.
- `curl -fsS http://127.0.0.1:8080/healthz` returned `Healthy` with exit code 0.

All commands and exit codes are recorded in `commands.log`.

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| PostgreSQL | healthy | Compose health check passed. |
| authentik server | healthy | Compose health check passed. |
| authentik worker | healthy | Running. |
| Nebula API | healthy | `/healthz` returned `Healthy`. |
| Temporal | running | Existing dependency; no F0026 workflow is introduced. |
| Frontend validation runtime | ready on demand | Repository-defined build/test/E2E jobs run at G2 after implementation exists. |

## Restore Steps If Unavailable

1. Run `docker compose ps` and inspect the failing service.
2. Restore infrastructure with `docker compose up -d db authentik-server authentik-worker temporal temporal-ui` as needed.
3. Restore the API with `docker compose up -d api`.
4. Verify `http://127.0.0.1:8080/healthz`; the readiness path is `/healthz`, not `/health`.
5. If a validation command reports runtime symptoms, rerun this preflight and then rerun the unchanged validation command before editing code.

## Recommendations (when `WITH RECOMMENDATIONS`)

None.

## Result

PASS
