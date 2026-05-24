# Story F0035-S0001: Silent Token Renewal with Concurrent Request Coalescing

## Story Header

**Story ID:** F0035-S0001
**Feature:** F0035 — Session Continuity & Token Refresh
**Title:** Silent Token Renewal with Concurrent Request Coalescing
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Distribution User, Underwriter, or Broker Relationship Manager actively using Nebula
**I want** my session to renew silently when my access token expires but my upstream authentik session is still valid
**So that** I am not interrupted mid-workflow by an unnecessary login redirect

## Context & Background

Today (per F0009), the frontend API client clears the session and redirects to `/login` on any `401`. Most of those `401`s happen because a short-lived access token expired while the upstream authentik session is still good. Silent renewal recovers the session in place. The story also covers concurrent-request coalescing because high-API-count pages (Policy 360, dynamic LOB panels, dashboard modules) routinely have 4–8 protected requests in flight simultaneously, and naïve renewal would race them.

This is the core recoverable-case flow that the rest of F0035 builds on.

## Acceptance Criteria

**Happy Path:**

- **Given** a Distribution User is on the Policy 360 page and has been working for over an hour
- **When** the access token expires and a protected API request returns `401-token-expired`
- **Then** the frontend silently requests a renewed token via the refresh path, replaces the stored token, retries the original request once, and the UI continues with no visible interruption
- **And** a telemetry event `silent-renewal-success` is emitted

**Concurrent Coalescing:**

- **Given** the Policy 360 page issues 6 concurrent protected requests (account, policy detail, versions, endorsements, timeline, documents)
- **When** all 6 receive `401-token-expired` near-simultaneously because the token expired between page navigation and request dispatch
- **Then** exactly ONE renewal attempt is made; all 6 requests queue behind it; on success all 6 retry exactly once each
- **And** the user observes a single coherent outcome (page loads normally), not 6 competing redirects or stale-state UI
- **And** only one `silent-renewal-success` event is emitted, with `coalesced_request_count: 6`

**Renewal Failure (fall-through to forced re-auth):**

- **Given** silent renewal is attempted
- **When** the refresh-token call returns a hard failure (refresh token revoked at authentik, refresh expired, or authentik unreachable after retry budget)
- **Then** the API client falls through to the forced-re-auth path defined in S0003 (route preservation, form-state preservation if applicable)
- **And** a telemetry event `silent-renewal-fail` is emitted with `cause` set to one of `refresh_revoked`, `refresh_expired`, `idp_unreachable`
- **And** a `forced-redirect` event is emitted with `cause` reflecting the renewal-fail cause
- **And** both events are persisted to the §5a deferred-emit buffer BEFORE the normal emit attempt (per ADR-024 §5a, persist-before-emit is mandatory for failure-class events; otherwise the very auth failure being measured can drop the event). Implementation contract is owned by S0005 acceptance criteria.

**Renewal Throttling:**

- **Given** silent renewal succeeded less than 5 seconds ago (suggesting a malformed or overly-aggressive renewal loop)
- **When** another `401-token-expired` arrives
- **Then** the second renewal attempt is suppressed and the request falls through to forced re-auth with `cause: renewal_loop_detected`
- **And** a `silent-renewal-fail` event is emitted with that cause

**Read-vs-Mutation Distinction:**

- **Given** the original request that triggered renewal was a `GET` (read)
- **When** renewal succeeds
- **Then** the original `GET` is auto-retried silently and the user sees the data appear

- **Given** the original request was a mutation (`POST`, `PUT`, `PATCH`, `DELETE`)
- **When** renewal succeeds
- **Then** the original mutation is NOT auto-replayed; the UI surfaces a non-blocking notification (per S0003 contract) inviting the user to retry
- **And** no `mutation-auto-replayed` event is emitted (because it must never happen)

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Any protected page (background) | None — automatic | N/A — system-driven | New access token persisted to session storage; original GET requests retried once | Next protected API call uses renewed token and succeeds without redirect; telemetry shows `silent-renewal-success` | All authenticated roles (DistributionUser, Underwriter, BrokerUser, Admin); only when upstream IdP session is still valid |

Required checks:
- [x] Render-only behavior cannot satisfy this story: the user must observe their workflow continuing without redirect, AND telemetry events must be emitted.
- [x] The renewal path has failure handling: refresh_revoked / refresh_expired / idp_unreachable / renewal_loop_detected each fall through to forced re-auth with cause.
- [x] A successful renewal has a telemetry-event expectation.
- [x] Tests prove that with token-expiry simulated, the next protected request succeeds without leaving the current route, AND that 6 concurrent expired requests trigger exactly 1 renewal call.

## Data Requirements

**Required Fields (telemetry event payload — `silent-renewal-success`):**

- `event_name`: literal `silent-renewal-success`
- `timestamp`: ISO 8601 with TZ
- `user_id`: stable internal `UserId` (per ADR-006)
- `coalesced_request_count`: integer (≥ 1)
- `renewal_duration_ms`: integer
- `original_request_count_after_retry`: integer (should equal coalesced_request_count when all retried successfully)

**Required Fields (telemetry event payload — `silent-renewal-fail`):**

- `event_name`: literal `silent-renewal-fail`
- `timestamp`: ISO 8601 with TZ
- `user_id`: stable internal `UserId`
- `cause`: enum `refresh_revoked` | `refresh_expired` | `idp_unreachable` | `renewal_loop_detected`
- `coalesced_request_count`: integer

