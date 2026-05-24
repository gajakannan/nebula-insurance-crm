# Story F0035-S0003: Forced Re-Auth with Route and Form State Preservation

## Story Header

**Story ID:** F0035-S0003
**Feature:** F0035 — Session Continuity & Token Refresh
**Title:** Forced Re-Auth with Route and Form State Preservation
**Priority:** High
**Phase:** MVP

## User Story

**As a** Broker Relationship Manager drafting outreach notes or a Underwriter mid-edit on a policy form
**I want** to return to exactly the page I was on with my unsaved input intact, when a true session boundary forces me to re-authenticate
**So that** a forced re-auth does not destroy my in-flight work

## Context & Background

Today (per F0009) a `401` clears the session and dumps the user at `/login`, then returns them to the role-based default landing page after sign-in. Any in-progress edits are lost; even route context (which feature/account/policy they were viewing) is lost. This is the second-most-painful symptom of the current behavior after the silent-renewal gap. F0035 addresses both:

- S0001 prevents most `401`s from reaching this path at all.
- This story (S0003) makes the *unavoidable* forced re-auth lossless.

The operator decision (plan run 2026-05-23-41109356) is: preserve route + form state, but do NOT auto-replay any in-flight mutation. The user must explicitly re-confirm their save. This avoids accidental double-mutations on stale-state corruption.

## Acceptance Criteria

**Happy Path — Read Restore:**

- **Given** an Underwriter is viewing Policy 360 for policy `POL-1234`, no forms dirty
- **When** silent renewal fails and forced re-auth is triggered (per S0001 fall-through)
- **Then** the user is redirected to `/login?reason=session_expired` with the original route encoded in a return parameter
- **And** after successful sign-in the user lands on the exact same Policy 360 URL with the same policy displayed
- **And** GET requests automatically re-issue and populate the page

**Happy Path — Form State Restore (Mutation Story):**

- **Given** a Broker Relationship Manager is on Contact Edit form for `contact-789`, has typed new content into the "Notes" field (form state is dirty), and clicks "Save"
- **When** the Save mutation returns `401` and silent renewal fails (or skipped because the original request was a mutation per S0001 rule)
- **Then** the form state is snapshotted to sessionStorage keyed by `(user_id, route, form_key)` before redirect
- **And** the user is redirected to `/login?reason=session_expired`
- **And** after successful sign-in the user lands on the same Contact Edit URL with the same form fields populated with their typed content (including the unsaved "Notes" change)
- **And** the form is marked dirty (Save button enabled)
- **And** a non-blocking inline notification appears: "We saved your edits while you signed in. Click Save when ready."
- **And** the Save mutation is NOT auto-replayed — the user must click Save explicitly

**Forbidden — No Mutation Auto-Replay:**

- **Given** the conditions above
- **When** sign-in completes
- **Then** no telemetry event `mutation-auto-replayed` is emitted (because no such event class exists; presence in logs is a bug)
- **And** no server-side mutation observable evidence (timeline event, persisted change) exists between the failed Save and the user's explicit re-click

**Cross-User Boundary — Different User Signs In:**

