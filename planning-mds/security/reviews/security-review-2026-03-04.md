# F0009 Security Design Review
**Date:** 2026-03-04
**Reviewer:** Security Agent
**Feature:** F0009 — Authentication and Role-Based Login
**Phase:** B (Design Review — pre-implementation)
**Overall Assessment:** CONDITIONAL PASS

---

## Summary

F0009 introduces OIDC authentication (Authorization Code + PKCE via oidc-client-ts), a BrokerUser external role with tenant-scoped data access, and role-based navigation and landing routes. The architecture is sound. Ten findings were identified across authentication flow, session management, authorization enforcement, data visibility, and build-time security gates. All ten findings were assigned remediation contracts and are design-mitigated; implementation verification is pending. The feature is cleared for implementation with the mitigations described below enforced at release.

---

## Assessment: CONDITIONAL PASS

Release is gated on implementation verification of all HIGH and MEDIUM mitigations. LOW findings are documentation/UX gaps; they do not block release but must be in scope for the release cut.

| Severity | Count | Design Status at Review Close |
|----------|-------|------------------------|
| HIGH | 3 | Design-mitigated (F-001, F-002, F-003); implementation verification pending |
| MEDIUM | 6 | Design-mitigated (F-004, F-005, F-006, F-007, F-008, F-009); implementation verification pending |
| LOW | 1 | Design-mitigated (F-010); implementation verification pending |

---

## Findings

### F-001 — Dev-Auth Deployment Gate Missing
**Severity:** HIGH
**Component:** Frontend build / DevOps
**STRIDE category:** Spoofing (authentication bypass)

**Description:**
`VITE_AUTH_MODE=dev` enables a local development bypass (`dev-auth.ts`) that returns a static token without any IdP challenge. There was no build-time or CI-time guard preventing this mode from being shipped to production. An accidental or deliberate production build with `VITE_AUTH_MODE=dev` would disable all authentication for every user.

**Exploit scenario:**
A developer sets `VITE_AUTH_MODE=dev` in a `.env.local` file. If that variable leaks into a CI environment variable or a production Dockerfile, the built bundle bypasses authentik entirely. Any HTTP request with no credentials would be treated as authenticated.

**Remediation (§13 of IMPLEMENTATION-CONTRACT.md):**
- Vite plugin (`nebula-auth-mode-guard`) added to `vite.config.ts` `buildStart` hook — throws a hard error if `VITE_AUTH_MODE=dev AND NODE_ENV=production`.
- CI assertion step added to `.github/workflows/frontend-ui.yml` before the build step — fails fast if the env var is set.
- `.env.example`, `.env.staging`, `.env.production` committed with `VITE_AUTH_MODE=oidc`.
- `.env.development.local.example` documents `VITE_AUTH_MODE=dev` as dev-only.
- `.gitignore` prevents `.env.local` and `.env.development.local` from being committed.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-002 — Incomplete Session Teardown
**Severity:** HIGH
**Component:** Backend (`POST /auth/logout`) + Frontend
**STRIDE category:** Elevation of privilege (session reuse after logout)

**Description:**
No explicit logout endpoint existed. On logout or 401, there was no defined sequence for: revoking the refresh token at authentik, clearing the httpOnly refresh cookie, clearing in-memory OIDC user state, or removing PKCE/state artifacts from sessionStorage. A user who logged out could have their refresh token reused by an attacker with cookie access until the token expired naturally.

**Exploit scenario:**
User logs out. The httpOnly refresh cookie is not cleared (no `Set-Cookie: Max-Age=0` response). An attacker with network access or a compromised proxy replays the refresh token to obtain new access tokens after the user believes they have logged out.

**Remediation (§2.1 of IMPLEMENTATION-CONTRACT.md):**
- `POST /auth/logout` endpoint created. Backend: extracts refresh token from httpOnly cookie, calls authentik revocation endpoint (best-effort), unconditionally responds with `Set-Cookie: refresh_token=; Max-Age=0; HttpOnly; Secure; SameSite=Strict; Path=/`, returns 204. Accepts unauthenticated requests.
- Frontend `useSessionTeardown` hook: fire-and-forget POST → `removeUser()` → `clearStaleState()` → redirect. Ordering is mandatory.
- `authEvents` event bus decouples `api.ts` (plain module) from React hooks. Both logout action and 401 intercept trigger teardown.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-003 — JWT Audience Validation Not Explicit
**Severity:** HIGH
**Component:** Backend JWT middleware
**STRIDE category:** Spoofing (token reuse from another application)

**Description:**
The JWT bearer middleware was configured without explicitly setting `ValidateAudience = true` or providing `ValidAudiences`. The default ASP.NET Core JWT bearer behavior may skip audience validation or use a permissive default. A token issued for a different application on the same authentik instance (with the same issuer but a different `aud`) would be accepted.

