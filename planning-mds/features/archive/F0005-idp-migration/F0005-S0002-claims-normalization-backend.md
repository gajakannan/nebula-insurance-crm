# F0005-S0002 — Claims Normalization Layer + Principal Key (Backend)

**Feature:** F0005 — IdP Migration
**Story ID:** F0005-S0002
**Owner:** Backend Developer
**Depends on:** F0005-S0001 (authentik running), F0005-S0004 (entity field renames)
**Priority:** Must-complete before F0001/F0002 backend implementation

---

## Story

As a backend developer, I need the API to validate authentik JWTs, map `(iss, sub)` to a stable internal `UserId`, and expose a typed `NebulaPrincipal` to all endpoints, so that no business logic ever references a raw IdP subject claim.

---

## Acceptance Criteria

1. The backend validates JWTs signed by authentik's JWKS endpoint. Invalid tokens return HTTP 401.
2. A `IClaimsPrincipalNormalizer` service maps `(iss, sub)` → `UserProfile.UserId (uuid)` via DB upsert.
3. On every authenticated request, `UserProfile` is upserted with latest Email, DisplayName, Roles, Regions from JWT claims. A 5-minute in-memory cache keyed by `(iss, sub)` prevents per-request DB writes.
4. All downstream application code receives a `NebulaPrincipal { UserId, Email, DisplayName, Roles, Regions }` — never a raw `ClaimsPrincipal` or `sub` string.
5. Casbin enforcer receives `r.sub` as the `NebulaPrincipal` object; `r.sub.id` resolves to `NebulaPrincipal.UserId`.
6. `NebulaPrincipal.Roles` is populated from the `nebula_roles` JWT claim (flat string array). If the claim is absent, Roles is empty and all ABAC checks fail (deny by default).
7. Unit tests cover: valid token → correct `UserId` returned; missing `nebula_roles` claim → empty roles; same `(iss, sub)` on second call → same `UserId` (idempotency).

---

## Implementation Notes

### Layer placement

```
API Layer
  └─ JWT middleware (validates signature, exp, aud, iss)
  └─ NebulaPrincipalMiddleware (calls IClaimsPrincipalNormalizer → injects NebulaPrincipal)

Application Layer
  └─ IClaimsPrincipalNormalizer (interface)
  └─ NebulaPrincipal (record type)
  └─ IAuthentikOidcService (interface for token refresh/revoke)

Infrastructure Layer
  └─ ClaimsPrincipalNormalizer : IClaimsPrincipalNormalizer (implements)
  └─ AuthentikOidcService : IAuthentikOidcService (replaces KeycloakService)
```

### NebulaPrincipal record

```csharp
public sealed record NebulaPrincipal(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Regions,
    bool IsInternalUser      // always true for MVP; external users denied at policy layer
);
```

### Claims extraction (authentik JWT)

| Claim | Source | Notes |
|-------|--------|-------|
| `iss` | Standard | Must match `Authentication:Authority` config |
| `sub` | Standard | authentik-generated unique subject per user |
| `email` | Standard OIDC scope | |
| `name` | Standard OIDC scope | Display name |
| `nebula_roles` | Custom property mapping | Flat `string[]` of group names |
| `regions` | Custom property mapping (future) | `string[]`; defaults to `[]` if absent |

### Casbin integration

```csharp
// Build ABAC subject from NebulaPrincipal
var casbinSubject = new
{
    id   = principal.UserId.ToString(),
    role = principal.Roles.FirstOrDefault() ?? string.Empty,
    // multi-role: iterate; for MVP single-role enforcement is sufficient
};

var allowed = await _enforcer.EnforceAsync(casbinSubject, resource, action);
```

### Token refresh endpoint update

`/auth/refresh` replaces `IKeycloakService.RefreshTokensAsync` with `IAuthentikOidcService.RefreshTokensAsync`. The external contract (httpOnly refresh cookie in → `{ access_token }` out) is unchanged.

---

## Edge Cases

- `nebula_roles` claim absent or empty → `NebulaPrincipal.Roles = []` → all ABAC enforcements deny → HTTP 403 on protected endpoints. Do not throw; log a warning and proceed.
- `UserProfile` upsert race condition (concurrent first logins for same user) → use `ON CONFLICT (IdpIssuer, IdpSubject) DO UPDATE` (Postgres upsert) to handle safely.
- Cache eviction: 5-minute TTL means profile changes (role assignment in authentik) take up to 5 minutes to propagate. Acceptable for MVP.
- `iss` claim URL with trailing slash: normalize before comparison (authentik always emits trailing slash; ensure config matches).

---

## Audit / Timeline Requirements

- `UserProfile` upsert is **not** an auditable event (it is infrastructure, not a business action).
- If a user is created via first login, log an informational structured log entry: `UserProfile created for {Email} ({UserId})`.