- **Given** User A was redirected with a sessionStorage snapshot containing their form state
- **When** User B (different `user_id`) signs in on the same browser
- **Then** User A's sessionStorage snapshot is discarded (not used to populate User B's forms)
- **And** User B lands on their role-based default landing route (not User A's prior route)

**Sign-Out Cleanup:**

- **Given** a user explicitly signs out (via S0002 "Sign out" button or any future sign-out control)
- **When** the local session is cleared
- **Then** any pending sessionStorage form snapshots for that user are cleared in the same operation

**Hard-Cap Reached:**

- **Given** the active session has reached the 8-hour hard cap regardless of activity
- **When** the next protected action occurs
- **Then** silent renewal is NOT attempted (cap is unconditional)
- **And** forced re-auth begins with route + form-state preservation as above
- **And** a `forced-redirect` event is emitted with `cause: hard_cap_reached`
- **And** the `forced-redirect` event is persisted to the §5a deferred-emit buffer BEFORE the redirect (the post-redirect ingest call may fail because the session is now gone; the buffer guarantees the event reaches the metric on the next successful sign-in). Buffer contract owned by S0005.

**Storage Constraints:**

- **Given** a form's snapshot would exceed 256 KB serialized (sanity bound)
- **When** the snapshot is attempted
- **Then** the snapshot is dropped, route-only preservation is used, and a telemetry event `form-snapshot-skipped` is emitted with `cause: oversize` and `route` (no form content)
- **And** the inline notification on return reads: "We were unable to preserve your edits. Please re-enter and click Save."

**Non-Form Mutation (Confirm Dialog, Toggle, Etc.):**

- **Given** the original mutation was triggered by a confirm dialog (e.g. "Confirm cancellation" button on a Policy detail) rather than a form
- **When** forced re-auth occurs mid-mutation
- **Then** route is preserved, the user returns to the same Policy detail page, but no form-state restore is attempted (none exists)
- **And** the user must re-invoke the original action manually

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Any protected route with dirty form, then 401 + silent-renewal-fail | (implicit) — system-driven redirect | Form state snapshotted, then editable on return | Original mutation is NOT performed by this story; user must re-click Save on return | After return, reload of the route shows the snapshotted form values; explicit Save click then performs the mutation; server-side evidence (persisted record, timeline event) only after that explicit click | All authenticated roles |
| `/login?reason=session_expired&return_to=<encoded route>` | Sign-in via OIDC | N/A | OIDC callback consumes `return_to`; navigates back; restore hook reads sessionStorage snapshot and rehydrates form | URL matches `return_to`; form fields show preserved values; Save button enabled; inline notification visible | All authenticated roles |

Required checks:
- [x] Render-only behavior cannot satisfy: the form fields must actually rehydrate to the snapshotted values, AND the explicit Save click must be required to persist.
- [x] Save path has validation: oversized snapshot drop is explicit; explicit Save still goes through normal form validation.
- [x] Successful mutation (explicit re-click) has the existing audit/timeline expectation from the underlying entity (not new behavior in this story).
- [x] Tests prove: snapshot persists across redirect; rehydration only occurs for same user_id; explicit Save click required.

## Data Requirements

**Required Fields (sessionStorage snapshot record):**

- `user_id`: stable internal `UserId`
- `route`: full route path including dynamic segments (no query string with PII)
- `form_key`: opaque identifier for the form (e.g. `contact-edit:789`); set by the form's React Hook Form binding
- `form_values`: serialized form state (subject to 256 KB cap)
- `dirty_field_paths`: array of field paths that were dirty at snapshot time (used to selectively re-mark dirty on rehydrate)
- `snapshot_timestamp`: ISO 8601

**Forbidden in snapshot:**

- Raw access/refresh tokens (those are handled separately in session storage).
- Any field that the form classified as sensitive (Architect to define the classification mechanism in Phase B; default-deny if unsure).

**Validation Rules:**

- Snapshots are TTL-bounded: discarded if older than 1 hour at restore time (covers the 8-hour hard cap case where a snapshot might survive sign-in by a different session).
- Snapshot key namespace: `nebula.session-restore.v1.<user_id>.<form_key>`.
- Restore key validation: snapshot is consumed (deleted from storage) on successful rehydrate to prevent stale data on subsequent navigations.

## Role-Based Visibility

**Roles that experience this story:**

- All authenticated roles. BrokerUser experience is identical (any BrokerUser form that fails Save with 401 gets the same preservation).

**Data Visibility:**

- InternalOnly content: snapshots may temporarily contain `InternalOnly` form fields if the user had access (they were editing them); snapshots are local-browser-only and namespaced by `user_id`. Architect to confirm in Phase B that sessionStorage is sufficient isolation (vs requiring encrypted-at-rest snapshots).

## Non-Functional Expectations

- **Performance:** Snapshot write before redirect: < 100ms. Rehydrate on return: < 200ms (after sign-in completes).
- **Security:**
  - sessionStorage is namespaced per browser tab; another tab cannot read the snapshot. Architect confirms this assumption is correct for the target React + Vite setup in Phase B.
  - Snapshots are NOT written to `localStorage` (which would persist across browser restarts).
  - Snapshots are cleared on explicit sign-out (per acceptance criteria above).
  - Different-user sign-in invalidates any prior user's snapshots before form rehydration logic runs.
- **Reliability:**
  - Snapshot oversize is gracefully handled (route-only preservation, telemetry, user-visible message).
  - Snapshot TTL expiry is gracefully handled (route preserved, snapshot discarded silently if older than 1 hour).
  - Concurrent forms on same route: each form has its own `form_key`; snapshots do not collide.

## Dependencies

**Depends On:**

- F0035-S0001 — Silent Token Renewal (this story is the fall-through path when renewal fails)
- F0035-S0004 — Auth Error Semantic Distinction (forced re-auth only triggers on `401-auth-failed`, not `403`)
- F0035-S0005 — Session Continuity Telemetry Events (consumes events emitted here)
- F0009-S0001 — Login Screen and OIDC Redirect (provides the `/login` redirect target)
- F0009-S0002 — OIDC Callback and Session Bootstrap (callback must honor `return_to` parameter)

**Related Stories:**

- F0035-S0002 — Idle Warning Modal (grace-period-expired flows through this story)

## Business Rules

1. **No mutation auto-replay (ABSOLUTE).** Operator-mandated, plan run 2026-05-23-41109356. Any future request to enable auto-replay requires a new feature with explicit security review.
2. **Per-user snapshot isolation.** Snapshot keys include `user_id`; cross-user rehydration is forbidden.
3. **Snapshot TTL ≤ 1 hour.** Older snapshots are stale and may reflect outdated business rules; discard silently.
4. **Sign-out clears snapshots.** Explicit sign-out is a privacy boundary; forms-in-progress are forfeit.
5. **Oversize is graceful.** A form too large to snapshot does not block redirect; route-only preservation is the fallback.

## Out of Scope

- Multi-tab snapshot sharing or coordination (each tab is isolated).
- Server-side draft persistence (snapshots are browser-local; persistent draft features would be separate stories in other features).
- Field-level encryption of snapshots (sessionStorage isolation is considered sufficient in MVP; revisit if security review finds it inadequate).
- Snapshot compression (256 KB cap is generous for typical forms; compression deferred until evidence of need).
- Restoring scroll position, expanded panel state, or other ephemeral view state (only form-managed values are restored).
- Modifying the OIDC callback path itself beyond `return_to` handling (F0009-S0002 may need a minor amendment; flagged for Architect Phase B).

## UI/UX Notes

- Screens involved: existing `/login?reason=session_expired` (F0009) — copy may be refined: from "Your session has expired. Please sign in again." to "Your session expired. Sign in again to continue where you left off." (clarifies preservation intent).
- New UI element on return: a non-blocking inline notification near the top of the form: "We saved your edits while you signed in. Click Save when ready." — Frontend Developer to choose toast vs banner placement.
- The Save button must be visibly enabled on return (matches restored-dirty state).

## Questions & Assumptions

**Resolved by ADR-024 (Phase B):**

- [x] Does the F0009 OIDC callback path already support a `return_to` parameter, or does F0035 add it? — **Resolved (ADR-024 §4):** F0035 amends the F0009-S0002 OIDC callback to consume a `return_to` query parameter on the `/login` redirect. Implementation work lands during the feature action; the contract is fixed by ADR-024.
- [x] Form classification mechanism for sensitive fields — **Resolved (ADR-024 Options Considered §"Form state preservation storage" + Phase 2 Candidates):** for MVP, sessionStorage isolation + per-user namespacing + 1-hour TTL + 256 KB cap is the accepted boundary. A full sensitive-field classifier is recorded as a Phase 2 candidate. Security Reviewer (Architect-confirmed required role) may upgrade this to MVP-required at the feature-action security review — flagged explicitly in ADR-024 "Risks and mitigations" and F0035 STATUS.md Required Signoff Roles for Security Reviewer.

**Open Questions:** (none — all closed by ADR-024; the Phase 2 sensitive-field classifier is tracked, not open)

**Assumptions (to be validated):**

- React Hook Form's `getValues()` produces a snapshot suitable for serialization to JSON for all current form usages.
- sessionStorage is available in all supported browsers (it is, per F0009 baseline target browsers).
- 1-hour TTL is sufficient (users who sign in within an hour of forced redirect get their content back; longer absences forfeit the snapshot — acceptable per MVP scope).

## Definition of Done

- [ ] Acceptance criteria met (read restore, form restore, no auto-replay, cross-user, sign-out cleanup, hard-cap, oversize, non-form mutation)
- [ ] Edge cases handled (TTL expiry, snapshot collision, browser-tab isolation)
- [ ] Permissions enforced (per-user snapshot isolation; broker-scope unchanged)
- [ ] Audit/timeline logged (telemetry events per S0005; no auto-replay events ever)
- [ ] Tests pass (unit: snapshot key generation, TTL; integration: 401-renewal-fail → redirect → return rehydrate; E2E: full forced-re-auth journey with dirty form)
- [ ] Documentation updated (GETTING-STARTED notes the snapshot mechanism; STATUS.md provenance)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
