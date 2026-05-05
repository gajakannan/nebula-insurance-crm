# G1 Runtime Preflight Evidence - F0020

Run ID: `7e15d9c8-c2a0-4442-99a7-082bc0b560f5`
Timestamp: `2026-05-04T08:13:47-04:00`

## Commands

- `docker compose ps`
- `docker inspect nebula-api`
- `docker inspect nebula-db`
- `docker compose logs --tail=80 api`
- `curl -L -s http://localhost:8080/healthz`

## Result

PASS.

`docker compose ps` showed the runtime stack running:

| Container | Service | Status |
|-----------|---------|--------|
| `nebula-api` | `api` | Up 2 hours |
| `nebula-db` | `db` | Up 2 hours (healthy) |
| `nebula-authentik-server` | `authentik-server` | Up 2 hours (healthy) |
| `nebula-authentik-worker` | `authentik-worker` | Up 2 hours (healthy) |
| `nebula-temporal` | `temporal` | Up 2 hours |
| `nebula-temporal-ui` | `temporal-ui` | Up 2 hours |

`docker inspect nebula-api` confirmed:

- `State.Status = running`
- `State.Running = true`
- `State.ExitCode = 0`
- API port `8080/tcp` published to host `8080`

`docker inspect nebula-db` confirmed:

- `State.Status = running`
- `State.Running = true`
- `State.ExitCode = 0`
- `State.Health.Status = healthy`
- latest health check output: `/var/run/postgresql:5432 - accepting connections`

API logs showed startup completed:

- `No migrations were applied. The database is already up to date.`
- `Now listening on: "http://[::]:8080"`
- `Application started.`

`curl -L -s http://localhost:8080/healthz` returned:

```text
Healthy
```

## Notes

An earlier probe to `/health` returned 404 because the application maps health checks at `/healthz`. This is not a runtime failure.
