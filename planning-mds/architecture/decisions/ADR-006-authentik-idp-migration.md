# ADR-006: Replace Keycloak with authentik as Identity Provider

**Status:** Accepted

**Date:** 2026-03-01

**Deciders:** Architecture Team

**Supersedes:** ADR-Authentication-Strategy (Keycloak)

**Technical Story:** F0005 — IdP Migration (Keycloak → authentik)

---

## Context and Problem Statement

Nebula CRM was originally designed with Keycloak as the OIDC identity provider. Before production data exists and before the backend is implemented, we are switching to **authentik** as the IdP. This is the optimal window to make this change: zero production principals to migrate, no deployed data, and the principal key fields in the data model can be redesigned correctly from the start.

This ADR covers:
1. The rationale for switching from Keycloak to authentik.
2. The revised token validation and OIDC configuration.
3. The **principal key architecture** — how the app maintains a stable internal `UserId` independent of any specific IdP.
4. The claims normalization layer required to map authentik JWT claims to Nebula's role model.
5. Casbin impact and the (unchanged) authorization model.

---

## Decision Drivers

- **Operational simplicity:** authentik has a cleaner UI, better documentation for small teams, and first-class Python extensibility.
- **Modern defaults:** authentik ships with OAuth2/OIDC, SAML, LDAP proxy, and outpost support out of the box; less realm/client configuration overhead than Keycloak.
- **Docker footprint:** authentik runs as a server + worker pair, similar complexity to Keycloak; Redis is added as a dependency but is low-overhead.
- **Pre-production window:** No production users or data exist. This is the right moment to introduce a stable internal `UserId` layer, making the app IdP-agnostic for all future migrations.
- **Security posture:** authentik maintains active development with regular CVE response; Keycloak's upgrade cycle is more disruptive.

---

## Considered Options

1. **Keep Keycloak** — No migration needed; more team familiarity; heavier admin UI.
2. **Switch to authentik** — Cleaner admin UX; more flexible property mappings; active development.
3. **Switch to Auth0 / Okta** — SaaS removes infra burden; per-user licensing cost prohibitive for internal-only app; SaaS dependency unacceptable for insurance data.

---

## Decision Outcome

**Chosen option: authentik**

Nebula CRM will use **authentik** as the OIDC provider. The app will implement a **claims normalization layer** (described below) so that business logic never references the raw IdP `sub` claim. All identity references within the application use a stable internal `UserId (uuid)`.

---

## Architecture Changes

### 1. OIDC Configuration

| Parameter | Keycloak (old) | authentik (new) |
|-----------|---------------|-----------------|
| Issuer (iss) | `http://keycloak:8080/realms/nebula` | `http://authentik-server:9000/application/o/nebula/` |
| JWKS endpoint | `http://keycloak:8080/realms/nebula/protocol/openid-connect/certs` | `http://authentik-server:9000/application/o/nebula/jwks/` |
| Discovery URL | `http://keycloak:8080/realms/nebula/.well-known/openid-configuration` | `http://authentik-server:9000/application/o/nebula/.well-known/openid-configuration` |
| Token endpoint | `http://keycloak:8080/realms/nebula/protocol/openid-connect/token` | `http://authentik-server:9000/application/o/nebula/token/` |
| Audience (aud) | `nebula-api` | `nebula` |
| Roles claim path | `realm_access.roles` (nested) | `nebula_roles` (flat array, custom property mapping) |

**Backend configuration change:**
```yaml
# Environment variable → docker-compose and app settings
Authentication__Authority: http://authentik-server:9000/application/o/nebula/
Authentication__Audience: nebula
Authentication__RolesClaim: nebula_roles
```

### 2. Principal Key Architecture (IdP-Agnostic Design)

**Problem:** If the app stores raw IdP `sub` values in entity fields (e.g., `AssignedTo`, `CreatedBy`, `ActorSubject`), any future IdP migration invalidates all stored principals — even if the same human is migrated.

**Solution:** Introduce a stable internal `UserId (uuid)` at the `UserProfile` table. The app maps `(iss, sub)` → `UserId` on every authenticated request. All business entities store `UserId`, not the IdP `sub`.

```
JWT claims                  App layer
─────────────               ──────────────────────────────────────
iss = "https://..."         UserProfile table:
sub = "abc123"      ─────►  UserId (uuid, PK) = stable internal key
                            IdpIssuer = iss value
                            IdpSubject = sub value
                            UNIQUE(IdpIssuer, IdpSubject)
```

