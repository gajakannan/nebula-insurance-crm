# Story F0035-S0002: Idle Warning Modal with Grace Period

## Story Header

**Story ID:** F0035-S0002
**Feature:** F0035 — Session Continuity & Token Refresh
**Title:** Idle Warning Modal with Grace Period
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Underwriter who steps away from my desk
**I want** Nebula to warn me before my session is ended for inactivity, with a clear option to continue
**So that** I do not return to find my session unexpectedly ended and my workflow disrupted

## Context & Background

S0001 (silent renewal) handles token-expiry while the user is *active*. This story covers the *inactive* case: a user who has stopped interacting with the page (no clicks, keystrokes, scrolls, or navigations) for an extended period. The operator-confirmed policy is a 25-minute inactivity threshold with a 5-minute grace period (30 minutes total), surfaced through a modal with explicit "Stay signed in" / "Sign out" controls. This pattern matches enterprise CRM conventions and gives users agency over their session lifecycle.

The modal is the only new UI surface introduced by F0035.

## Acceptance Criteria

**Happy Path — "Stay signed in":**

- **Given** an authenticated Underwriter has had no input activity for 25 minutes
- **When** the idle threshold is reached
- **Then** the idle-warning modal appears centered, dims the underlying page, and displays the countdown starting at 5:00
- **And** a telemetry event `idle-warning-shown` is emitted
- **When** the user clicks "Stay signed in" before the countdown expires
- **Then** the modal dismisses, the session is silently renewed (per S0001 mechanism), the idle timer resets to 0, and the user is back on the same page
- **And** a telemetry event `idle-warning-accepted` is emitted with `time_remaining_ms` indicating how much grace period was left

**Happy Path — "Sign out":**

- **Given** the idle-warning modal is shown
- **When** the user clicks "Sign out"
- **Then** the local session is cleared, the user is redirected to `/login?reason=signed_out` (NOT `?reason=session_expired`)
- **And** a telemetry event `idle-warning-dismissed` is emitted with `dismissal_action: user_signed_out`

**Unattended Grace Period Expiry:**

- **Given** the idle-warning modal is shown
- **When** 5 minutes pass with no user interaction (no click on either button, no other input)
- **Then** Nebula performs forced re-auth with route preservation per S0003 (route preserved; if a form was dirty, form state is preserved)
- **And** a telemetry event `idle-warning-dismissed` is emitted with `dismissal_action: grace_period_expired`
- **And** a `forced-redirect` event is emitted with `cause: idle_timeout`

**Activity Detection:**

- **Given** an authenticated user is interacting with Nebula
- **When** any of the following events occur: mousedown, keydown, touchstart, navigation (route change), form input
- **Then** the idle timer resets to 0
- **And** if the warning modal was about to appear (within the next 100ms), its appearance is cancelled

**Modal Not Shown When Already Forced Re-Auth In Flight:**

- **Given** silent renewal failed and forced re-auth is already in progress (S0003 redirect path)
- **When** the idle threshold would otherwise be reached
- **Then** the modal is suppressed (a user already mid-redirect should not see a warning)

**Countdown Display:**

- **Given** the idle-warning modal is shown
- **When** the countdown runs
- **Then** the displayed `M:SS` updates each second
- **And** below 30 seconds the countdown text turns red (warning state) without changing the modal layout

**Responsive Behavior:**

- **Given** the viewport is narrow (< 768px)
- **When** the modal renders
- **Then** the "Stay signed in" and "Sign out" buttons stack vertically with full-width touch targets (≥ 44px height)

**Accessibility:**

- The modal traps focus on appearance; "Stay signed in" receives initial focus.
- `Escape` key triggers the same path as "Sign out" (clean sign-out, not grace-period-expired).
- The countdown is announced to screen readers via `aria-live="polite"` at 60-second intervals plus the final 10 seconds.
- The modal carries `role="alertdialog"` and `aria-labelledby` / `aria-describedby` references.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Idle-warning modal (overlay on any protected route) | "Stay signed in" click | N/A — button only | Calls renewal flow (S0001 mechanism); resets idle timer; dismisses modal | Next protected API call succeeds without redirect; `idle-warning-accepted` event emitted; reload shows page in normal state | All authenticated roles |
| Idle-warning modal | "Sign out" click | N/A — button only | Clears local session; redirects to `/login?reason=signed_out` | URL shows `/login?reason=signed_out`; `idle-warning-dismissed` event emitted with `user_signed_out` | All authenticated roles |
| Idle-warning modal | No action for 5 min after appearance | N/A — system-driven | Triggers forced re-auth per S0003; preserves route + dirty form state | URL shows `/login?reason=session_expired`; on return user lands on prior route with form state restored; `grace_period_expired` event emitted | All authenticated roles |

Required checks:
- [x] Render-only behavior cannot satisfy this story: modal appearance, button-click effects, countdown updates, and event emission are all required.
- [x] Save path has validation: "Stay signed in" can fail (if S0001 renewal returns a hard failure), in which case the modal closes and forced re-auth begins per S0003 (same as grace-period expiry).
- [x] A successful "Stay signed in" has telemetry-event expectation.
- [x] Tests prove the user can perform each action AND the resulting state (route/redirect, idle-timer reset, telemetry event) is observable.

## Data Requirements

**Required Fields (telemetry payload — `idle-warning-shown`):**

