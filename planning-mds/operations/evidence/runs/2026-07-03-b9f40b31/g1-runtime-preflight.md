# G1 Runtime Preflight — F0022 run 2026-07-03-b9f40b31

## Verdict

PASS

## Runtime Surfaces

| Surface | Result | Evidence |
|---------|--------|----------|
| Docker Compose access | PASS after escalation | `docker compose ps` succeeded after Docker socket approval. |
| Database container | PASS | `nebula-db` healthy; `pg_isready` returned accepting connections. |
| Auth service | PASS after recovery | `nebula-authentik-server` and worker healthy after missing local databases were created and gitignored `.env` values were added. |
| API container | PASS | `nebula-api` running and listening on port 8080. |
| API health endpoint | PASS | `curl -fsS http://localhost:8080/healthz` returned `Healthy`. |
| Frontend package manager | PASS | `corepack pnpm --dir experience --version` returned `11.9.0`. |
| Local dotnet build probe | NON-GATING INCONCLUSIVE | `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore` produced no output for roughly five minutes and was terminated. Runtime container health is the G1 gate signal; compile/test evidence is deferred to implementation/QE gates. |

## Recovery Actions

- Created missing local development databases expected by `docker/postgres/init-databases.sh`: `authentik`, `pactbroker`, and `sonarqube`; `temporal` and `temporal_visibility` already existed.
- Added local gitignored `.env` values for compose so authentik did not boot with blank secret/password values.
- Restarted `authentik-server`, `authentik-worker`, and `api` with `docker compose up -d`.

## Notes

- `/health` returned `404`; the application maps health checks at `/healthz`.
- API logs show known EF query-filter warnings and a container Kerberos library warning, but the process started and reported `Now listening on: http://[::]:8080`.
- The `.env` file is ignored by `.gitignore` and `.dockerignore`; it is local runtime recovery state, not a committed artifact.
