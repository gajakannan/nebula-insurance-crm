---
template: feature
version: 1.0
applies_to: product-manager
---

# F0035: Session Continuity & Token Refresh

**Feature ID:** F0035
**Feature Name:** Session Continuity & Token Refresh
**Priority:** High
**Phase:** Release Enablement / Platform Operations
**Status:** Draft (Phase A + Phase B complete and approved; awaiting `agents/actions/feature.md` for implementation. Plan run `2026-05-23-41109356` — see `planning-mds/operations/evidence/runs/2026-05-23-41109356/`.)

## Feature Statement

**As an** authenticated Nebula user
**I want** my active work session to remain valid while I am using the application
**So that** I am not unexpectedly sent to the login page during normal CRM workflows

## Business Objective

- **Goal:** Remove disruptive mid-workflow login redirects caused by normal access-token expiration.
- **Metric:** Number of user-visible forced login redirects during active usage (measured via telemetry events introduced by this feature).
- **Baseline:** Current OIDC access tokens (F0005/F0009 baseline) can expire during active usage, and the next protected API call clears the session and redirects to `/login` (per F0009 rule "API 401 → clear session + redirect /login"). The redirected user is often already authenticated upstream at authentik, so the interruption feels unnecessary.
- **Target:**
  - Silent token renewal succeeds in ≥ 95% of recoverable expirations (telemetry-measured) during the first 30 days post-launch.
  - User-visible forced login redirects on still-valid upstream sessions reduced to ≤ 5% of recoverable cases.
  - 100% of re-auth journeys preserve the original route on return; 100% preserve in-flight form state where the original action was a mutation.

## Problem Statement

- **Current State:** The application can suddenly navigate to `/login?reason=session_expired` while the user is working. Clicking Sign In often returns the user to the application because the upstream identity-provider session is still valid, which makes the interruption feel unnecessary. F0005/F0009 left "silent token renewal" explicitly out of scope.
- **Desired State:** Nebula distinguishes recoverable token expiration from true session expiration, renews the session when possible, only sends the user to login when re-authentication is actually required, and when it does require re-auth it preserves the user's route plus any unsaved form state.
- **Impact:** Unexpected login redirects interrupt underwriting, policy, renewal, document, and account workflows; they reduce trust in the application even when no data is lost. With more high-API-count pages now live (Policy 360, document panels, dashboard modules, dynamic LOB attribute panels per F0034), the friction is increasing.

## Scope & Boundaries

**In Scope:**

- Transparent silent session continuation during active application usage, including coalesced renewal under concurrent in-flight requests.
- Idle-warning modal with explicit "Stay signed in" / "Sign out" controls and a defined grace period before forced redirect.
- Preserving the user's current route AND in-flight form state when re-authentication is required.
- Distinguishing `401-token-expired` (recoverable), `401-auth-failed` (forced re-auth), and `403-authorization-denied` (no re-auth) semantically.
- Session-continuity telemetry event emission (silent-renewal-success/fail, forced-redirect-cause, idle-warning-shown/accepted/dismissed).
- Defined active-session bounds: 4-hour rolling window, 8-hour hard cap.
- Validation that high-API-count pages (Policy 360, document panels, dashboard modules, dynamic LOB panels) do not amplify token-expiry interruptions.

**Out of Scope:**

- Replacing authentik as the identity provider.
- Changing role, broker-scope, or resource authorization rules.
- Building cross-application single logout.
- Adding broad user-account administration.
- Weakening token lifetime, audience, or issuer validation to hide the symptom.
- Auto-replaying user-initiated mutations after silent renewal (mutations require explicit user re-trigger; reads may auto-replay).
- Building a session-continuity analytics dashboard (telemetry emission only; dashboards are a follow-up).
- Changes to F0009's `BROKER-VISIBILITY-MATRIX.md` or `policy.csv` (authorization boundaries unchanged).

## Personas & Jobs

Aligned with `planning-mds/BLUEPRINT.md` Section 1.2.

