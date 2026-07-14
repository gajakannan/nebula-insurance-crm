# G1 Runtime Preflight - F0025

Feature: F0025 - Commission, Producer Splits & Revenue Tracking
Run ID: `2026-07-07-9859bad4`
Gate: G1 Runtime Preflight
Verdict: PASS

## Scope Review

- Runtime-bearing feature work is expected after G1 because F0025 will add backend code, migrations, frontend code, tests, and deployability evidence.
- No feature implementation command ran before this preflight.

## Preflight Commands

| Command | Result | Notes |
|---------|--------|-------|
| `docker compose ps` | Initial FAIL, then PASS | First attempt was blocked by sandbox Docker socket permission, then showed Authentik unhealthy. After local dev `.env` restore and Compose restart, DB, Authentik server, Authentik worker, and API were up; DB/Auth services healthy. |
| `dotnet --info` | PASS | .NET SDK/runtime available locally. |
| `pnpm --dir experience --version` | PASS | pnpm available. |
| `test -d experience/node_modules` | PASS | Frontend dependencies already installed. |
| `docker compose up -d db authentik-server authentik-worker api` | PASS after restore | Restored required runtime services after missing local Authentik env values were detected. |

## Runtime Health

- `nebula-db`: Up and healthy on `5433`.
- `nebula-authentik-server`: Up and healthy on `9000`/`9443`.
- `nebula-authentik-worker`: Up and healthy.
- `nebula-api`: Up on `8080`.
- `nebula-temporal` and `nebula-temporal-ui`: Up; not directly required for F0025 G1 but present in Compose status.

## Blockers Resolved

- Docker status inspection initially required escalation because sandbox access to the Docker socket was denied.
- Authentik initially failed because local `.env` was missing documented dev values. `.env` is ignored by Git; a local dev copy was restored from `.env.example` and placeholders were replaced with local-only values.

## Validation Evidence

- G1 status is summarized in this file and command telemetry is recorded in `commands.log`.
- G1 feature evidence validation must pass before implementation starts.

Result: PASS