**Exploit scenario:**
A second application on the same authentik instance issues tokens with `aud = "other-app"`. An attacker with a valid token for `other-app` presents it to Nebula's API. Without explicit audience validation, the token passes and the attacker gains access under whatever `nebula_roles` are present in the token (which could be injected if the IdP property mapping is not locked down).

**Remediation (§5 of IMPLEMENTATION-CONTRACT.md):**
- `ValidateAudience = true` and `ValidAudiences = [audience]` added explicitly to `TokenValidationParameters` in `Program.cs`.
- Null-guard added: `Authentication:Audience` configuration value throws `InvalidOperationException` at startup if absent — prevents silent misconfiguration.
- `aud` claim documented as required in §5 with mandatory value `nebula`. Validation enforced in middleware, not in application code.
- Integration tests: wrong `aud` → 401, missing `aud` → 401, correct `aud = "nebula"` → not 401.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-004 — ActivityTimelineEvent Leaks Internal Description to BrokerUser
**Severity:** MEDIUM
**Component:** Backend timeline service / DTO shaping
**STRIDE category:** Information disclosure

**Description:**
`ActivityTimelineEvent.EventDescription` contains full internal narrative including staff names, system references, and policy metadata. There was no broker-safe alternative. Simply filtering the field at read time risks returning null/empty descriptions that break BrokerUser timeline UX, and any future change to the filter logic risks accidental re-exposure.

**Exploit scenario:**
BrokerUser calls `GET /brokers/{id}/timeline`. The service returns `eventDescription` values such as "Submission triaged by underwriter J. Smith — referred to Program X — risk flag: high arson score". This leaks internal staff identity, workflow state, and policy diagnostic metadata to the external user.

**Remediation (§8.1 and BROKER-VISIBILITY-MATRIX.md):**
- `BrokerDescription varchar(500) NULL` column added to `ActivityTimelineEvent` (Migration 005).
- Domain services populate `BrokerDescription` at event creation time using fixed safe templates — never derived post-hoc from `EventDescription`.
- Template list codified in `BROKER-VISIBILITY-MATRIX.md §BrokerDescription Template Ownership`.
- `EventDescription` classified `InternalOnly` — never returned to BrokerUser.
- Non-approved event types excluded entirely from BrokerUser query results (not returned with null field).
- BrokerUser timeline DTO omits `eventDescription` and `actorUserId`; returns `brokerDescription`, `actorDisplayName`.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-005 — BrokerUser Task Scope Undefined
**Severity:** MEDIUM
**Component:** Backend task service
**STRIDE category:** Broken access control

**Description:**
The task access model for BrokerUser was unspecified. It was unclear whether BrokerUser could read all tasks, tasks assigned to them, or tasks linked to their broker entity. Without a defined scope rule, the implementation would either over-share (tasks from unrelated entities) or under-share (no tasks at all).

**Exploit scenario:**
Without an explicit scope predicate, a BrokerUser could enumerate tasks linked to other brokers, internal submissions, or renewal workflows — leaking assignee identity, workflow state, and internal notes.

**Remediation (§12 of IMPLEMENTATION-CONTRACT.md):**
- Scope rule: `LinkedEntityType = 'Broker' AND LinkedEntityId = <broker_id resolved from broker_tenant_id> AND IsDeleted = false`.
- No schema change required — existing polymorphic link fields cover the scope.
- Cross-broker and non-broker-linked tasks are not returned.
- If broker scope resolution fails (missing/unknown/ambiguous `broker_tenant_id` mapping), return HTTP 403 `broker_scope_unresolvable`; if scope is valid and resolved but records do not match filters, return an empty result set.
- BrokerUser task DTO omits `assignedToUserId`, `createdByUserId`, `updatedByUserId`, `rowVersion`, and audit timestamps except `createdAt`.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-006 — Dashboard BrokerUser Resource Classification Missing
**Severity:** MEDIUM
**Component:** Backend dashboard service / policy.csv
**STRIDE category:** Information disclosure

**Description:**
`dashboard_kpi`, `dashboard_pipeline`, and `dashboard_nudge` were not classified for BrokerUser in the authorization matrix or BROKER-VISIBILITY-MATRIX.md. Three of four KPI fields and all pipeline fields are built on submission/renewal data, which is DENY for BrokerUser. Without explicit classification, the implementation risked returning submission/renewal aggregates to BrokerUser.

**Exploit scenario:**
BrokerUser calls `GET /dashboard/kpis`. Response includes `openSubmissions` (count of open submissions across all accounts), `renewalRate`, and `avgTurnaroundDays` — all computed from resources the BrokerUser is not authorized to see individually.

