# F0009 Architecture — Authentication and Broker Boundaries

**Owner:** Architect  
**Status:** Draft (Phase B)  
**Last Updated:** 2026-03-04

## 1. Scope

This document defines implementation architecture for:

- Real OIDC login and callback in frontend.
- Session bootstrap and route guards.
- Role-based landing and navigation protection.
- BrokerUser tenant scope and field-level response boundaries.

## 2. Container View (F0009 Delta)

```mermaid
C4Container
    title F0009 Authentication + Authorization Flow
    Person(user, "Nebula User", "DistributionUser / Underwriter / BrokerUser")
    System_Boundary(nebula, "Nebula CRM") {
        Container(spa, "experience", "React + TypeScript", "Login, callback, guarded routes")
        Container(api, "engine API", ".NET Minimal APIs", "JWT auth + ABAC authorization + response filtering")
        ContainerDb(db, "PostgreSQL", "UserProfile + Broker + domain data", "Scope linkage and business data")
    }
    System_Ext(idp, "authentik", "OIDC provider")

    Rel(user, spa, "Uses", "HTTPS")
    Rel(spa, idp, "OIDC auth code + PKCE")
    Rel(spa, api, "Bearer token API calls", "HTTPS/JSON")
    Rel(api, idp, "JWT metadata/JWKS validation", "OIDC")
    Rel(api, db, "Reads/writes domain + user profile data", "EF Core")
```

## 3. Runtime Sequence

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Frontend SPA
    participant IDP as authentik
    participant API as Nebula API
    participant DB as Postgres

    U->>FE: Open protected route
    FE->>FE: No active session -> redirect /login
    U->>FE: Click Sign in
    FE->>IDP: Authorization request (code + PKCE)
    IDP-->>FE: Redirect /auth/callback?code&state
    FE->>FE: Validate callback state, bootstrap session
    FE->>API: GET protected resource (Bearer token)
    API->>API: Validate JWT + roles
    API->>DB: Resolve user profile + resource scope
    API-->>FE: 200 / 401 / 403 ProblemDetails
    FE->>FE: Apply deterministic route/error behavior
```

## 4. Authorization Model (F0009)

- Role claim source: `nebula_roles`.
- Multi-role precedence: `Admin > DistributionManager > DistributionUser > Underwriter > BrokerUser`.
- Route-level checks provide UX gating only.
- API-level ABAC is authoritative.

### BrokerUser Constraints

- Allowed actions/resources come only from:
  - `planning-mds/security/authorization-matrix.md` section 2.10
  - `planning-mds/security/policies/policy.csv` BrokerUser rows
- Default deny if resource/action not explicitly allowed.
- Tenant scope resolution:
  - token must include `broker_tenant_id` claim.
  - exactly one active broker tenant mapping must resolve from `broker_tenant_id`.
  - missing, unknown, or ambiguous mapping => deny.
- Field boundaries enforced server-side per:
  - `planning-mds/features/F0009-authentication-and-role-based-login/BROKER-VISIBILITY-MATRIX.md`

Enforcement order:
1. query/service layer tenant isolation (cross-tenant rows filtered pre-response)
2. Casbin ABAC resource/action decision
3. DTO/response field filtering (`InternalOnly` stripped)

## 5. Route and Session Contracts

Required routes:

- `/login`
- `/auth/callback`
- `/unauthorized`

Deterministic behavior:

- Unauthenticated route access -> `/login`
- Authenticated but unauthorized route -> `/unauthorized`
- API `401` -> clear session + `/login`
- API `403` -> keep context + permission-safe message with `traceId` when present

Session:

- Source of truth: OIDC session (`oidc-client-ts`).
- Silent renew is deferred (not in F0009).
- Expired token path: clear state and redirect `/login?reason=session_expired`.

## 6. Policy Artifact Requirements

F0009 is release-blocked unless all are true:

1. Matrix and policy parity for BrokerUser entries is verified.
2. Broker scope resolution logic implemented in backend services.
3. Server-side response filtering excludes `InternalOnly` fields for BrokerUser.

## 6.1 RLS Position (F0009)

- PostgreSQL RLS is **not required** for F0009 Phase 1 delivery.
- Compensating controls required in Phase 1:
  - no raw SQL tool exposure for agent/MCP interfaces
  - mandatory service-layer tenant filtering by `broker_tenant_id`
  - mandatory Casbin ABAC checks for every protected action
  - mandatory server-side field filtering
  - audit logging for policy decisions and response classification
- RLS is planned as Phase 2 hardening for high-risk tenant tables.

## 7. Implementation Handoffs

Backend:

- Implement BrokerUser scope filters on broker/contact/dashboard/timeline/task reads.
- Enforce field filtering for BrokerUser response DTOs.
- Keep ProblemDetails semantics for 401/403.

Frontend:

- Implement login + callback + unauthorized routes.
- Add auth mode support (`oidc` primary, `dev` fallback flag only).
- Replace default `getDevToken()` path in real-login mode.

QA:

- Add matrix tests for all three seeded users.
- Add cross-broker deny and InternalOnly field exclusion tests.

DevOps:

- Ensure authentik seed/provisioning includes `BrokerUser` group and required users.

## 8. Secondary Channel Security (MCP/Agent)

If MCP/agent access is enabled for F0009-related resources:

- MCP tools must enforce the same ABAC + tenant + field policies as API.
- Raw SQL tools are prohibited.
- Tool contracts must accept subject context including `broker_tenant_id`.
- Tool calls must be audit-logged with policy decision outcomes.
