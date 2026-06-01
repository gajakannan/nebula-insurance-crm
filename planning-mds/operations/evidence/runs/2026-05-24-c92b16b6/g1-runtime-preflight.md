# G1 Runtime Preflight - F0035

**Result:** PASS
**Reviewer:** DevOps Agent
**Review Date:** 2026-05-24
**Evidence Path:** planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/g1-runtime-preflight.md

## Scope

F0035 is runtime-bearing because it modifies session continuity, silent token renewal, authenticated telemetry, and post-renewal context behavior. G1 verifies that the local application runtime stack is available before implementation and test gates continue.

## Commands And Observations

| Check | Result | Notes |
| --- | --- | --- |
| `docker ps --format ...` | PASS | Docker daemon was available; unrelated infrastructure containers were already running. |
| `docker compose ps` before restore | PASS | Product compose project had no application services running, so preflight restoration was required. |
| `.env` presence check | PASS | Product runtime `.env` exists. No secret values were printed or copied. |
| `docker compose up -d db authentik-server authentik-worker temporal temporal-ui api` | PASS | Compose created the product runtime network and started the required application services. |
| Authentik migration wait | PASS | Worker initially reported unhealthy while the server applied fresh-volume migrations, then recovered after migrations completed. |
| Final `docker compose ps` | PASS | `nebula-db`, `nebula-authentik-server`, and `nebula-authentik-worker` reported healthy; `nebula-api`, `nebula-temporal`, and `nebula-temporal-ui` were running. |
| Authentik live endpoint probe | PASS | `curl -fsS http://localhost:9000/-/health/live/` exited 0. |
| API listener probe | PASS | `curl -i -s http://localhost:8080/` returned a Kestrel JSON `404 Not Found`, confirming the API container is serving HTTP. |

## Runtime Services

| Service | Container | Final State |
| --- | --- | --- |
| API | `nebula-api` | Up, listening on `localhost:8080` |
| PostgreSQL | `nebula-db` | Up, healthy, mapped to `localhost:5433` |
| Authentik server | `nebula-authentik-server` | Up, healthy, mapped to `localhost:9000` and `localhost:9443` |
| Authentik worker | `nebula-authentik-worker` | Up, healthy after migration completion |
| Temporal | `nebula-temporal` | Up, mapped to `localhost:7233` |
| Temporal UI | `nebula-temporal-ui` | Up, mapped to `localhost:8082` |

## Findings

- Critical findings: 0
- High findings: 0
- Blocking findings: 0

## Decision

PASS. Runtime preflight was restored and verified. Feature work may proceed to G2, with the DevOps deployability report still required at G2.

## Follow-ups

- Capture refresh-token issuance configuration during the later DevOps deployability check; G1 only verifies runtime availability.