**Remediation (§14 of IMPLEMENTATION-CONTRACT.md, BROKER-VISIBILITY-MATRIX.md §Dashboard Resources):**
- `dashboard_kpi` → DENY. Policy lines removed; implicit Casbin deny applies.
- `dashboard_pipeline` → DENY. Policy lines removed.
- `dashboard_nudge` → ALLOW with mandatory 4-condition query filter: `nudgeType = 'OverdueTask' AND linkedEntityType = 'Broker' AND linkedEntityId IN (resolved broker scope)`. `StaleSubmission` and `UpcomingRenewal` nudge types excluded entirely.
- `authorization-matrix.md §2.10` updated. `policy.csv` BrokerUser block updated.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-007 — No Policy/Matrix Parity Enforcement
**Severity:** MEDIUM
**Component:** CI / QE
**STRIDE category:** Security process gap

**Description:**
`authorization-matrix.md §2.10` and `policy.csv` are the two sources of truth for BrokerUser authorization. They are maintained by different agents (Architect/Security vs. DevOps) and there was no automated check to detect drift between them. A change to one document not reflected in the other would silently create a gap between stated policy and enforced policy.

**Exploit scenario:**
Architect adds a new BrokerUser ALLOW row to `authorization-matrix.md` for a new resource as part of a future feature. If the corresponding `policy.csv` line is not added, Casbin silently denies the resource — or vice versa: if `policy.csv` is updated without updating the matrix, BrokerUser gains access to a resource not reflected in the approved authorization model.

**Remediation:**
- `scripts/check-policy-parity.py` created. Parses `policy.csv` BrokerUser rows and `authorization-matrix.md §2.10` ALLOW decisions. Diffs both directions. Exits 1 on mismatch.
- CI step added to `.github/workflows/ci-gates.yml` running the parity check on every push. Release is blocked if matrix and `policy.csv` are out of sync for BrokerUser resources/actions.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-008 — BrokerUser Read Operations Not Audit-Logged
**Severity:** MEDIUM
**Component:** Backend services
**STRIDE category:** Repudiation (no audit trail)

**Description:**
The security posture for Phase 1 explicitly defers Row-Level Security to Phase 2, relying on compensating controls: tenant-scoped query filters, ABAC checks, server-side field filtering, and audit logs. The audit log compensating control was unimplemented. Without it, there is no record of which BrokerUser read which broker data and when, making post-incident forensics impossible.

**Exploit scenario:**
A BrokerUser account is compromised. The attacker enumerates broker records and contacts over a period of days. Without audit logs, the security team cannot determine what data was accessed, how many records were viewed, or the precise timeline of the breach.

**Remediation (§16 of IMPLEMENTATION-CONTRACT.md):**
- `ICurrentUserService.BrokerTenantId` property added — reads `broker_tenant_id` claim.
- `AuditBrokerUserRead` private helper added to `BrokerService`, `ContactService`, `TimelineService`, `TaskService`, `DashboardService`.
- Guard: emits only when `Roles.Contains("BrokerUser")`. Zero overhead for internal roles.
- Structured log properties: `EventType`, `Resource`, `BrokerTenantId`, `ResolvedBrokerId`, `EntityId`, `OccurredAt`. Log level: `Information`.
- Covered resources: `broker.list`, `broker.detail`, `broker.contacts`, `broker.timeline`, `broker.tasks`, `dashboard.nudges`.
- Write operations excluded — those are covered by `ActivityTimelineEvent` domain events.

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-009 — No Content Security Policy for OIDC Flow
**Severity:** MEDIUM
**Component:** Frontend (Vite dev server + production nginx)
**STRIDE category:** Information disclosure (XSS → PKCE state theft)

**Description:**
`oidc-client-ts` stores PKCE `code_verifier` and `state` in `sessionStorage` during the authorization code flow. Without a Content Security Policy, a successful XSS attack on any page of the application can read `sessionStorage` and exfiltrate the PKCE state before the callback completes, potentially enabling an authorization code injection attack.

**Exploit scenario:**
An XSS payload (e.g., from a reflected parameter on a search page) executes `sessionStorage.getItem('oidc.user:...')` during the OIDC flow window, extracts the `state` and `code_verifier`, and relays them to an attacker-controlled endpoint. If the attacker can intercept or predict the authorization code, they can complete the PKCE exchange.

