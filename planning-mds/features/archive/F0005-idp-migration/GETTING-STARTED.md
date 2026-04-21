# F0005 — Getting Started

## Prerequisites

- Docker Desktop running
- `.env` file at repo root (copy from `.env.example` and fill in values — see step 1)

## Step 1 — Set secrets

```bash
# Generate a random 50-character secret key
python3 -c "import secrets; print(secrets.token_urlsafe(50))"
```

Minimum `.env` required:
```
AUTHENTIK_SECRET_KEY=<generated above>
AUTHENTIK_BOOTSTRAP_PASSWORD=akadmin123
```

## Step 2 — Start the stack

```bash
docker-compose up -d
```

Services start in order: `db` → `authentik-worker` → `authentik-server` → `temporal` → `api`.

- authentik admin UI: http://localhost:9000/
- authentik bootstrap user: `akadmin` / value of `AUTHENTIK_BOOTSTRAP_PASSWORD`

> **Note:** authentik 2026.2+ no longer requires Redis. PostgreSQL handles caching, tasks, and WebSockets.

## Step 3 — Blueprint auto-apply

The authentik worker automatically picks up `docker/authentik/blueprints/nebula-dev.yaml` on
startup and provisions:

| Object | Details |
|--------|---------|
| Groups | `DistributionUser`, `DistributionManager`, `Underwriter`, `RelationshipManager`, `ProgramManager`, `Admin` |
| Property Mapping | `Nebula Roles` — scope `nebula_roles`, maps group memberships to JWT claim |
| OAuth2/OIDC Provider | name `nebula`, `client_id=nebula`, public, PKCE, RS256 |
| Application | `Nebula CRM`, slug `nebula` |
| Group membership | `akadmin` added to `Admin` group |

Check blueprint status (~30 s after startup):

```bash
docker exec nebula-db psql -U postgres -d authentik -c \
  "SELECT name, status FROM authentik_blueprints_blueprintinstance WHERE name = 'Nebula CRM Dev Blueprint';"
```

Expected: `status: successful`. If `status: error`, see **Troubleshooting** below.

To force re-apply after a blueprint file change:

```bash
docker-compose restart authentik-worker
```

---

## Manual setup (fallback if blueprint keeps failing)

If the blueprint status remains `error` after a restart, create the objects manually via the
admin UI at **http://localhost:9000/if/admin/**. Do steps A–E in order.

### A — Groups

**Directory → Groups → Create** — create one group per name:

`DistributionUser`, `DistributionManager`, `Underwriter`, `RelationshipManager`, `ProgramManager`, `Admin`

No extra settings needed — just the name.

### B — Property Mapping (Scope Mapping)

**Customisation → Property Mappings → Create** → type: **Scope Mapping**

| Field | Value |
|-------|-------|
| Name | `Nebula Roles` |
| Scope name | `nebula_roles` |
| Expression | `return list(request.user.ak_groups.values_list("name", flat=True))` |

### C — OAuth2/OIDC Provider

**Applications → Providers → Create** → type: **OAuth2/OpenID Provider**

| Field | Value |
|-------|-------|
| Name | `nebula` |
| Authorization flow | `default-provider-authorization-implicit-consent` |
| Client type | Public |
| Client ID | `nebula` ← clear the auto-generated value and type this |
| Redirect URIs | `http://localhost:5173/callback` and `http://localhost:5173/` |
| Signing Key | `authentik Self-signed Certificate` |

Under **Advanced protocol settings → Selected Scopes**, add:
- `authentik default OAuth Mapping: OpenID 'openid'`
- `authentik default OAuth Mapping: OpenID 'email'`
- `authentik default OAuth Mapping: OpenID 'profile'`
- `Nebula Roles` (created in step B)

### D — Application

**Applications → Applications → Create**

| Field | Value |
|-------|-------|
| Name | `Nebula CRM` |
| Slug | `nebula` ← must be exactly this; matches `Authentication__Authority` in docker-compose |
| Provider | `nebula` (created in step C) |

### E — Add akadmin to Admin group

**Directory → Groups → Admin → Users tab → Add `akadmin`**

---

## Step 4 — Verify OIDC discovery

```bash
curl -s http://localhost:9000/application/o/nebula/.well-known/openid-configuration | python3 -m json.tool
```

Confirm:
- `"issuer"` = `"http://localhost:9000/application/o/nebula/"`
- `"nebula_roles"` appears in `"scopes_supported"`

## Step 5 — Test API

```bash
# Health (no auth)
curl http://localhost:5113/healthz

# Authenticated (dev mode — fake token, no real OIDC needed)
# Start the frontend: cd experience && pnpm dev
# Open http://localhost:5173 — if the dashboard loads data, the full stack works.
```

## Troubleshooting

| Issue | Resolution |
|-------|-----------|
| `authentik-server` fails to start | Check `AUTHENTIK_SECRET_KEY` is set in `.env`. |
| Blueprint `status: error`, `context: {}` | Worker logs: `docker-compose logs authentik-worker --tail=100`. Restart worker: `docker-compose restart authentik-worker`. If it persists, use the manual setup above. |
| OIDC discovery returns 404 | Application slug is wrong. Must be `nebula`. Check in admin UI: Applications → Nebula CRM → Slug field. |
| JWT audience mismatch (`401` on API) | `aud` claim must equal `nebula`. Requires `client_id=nebula` on the provider. |
| `nebula_roles` claim missing from token | Scope mapping not assigned to provider. Re-do step C, add `Nebula Roles` to selected scopes. |
| API returns `401` on first request after login | `HttpCurrentUserService` auto-creates a `UserProfile` on first request. Check API logs for DB errors. |
| Full reset | `docker-compose down -v && docker-compose up -d` — wipes the DB and re-provisions from scratch. |
