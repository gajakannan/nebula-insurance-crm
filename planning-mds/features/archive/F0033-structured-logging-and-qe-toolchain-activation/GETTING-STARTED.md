# F0033 — Structured Logging and QE Toolchain Activation — Getting Started

## Prerequisites

- [ ] Docker and Docker Compose available
- [ ] .NET 10 SDK available for backend build/test work
- [ ] Node 20+ and pnpm 10 available for frontend and Bruno/Lighthouse tooling
- [ ] Java 17 available locally or in CI for Sonar scanner workflows
- [ ] Nebula local stack can start successfully via `docker compose up -d db authentik-server authentik-worker api`

## Services to Run

```bash
# Baseline application stack
docker compose up -d db authentik-server authentik-worker api

# Optional QE overlay services once F0033 is implemented
docker compose -f docker-compose.yml -f docker-compose.qe.yml up -d pact-broker sonarqube

# Backend tests / provider verification
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj

# Frontend local runtime
pnpm --dir experience dev
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `NEBULA_API_BASE_URL` | Base URL for Bruno and Pact provider checks | `http://localhost:8080` |
| `BRUNO_ENV` | Bruno environment selection | `local` |
| `PACT_BROKER_BASE_URL` | Pact Broker endpoint for publish/verify flows | `http://localhost:9292` |
| `SONAR_HOST_URL` | SonarQube Community server URL | `http://localhost:9001` |
| `SONAR_TOKEN` | Token for authenticated Sonar scanner runs | unset |
| `VITE_AUTH_MODE` | Frontend auth mode used by the selected runtime | `oidc` for normal builds, approved non-production override for perf runs only |
| `NEBULA_API_PROXY_TARGET` | Frontend dev server proxy target | `http://localhost:8080` |

## Seed Data

F0033 does not own new domain seed data. It relies on the existing Nebula seeded users and API data so the toolchain can exercise real routes:

- **Dev users / tokens:** Existing authentik + dev-auth setup from F0005/F0014
- **Broker list data:** Existing broker seed data for Bruno, Lighthouse, and Pact representative flows
- **Task data:** Existing task API seed data for representative API validation

## How to Verify

1. Start the base Nebula stack and confirm `http://localhost:8080/healthz` returns success.
2. Run the Bruno entry point and confirm a machine-readable report is emitted for health, auth, brokers, and task read flows.
3. Run the Lighthouse CI entry point and confirm reports are generated for `/login`, `/`, and `/brokers` using the approved performance runtime profile.
4. Run the Pact consumer and provider verification paths and confirm the representative broker list contract is both generated and verified.
5. Run the SonarQube entry point and confirm frontend and backend coverage reports are imported into a single solution analysis.
6. Trigger the related GitHub workflows or equivalent CI commands and confirm artifact paths are stable and reviewable.

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Backend | `engine/src/Nebula.Api/Program.cs` | Serilog bootstrap and request logging pipeline |
| Backend | `engine/src/Nebula.Api/appsettings.json` | Logging configuration and sink defaults |
| Backend Tests | `engine/tests/Nebula.Tests/` | Pact provider verification and logging assertions |
| Frontend | `experience/package.json` | Lighthouse and contract scripts |
| Frontend | `experience/lighthouserc.json` | Route list and thresholds for Lighthouse CI |
| Tooling | `bruno/` | API collections and environment templates |
| Tooling | `docker-compose.qe.yml` | Optional QE overlay services (Pact Broker / SonarQube) |
| Tooling | `scripts/` | Bruno, Lighthouse, Pact, and Sonar execution entry points |
| CI | `.github/workflows/` | Repo-native workflow activation for the QE stack |

## Dev User Credentials

Use the existing Nebula seeded/dev-auth model already documented in the repo:

| Username | Role | Credential / Token Key |
|----------|------|----------------------|
| `lisa.wong` | DistributionUser | `nebula-dev-token` |
| `john.miller` | Underwriter | `nebula-dev-token` |
| `broker001` | BrokerUser | `nebula-dev-token` |
| `akadmin` | Admin | `nebula-dev-token` |

Manual token acquisition example:

```bash
curl -X POST http://localhost:9000/application/o/token/ \
  -d "grant_type=password&client_id=nebula&username=lisa.wong&password=nebula-dev-token&scope=openid profile email nebula_roles broker_tenant_id"
```

## Notes

- The Lighthouse execution model must not weaken the production auth-mode guard. If authenticated routes need a non-production auth profile, that profile must stay isolated to perf-only execution.
- Pact Broker and SonarQube should remain opt-in services so normal local app development is not forced to carry them on every run.
- Serilog activation should favor request metadata and explicit contextual properties over raw payload logging.