**Remediation:**
- Dev CSP added to `vite.config.ts` `server.headers`: `default-src 'self'`, `script-src 'self' 'unsafe-eval'` (Vite HMR requirement), `connect-src 'self' http://localhost:9000` (authentik discovery + token endpoints), `frame-src 'none'`, `object-src 'none'`.
- Production CSP added to `experience/nginx.conf`: same policy without `unsafe-eval`, with `REPLACE_WITH_OIDC_AUTHORITY` placeholder for deployers.
- Security headers added: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`.
- Residual risk documented in `planning-mds/security/F0009-csp-residual-risk.md`: `unsafe-inline` accepted for style-src; sessionStorage PKCE state residual risk noted for Phase 2 (consider memory-only PKCE storage).

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

### F-010 — Deactivated Broker UX Path Undefined
**Severity:** LOW
**Component:** Backend scope resolution + Frontend route guard
**STRIDE category:** Denial of service (UX — user cannot determine why access fails)

**Description:**
A BrokerUser whose broker tenant mapping is deactivated or missing in Nebula can still authenticate successfully at authentik (the IdP has no knowledge of broker lifecycle state). After login they are redirected to `/brokers`. The first API call fails. There was no contract for what HTTP status/code the backend returns, how the frontend handles it, or what message the user sees. Without a defined path, the user would see a generic error or an infinite loading state with no actionable information.

**Exploit scenario:**
Not a direct security bypass. The risk is: a deactivated broker user lands on `/brokers`, all API calls return 403, TanStack Query retries, the UI shows a broken state. In the worst case, if the generic 403 handler triggers a navigation that re-triggers the API call, the user sees a redirect loop.

**Remediation (§6.1 and §15 of IMPLEMENTATION-CONTRACT.md):**
- Backend: scope resolution failure returns HTTP 403 with `code: "broker_scope_unresolvable"` ProblemDetails. Does not trigger session teardown.
- Frontend `api.ts` interceptor: detects `status === 403 AND code === "broker_scope_unresolvable"`, emits `'broker_scope_unresolvable'` on `authEvents` bus, returns never-resolving promise (TanStack Query does not retry).
- `useAuthEventHandler`: handles event, calls `navigate('/unauthorized?reason=broker_inactive', { replace: true })`. No session teardown.
- `UnauthorizedPage` reads `reason` param: `broker_inactive` → "Your broker account is currently inactive. Please contact your administrator."
- Distinguished from: 401 (session teardown), generic 403 (stay in context), route-level deny (generic `/unauthorized`).

**Status:** DESIGN-MITIGATED (pending implementation verification)

---

## Dependency Order (Resolution Waves)

```
Pre-work (Architect)
  F-004 (BrokerDescription split-field contract)
  F-005 (task scope query contract)
    │
    └─ unblocks Wave 1 + Wave 2

Wave 1 (parallel — no inter-finding dependencies)
  F-001  DevOps + Frontend       Dev-auth gate
  F-002  Backend + Frontend      Session teardown
  F-003  Backend                 Audience validation
  F-007  QE + DevOps             Policy parity check CI
  F-009  Frontend                CSP headers

Wave 2 (needs F-005 complete, F-006 must precede F-008)
  F-006  Architect               Dashboard classification
  F-010  Architect + Frontend    Deactivated broker UX

Wave 3 (needs F-005 + F-006 complete)
  F-008  Backend                 BrokerUser audit events
```

---

## Release Gate Checklist

All items below must be verified before F0009 ships to staging:

- [ ] `VITE_AUTH_MODE=dev` build guard throws on `NODE_ENV=production`
- [ ] CI assertion step rejects `VITE_AUTH_MODE=dev` in pipeline
- [ ] `POST /auth/logout` clears httpOnly cookie and revokes refresh token (best-effort)
- [ ] 401 from any API endpoint triggers full session teardown and redirect to `/login?reason=session_expired`
- [ ] JWT with wrong or missing `aud` → 401 (integration test)
- [ ] `BrokerDescription` templates match `BROKER-VISIBILITY-MATRIX.md` exactly
- [ ] `EventDescription` is absent from all BrokerUser timeline responses
- [ ] Non-approved timeline event types are excluded (not returned with null field)
- [ ] BrokerUser task query returns only tasks where `LinkedEntityType='Broker' AND LinkedEntityId=<resolved_id>`
- [ ] `dashboard_kpi` and `dashboard_pipeline` return 403 for BrokerUser
- [ ] `dashboard_nudge` returns only `OverdueTask` nudges within broker scope
- [ ] `policy.csv` BrokerUser rows match `authorization-matrix.md §2.10` (parity script exit 0)
- [ ] BrokerUser read operations produce structured `BrokerUserResourceRead` log entries
- [ ] Deactivated broker → 403 `broker_scope_unresolvable` → `/unauthorized?reason=broker_inactive`
- [ ] CSP headers present in dev (Vite) and production (nginx) responses
- [ ] Cross-broker list/detail denial — integration tests pass
- [ ] Missing `broker_tenant_id` claim → deny all broker resources — integration tests pass
