# G1 Runtime Preflight — F0027

**Run ID:** 2026-07-02-b9316621
**Gate:** G1 RUNTIME PREFLIGHT
**Decision:** PASS

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `docker compose ps` | Initial sandbox failure | Docker socket access denied inside sandbox. |
| `docker compose ps` | PASS | Re-run with approved escalation; runtime containers listed successfully. |

## Runtime Status

| Service | Status |
|---------|--------|
| `api` / `nebula-api` | Up 5 hours, port `8080` |
| `db` / `nebula-db` | Up 5 hours, healthy, port `5433->5432` |
| `authentik-server` | Up 5 hours, healthy, ports `9000`, `9443` |
| `authentik-worker` | Up 5 hours, healthy |

## Decision

Runtime preflight is green enough to proceed to implementation validation gates. The compose command emitted warnings that `AUTHENTIK_SECRET_KEY` and `AUTHENTIK_BOOTSTRAP_PASSWORD` are unset, but the existing containers are running and healthy.
