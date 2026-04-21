# F0009 — Test Plan

## Scope

Authentication and Role-Based Login — all stories S0001–S0005.

## 1. Unit Tests

### 1.1 Backend — BrokerScopeResolver (§6 contract)
Location: `engine/tests/Nebula.Tests/Unit/BrokerScopeResolverTests/BrokerScopeResolverTests.cs`

| # | Test | Expected |
|---|------|----------|
| U-01 | Exactly one broker matches broker_tenant_id | Returns brokerId |
| U-02 | Zero brokers match broker_tenant_id | BrokerScopeUnresolvableException |
| U-03 | Multiple (ambiguous) brokers match broker_tenant_id | BrokerScopeUnresolvableException |
| U-04 | broker_tenant_id claim is null | BrokerScopeUnresolvableException |
| U-05 | broker_tenant_id claim is empty string | BrokerScopeUnresolvableException |
| U-06 | Null broker_tenant_id — repo not called | DB not consulted (short-circuit) |

### 1.2 Frontend — Vite auth-mode guard (§13 contract)
Location: `experience/src/features/auth/tests/authModeGuard.test.ts` (pre-existing)

### 1.3 Frontend — Session teardown (§2.1 contract)
Location: `experience/src/features/auth/tests/useSessionTeardown.test.tsx` (pre-existing)

### 1.4 Frontend — ProtectedRoute (§3 contract)
Location: `experience/src/features/auth/tests/ProtectedRoute.test.tsx`

| # | Test | Expected |
|---|------|----------|
| V-01 | Valid session → renders children | Protected content visible |
| V-02 | No session (user null) → redirect | Navigates to /login |
| V-03 | Expired session → redirect with reason | Navigates to /login?reason=session_expired |
| V-04 | Loading state → render null | No content flash |

### 1.5 Frontend — LoginPage (S0001)
Location: `experience/src/features/auth/tests/LoginPage.test.tsx`

| # | Test | Expected |
|---|------|----------|
| V-05 | Renders Sign In button | Button present |
| V-06 | ?reason=session_expired | Session expired notice shown |
| V-07 | ?error=callback_failed | Callback error shown |
| V-08 | Click Sign In — success | signinRedirect() called once |
| V-09 | Click Sign In — IdP unavailable | Inline error shown, no navigation |

## 2. Integration Tests

Location: `engine/tests/Nebula.Tests/Integration/BrokerUserAccessTests.cs`

| # | Test | Expected |
|---|------|----------|
| W-01 | GET /brokers as BrokerUser — response shape | No rowVersion, no isDeactivated in response |
| W-02 | GET /brokers/{id} as BrokerUser — cross-scope request | 403 + code=broker_scope_unresolvable |
| W-03 | GET /brokers as BrokerUser — no broker_tenant_id claim | 403 + code=broker_scope_unresolvable |
| W-04 | GET /dashboard/kpis as BrokerUser | 403 (not in policy §2.10) |

## 3. End-to-End Tests (Manual / Playwright)

Requires authentik running + dev seed applied.

| # | Scenario | Verify |
|---|----------|--------|
| E-01 | Navigate unauthenticated to `/` | Redirect to /login |
| E-02 | Sign in as lisa.wong@nebula.local (DistributionUser) | Landing at `/`, dashboard visible |
| E-03 | Sign in as broker001@example.local (BrokerUser) | Landing at `/brokers`, scoped broker list |
| E-04 | BrokerUser — navigate to `/dashboard/kpis` directly | 403 in-page error |
| E-05 | BrokerUser — verify broker response has no RowVersion | Response JSON inspection |
| E-06 | Session expire (clear sessionStorage) — then navigate | Redirect to /login?reason=session_expired |
| E-07 | Sign out (explicit logout button) | POST /auth/logout called, redirect /login, cookie cleared |
| E-08 | BrokerUser — deactivate their linked broker | Navigate to /unauthorized?reason=broker_inactive |

## 4. Security Gate Checks

| # | Check | Pass Criteria |
|---|-------|---------------|
| S-01 | `pnpm build` with VITE_AUTH_MODE=dev | Build fails with FATAL error |
| S-02 | CI workflow — no VITE_AUTH_MODE=dev env in CI | Step asserts and exits 1 if set |
| S-03 | policy.csv vs authorization-matrix parity | `scripts/check-policy-parity.sh` passes |
| S-04 | BrokerUser cannot read other broker's data | W-02 integration test |
| S-05 | BrokerUser response never contains InternalOnly fields | W-01 integration test |

## 5. Test Identity Matrix (§9)

| User | Role | broker_tenant_id | Expected Landing |
|------|------|------------------|-----------------|
| lisa.wong@nebula.local | DistributionUser | N/A | / |
| john.miller@nebula.local | Underwriter | N/A | / |
| broker001@example.local | BrokerUser | broker001-tenant-001 | /brokers |

## 6. Release Blockers

The following must pass before merge to main:
- All unit tests (U-01 through V-09)
- All integration tests (W-01 through W-04)
- E2E smoke: E-01, E-02, E-03, E-06, E-07
- Security gates S-01 through S-05
