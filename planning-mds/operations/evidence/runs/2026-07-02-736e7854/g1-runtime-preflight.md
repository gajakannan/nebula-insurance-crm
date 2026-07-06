# G1 Runtime Preflight — F0028 Carrier & Market Relationship Management

Run ID: `2026-07-02-736e7854`
Timestamp: `2026-07-02T20:13:29+05:30`
Product root: `/Users/wallstreet289/Documents/workspace/nebula-insurance-crm-sagar`

## Decision

PASS

## Scope

Runtime-bearing services required before F0028 implementation validation:

- `db`
- `authentik-server`
- `authentik-worker`
- `api`

Frontend dev server is not required for this gate because no frontend implementation validation has run yet. It remains in scope for later frontend validation and QE evidence.

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `docker compose ps` | PASS | Initial inspection showed `db` healthy, `authentik-server` starting, `authentik-worker` restarting, and `api` absent. |
| `docker compose logs --tail=80 authentik-worker api` | PASS | Confirmed Authentik worker failed because `AUTHENTIK_SECRET_KEY` was missing. |
| `ls -la .env .env.example docker-compose.yml` | PASS | Confirmed `.env` was missing and `.env.example` existed. |
| `openssl rand -base64 48` | PASS | Generated local development `AUTHENTIK_SECRET_KEY`; value is redacted from evidence. |
| local `.env` creation | PASS | Added gitignored development `.env` with Authentik local values. |
| `docker compose up -d --force-recreate db authentik-server authentik-worker api` | PASS | Recreated runtime services using local `.env`. |
| `docker compose logs --tail=120 authentik-server` | PASS | Identified reused-volume issue: database `authentik` did not exist. |
| `docker compose exec -T db psql -U postgres -c "CREATE DATABASE authentik;"` | PASS | Created missing local Authentik database in the dev Postgres volume. |
| `docker compose ps` | PASS | Final status shows `db`, `authentik-server`, `authentik-worker`, and `api` up; Authentik services healthy. |
| `curl -sS -i http://127.0.0.1:8080/healthz` | PASS | API responded `HTTP/1.1 200 OK` with body `Healthy`. |

## Final Runtime State

| Service | Status |
|---------|--------|
| `nebula-db` | Up, healthy, port `5433->5432` |
| `nebula-authentik-server` | Up, healthy, ports `9000` and `9443` |
| `nebula-authentik-worker` | Up, healthy |
| `nebula-api` | Up, port `8080`; `/healthz` returned `200 Healthy` |

## Remediations Applied

- Created local gitignored `.env` because the required Authentik variables were absent.
- Created missing `authentik` database in the existing local Postgres volume because Docker init scripts do not rerun on already-initialized volumes.

## Notes

- No production credentials were introduced.
- `.env` remains ignored by repository policy.
- The failed `/health` and `/swagger/index.html` probes are non-blocking: `Program.cs` maps the health endpoint at `/healthz`, and `/healthz` passed.
