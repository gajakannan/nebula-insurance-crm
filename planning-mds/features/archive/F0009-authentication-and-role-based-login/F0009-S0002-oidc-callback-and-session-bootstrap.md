# F0009-S0002: OIDC Callback and Session Bootstrap

**Story ID:** F0009-S0002
**Feature:** F0009 — Authentication + Role-Based Login
**Title:** Establish session from OIDC callback and bootstrap user context
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** authenticated Nebula user
**I want** the app to process OIDC callback and initialize my session
**So that** I can access protected routes without manual token handling

## Acceptance Criteria

- **Given** IdP returns valid callback response
- **When** `/auth/callback` is processed
- **Then** app creates authenticated session and routes to role landing page

- **Given** callback validation fails (state/nonce/code)
- **When** callback processing runs
- **Then** session creation fails closed and user is routed to `/login?error=callback_failed`

- **Given** browser refresh occurs with active session
- **When** app bootstraps
- **Then** protected routes remain accessible without re-login

- **Given** session is expired
- **When** protected route loads
- **Then** session state is cleared and user is redirected to `/login?reason=session_expired`

- **Given** same auth code callback is replayed
- **When** callback executes
- **Then** second attempt fails safely with deterministic login retry path

## Implementation Contract

- Callback route path: `/auth/callback`
- Session source of truth: valid OIDC user from `oidc-client-ts`
- Silent renew: out of scope in Phase 1 (full re-auth on expiry)
- Claims required for bootstrap: `iss`, `sub`, `email`, `nebula_roles`
- Additional required claim for BrokerUser bootstrap: `broker_tenant_id`

## Validation Rules

- Callback processing fails closed on any protocol validation failure
- Missing or malformed `nebula_roles` results in unauthorized user state
- BrokerUser without valid `broker_tenant_id` results in unauthorized user state
- Token internals are never exposed in UI error content

## Non-Functional Expectations

- Callback + first authorized render p95 <= 3s
- Refresh with active session >= 99% success in non-outage windows

## Dependencies

- F0009-S0001
- F0005 claims normalization + JWT validation

## Definition of Done

- [ ] `/auth/callback` implemented
- [ ] Session bootstrap deterministic for success/failure/expiry
- [ ] Replay and direct-callback edge cases handled safely
- [ ] Tests cover callback validation failure paths and expiry redirect
