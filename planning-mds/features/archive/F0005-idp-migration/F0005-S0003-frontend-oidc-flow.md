# F0005-S0003 — Frontend OIDC Flow Update

**Feature:** F0005 — IdP Migration
**Story ID:** F0005-S0003
**Owner:** Frontend Developer
**Depends on:** F0005-S0001 (authentik running)
**Priority:** Required before real login flow is implemented; dev-auth.ts fix is immediate

---

## Story

As a frontend developer, I need the React app to authenticate via **authentik** instead of Keycloak, so that the OIDC flow and dev-auth helper target the correct endpoints.

---

## Acceptance Criteria

1. `experience/src/services/dev-auth.ts` targets the authentik token endpoint (`http://localhost:9000/application/o/nebula/token/`) with `client_id=nebula-crm`.
2. When the full OIDC login flow is implemented (future story), it uses `oidc-client-ts` (or equivalent standard OIDC library) rather than `keycloak-js`.
3. No references to `keycloak`, `realms`, or Keycloak-specific URL patterns remain in frontend source code or config files.
4. The `api.ts` service continues to inject Bearer tokens from `getDevToken()` unchanged — the token source switches transparently.
5. E2E smoke test: `getDevToken()` returns a non-empty string; a GET to `/health` with that token returns 200.

---

## Implementation Notes

### dev-auth.ts changes (immediate)

```typescript
// Replace:
const KEYCLOAK_URL = 'http://localhost:8081/realms/nebula/protocol/openid-connect/token';
const CLIENT_ID = 'nebula-crm';

// With:
const AUTHENTIK_TOKEN_URL = 'http://localhost:9000/application/o/nebula/token/';
const CLIENT_ID = 'nebula-crm';
```

The rest of `dev-auth.ts` (token caching, `expires_in` handling) is **unchanged**.

### Full OIDC flow (when implemented — future sprint)

Replace `keycloak-js` with `oidc-client-ts`:

```typescript
import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const oidcConfig = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY,
  // e.g., http://localhost:9000/application/o/nebula/
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
  // e.g., nebula-crm
  redirect_uri: `${window.location.origin}/callback`,
  scope: 'openid profile email',
  response_type: 'code',
  // PKCE enabled by default in oidc-client-ts
};

export const userManager = new UserManager(oidcConfig);
```

Environment variables (add to `experience/.env.local`):
```bash
VITE_OIDC_AUTHORITY=http://localhost:9000/application/o/nebula/
VITE_OIDC_CLIENT_ID=nebula-crm
```

### Token storage

The ADR-Auth-Token-Storage hybrid strategy is **unchanged**:
- Access token: React context (in-memory)
- Refresh token: httpOnly cookie managed by backend `/auth/refresh`

The `oidc-client-ts` `UserManager` access token is extracted and stored in React context on login; the refresh token is NOT stored in the browser — it is sent to `/auth/token` to be stored server-side as an httpOnly cookie, same as the Keycloak flow.

---

## Edge Cases

- `dev-auth.ts` is dev-only and should never be used in production builds. The existing comment should remain.
- PKCE challenge: authentik public clients require PKCE. `oidc-client-ts` enables PKCE by default; no extra config needed.
- Logout: authentik end-session endpoint is `http://localhost:9000/application/o/nebula/end-session/`. Update any logout redirect logic accordingly.

---

## Audit / Timeline Requirements

None — frontend infrastructure change, no app data mutations.
