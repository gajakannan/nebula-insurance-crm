# Runtime Preflight — F0038-neuron-day-at-a-glance-shell run 2026-07-01-90a75ace

## Feature

- Feature ID: F0038
- Run ID: 2026-07-01-90a75ace
- Date: 2026-07-01
- Owner: DevOps / Feature Orchestrator

## Runtime Services / Containers / Jobs

F0038 validation runs against the baseline application runtime stack (`docker-compose.yml`):

- `nebula-db` (Postgres 16) — CRM application schemas + the new Neuron-owned `neuron.*` schema (S0001 persistence).
- `nebula-api` (engine .NET API) — CRM source of truth + authorization boundary; F0038 adds 5 endpoints + `renewal:draft_outreach`.
- `nebula-authentik-server` / `-worker` — OIDC identity provider; Neuron forwards the user token to the engine (on-behalf-of).
- `nebula-temporal` / `-ui` — infrastructure-only (no F0038 dependency; provisioned).

Not yet standing (built during this run / tested out-of-container):
- `neuron/` FastAPI service — **does not exist yet**; S0001 builds it. Its container + health-check contract is authored by DevOps and validated in the G2 deployability check.
- `experience/` frontend — validated via `pnpm` (vitest/Playwright), not a standing dev-server container; `node_modules` present.

## Command Evidence

Preflight commands (see `commands.log` and `artifacts/test-results/g1-runtime-preflight-evidence.txt`):

```text
- docker compose -f docker-compose.yml up -d --build  → 6/6 services running (pull retries cleared transient Docker Hub TLS timeouts)
- docker compose ps                                    → db/authentik-server/authentik-worker healthy; api/temporal/temporal-ui running
- docker exec nebula-db pg_isready -U postgres         → accepting connections
- curl http://localhost:9000/-/health/live/            → HTTP 200 (authentik live)
- curl http://localhost:8080/health                    → HTTP 404 (route not defined in engine; HTTP listener up and routing — reachable)
```

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| nebula-db | healthy | `pg_isready` accepting connections; port 5433→5432 |
| nebula-api (engine) | reachable | container running 23m; HTTP listener responds (404 on `/health` — no such route; server serving) |
| nebula-authentik-server | healthy | live probe HTTP 200; port 9000 |
| nebula-authentik-worker | healthy | background worker up |
| nebula-temporal | running | infra-only; no F0038 dependency |
| nebula-temporal-ui | running | infra-only |
| neuron (FastAPI) | not built | built in S0001; runtime/health contract validated at G2 deployability |
| experience (web) | out-of-container | validated via pnpm vitest/Playwright |

## Restore Steps If Unavailable

- Docker Desktop WSL integration must be enabled; socket access granted (`chmod 666 /var/run/docker.sock` on this dev box).
- `.env` seeded from `.env.example` with dev-only Authentik secrets (`AUTHENTIK_SECRET_KEY` generated; `ANTHROPIC_API_KEY` placeholder — LLM mocked this run).
- Bring-up: `docker compose --project-directory <PRODUCT_ROOT> -f docker-compose.yml up -d --build`; retry pulls on transient Docker Hub TLS timeouts. Authentik first-boot migration honors the 5-minute `start_period`.

## Result

**PASS** — baseline runtime healthy; feature validation commands may proceed. The new `neuron` service and its health contract are introduced during implementation (S0001) and preflighted at the G2 deployability check.
