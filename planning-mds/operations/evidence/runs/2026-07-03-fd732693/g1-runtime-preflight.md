# G1 Runtime Preflight - F0008 Broker Insights

## Summary

- Feature: F0008 Broker Insights
- Run ID: 2026-07-03-fd732693
- Owner: DevOps
- Result: PASS
- Date: 2026-07-03

## Runtime Services

| Check | Result | Evidence |
|-------|--------|----------|
| Docker Compose status | PASS | `docker compose ps` exited 0 with `nebula-api`, `nebula-db`, `nebula-authentik-server`, `nebula-authentik-worker`, `nebula-temporal`, and `nebula-temporal-ui` up. |
| Database health | PASS | `nebula-db` reported healthy in Compose status. |
| Auth service health | PASS | `nebula-authentik-server` and `nebula-authentik-worker` reported healthy in Compose status. |
| API process | PASS | API logs show `Now listening on: "http://[::]:8080"`. |
| API health endpoint | PASS | `curl -i http://localhost:8080/healthz` returned `HTTP/1.1 200 OK` and body `Healthy`. |

## Runtime Notes

- The first sandboxed `docker compose ps` failed because Docker was not running.
- Docker Desktop was launched with operator approval.
- A second sandboxed Docker check failed with socket permission restrictions; the same Compose command passed with approved escalation.
- Sandboxed `curl` could not reach `localhost:8080`; the same health probe passed with approved escalation.
- Compose emitted warnings for unset Authentik bootstrap secret variables. These are pre-existing local-runtime warnings and did not prevent service health.

## Decision

G1 passes. Feature implementation and validation commands may proceed against the application runtime.
