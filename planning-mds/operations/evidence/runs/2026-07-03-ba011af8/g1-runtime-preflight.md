# G1 Runtime Preflight — F0024

> Required at G1 because `runtime_bearing = true` for backend, frontend, EF persistence, authorization, and API surface changes.

Run ID: `2026-07-03-ba011af8`
Owner: DevOps / Feature Orchestrator
Result: PASS WITH RECOMMENDATIONS

## Runtime Probes

| Surface | Command | Result |
|---------|---------|--------|
| Docker stack | `docker compose ps --format json` | PASS — `nebula-db`, `nebula-authentik-server`, and `nebula-authentik-worker` are healthy; `nebula-api` is running on host port `8080`. |
| API health | `curl -fsS -o /tmp/f0024-api-health.txt -w "api_health:%{http_code}\n" http://127.0.0.1:8080/healthz` | PASS — `api_health:200`. |
| API OpenAPI | `curl -fsS -o /tmp/f0024-api-openapi.json -w "api_openapi:%{http_code}\n" http://127.0.0.1:8080/openapi/v1.json` | PASS — `api_openapi:200`. |
| Authentik health | `curl -fsS -o /tmp/f0024-authentik-health.txt -w "authentik:%{http_code}\n" http://127.0.0.1:9000/-/health/live/` | PASS — `authentik:200`. |
| API logs | `docker compose logs --tail=80 api` | PASS — API reports `Now listening on: "http://[::]:8080"` and migrations are up to date. |

## Toolchain Probes

| Tool | Command | Result |
|------|---------|--------|
| .NET SDK | `dotnet --version` | PASS — `10.0.300` available. |
| Node.js | `node --version` | PASS — `v24.15.0` available. |
| Corepack | `corepack --version` | PASS — `0.34.6` available. |
| pnpm via Corepack | `corepack pnpm --version` | PASS — `11.9.0` available. |
| direct pnpm | `pnpm --dir experience --version` | NON-BLOCKING — direct `pnpm` is not on PATH; use `corepack pnpm --dir experience ...`. |

## Notes

- `/swagger/index.html` returned `404`; this is expected for the current API because development OpenAPI is mapped at `/openapi/v1.json`.
- A local `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore` probe emitted an existing `NU1903` warning for `Microsoft.OpenApi 2.0.0` and did not complete promptly. This is recorded as a non-blocking preflight limitation because the containerized API is healthy, serving OpenAPI, and reports migrations up to date. Build/test validation remains mandatory in later QE gates.
- Docker Compose emits warnings that `AUTHENTIK_SECRET_KEY` and `AUTHENTIK_BOOTSTRAP_PASSWORD` are unset and default to blank. The Authentik runtime is nevertheless healthy for local G1.

## Recommendations

- [low] Use `/openapi/v1.json` instead of `/swagger/index.html` for local API discovery unless a Swagger UI package is added later — owner: DevOps; follow-up: deferred-no-followup.
- [low] Use `corepack pnpm --dir experience ...` for frontend commands on this workstation because direct `pnpm` is not on PATH — owner: Frontend Developer; follow-up: deferred-no-followup.
- [medium] Re-run backend build/test validation during QE gates and capture the existing `Microsoft.OpenApi 2.0.0` `NU1903` dependency warning in the dependency scan — owner: Quality Engineer; follow-up: required-before-closeout.

## Verdict

`PASS WITH RECOMMENDATIONS` — all required local runtime surfaces are reachable. Proceed to implementation roles under the feature harness.