| Persona | Job To Be Done | Why It Matters |
|---------|----------------|----------------|
| Distribution User (incl. Distribution & Marketing Manager) | When I am reviewing submissions and broker activity, I want the app to keep my session alive while I work, so I can finish follow-up without restarting context. | Submission, broker, and renewal triage involves repeated reads and edits across several pages; current behavior forces unnecessary re-auth mid-task. |
| Underwriter | When I am reviewing policy or renewal detail, I want background data refreshes to handle token expiry without redirecting me, so I can keep my underwriting train of thought. | Policy 360, dynamic LOB panels, and document attachments each issue multiple protected requests on load; current behavior amplifies interruption frequency. |
| Broker Relationship Manager / MGA Program Manager | When I am drafting an outreach note or updating a contact, I want unsaved input preserved through any required re-auth, so a session boundary does not destroy in-flight work. | Form data lost across redirect is the most painful symptom; trust in the system drops fastest here. |
| Admin | When session behavior changes, I want security boundaries preserved and observable through telemetry, so operational reliability does not come at the expense of access control. | Session continuity must remain auditable; silent renewal must not mask real authentication or authorization failures. |

## Success Criteria

- Active users are not redirected to login solely because a short-lived access token expires while the upstream authentik session remains valid.
- If re-authentication is required, the user returns to the same route they were using before login.
- When the action that triggered re-auth was a mutation (Save/Submit/Approve/Transition click), in-flight form state is preserved and the user must re-confirm the action; the mutation is not auto-replayed.
- When the action that triggered re-auth was a read (GET), the read silently retries after renewal succeeds.
- API 401 handling distinguishes recoverable token expiration from unrecoverable authentication failure; API 403 (authorization denied) never triggers renewal or login redirect.
- Idle users see an idle-warning modal at 25 minutes of inactivity; auto-redirect occurs at 30 minutes if no action is taken.
- Active-session maximum: 4-hour rolling window from last user activity, 8-hour absolute cap regardless of activity.
- Session continuity behavior is covered by focused frontend tests and at least one end-to-end or smoke validation path.
- Session-continuity telemetry events are emitted for silent-renewal-success, silent-renewal-fail, forced-redirect (with cause), idle-warning-shown, idle-warning-accepted, idle-warning-dismissed.
- Security-sensitive cases still fail closed: invalid audience, invalid issuer, revoked upstream session, and unauthorized resource access do not become silent success cases.

## Product Requirements

1. **Recoverable Expiration (Silent Renewal)**
   - Given an authenticated user is actively using Nebula
   - When the access token expires but the identity-provider session is still valid
   - Then Nebula should renew the usable session without navigating the user away from the current page.
   - Concurrent in-flight requests during expiry must coalesce on a single renewal attempt; on success all queued requests proceed; on failure all queued requests surface a single coherent outcome (not a cascade of competing redirects).

2. **True Session Expiration (Forced Re-Auth)**
   - Given an authenticated user's identity-provider session is no longer valid (or hard-cap reached)
   - When Nebula can no longer obtain valid authentication
   - Then Nebula should redirect to login with a clear, non-blaming session-expired reason.

3. **Return To Current Work (Route + State Restore)**
   - Given a user is redirected to login because re-authentication is required
   - When sign-in succeeds
   - Then the user should return to the route that initiated re-authentication, not only a role-based default landing page.
   - When the action that triggered re-auth was a read, Nebula re-issues the read silently after return.
   - When the action that triggered re-auth was a mutation, Nebula restores the form state but does NOT auto-replay the mutation; the user must explicitly re-confirm.

4. **No Authorization Masking**
   - Given a user lacks access to a resource
   - When the API returns `403`
   - Then Nebula should NOT treat that as session renewal or login recovery; the existing F0009 in-page permission-safe message with trace id applies unchanged.

5. **Auth Error Semantic Distinction**
   - The frontend API client must distinguish three error classes from a single `401`/`403` surface:
     - `401-token-expired` → attempt silent renewal (Requirement 1)
     - `401-auth-failed` → forced re-auth path (Requirement 2)
     - `403-authorization-denied` → no renewal, no redirect (Requirement 4)
   - The discriminator is the `WWW-Authenticate` challenge plus the response problem-details `type` per ADR (to be authored in Phase B).

