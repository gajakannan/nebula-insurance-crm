# F0014 — DevOps Smoke Test Automation — Getting Started

## Prerequisites

- [x] Docker and docker compose v2 installed
- [x] `bash`, `curl`, `python3` available on PATH
- [x] Nebula repository cloned with `docker/authentik/blueprints/nebula-dev.yaml` present

## Services to Run

```bash
# Option 1: Full reset (tears down, rebuilds, waits, runs smoke tests)
./scripts/dev-reset.sh

# Option 2: Start services manually, then run smoke tests
docker compose up -d --build
./scripts/smoke-test.sh
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `API_BASE` | Nebula API base URL | `http://localhost:8080` |
| `AUTHENTIK_BASE` | authentik base URL | `http://localhost:9000` |
| `CLIENT_ID` | OAuth2 client ID | `nebula` |
| `APP_PASSWORD` | Shared dev app-password token key | `nebula-dev-token` |
| `TEST_USER` | Default user for smoke tests | `lisa.wong` |

## Seed Data

- **authentik blueprint** (`docker/authentik/blueprints/nebula-dev.yaml`): Provisions OAuth2Provider with `authentication_flow`, dev users (lisa.wong, john.miller, broker001, akadmin), and app-password tokens with shared key `nebula-dev-token`
- **UserProfile**: Auto-created on first authenticated API request (upsert from JWT claims)

## How to Verify

1. Run `./scripts/dev-reset.sh` — full clean verification from scratch
2. Observe 9/9 smoke tests pass with exit code 0
3. Run `./scripts/smoke-test.sh --user john.miller` — verify Underwriter access
4. Run `./scripts/smoke-test.sh --user akadmin` — verify Admin access
5. Run `./scripts/smoke-test.sh --all-users` — verify all 4 dev users with role assertions (31 total tests)

## Dev User Credentials (ROPC)

| Username | Casbin Role | Token Key |
|----------|-------------|-----------|
| lisa.wong | DistributionUser | `nebula-dev-token` |
| john.miller | Underwriter | `nebula-dev-token` |
| broker001 | BrokerUser (ExternalUser) | `nebula-dev-token` |
| akadmin | Admin | `nebula-dev-token` |

Manual token acquisition:
```bash
curl -X POST http://localhost:9000/application/o/token/ \
  -d "grant_type=password&client_id=nebula&username=lisa.wong&password=nebula-dev-token&scope=openid profile email nebula_roles"
```

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Script | `scripts/smoke-test.sh` | 9-test API smoke suite + multi-role `--all-users` mode (31 tests) |
| Script | `scripts/dev-reset.sh` | Clean teardown → rebuild → health wait → smoke test |
| Blueprint | `docker/authentik/blueprints/nebula-dev.yaml` | authentik dev provisioning (users, tokens, OAuth2Provider) |
| CI | `.github/workflows/smoke-test.yml` | GitHub Actions merge gate — runs `--all-users` on PR and push to main |

## Notes

- **ROPC gotcha:** authentik 2026.2 password grant requires app-password tokens (`intent=app_password`), NOT the user's login password. All dev users share key `nebula-dev-token`.
- **Health endpoints:** API uses `/healthz`, authentik uses `/-/health/live/`
- **Exit codes:** 0 = all pass, 1 = test failure, 2 = infra failure
- **Timeline verification:** Test #9 queries PostgreSQL directly via `docker compose exec -T db psql` to verify ActivityTimelineEvent records
