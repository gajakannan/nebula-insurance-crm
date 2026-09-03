# Runtime Preflight — F0040 run `2026-09-01-fd408477`

## Feature

- Feature ID: F0040
- Run ID: `2026-09-01-fd408477`
- Date: 2026-09-01
- Owner: DevOps

## Runtime Services / Containers / Jobs

F0040 requires the existing PostgreSQL, authentik, engine API, and Neuron containers plus the existing frontend toolchain. It adds no service, port, secret, environment variable, migration, or topology. The experience application is not a Compose service and does not need a live browser server for the G1 pre-change baseline.

## Command Evidence

Commands and exit codes are recorded in `commands.log`:

- `docker compose config --services` — failed: Docker is unavailable in this WSL distro.
- `docker compose ps --format json` — failed for the same reason.
- `command -v docker` — resolved the Docker Desktop WSL shim at `/mnt/c/Program Files/Docker/Docker/resources/bin/docker`.
- `command -v podman`, `command -v nerdctl`, `command -v containerd` — no alternate runtime available.
- Direct `docker.exe version`, `docker.exe compose version`, and `docker.exe context ls` — failed with `UtilBindVsockAnyPort: socket failed 1`, confirming Docker Desktop WSL integration is not usable from this session.

No compile, test, lint, scan, or application-code command was run after the failed preflight.

### Retry — 2026-09-01T21:14:45-04:00

- Docker CLI `29.6.2` and Compose `v5.3.1` are now installed, and `docker compose config --services` resolves the expected stack.
- The Docker daemon remains inaccessible: the default `/var/run/docker.sock` and Docker Desktop guest/host proxy sockets all return `permission denied` from this managed session.
- No daemon process is visible inside the session, `sudo` is unavailable because the environment enforces `no new privileges`, and the Docker Desktop context cannot be used from WSL.
- `docker compose ps` therefore cannot confirm or start application services. The health verdict remains unchanged.

### Successful retry — 2026-09-01T21:27:11-04:00

- `docker version` now reaches Docker Desktop 4.83.0 / Engine 29.6.2, and Compose v5.3.1 remains available.
- `docker compose up -d` created and started the existing stack. The initially reused Neuron image was stale and failed fast because it predated the PostgreSQL repository added by F0039.
- `docker compose build neuron` rebuilt the image from the current checkout; `docker compose up -d neuron` recreated it successfully.
- `docker compose exec -T db pg_isready -U postgres` reports that PostgreSQL is accepting connections.
- authentik `/-/health/live/`, engine `/healthz`, engine `/openapi/v1.json`, Neuron `/health`, and Neuron `/ready` all return success. Neuron readiness reports the expected mock model provider and PostgreSQL persistence.
- `docker compose ps` reports PostgreSQL, authentik server/worker, and Neuron healthy; engine API and the existing Temporal services are running.
- Frontend prerequisites are present: Node `v24.16.0`, pnpm `11.17.0`, and `experience/node_modules` exists.

## Health Status

| Service | Status | Notes |
|---------|--------|-------|
| PostgreSQL | healthy | `pg_isready` succeeds; Compose health is healthy. |
| authentik | healthy | Live probe succeeds; server and worker health are healthy. |
| engine API | running | `/healthz` and `/openapi/v1.json` return success. |
| Neuron | healthy/ready | Rebuilt from current source; `/health` and `/ready` return success. |
| experience toolchain | ready | Node, pnpm, and installed dependencies are present; no G1 browser server is required. |

## Restore Steps

1. If a later command fails with a runtime symptom, stop code edits and rerun the same Docker, Compose, and HTTP health probes recorded above.
2. Rebuild the affected service image from the current checkout when its startup traceback identifies an image/source mismatch.
3. Restore the existing stack with `docker compose up -d`, then rerun the exact failed validation command unchanged.
4. Continue to use run `2026-09-01-fd408477`; do not create a new run ID.

## Result

**PASS** — the existing application runtime and frontend prerequisites are available for F0040 implementation and validation. The earlier runtime-blocked attempts remain recorded above.