6. **Idle Warning + Grace Period**
   - Given an authenticated user has had no input activity for 25 minutes
   - When the idle threshold is reached
   - Then Nebula shall display an idle-warning modal with "Stay signed in" (renews and resets idle timer) and "Sign out" (clean redirect to `/login?reason=signed_out`).
   - If the modal is dismissed or ignored for an additional 5 minutes (total 30 minutes inactive), Nebula performs forced re-auth with route preservation per Requirement 3.

7. **Workflow Stability**
   - Given a page issues multiple protected API requests
   - When token expiration occurs during those requests
   - Then the user should see at most one coherent session-continuity outcome, not a cascade of competing redirects or stale UI states.

8. **Active-Session Bounds**
   - Rolling activity window: 4 hours. Any meaningful user action (mouse, keyboard, navigation, form interaction) resets the window.
   - Hard cap: 8 hours from initial sign-in regardless of activity. Reaching the hard cap forces re-auth per Requirement 2.

9. **Telemetry (MVP)**
   - Emit structured events for: silent-renewal-success, silent-renewal-fail (with cause class), forced-redirect (with cause class: hard-cap, idle-timeout, idp-revoked, network), idle-warning-shown, idle-warning-accepted, idle-warning-dismissed.
   - Events go to existing F0033 Serilog structured logging baseline; no new dashboard is built in MVP.
   - Events must not include any user PII beyond stable internal `UserId` (per ADR-006).
   - **Failure-class event durability:** the four events that coincide with auth disruption (`silent-renewal-fail`, `forced-redirect`, `auth-classifier-fallback`, `auth-classifier-conflict`) must survive the very session loss that they measure. The ingest endpoint requires bearer auth, so the post-failure HTTP call is itself at risk. The architecture (ADR-024 §5a) persists these events to local browser storage before the redirect/session-clear path, and drains them on the next successful sign-in. This is what makes the success metric reliable.

## Screen Layouts (ASCII)

Only one new UI surface is introduced: the idle-warning modal. The existing `/login` and `/login?reason=*` screens (F0009) are reused; their messaging may be refined in S0003.

### Idle Warning Modal — Desktop (≥ 1024px)

```
+----------------------------------------------------------------------+
|                                                                      |
|                       [Nebula header bar — unchanged]                |
|                                                                      |
+----------------------------------------------------------------------+
|                                                                      |
|         [Page content dimmed; modal overlay centered]                |
|                                                                      |
|         +----------------------------------------------+             |
|         |   Still working?                             |             |
|         |                                              |             |
|         |   You've been inactive for 25 minutes.       |             |
|         |   For security, your session will end in     |             |
|         |   5 minutes unless you continue.             |             |
|         |                                              |             |
|         |              ⏱ 5:00 remaining                |             |
|         |                                              |             |
|         |   [ Stay signed in ]    [ Sign out ]         |             |
|         +----------------------------------------------+             |
|                                                                      |
+----------------------------------------------------------------------+
```

Interaction:
- "Stay signed in" → silently renew session, dismiss modal, reset idle timer
- "Sign out" → clean redirect to `/login?reason=signed_out` (no session-expired styling)
- No action for 5 minutes → forced re-auth with route preservation per Requirement 3
- The countdown timer is visible and updates each second

### Idle Warning Modal — Narrow / Mobile (< 768px)

```
+--------------------------------+
| [Nebula header bar]            |
+--------------------------------+
| [Page content dimmed]          |
|                                |
| +----------------------------+ |
| | Still working?             | |
| |                            | |
| | You've been inactive for   | |
| | 25 minutes. Your session   | |
| | will end in 5 minutes      | |
| | unless you continue.       | |
| |                            | |
| |        ⏱ 5:00              | |
| |                            | |
| | [   Stay signed in   ]     | |
| |                            | |
| | [      Sign out      ]     | |
| +----------------------------+ |
|                                |
+--------------------------------+
```

Buttons stack vertically on narrow viewport with full-width touch targets.

## UX Notes

