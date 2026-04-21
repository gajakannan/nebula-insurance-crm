# F0009-S0001: Login Screen and OIDC Redirect

**Story ID:** F0009-S0001
**Feature:** F0009 — Authentication + Role-Based Login
**Title:** Provide login entry screen and IdP sign-in redirect
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** Nebula user
**I want** a clear login screen that starts sign-in with the identity provider
**So that** I can authenticate through a supported, explicit user flow

## Acceptance Criteria

- **Given** I open Nebula without an active session
- **When** I navigate to any protected route
- **Then** I am redirected to `/login`

- **Given** I am on `/login`
- **When** I click `Sign in`
- **Then** the app starts OIDC Authorization Code + PKCE redirect to configured authority

- **Given** OIDC configuration is missing
- **When** `/login` renders
- **Then** sign-in action is disabled and a deterministic support-safe error is shown

- **Given** I attempt to access callback route directly without valid OIDC state
- **When** callback validation executes
- **Then** login retry path is shown without exposing protocol details

- **Given** the IdP is unavailable
- **When** sign-in is attempted
- **Then** deterministic error and retry guidance are shown

## Implementation Contract

- Route path: `/login`
- Frontend OIDC library: `oidc-client-ts`
- No role picker on login page (claims-driven role resolution after callback)
- `dev-auth` fallback is allowed only under explicit feature flag in non-production

## Data Requirements

**Required Fields:**
- OIDC authority URL
- OIDC client ID
- Redirect URI

**Validation Rules:**
- Sign-in is disabled when required OIDC config is absent
- Login diagnostics must not log tokens/secrets/credentials

## Non-Functional Expectations

- Performance: login screen render p95 <= 1.5s
- Security: no token/secret in browser logs
- Reliability: sign-in redirect failure <= 1% excluding IdP outages

## Dependencies

- F0005 authentik baseline
- Route guard implementation (F0009-S0003)

## Definition of Done

- [ ] `/login` implemented and routable
- [ ] Redirect to IdP works with PKCE
- [ ] Missing-config and IdP-error states deterministic
- [ ] Tests cover login route guard redirect + error states