**UserProfile table (revised):**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| UserId | uuid | PK, NOT NULL | Stable internal identifier; gen_random_uuid() on first login |
| IdpIssuer | varchar(255) | NOT NULL | JWT `iss` claim value |
| IdpSubject | varchar(255) | NOT NULL | JWT `sub` claim value |
| Email | varchar(255) | NOT NULL | From JWT `email` claim |
| DisplayName | varchar(255) | NOT NULL | From JWT `name` or `given_name`+`family_name` |
| Department | varchar(100) | NULL | From JWT `department` custom claim |
| RegionsJson | jsonb | NULL | From JWT `regions` custom claim |
| RolesJson | jsonb | NOT NULL | From JWT `nebula_roles` custom claim |
| CreatedAt | timestamptz | NOT NULL | UTC |
| UpdatedAt | timestamptz | NOT NULL | UTC |

**Unique index:** `UQ_UserProfile_IdpIssuer_IdpSubject` on `(IdpIssuer, IdpSubject)`.

**Entity field renames (all "Keycloak subject" fields → UUID):**

| Entity | Old field | New field | Type change |
|--------|-----------|-----------|-------------|
| BaseEntity | `CreatedBy (string)` | `CreatedByUserId (uuid)` | string → uuid |
| BaseEntity | `UpdatedBy (string?)` | `UpdatedByUserId (uuid?)` | string → uuid |
| BaseEntity | `DeletedBy (string?)` | `DeletedByUserId (uuid?)` | string → uuid |
| Broker | `ManagedBySubject (string?)` | `ManagedByUserId (uuid?)` | string → uuid |
| Program | `ManagedBySubject (string?)` | `ManagedByUserId (uuid?)` | string → uuid |
| Submission | `AssignedTo (string)` | `AssignedToUserId (uuid)` | string → uuid |
| Renewal | `AssignedTo (string)` | `AssignedToUserId (uuid)` | string → uuid |
| Task | `AssignedTo (string)` | `AssignedToUserId (uuid)` | string → uuid |
| Task | `CreatedBy (string)` | inherited from BaseEntity | — |
| ActivityTimelineEvent | `ActorSubject (string)` | `ActorUserId (uuid)` | string → uuid |
| WorkflowTransition | `ActorSubject (string)` | `ActorUserId (uuid)` | string → uuid |

**FK intent:** All `*UserId` fields reference `UserProfile.UserId`. For `ActivityTimelineEvent` and `WorkflowTransition` (append-only tables), use a logical reference without a hard FK constraint to preserve immutability and avoid cascade issues.

### 3. Claims Normalization Layer

A new application-layer service `IClaimsPrincipalNormalizer` handles the mapping from JWT claims to the Nebula principal model. This runs in middleware after JWT signature validation.

**Interface (Application layer):**
```csharp
public interface IClaimsPrincipalNormalizer
{
    Task<NebulaPrincipal> NormalizeAsync(ClaimsPrincipal jwtPrincipal);
}

public sealed record NebulaPrincipal(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Regions,
    bool IsInternalUser
);
```

**Implementation steps (Infrastructure layer):**
1. Extract `iss` and `sub` from `jwtPrincipal`.
2. Extract `nebula_roles` claim (flat string array) from JWT — see authentik property mapping config.
3. Upsert `UserProfile` by `(IdpIssuer=iss, IdpSubject=sub)`:
   - On match: update Email, DisplayName, Department, RegionsJson, RolesJson, UpdatedAt.
   - On no match: INSERT with new `UserId = gen_random_uuid()`.
4. Return `NebulaPrincipal` with the stable `UserId`.
5. Cache result for 5 minutes keyed by `(iss, sub)` to avoid DB upsert on every request.

**authentik property mapping (configured in authentik admin):**
```python
# authentik Property Mapping — Expression
# Maps group memberships to nebula_roles claim
return list(request.user.ak_groups.values_list("name", flat=True))
```

Groups in authentik should match Nebula role names exactly:
- `DistributionUser`, `DistributionManager`, `Underwriter`, `RelationshipManager`, `ProgramManager`, `Admin`

### 4. Token Validation (Backend)

The `.NET 10 JwtBearer` configuration changes only the `Authority` and `Audience` values. The JWKS key rotation, RS256 signature validation, and `exp`/`nbf` checking are standard and unchanged.

```csharp
// Program.cs (updated)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        // authentik: http://authentik-server:9000/application/o/nebula/
        options.Audience = builder.Configuration["Authentication:Audience"];
        // authentik: nebula
        options.RequireHttpsMetadata = false; // dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // JWKS fetched automatically from Authority/.well-known/openid-configuration
        };
    });
```