- The idle warning modal is the only new primary visual element.
- The existing `/login?reason=session_expired` page (F0009) wording is refined in S0003 to distinguish forced re-auth ("Please sign in again to continue where you left off") from clean sign-out ("You've been signed out").
- Existing protected pages preserve route context through any required login cycle (Requirement 3).
- Any visible session prompt must be concise and avoid blaming the user.
- Form state preservation surface in MVP: any React Hook Form-managed form with `formState.isDirty === true` at the moment of forced redirect. Preservation strategy is `sessionStorage` keyed by `(userId, route, formKey)`; the Architect will finalize the key shape in Phase B.

## Interaction Contracts

Every mutation-touching story includes a full interaction contract per the framework story template. Summary here:

| Story | Mutation Surface | Save Path | Persistence Evidence |
|-------|------------------|-----------|----------------------|
| S0002 (idle modal) | "Stay signed in" → renews session and clears idle timer | calls token-renewal endpoint; sets new tokens in session storage | next protected API call succeeds without redirect; telemetry event `silent-renewal-success` emitted |
| S0003 (forced re-auth + restore) | Form Save fails with 401 → user redirected → returns to same route with form values intact → must re-click Save | original mutation NOT auto-replayed; user explicit re-confirm | server-side mutation observable in domain entity / timeline event only after explicit re-confirm |

## Dependencies

- F0009 Authentication + Role-Based Login (auth foundation; current 401-redirects-to-login behavior is the baseline being refined)
- F0005 IdP Migration: Keycloak → authentik (authentik session/refresh-token semantics)
- F0018 Policy Lifecycle & Policy 360 (high-API-count surface — validation target)
- F0020 Document Management & ACORD Intake (high-API-count surface — validation target)
- F0034 Product Schema Registry and Dynamic LOB Attributes (dynamic panels — validation target)
- F0033 Structured Logging and QE Toolchain Activation (Serilog baseline for telemetry events)

## Risks & Assumptions

- **Risk:** Session renewal could accidentally retry user-initiated mutations too aggressively and create duplicate writes.
  - **Mitigation:** PRD Requirement 3 forbids auto-replay of mutations; only reads auto-replay. Architect will document idempotency boundaries in Phase B ADR.
- **Risk:** A UX-only fix could hide real authentication failures.
  - **Mitigation:** Requirement 4 + 5 keep the security boundary explicit; S0004 covers the semantic distinction; failure cases are telemetry-observable per Requirement 9.
- **Risk:** Form state preservation via `sessionStorage` could leak across users on a shared device.
  - **Mitigation:** Keys include `UserId`; the key is cleared on successful sign-out and on a different user logging in. Architect will confirm key scheme in Phase B.
- **Risk:** Telemetry events without PII may be insufficient for troubleshooting individual user issues.
  - **Mitigation:** Stable internal `UserId` is included (per ADR-006); no email, no name, no IP. Sufficient for correlated diagnostics through existing F0033 logging baseline.
- **Assumption:** authentik remains the identity provider for this feature.
- **Assumption:** authentik refresh-token issuance is enabled at the OIDC client configuration (DevOps to confirm in Phase B preflight).
- **Assumption:** F0033 Serilog structured logging accepts new event categories without dashboard work.

## Resolved Open Questions (Phase A clarification gate)

| Question | Resolution | Source |
|----------|------------|--------|
| Idle behavior: silent termination vs warning? | Idle warning modal at 25 min inactivity, 5 min grace period, auto-redirect at 30 min. | Operator decision, plan run 2026-05-23-41109356 |
| Route restore for mutations? | Restore route + form state; do NOT auto-replay mutations (user must re-confirm). Reads auto-replay silently. | Operator decision, plan run 2026-05-23-41109356 |
| Telemetry MVP or follow-up? | MVP — emit structured events through F0033 baseline. No dashboard build in MVP. | Operator decision, plan run 2026-05-23-41109356 |
| Active-session duration? | 4-hour rolling window, 8-hour hard cap. | Operator decision, plan run 2026-05-23-41109356 |

## Out of Scope (Reiterated for Story Authoring Clarity)

