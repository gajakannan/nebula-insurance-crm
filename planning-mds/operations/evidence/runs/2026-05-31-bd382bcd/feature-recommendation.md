# Feature Recommendation — Proactive Silent Token Renewal

> Authored within defect run `2026-05-31-bd382bcd`. This is a **recommendation
> only**. It does **not** create or modify `planning-mds/features/*` and does
> **not** reserve a feature ID. `ALLOW_FEATURE_PROPOSAL=false` at intake; this
> document was produced at the operator's explicit request and **stops for the
> operator's promotion decision** (see Decision block).

## Recommendation Summary

Promote the durable hardening to a formal feature: **proactive silent token
renewal** so OIDC access tokens are refreshed *before* they expire, instead of
being renewed reactively on the live request/navigation path. This removes the
root condition behind defect run `2026-05-31-bd382bcd` (random redirect to
login) rather than only mitigating its symptom.

- **Proposed feature ID:** `F0037` (next available per `REGISTRY.md`; not reserved here)
- **Proposed name:** Proactive Silent Token Renewal
- **Predecessors / context:** F0009 (Authentication & Role-Based Login — deferred
  this work), F0035 (Session Continuity & Token Refresh — owns the renewal/idle
  subsystem), ADR-002 (access token held in-memory)
- **Recommended classification:** **High** severity (user-facing session
  reliability). Urgency reduced to **Medium** in practice by the shipped
  transient-retry mitigation from this defect run.
- **Recommended next action:** start a new session under the formal `plan` (then
  `feature`) evidence contract with its own feature-scoped run ID.

## Problem & Why a Defect Fix Is Not Enough

`oidcUserManager.ts:45` sets `automaticSilentRenew: false` with the comment
"Silent renew is deferred to a later F0009 phase." As a result every access-token
expiry is handled **reactively**: the next request or route navigation detects the
expired token and runs `signinSilent()` inline (`api.ts` `resolveToken` /
`handleExpiredToken`; `ProtectedRoute.tsx`). Any renewal failure forces a
`/login?…&return_to=…` redirect.

The defect run shipped a **bounded transient retry** (`sessionRenewal.ts`:
retry once on `idp_unreachable`) which materially reduces spurious redirects from
momentary blips. But it is a **mitigation, not a cure**:

- Renewals still race the live request path; a slow IdP beyond the retry budget,
  or a transient failure on *both* attempts, still logs an active user out.
- The user still experiences a renewal stall on the critical path at every token
  expiry (latency, not just failure).
- Mutations that hit an expired token still return `mutation_retry_required`,
  forcing the user to redo the action.

Proactively renewing ahead of expiry removes the race entirely: tokens are fresh
before any request needs them, so transient renewal failures happen off the
critical path with time to retry, and forced reauth becomes a true last resort.

## Proposed Scope (MVP)

1. **Proactive renewal scheduler.** Re-enable `automaticSilentRenew` (or an
   app-owned pre-expiry scheduler) that renews at a configurable lead time before
   `expires_at` (e.g. 60–120s prior), reusing the existing coalescing + loop
   guard in `sessionRenewal.ts`.
2. **Off-critical-path failure handling.** On proactive-renewal failure, retry
   with backoff within the lead window; only fall back to `forced_reauth` when the
   token actually expires and renewal is still failing.
3. **Reconcile with reactive path.** Keep the existing reactive renewal as a
   safety net (tab wake, clock skew, scheduler miss) but ensure the proactive
   path is the common case; avoid double-renewal via the loop guard.
4. **Timeout classification.** Distinguish a silent-renew **timeout** from a hard
   IdP-unreachable failure (today both map to `idp_unreachable` via the default
   branch) and consider widening `silentRequestTimeoutInSeconds`.
5. **Telemetry.** Emit proactive-renewal scheduled / success / fallback events and
   count transient-retry successes vs exhaustions to size real-world impact.

## Non-Goals

- Changing the IdP, token lifetimes, or the OIDC grant (refresh-token / PKCE).
- Changing the forced-reauth UX, `return_to` restore, or dirty-form snapshot
  behavior (owned by F0035; unchanged).
- Idle-timeout / rolling-window / hard-cap session policy (owned by F0035).
- Backend/API changes — this is a frontend session-management feature.

## Acceptance Criteria (draft, for the formal PRD to refine)

- With a valid IdP session, a user active for longer than the access-token
  lifetime is **never** redirected to `/login` due to routine expiry (no forced
  reauth on the happy path).
- Access token is refreshed before expiry under normal conditions; no inline
  renewal stall is observable on user-initiated navigation/requests.
- A single transient renewal failure within the lead window self-heals without
  user impact; forced reauth occurs only after the token has actually expired and
  renewal still fails.
- Terminal causes (`refresh_revoked`, `refresh_expired`) still force reauth
  promptly.
- Regression coverage: scheduler timing, lead-window retry/backoff, coalescing
  with reactive path, fallback-to-forced-reauth, and timeout classification.

## Affected Components (anticipated)

- `experience/src/features/auth/oidcUserManager.ts` (renewal config)
- `experience/src/features/session-continuity/sessionRenewal.ts` (scheduler + coalescing)
- `experience/src/features/session-continuity/SessionContinuityProvider.tsx` / a new scheduler hook
- `experience/src/services/api.ts`, `experience/src/features/auth/ProtectedRoute.tsx` (reactive safety net reconciliation)
- `experience/src/features/session-continuity/sessionTelemetry.ts` (new events)

## Risk & Effort (rough)

- **Effort:** small–medium (one frontend vertical slice; reuses existing renewal
  primitive and telemetry).
- **Risk:** medium — scheduler/timer correctness across tab background/wake and
  clock skew; must avoid renewal storms (loop guard already present) and
  double-renew with the reactive path.
- **Dependencies:** F0009 auth config, F0035 session-continuity subsystem; ADR-002.

## Relationship to This Defect Run

- The defect fix (transient retry in `sessionRenewal.ts`) **stays** — it is the
  correct defect-scope mitigation and the reactive safety net under this feature.
- This recommendation is the recurrence-preventing change deliberately kept
  **out** of the defect scope (smallest-correct-fix rule).

## Decision (operator)

This defect run will **not** create the feature. To proceed, the operator must
explicitly approve promotion. On approval, per the run contract:

1. Record the promotion decision in `gate-decisions.md`.
2. Stop this defect run cleanly (no further defect-run edits).
3. Begin a **new session** under the formal `plan` → `feature` evidence contract
   with its own feature-scoped run ID; the Product Manager reserves `F0037` in
   `REGISTRY.md` and authors the PRD there (not in this run folder).

- [ ] **Approve promotion** to formal feature work (F0037) — _initially approved 2026-05-31, then **withdrawn** (see Final Disposition)_
- [x] **Do not pursue as a separate feature** — defect fix is sufficient; proactive renewal parked as an optional backlog note — _operator, 2026-05-31_
- [ ] **Decline** — accept residual risk with the shipped mitigation

> **FINAL DISPOSITION (revised 2026-05-31): promotion WITHDRAWN.** After reviewing
> F0035's PRD, the reported bug is a defect *within* F0035's reactive silent
> renewal (no transient-failure tolerance), already fixed by this defect run.
> F0035 deliberately scoped renewal as **reactive** (target ≤5% forced redirects,
> not 0%); proactive pre-expiry renewal is **optional** hardening, **not** required
> to close the report. It is **not** tracked as a separate feature (F0037 not
> pursued). No feature/PRD/`REGISTRY.md` artifacts were created. If proactive
> renewal is ever wanted, fold it into an **F0035 follow-on**, not a new feature.
> This document is retained as the rationale record only.
