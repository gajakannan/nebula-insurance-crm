# Story F0035-S0004: Auth Error Semantic Distinction

## Story Header

**Story ID:** F0035-S0004
**Feature:** F0035 — Session Continuity & Token Refresh
**Title:** Auth Error Semantic Distinction (401-token-expired / 401-auth-failed / 403-authorization-denied)
**Priority:** High
**Phase:** MVP

## User Story

**As a** Nebula user
**I want** the application to respond to authentication and authorization errors with the right behavior for each cause
**So that** a recoverable token expiration silently renews, a true authentication failure redirects me to sign in, and an authorization denial shows me a permission message — without confusing the three cases

## Context & Background

The current F0009 behavior collapses all `401`s into "clear session and redirect to /login." This is correct for true authentication failures but wrong for token expiry (which is recoverable) and wrong for cases where `403` should never trigger a redirect at all.

F0035-S0001 introduces silent renewal but depends on the frontend being able to *tell* a `401-token-expired` from a `401-auth-failed`. This story is the discriminator: it defines the wire-level signal that distinguishes the three error classes and gives the frontend API client the dispatch logic to route each to the correct handler.

This is a small but architecturally load-bearing story. Without it, S0001 cannot work correctly (it would either over-renew on real auth failures, or never renew because it cannot distinguish).

## Acceptance Criteria

**401-token-expired Discriminator:**

- **Given** any authenticated user with an expired access token but a valid upstream authentik session
- **When** a protected backend endpoint receives a request with an expired-but-validly-formed token
- **Then** the backend returns HTTP `401` with:
  - `WWW-Authenticate: Bearer error="invalid_token", error_description="The access token expired"`
  - response body conforming to RFC 7807 ProblemDetails with `type: https://nebula.local/problems/auth/token-expired`
- **And** the frontend API client classifies the response as `auth_token_expired` and dispatches to S0001 silent renewal

**401-auth-failed Discriminator:**

- **Given** an authenticated user whose token signature is invalid, audience is wrong, issuer is wrong, or whose upstream IdP session has been revoked
- **When** a protected backend endpoint receives the request
- **Then** the backend returns HTTP `401` with:
  - `WWW-Authenticate: Bearer error="invalid_token", error_description="<specific cause>"`
  - response body ProblemDetails with `type: https://nebula.local/problems/auth/invalid-token` OR `type: https://nebula.local/problems/auth/session-revoked`
- **And** the frontend API client classifies the response as `auth_token_invalid` (or `auth_session_revoked`) and dispatches to S0003 forced re-auth path (NOT silent renewal)
- **And** a telemetry event `forced-redirect` is emitted with `cause` matching the specific failure class

**403-authorization-denied Behavior (Unchanged from F0009):**

- **Given** an authenticated user whose token is valid but who lacks permission for a specific resource/action
- **When** a protected backend endpoint returns HTTP `403`
- **Then** the response includes `WWW-Authenticate` is NOT set (per RFC; `WWW-Authenticate` is for `401` only)
- **And** the response body is ProblemDetails with `type: https://nebula.local/problems/authz/forbidden` and an optional trace id
- **And** the frontend API client classifies the response as `authz_forbidden`
- **And** the existing F0009 in-page permission-safe message with trace id is displayed
- **And** NO session renewal is attempted, NO redirect to `/login` occurs
- **And** NO `forced-redirect` telemetry event is emitted (this is not a session continuity event)

**Missing or Malformed Discriminator (Defensive Default):**

- **Given** a backend response with HTTP `401` but no `WWW-Authenticate` header AND no recognizable ProblemDetails `type`
- **When** the frontend receives it
- **Then** the frontend classifies the response as `auth_unknown` and treats it as `auth_token_invalid` (defensive default — force re-auth instead of silently renewing on ambiguous evidence)
- **And** a telemetry event `auth-classifier-fallback` is emitted with the URL and response headers seen (no body content, no PII)
- **And** the `auth-classifier-fallback` event is persisted to the §5a deferred-emit buffer BEFORE the normal emit attempt (per ADR-024 §5a; this event coincides with auth failure so the normal ingest path may itself be unable to deliver). Buffer contract owned by S0005.
- **And** the response is logged server-side as a backend-contract violation

**Conflict Resolution (Both WWW-Authenticate and ProblemDetails Present):**

- **Given** a response with both `WWW-Authenticate` and a ProblemDetails `type`
- **When** they agree on the classification
- **Then** the classification is unambiguous
- **When** they disagree
- **Then** the ProblemDetails `type` wins (it is the more specific contract), and a telemetry event `auth-classifier-conflict` is emitted server-side, also persisted to the §5a deferred-emit buffer on the frontend side to ensure delivery even when the conflict accompanies a session-affecting failure (per ADR-024 §5a; buffer contract owned by S0005)

## Interaction Contract

This story is a wire-protocol + dispatcher story; there is no user-visible interaction. The "users" are the other F0035 stories (S0001, S0003) and the existing F0009 authorization paths.

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Backend protected endpoint | (none — system) | N/A | Returns `401` or `403` with disambiguator headers/ProblemDetails | Frontend integration test consuming each fixture (401-expired, 401-invalid, 401-revoked, 403-forbidden) verifies correct classification and dispatch | Read-only story — no mutation |

This story is read-only (N/A — read-only story for mutation purposes). The "edit" is in the contract surface, not user-facing.

## Data Requirements

**Required Fields (response headers on `401`):**

- `WWW-Authenticate`: `Bearer error="invalid_token", error_description="<text>"`

**Required Fields (response body ProblemDetails for `401`):**

- Conform to RFC 7807; `type` URI from the curated set:
  - `https://nebula.local/problems/auth/token-expired`
  - `https://nebula.local/problems/auth/invalid-token`
  - `https://nebula.local/problems/auth/session-revoked`