**Forbidden in event payload:**

- User email, name, IP address, raw access/refresh token contents, broker_tenant_id, role list (these are derivable from `UserId` in audit context if needed).

**Validation Rules:**

- The frontend never logs raw token contents to telemetry or browser console (must redact at source).
- The renewal call timeout must be bounded (Architect to set in Phase B, ≤ 10 seconds expected).
- Coalescing window: from the first `401-token-expired` until the renewal call completes or times out.

## Role-Based Visibility

**Roles that experience this story:**

- DistributionUser, Underwriter, BrokerUser, Admin, DistributionManager — all roles, no exceptions.

**Data Visibility:**

- InternalOnly content: telemetry events are server-side / logging-only; never exposed in the UI.
- BrokerVisible content: the story is fully UI-invisible (renewal is silent); BrokerUser experiences the same continuity as internal users.

## Non-Functional Expectations

- **Performance:** Renewal round-trip including queued-request retry completes in < 2 seconds p95 under nominal network conditions. The user should perceive no UI delay beyond a normal request.
- **Security:**
  - Refresh-token transport is the existing authentik refresh-token flow (authentication: client-managed; transport: HTTPS).
  - Token storage scope is the existing session storage scheme from F0009 (no new persistent storage in this story).
  - Failure cases never leak the new token to telemetry, console, or DOM.
- **Reliability:**
  - Coalescing guarantees exactly-once renewal per expiry burst.
  - Renewal-loop throttle prevents tight-loop failure modes.
  - Network failure → forced re-auth (no infinite retry).

## Dependencies

**Depends On:**

- F0009-S0002 — OIDC Callback and Session Bootstrap (existing token storage scheme)
- F0005-S0003 — Frontend OIDC Flow (existing oidc-client-ts integration)
- F0033-S0001 — Establish Serilog structured logging baseline (event emission infrastructure)

**Related Stories (this feature):**

- F0035-S0003 — Forced Re-Auth with Route and Form State Preservation (renewal-failure fall-through)
- F0035-S0004 — Auth Error Semantic Distinction (provides the `401-token-expired` discriminator this story consumes)
- F0035-S0005 — Session Continuity Telemetry Events (consumes events emitted here)

## Business Rules

1. **Silent renewal applies only to recoverable expiration.** If the renewal call itself returns a failure indicating the upstream session is gone (refresh_revoked / refresh_expired), the user MUST be sent through forced re-auth — never present a "renewed" state when the IdP says the session is invalid.
2. **No mutation auto-replay.** A successful renewal after a failed mutation does NOT replay the mutation. The user re-confirms explicitly. This rule is operator-mandated (plan run 2026-05-23-41109356).
3. **Single renewal per burst.** A coalescing semaphore ensures exactly one in-flight renewal regardless of how many requests are queued. Architect will define the implementation primitive in Phase B.
4. **No PII in telemetry.** Telemetry payloads carry only stable internal IDs and operationally-meaningful fields.

## Out of Scope

- Multi-tab renewal coordination (each tab renews independently; tab-to-tab synchronization is deferred).
- Pre-emptive renewal (renewing before expiry based on `exp` claim) — deferred to Phase 2 once telemetry baseline shows actual expiry patterns.
- Custom retry backoff per endpoint (single-retry-only is sufficient for MVP).
- Configurable renewal-loop threshold (5 seconds is hard-coded for MVP; Architect may parameterize later).

## UI/UX Notes

- Screens involved: NONE directly. This story is fully background-invisible by design.
- Key interactions: The only observable user signal of correct behavior is "I kept working without seeing the login page." Test fixtures must therefore assert absence of redirect AND presence of telemetry events.

## Questions & Assumptions

**Resolved by ADR-024 (Phase B):**

- [x] Renewal call endpoint shape and refresh-token transport — **Resolved (ADR-024 §2 + Options Considered):** frontend-mediated direct authentik OIDC token endpoint call via existing `oidc-client-ts` integration (mirrors F0005-S0003 pattern; no new backend surface). Backend-mediated HttpOnly cookie tightening is recorded as a Phase 2 candidate in F0035 STATUS.md (not MVP).

**Open Questions:** (none — all closed by ADR-024)

**Assumptions (to be validated):**

- authentik refresh-token issuance is enabled at the OIDC client (DevOps preflight — listed as required signoff in F0035 STATUS.md Required Signoff Roles).
- The existing API client (`oidc-client-ts` integration from F0005-S0003) exposes a hook surface where we can intercept `401` responses before they bubble to UI; otherwise an axios-style interceptor layer is added.

## Definition of Done

- [ ] Acceptance criteria met (happy path, coalescing, failure, throttling, read-vs-mutation)
- [ ] Edge cases handled (network timeout, malformed refresh response, renewal-loop)
- [ ] Permissions enforced (no role bypass; all authenticated roles get continuity)
- [ ] Audit/timeline logged (telemetry events emitted per spec, no PII)
- [ ] Tests pass (unit: coalescing primitive, throttle; integration: 6-concurrent expiry path; E2E: navigate-expire-continue smoke)
- [ ] Documentation updated (GETTING-STARTED notes telemetry events; STATUS.md provenance row)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
