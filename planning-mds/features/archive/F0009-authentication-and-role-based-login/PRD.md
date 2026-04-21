# F0009: Authentication + Role-Based Login

**Feature ID:** F0009
**Feature Name:** Authentication + Role-Based Login
**Priority:** Critical
**Phase:** Phase 1

## Feature Statement

**As a** Nebula user (DistributionUser, Underwriter, or BrokerUser)
**I want** to sign in through a real login flow and enter a role-appropriate workspace
**So that** access is secure, explicit, and no longer dependent on development token helpers

## Business Objective

- Replace implicit dev-token access with real OIDC login/session entry.
- Enforce deterministic route and API authorization behavior.
- Enable BrokerUser pilot access with strict scope and field boundaries.

## Success Metrics

- 95% of successful logins complete in <= 5 seconds (excluding IdP outages).
- 100% of protected routes require authenticated session.
- 100% of BrokerUser responses exclude `InternalOnly` fields.
- 100% of BrokerUser reads are tenant-scoped or denied.

## Scope

### In Scope

- Login screen + OIDC redirect/callback flow.
- Session bootstrap and expiry handling.
- Role-based landing and protected navigation.
- BrokerUser scoped access boundaries for Phase 1.
- Deterministic seeded identity matrix for acceptance testing.

### Out of Scope

- User self-registration.
- Password reset inside Nebula.
- MFA policy changes in Nebula.
- Broker submission/renewal self-service.
- Silent token renewal.

## Mandatory Implementation Decisions (How)

1. OIDC flow is Authorization Code + PKCE using `oidc-client-ts`.
2. Required auth routes: `/login`, `/auth/callback`, `/unauthorized`.
3. Route behavior is deterministic:
   - unauthenticated -> `/login`
   - authenticated but unauthorized route -> `/unauthorized`
   - API `401` -> clear session + redirect `/login`
   - API `403` -> in-page permission-safe message with trace id when present
4. Landing routes:
   - DistributionUser -> `/`
   - Underwriter -> `/`
   - BrokerUser -> `/brokers`
5. Role precedence for multi-role users:
   - `Admin` > `DistributionManager` > `DistributionUser` > `Underwriter` > `BrokerUser`
6. Broker scope resolution anchor is `broker_tenant_id` claim (stable IdP-issued broker identity):
   - BrokerUser token must include `broker_tenant_id`
   - exactly one active broker tenant mapping must resolve from that value
   - missing/unknown/ambiguous mapping -> deny
7. Matrix/policy synchronization is release-blocking:
   - BrokerUser section in `authorization-matrix.md` must match `policy.csv` rows.
8. Broker field boundaries are enforced server-side per `BROKER-VISIBILITY-MATRIX.md`.
9. Broker isolation + field visibility enforcement order is mandatory:
   - query/service layer tenant isolation first (cross-broker rows never fetched for BrokerUser)
   - Casbin ABAC resource/action decision second
   - DTO/response filtering third for `InternalOnly` field stripping

## Broker Visibility Rules

1. Broker users can view only records linked to their broker scope.
2. Cross-broker visibility is never allowed.
3. Every exposed field is classified as `BrokerVisible` or `InternalOnly`.
4. `InternalOnly` fields must never be returned for BrokerUser.
5. Unknown scope or unknown field classification defaults to deny.

## Gap Closure Matrix

| Gap | Current State | Required Closure in F0009 |
|-----|---------------|---------------------------|
| No login route | App routes directly into protected pages | Implement `/login` and enforce guard redirect on all protected routes |
| No callback bootstrap | No real browser OIDC callback processing | Implement `/auth/callback` with strict state validation and fail-closed behavior |
| Dev token dependency | API client always uses `getDevToken()` | Add OIDC mode path where token source is OIDC session, not `dev-auth.ts` |
| Broker policy not implemented | Matrix has BrokerUser section but policy.csv missing rows | Add explicit BrokerUser policy rows and parity check against matrix |
| Broker field boundary undefined | No concrete response field classification | Enforce `BROKER-VISIBILITY-MATRIX.md` on all BrokerUser-allowed endpoints |
| Broker scope key instability | Email-based linkage can drift with profile changes | Use stable IdP-issued `broker_tenant_id` claim and explicit tenant mapping |
| Seed identity ambiguity | Broker record exists, IdP user provisioning unclear | Seed/provision `broker001@example.local` as BrokerUser in authentik, idempotently |
| Unauthorized UX ambiguity | Redirect vs 403 behavior varies | Standardize behaviors defined in this PRD and implementation contract |

## Prerequisites

- F0005 IdP migration remains stable.
- BrokerUser policy rows implemented in Casbin artifacts.
- Broker visibility matrix approved and testable.
- Seed identities provisioned with `nebula_roles` claims.

## Dependencies

- F0005 (IdP migration and claims normalization)
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- Feature docs in this folder:
  - `IMPLEMENTATION-CONTRACT.md`
  - `BROKER-VISIBILITY-MATRIX.md`

## Related Stories

- F0009-S0001 - Login Screen and OIDC Redirect
- F0009-S0002 - OIDC Callback and Session Bootstrap
- F0009-S0003 - Role-Based Entry and Protected Navigation
- F0009-S0004 - BrokerUser Access Boundaries
- F0009-S0005 - Seeded User Access Validation Matrix