- `event_name`, `timestamp`, `user_id`, `route_at_warning` (path only, no query string with potential PII)

**Required Fields (telemetry payload — `idle-warning-accepted`):**

- `event_name`, `timestamp`, `user_id`, `time_remaining_ms` (integer, 0–300000)

**Required Fields (telemetry payload — `idle-warning-dismissed`):**

- `event_name`, `timestamp`, `user_id`, `dismissal_action` (enum: `user_signed_out` | `grace_period_expired`)

**Configuration (not user-facing data; stored in frontend config):**

- `IDLE_THRESHOLD_MS`: 1_500_000 (25 min)
- `GRACE_PERIOD_MS`: 300_000 (5 min)
- These values are not user-configurable in MVP. Architect may parameterize in Phase B if security-team wants tunability.

**Validation Rules:**

- The timer logic must use a monotonic clock source (e.g. `performance.now()`) and not a wall-clock (`Date.now()`) to avoid drift on system clock changes.
- The countdown displayed to the user is derived from a single source of truth (the grace-period deadline); UI never displays a value that disagrees with the underlying timer.

## Role-Based Visibility

**Roles that see this modal:**

- All authenticated roles (DistributionUser, Underwriter, BrokerUser, Admin, DistributionManager).

**Data Visibility:**

- InternalOnly content: the modal contains no business data; only generic copy and a countdown.
- BrokerVisible content: identical experience for BrokerUser.

## Non-Functional Expectations

- **Performance:** Modal must appear within 200ms of threshold being reached. Countdown update jitter must be ≤ 1 second.
- **Security:**
  - The "Sign out" path must NOT carry `?reason=session_expired` (that would mislead users that a session boundary was hit when in fact they actively signed out).
  - The grace-period-expired path uses the standard `?reason=session_expired` per F0009 baseline.
  - The activity-detection listeners must not capture or log keystroke contents (only event arrival).
- **Reliability:**
  - The idle timer survives tab backgrounding (when the tab returns to focus, the timer reflects real elapsed time, not paused-time).
  - The modal is not duplicated if multiple `idle-threshold` events fire (singleton pattern).
- **Accessibility:** WCAG 2.1 AA compliance for modal pattern (focus trap, ARIA roles, contrast). Validated via `@axe-core/playwright` in test suite.

## Dependencies

**Depends On:**

- F0035-S0001 — Silent Token Renewal (the "Stay signed in" button uses S0001's renewal path)
- F0035-S0003 — Forced Re-Auth with Route Preservation (grace-period-expired triggers this flow)
- F0035-S0005 — Session Continuity Telemetry Events (consumes events emitted here)

**Related Stories:**

- F0009-S0001 — Login Screen and OIDC Redirect (provides `/login?reason=*` target screen)

## Business Rules

1. **Idle threshold and grace period are fixed for MVP.** 25 min + 5 min = 30 min total. Operator decision; not user-configurable.
2. **Sign-out is intentional, not session-expired.** The "Sign out" button must distinguish itself from grace-period expiry in the URL parameter and the telemetry event.
3. **Grace period is real time, not virtual.** A backgrounded tab does not pause the countdown; on return to focus the displayed countdown matches actual elapsed time.
4. **The modal cannot be dismissed without one of the three terminal outcomes.** No "X" close button. (Escape == Sign out path.)

## Out of Scope

- User-configurable idle threshold (deferred; admin-level setting in a future feature).
- Different thresholds per role (BrokerUser potentially shorter; deferred).
- "Snooze" option (deferred; either stay or sign out).
- Cross-tab idle synchronization (each tab tracks its own idle state in MVP).

## UI/UX Notes

- Screens involved: the modal overlay (component); no new route.
- Key interactions: see ASCII layout in `PRD.md` `## Screen Layouts (ASCII)`.
- Copy must be reviewed by Frontend Developer for tone (concise, non-blaming).
- Countdown red-state at <30s is for visual urgency; should not flash or animate aggressively (accessibility — no rapid motion).

## Questions & Assumptions

**Resolved by ADR-024 (Phase B):**

- [x] Should "Stay signed in" perform a renewal call even if the access token is not yet expired? — **Resolved (ADR-024 §3):** yes, "Stay signed in" always invokes the silent-renewal path from S0001 to guarantee the freshly-confirmed session has maximum remaining lifetime. This also resets the idle timer in a single coherent operation.

**Open Questions:** (none — all closed by ADR-024)

**Assumptions (to be validated):**

- React 18's `useEffect` cleanup is sufficient for activity-listener teardown on unmount (no leaked listeners across navigations).
- The idle timer can coexist with TanStack Query background refetches without those background reads being mis-classified as user activity.

## Definition of Done

- [ ] Acceptance criteria met (happy path × 2, grace expiry, activity reset, suppression conditions, responsive, accessibility)
- [ ] Edge cases handled (tab background/return, modal singleton, screen-reader announcement)
- [ ] Permissions enforced (no role bypass; same threshold for all roles in MVP)
- [ ] Audit/timeline logged (three telemetry events with correct payloads)
- [ ] Tests pass (unit: idle-timer monotonic clock; component: modal render + buttons; E2E: simulate 30 minutes with timer mock)
- [ ] Documentation updated (GETTING-STARTED notes the modal + telemetry; STATUS.md provenance)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
