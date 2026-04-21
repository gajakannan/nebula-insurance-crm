# F0009 — Authentication + Role-Based Login — Status

**Overall Status:** Done (Archived)
**Last Updated:** 2026-03-10

## Story Checklist

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| F0009-S0001 | Login Screen and OIDC Redirect | Done | LoginPage, ProtectedRoute, Vite build guard, CI gate, env templates, tests |
| F0009-S0002 | OIDC Callback and Session Bootstrap | Done | AuthCallbackPage, useSessionTeardown, POST /auth/logout backend endpoint, tests |
| F0009-S0003 | Role-Based Entry and Protected Navigation | Done | Role-based landing (AuthCallbackPage), authEvents bus, useAuthEventHandler, UnauthorizedPage with reason param, api.ts 401/403 handling |
| F0009-S0004 | BrokerUser Access Boundaries | Done | BrokerScopeResolver, BrokerScopeUnresolvableException, tenant isolation + field filtering in all 5 services, BrokerUser DTOs, audit logging, integration tests |
| F0009-S0005 | Seeded User Access Validation Matrix | Done | All three dev identities provisioned in authentik blueprint; broker_tenant_id linked in DevSeedData; integration tests cover BrokerUser boundary checks |

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Login/session/role-boundary flows require end-to-end and regression validation. | Architect | 2026-02-14 |
| Code Reviewer | Yes | Independent review required for cross-cutting auth and route-guard behavior. | Architect | 2026-02-14 |
| Security Reviewer | Yes | Authentication, authorization, and tenant isolation are security-critical scope. | Architect | 2026-02-14 |
| DevOps | No | No net-new production runtime service introduced in final closeout scope. | Architect | 2026-02-14 |
| Architect | No | No unresolved architecture exceptions requiring explicit acceptance. | Architect | 2026-02-14 |

## Signoff Ledger (Execution Evidence)

| Role | Reviewer | Verdict | Evidence | Date | Notes |
|------|----------|---------|----------|------|-------|
| Quality Engineer | Quality Engineer agent | PASS | `experience/src/features/auth/tests/ProtectedRoute.test.tsx`; `experience/src/features/auth/tests/LoginPage.test.tsx`; `engine/tests/Nebula.Tests/Integration/AuthEndpointTests.cs` | 2026-03-07 | Core login, callback bootstrap, and protected-route flows validated. |
| Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0009-authentication-and-role-based-login/STATUS.md` | 2026-03-07 | Critical review findings addressed prior to completion. |
| Security Reviewer | Security agent | PASS | `planning-mds/security/F0009-security-review-checklist.md`; `planning-mds/security/F0009-csp-residual-risk.md` | 2026-03-07 | Security gate passed with documented residual risk acceptance. |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0009-S0001 | Quality Engineer | Quality Engineer agent | PASS | `experience/src/features/auth/tests/LoginPage.test.tsx` | 2026-03-07 | Login entry behavior and redirect handoff validated. |
| F0009-S0001 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0009-authentication-and-role-based-login/STATUS.md` | 2026-03-07 | Story accepted in final review gate. |
| F0009-S0001 | Security Reviewer | Security agent | PASS | `planning-mds/security/F0009-security-review-checklist.md` | 2026-03-07 | Login flow security controls verified. |
| F0009-S0002 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/AuthEndpointTests.cs`; `experience/src/features/auth/tests/useSessionTeardown.test.tsx` | 2026-03-07 | Callback bootstrap and logout/session teardown validated. |
| F0009-S0002 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0009-authentication-and-role-based-login/STATUS.md` | 2026-03-07 | Story accepted in final review gate. |
| F0009-S0002 | Security Reviewer | Security agent | PASS | `planning-mds/security/F0009-security-review-checklist.md` | 2026-03-07 | Session bootstrap and token handling controls reviewed. |
| F0009-S0003 | Quality Engineer | Quality Engineer agent | PASS | `experience/src/features/auth/tests/ProtectedRoute.test.tsx` | 2026-03-07 | Protected routing and unauthorized-path handling validated. |
| F0009-S0003 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0009-authentication-and-role-based-login/STATUS.md` | 2026-03-07 | Story accepted in final review gate. |
| F0009-S0003 | Security Reviewer | Security agent | PASS | `planning-mds/security/F0009-security-review-checklist.md` | 2026-03-07 | Route guard authorization checks reviewed. |
| F0009-S0004 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerUserAccessTests.cs` | 2026-03-07 | BrokerUser tenant isolation and field boundaries validated. |
| F0009-S0004 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0009-authentication-and-role-based-login/STATUS.md` | 2026-03-07 | Story accepted in final review gate. |
| F0009-S0004 | Security Reviewer | Security agent | PASS | `planning-mds/security/F0009-security-review-checklist.md` | 2026-03-07 | BrokerUser access-boundary controls accepted. |
| F0009-S0005 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerUserAccessTests.cs` | 2026-03-07 | Seeded identity validation coverage confirmed. |
| F0009-S0005 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0009-authentication-and-role-based-login/STATUS.md` | 2026-03-07 | Story accepted in final review gate. |
| F0009-S0005 | Security Reviewer | Security agent | PASS | `planning-mds/security/F0009-security-review-checklist.md`; `planning-mds/security/F0009-csp-residual-risk.md` | 2026-03-07 | Identity provisioning and residual-risk posture approved. |

## Prerequisite Tracking

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| F0005 IdP migration remains stable | Ready | authentik stack and claim normalization available |
| BrokerUser role added to authorization matrix | Ready | matrix section 2.10 exists |
| BrokerUser role added to policy.csv | Ready (artifact updated) | query-layer scope + field filtering implemented in code |
| Broker-visible data boundary list approved | Ready | `BROKER-VISIBILITY-MATRIX.md` added |
| Real-login frontend mode available (without `dev-auth.ts`) | Ready | VITE_AUTH_MODE=oidc is default; dev-auth.ts gated behind explicit VITE_AUTH_MODE=dev flag |
| Required authentik seeded identities provisioned | Ready | All three dev identities in blueprint: lisa.wong (DistributionUser), john.miller (Underwriter), broker001 (BrokerUser) |
| BrokerUser `broker_tenant_id` claim contract implemented | Ready | Scope mapping in blueprint, BrokerScopeResolver in engine, broker_tenant_id linked in DevSeedData |

## Release-Blocking Requirement Gaps

None. All prerequisite gaps are resolved.

## Coordination Notes

- F0001/F0002/F0005 status docs now explicitly mark auth/scope/runtime enforcement items as deferred to F0009.
- No cross-feature status entry should treat role/tenant enforcement as complete until F0009 stories are implemented and validated.

## Architecture-Ready Artifacts

- [x] PRD with mandatory "How" decisions
- [x] `IMPLEMENTATION-CONTRACT.md`
- [x] `BROKER-VISIBILITY-MATRIX.md`
- [x] `planning-mds/security/F0009-security-review-checklist.md`
- [x] Story-level deterministic acceptance criteria
