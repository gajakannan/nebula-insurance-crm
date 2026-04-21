# F0005-S0001 — Replace authentik Infrastructure (docker-compose + Bootstrap)

**Feature:** F0005 — IdP Migration
**Story ID:** F0005-S0001
**Owner:** DevOps
**Priority:** Must-complete before any backend story

---

## Story

As a developer, I need the local Docker stack to run **authentik** (server + worker + Redis) instead of Keycloak, so that I can develop and test authentication against the new IdP.

---

## Acceptance Criteria

1. `docker-compose up -d` starts without errors and authentik admin UI is accessible at `http://localhost:9000/`.
2. `keycloak` service and its volume mount (`docker/keycloak/`) are removed from `docker-compose.yml`.
3. `redis` service is present and authentik-worker is healthy before authentik-server starts.
4. The app API (`nebula-api`) uses `Authentication__Authority: http://authentik-server:9000/application/o/nebula/` and `Authentication__Audience: nebula`.
5. A dev Blueprint file exists at `docker/authentik/blueprints/nebula-dev.yaml` that provisions:
   - OAuth2/OIDC provider `nebula-crm` (public client, PKCE, RS256)
   - Application `Nebula CRM`
   - Property mapping `nebula_roles` (group names → JWT claim)
   - Groups: `DistributionUser`, `DistributionManager`, `Underwriter`, `RelationshipManager`, `ProgramManager`, `Admin`
   - Test user `admin` (email: `admin@nebula.dev`, group: `Admin`, password: `password`)
6. `docker/postgres/init-databases.sh` creates both `nebula` and `authentik` databases.
7. `AUTHENTIK_SECRET_KEY` is read from environment / `.env` and is **not hardcoded** in `docker-compose.yml`.
8. `docker-compose.yml` documents the `authentik-server` and `authentik-worker` service purpose with comments matching the Temporal comment style.

---

## Implementation Notes

### docker-compose service additions

```yaml
redis:
  image: redis:7-alpine
  container_name: nebula-redis
  restart: unless-stopped
  healthcheck:
    test: ["CMD-SHELL", "redis-cli ping | grep -q PONG"]
    interval: 5s
    timeout: 5s
    retries: 5

authentik-server:
  image: ghcr.io/goauthentik/server:2024.12
  container_name: nebula-authentik-server
  command: server
  restart: unless-stopped
  environment:
    AUTHENTIK_REDIS__HOST: redis
    AUTHENTIK_POSTGRESQL__HOST: db
    AUTHENTIK_POSTGRESQL__USER: postgres
    AUTHENTIK_POSTGRESQL__PASSWORD: postgres
    AUTHENTIK_POSTGRESQL__NAME: authentik
    AUTHENTIK_SECRET_KEY: ${AUTHENTIK_SECRET_KEY}
    AUTHENTIK_ERROR_REPORTING__ENABLED: "false"
    AUTHENTIK_BLUEPRINTS_DIR: /blueprints/custom
  ports:
    - "9000:9000"
    - "9443:9443"
  volumes:
    - ./docker/authentik/blueprints:/blueprints/custom:ro
  depends_on:
    db:
      condition: service_healthy
    redis:
      condition: service_healthy

authentik-worker:
  image: ghcr.io/goauthentik/server:2024.12
  container_name: nebula-authentik-worker
  command: worker
  restart: unless-stopped
  environment:
    AUTHENTIK_REDIS__HOST: redis
    AUTHENTIK_POSTGRESQL__HOST: db
    AUTHENTIK_POSTGRESQL__USER: postgres
    AUTHENTIK_POSTGRESQL__PASSWORD: postgres
    AUTHENTIK_POSTGRESQL__NAME: authentik
    AUTHENTIK_SECRET_KEY: ${AUTHENTIK_SECRET_KEY}
    AUTHENTIK_ERROR_REPORTING__ENABLED: "false"
  depends_on:
    db:
      condition: service_healthy
    redis:
      condition: service_healthy
```

### init-databases.sh addition

```bash
psql -U postgres -c "CREATE DATABASE authentik;"
```

### API env var changes

```yaml
# In api service environment:
Authentication__Authority: http://authentik-server:9000/application/o/nebula/
Authentication__Audience: nebula
Authentication__RolesClaim: nebula_roles
# Remove: KC_* and previous Keycloak-specific variables
```

---

## Edge Cases

- If `AUTHENTIK_SECRET_KEY` is missing, `authentik-server` will fail to start with a clear error message.
- If the `authentik` DB does not exist, authentik-worker crashes at startup; `init-databases.sh` must run before authentik containers.
- Blueprint idempotency: authentik replays blueprints on worker restart; all Blueprint objects must have stable `identifiers` to avoid duplicates.

---

## Audit / Timeline Requirements

None — infrastructure change, no app data mutations.