- Auto-replay of user-initiated mutations after renewal (explicitly forbidden).
- Multi-tab session synchronization (deferred; if one tab's session expires another tab may still hold a valid token until its own next protected request).
- Refresh-token rotation policy changes at authentik (DevOps may need to confirm refresh-token issuance is on; no rotation cadence change).
- Cross-application SLO (Nebula-only).

## Related User Stories

- F0035-S0001 — Silent Token Renewal with Concurrent Request Coalescing
- F0035-S0002 — Idle Warning Modal with Grace Period
- F0035-S0003 — Forced Re-Auth with Route and Form State Preservation
- F0035-S0004 — Auth Error Semantic Distinction (401-expired / 401-failed / 403-denied)
- F0035-S0005 — Session Continuity Telemetry Events (MVP)

## Architecture Traceability (Architect Phase B)

The technical design is governed by **ADR-024 — Session Continuity and Token Refresh Architecture** (`planning-mds/architecture/decisions/ADR-024-session-continuity-and-token-refresh.md`). Mapping from product requirements to architectural mechanisms:

| Product Requirement | ADR-024 Section | Canonical Nodes |
|---------------------|-----------------|-----------------|
| Req 1 — Silent renewal | §2 + §7 | `capability:silent-token-renewal`, `event:silent-renewal-success`, `event:silent-renewal-fail` |
| Req 2 — Forced re-auth | §4 | `capability:session-context-restore`, `event:forced-redirect` |
| Req 3 — Route + form state restore | §4 | `capability:session-context-restore`, `event:form-snapshot-skipped` |
| Req 4 — No authorization masking | §1 (preserves F0009/ADR-008 boundary) | `capability:authorization-enforcement` (unchanged) |
| Req 5 — Auth error semantic distinction | §1 | `capability:auth-error-classification`, `schema:problem-details`, `event:auth-classifier-fallback`, `event:auth-classifier-conflict` |
| Req 6 — Idle warning + grace | §3 | `capability:idle-warning`, `event:idle-warning-shown/accepted/dismissed` |
| Req 7 — Workflow stability (coalescing) | §2 | `capability:silent-token-renewal` |
| Req 8 — Active-session bounds (4h/8h) | §7 | `capability:session-continuity` |
| Req 9 — Telemetry (MVP) | §5 + §5a + §6 | `capability:session-telemetry`, `endpoint:internal-telemetry-session-continuity`, `schema:session-continuity-event` |
| Req 9 — Failure-class event durability | §5a (Deferred-Emit Buffer) | `capability:session-telemetry` (mechanism on top of the in-memory path) |

**Wire-level artifacts:**

- ProblemDetails type URIs (`https://nebula.local/problems/auth/{token-expired,invalid-token,session-revoked}` and `https://nebula.local/problems/authz/forbidden`) — registered in ADR-024 §1; extended via the existing shared `schema:problem-details`.
- `POST /internal/telemetry/session-continuity` endpoint — added to `planning-mds/api/nebula-api.yaml` under `SessionTelemetry` tag; governed by ADR-024 §6.
- `planning-mds/schemas/session-continuity-event.schema.json` — discriminated-union envelope; payload variants enforced via JSON Schema `allOf` conditionals per `event_name`; `additionalProperties: false` on every payload subobject for the PII boundary.

**Out of scope at architecture time** (per ADR-024 Follow-up Actions):

- Refresh-token transport tightening (frontend-mediated remains baseline; backend-mediated HttpOnly cookie is Phase 2).
- Form classifier for sensitive-field scrubbing in sessionStorage snapshots (Phase 2; Security review may upgrade to MVP-required at B2).
- Parameterized renewal-loop throttle (5s hard-coded for MVP).
- Multi-tab session-state synchronization (each tab is independent in MVP).
- Telemetry dashboard / visualization (event emission only; viewing via existing F0033 query tooling).

The `feature-assembly-plan.md` is intentionally **not** an output of plan A+B (per `agents/actions/plan.md` Deliverables Contract). It belongs to the feature action at Step 0 and consumes this PRD + ADR-024 as inputs.