### 5. Token Refresh Flow (Updated)

The hybrid token storage strategy (access token in memory + refresh token in httpOnly cookie) from ADR-Auth-Token-Storage is **unchanged**. The backend `/auth/refresh` endpoint replaces `IKeycloakService` calls with `IAuthentikService` calls. The external contract (cookie in → access token out) is identical.

**Rename:** `IKeycloakService` → `IAuthentikOidcService` (Infrastructure layer).

### 6. Casbin Authorization (Unchanged)

The Casbin `model.conf` and `policy.csv` are **not changed**. The model evaluates:
```
m = r.sub.role == p.sub && r.obj.type == p.obj && r.act == p.act && eval(p.cond)
```

`r.sub` is the `NebulaPrincipal` object. `r.sub.id` now resolves to `NebulaPrincipal.UserId (uuid)` — a stable internal identifier. The condition `r.obj.assignee == r.sub.id` compares stored `AssignedToUserId (uuid)` against `NebulaPrincipal.UserId` — this is functionally equivalent to the previous `sub`-based comparison.

**No policy rows need updating.** Roles (`DistributionUser`, etc.) are extracted from the JWT `nebula_roles` claim by the claims normalizer and remain identical.

### 7. Docker Compose Changes

| Old service | New services |
|-------------|-------------|
| `keycloak` (1 container) | `authentik-server` + `authentik-worker` (2 containers) |
| — | `redis` (new dependency for authentik) |
| `docker/keycloak/nebula-realm.json` | `docker/authentik/` bootstrap blueprints |

authentik requires a separate PostgreSQL database (`authentik`), separate from the app database (`nebula`). The existing `docker/postgres/init-databases.sh` must be updated to also create the `authentik` database.

**New environment variables:**
```bash
AUTHENTIK_SECRET_KEY=<random 50-char secret>   # required; set via .env
```

### 8. Frontend OIDC Changes

The frontend replaces `keycloak-js` with a standard OIDC client library (e.g., `oidc-client-ts` or `@auth/core`). The authorization code + PKCE flow is identical; only the endpoint URLs change.

**dev-auth.ts (dev-only password grant helper):**
```typescript
const AUTHENTIK_TOKEN_URL = 'http://localhost:9000/application/o/nebula/token/';
const CLIENT_ID = 'nebula-crm';
```

---

## Consequences

### Positive
- App is IdP-agnostic: future IdP migrations only require updating the normalization layer.
- Stable `UserId` makes all entity ownership fields safe across IdP migrations.
- Clean `NebulaPrincipal` type gives backend a consistent, typed principal object.
- authentik property mappings offer flexible claim customization without realm XML.
- Casbin policies are unchanged.

### Negative
- Adds Redis as a new infrastructure dependency.
- Authentik requires two containers (server + worker) instead of one.
- `authentik` has no declarative realm-import equivalent to Keycloak's `nebula-realm.json`; initial setup is done via authentik Blueprints (YAML) or admin UI.
- All entity field renames (`AssignedTo` → `AssignedToUserId`, etc.) require consistent update across all backend code.

### Neutral
- OIDC protocol and JWT validation semantics are unchanged (RS256, PKCE, refresh rotation).
- Token storage strategy (ADR-Auth-Token-Storage) is unchanged.
- All existing Casbin policies and the authorization matrix are unchanged.
- The `UserProfile` create-on-first-login pattern (SOLUTION-PATTERNS.md §9) is unchanged; only the PK structure evolves.

---

## Migration Path (Since No Production Data Exists)

1. **Planning docs:** Update BLUEPRINT.md, SOLUTION-PATTERNS.md, data-model.md to reflect authentik and internal `UserId`.
2. **docker-compose.yml:** Replace `keycloak` with `authentik-server`, `authentik-worker`, `redis`.
3. **Backend (F0005-S0002):** Implement `IClaimsPrincipalNormalizer`; update `UserProfile` entity; rename all `*Subject`/`AssignedTo` fields to `*UserId`.
4. **Frontend (F0005-S0003):** Replace `keycloak-js` with `oidc-client-ts`; update OIDC URLs.
5. **authentik setup (F0005-S0001):** Create application, provider, and property mappings in authentik; export as Blueprint for reproducible dev setup.

---

## Related ADRs

- ADR-Authentication-Strategy (superseded by this ADR)
- ADR-Auth-Token-Storage (updated; token storage strategy unchanged)
- ADR-001: JSON Schema Validation (unchanged)
- ADR-005: Caching Strategy (applies to `NebulaPrincipal` 5-minute cache)