**Required Fields (response body ProblemDetails for `403`):**

- `type: https://nebula.local/problems/authz/forbidden`
- Optional `trace_id` field (per existing F0009 behavior)

**Forbidden in response:**

- Detailed token internals (claims, raw token string, signature debug)
- User PII beyond what F0009 already includes
- Database error messages, stack traces, or internal-only diagnostics

**Validation Rules:**

- The backend must set both `WWW-Authenticate` AND ProblemDetails `type` on every `401`. Tests enforce this on every protected endpoint.
- The backend must NOT set `WWW-Authenticate` on `403` (RFC compliance).
- The set of recognized ProblemDetails `type` URIs is closed; introducing a new one requires updating S0004 contract.

## Role-Based Visibility

**Roles affected:** All roles (this is wire-level behavior).

**Data Visibility:**

- InternalOnly content: ProblemDetails fields are sanitized; no `InternalOnly` content leaks via error responses (BrokerUser `403` responses contain the same generic forbidden ProblemDetails as Internal users).

## Non-Functional Expectations

- **Performance:** Classification logic adds < 1ms per response. No measurable performance impact.
- **Security:**
  - The discriminator does NOT leak whether a token was specifically expired vs revoked to an attacker probing the API — both response classes look similar except in ways the legitimate frontend needs to dispatch correctly. Architect to confirm in Phase B that response field choices do not constitute an information leak per OWASP guidance.
  - Backend logs the specific cause server-side at INFO; client receives the generic error.
- **Reliability:**
  - Backend contract conformance is tested per endpoint (test asserts: every `401`-returning code path returns valid `WWW-Authenticate` + ProblemDetails).
  - Frontend defensive default (`auth_unknown` → forced re-auth) prevents bypass via malformed responses.

## Dependencies

**Depends On:**

- F0005-S0002 — Claims Normalization Backend (backend-side authentication infrastructure; this story extends it with semantic error classification)
- F0009-S0003 — Role-Based Entry and Protected Navigation (existing 401/403 baseline that this story refines)

**Related Stories (this feature):**

- F0035-S0001 — Silent Token Renewal (consumes the `auth_token_expired` classification)
- F0035-S0003 — Forced Re-Auth with Route + Form State (consumes the `auth_token_invalid` and `auth_session_revoked` classifications)
- F0035-S0005 — Session Continuity Telemetry Events (consumes `forced-redirect` and `auth-classifier-fallback` events)

## Business Rules

1. **403 NEVER triggers session renewal or login redirect.** F0009's permission-safe in-page behavior is preserved unchanged.
2. **401 ALWAYS carries WWW-Authenticate plus a recognized ProblemDetails type.** Backend tests enforce this contract.
3. **Frontend defaults to forced re-auth on unknown 401 cause.** "Fail closed" — assume the worst, never silently renew based on ambiguous evidence.
4. **The set of ProblemDetails type URIs is closed.** Adding a new one requires a contract change (ADR-level decision in Phase B).
5. **No information leak via response disambiguation.** Architect signs off in Phase B that the chosen response shape does not enable adversarial probing.

## Out of Scope

- Backend ABAC policy changes (none required; this is auth-error classification, not authorization policy).
- Frontend UX for `403` (unchanged from F0009).
- Custom backend error endpoints (this uses existing protected endpoints + classification on responses).
- Internationalization of error description text (deferred; English-only for MVP).

## UI/UX Notes

- Screens involved: NONE directly — this story is wire-protocol level.
- Key interactions: invisible to users; visible only as the *correctness* of S0001/S0003 behavior downstream.

## Questions & Assumptions

**Resolved by ADR-024 (Phase B):**

- [x] Should the backend distinguish `auth_session_revoked` from `auth_token_invalid`? — **Resolved (ADR-024 §1 ProblemDetails type registry):** yes, distinguished as `https://nebula.local/problems/auth/session-revoked` vs `https://nebula.local/problems/auth/invalid-token`. Both lead to the same frontend path (forced re-auth via S0003), but the `cause` differs in the `forced-redirect` telemetry event for operational diagnostics.
- [x] Can the existing ASP.NET Core JWT bearer middleware emit the specific ProblemDetails type, or does this require a new middleware/filter? — **Resolved (ADR-024 §1 Consequences):** an authentication-failure handler extension on the existing bearer middleware is the chosen approach. Concrete file paths and method signatures are deferred to `feature-assembly-plan.md` (owned by `agents/actions/feature.md` Step 0); the contract is fixed here.

**Open Questions:** (none — all closed by ADR-024)

**Assumptions (to be validated):**

- The existing ASP.NET Core JWT bearer authentication middleware can be configured/wrapped to emit the required `WWW-Authenticate` + ProblemDetails per failure class.
- All current protected endpoints route their auth failures through the standard middleware (no bespoke 401 handlers that would bypass classification).
- The set of three `auth/*` ProblemDetails types is sufficient for MVP (no additional classes like `auth_audience_mismatch`, `auth_issuer_mismatch` — those collapse to `invalid-token`).

## Definition of Done

- [ ] Acceptance criteria met (4 classes: token-expired, auth-failed, authz-denied, unknown-default)
- [ ] Edge cases handled (missing headers, conflicting headers, malformed response)
- [ ] Permissions enforced (no leak via response disambiguation; Architect signoff)
- [ ] Audit/timeline logged (server-side specific cause at INFO; client telemetry per fallback case)
- [ ] Tests pass (backend: per-endpoint contract conformance; frontend: classifier dispatch matrix; E2E: end-to-end with each error class fixture)
- [ ] Documentation updated (GETTING-STARTED notes the ProblemDetails type registry; STATUS.md provenance)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
