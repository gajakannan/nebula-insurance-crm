# Architect Analysis — Defect Run 2026-05-31-bd382bcd

Defect: **random redirect to login screen** during active use; clicking **Sign In**
returns the user to the previous screen.

> ## ⚠️ FINAL ROOT CAUSE (confirmed via runtime logs — supersedes everything below)
>
> The §1–§8 analysis below (transient silent-renewal failure → bounded retry) was an
> **early, incorrect hypothesis** reached from static reading. Live evidence (backend
> Serilog + browser DevTools) proved the real cause:
>
> 1. The authentik provider (`docker/authentik/blueprints/nebula-dev.yaml`) sets
>    `access_token_validity: minutes=5` and `refresh_token_validity: days=30`, **but its
>    `property_mappings` omit the `offline_access` scope mapping** — so even though the SPA
>    requests `offline_access`, authentik **never issues a refresh token**.
> 2. With no refresh token, `signinSilent()` (oidc-client-ts) falls back to a **hidden
>    iframe** to the IdP (`http://localhost:9000`).
> 3. The app CSP (`experience/vite.config.ts`, `experience/nginx.conf`) is
>    `frame-src 'self' blob:` — the IdP origin is **intentionally excluded** (the design
>    expected refresh-token renewal via `connect-src`). So the browser **blocks the iframe**:
>    `Framing 'http://localhost:9000/' violates ... frame-src`.
> 4. Net: every ~5-minute access-token expiry → silent renew via blocked iframe → fails →
>    `forced_reauth` → `/login?...&return_to=`. App-wide, every screen. "Random" = the
>    5-minute expiry cadence.
>
> **Confirming evidence:** backend log shows `/renewals` returned **200** (no server-side
> 401 — the redirect is purely client-side); DevTools console shows the CSP `frame-src`
> block on `http://localhost:9000`. Interactive **Sign In** works because it's a full-page
> redirect (not an iframe).
>
> **Fix (smallest correct):** add the built-in `offline_access` scope mapping to the
> provider's `property_mappings`. authentik then issues a refresh token; `signinSilent()`
> uses the **refresh-token grant** (a `fetch` to the token endpoint, already allowed by CSP
> `connect-src`) — **no iframe**, so CSP `frame-src` is never involved. One line, no CSP /
> `X-Frame-Options` weakening, aligned with the documented design intent.
>
> **Separate bug found (not the redirect):** the telemetry endpoint
> `/internal/telemetry/session-continuity` returns **500** because the backend DTO
> `SessionContinuityEventDto.UserId` is a `Guid`, but `sub_mode: user_username` makes the
> OIDC `sub` a **username string** that the SPA sends as `user_id` → `Guid` deserialization
> throws. This silently drops all session-continuity telemetry (which is why the
> `forced-redirect` cause never appeared in the backend log). Tracked as a follow-up;
> needs a small identity-contract decision.
>
> The transient-retry change this run originally shipped was **reverted** — it addressed
> nothing here.

## 1. Symptom → Mechanism Mapping

| Reported symptom | Code mechanism |
|------------------|----------------|
| "randomly redirects to login screen" while actively using the app | A `forced_reauth` auth event is emitted on the live request/navigation path and `useAuthEventHandler` navigates to `/login`. |
| "upon clicking Sign In goes back to the previous screen" | `forced_reauth` carries `returnTo` (`useAuthEventHandler.ts:47-63`) → `/login?reason=session_expired&return_to=…`. `LoginPage` (`LoginPage.tsx:56-58`) threads `return_to` into `signinRedirect({ state: { return_to } })`, so interactive sign-in restores the prior route. |

This distinguishes the defect from a true `session_expired` teardown — teardown
redirects to `/login?reason=session_expired` with **no** `return_to`
(`useSessionTeardown.ts:99-105`). The presence of "return to previous screen"
pins the path to **`forced_reauth`**.

## 2. Root Cause

Two facts combine:

1. **`automaticSilentRenew: false`** (`oidcUserManager.ts:45`, "deferred to a
   later F0009 phase"). The OIDC library never renews proactively, so the
   access token expires routinely during normal active use. Every expired-token
   request/navigation drives a **manual** `signinSilent()` via
   `renewSessionForExpiredToken()`.

2. **Zero tolerance for transient renewal failures.** Both renewal call sites
   force an immediate full reauth redirect on *any* renewal error:
   - `services/api.ts:84-92` (proactive expiry) and `api.ts:254-273`
     (401-driven) → `beginForcedReauth(cause, …)` → `emitAuthEvent('forced_reauth', …)`.
   - `features/auth/ProtectedRoute.tsx:57-72` → `emitAuthEvent('forced_reauth', …)`.

   `renewSessionForExpiredToken()` (`sessionRenewal.ts`) called `signinSilent()`
   exactly **once** and surfaced every failure. `mapRenewalError` maps any
   network/timeout/5xx (the default branch) to **`idp_unreachable`** — a
   *transient* class — yet it was treated identically to terminal causes
   (`refresh_revoked`, `refresh_expired`).

**Net effect:** with renewals forced onto the live path, a single momentary
transient failure — a network blip, a slow IdP exceeding the 10s
`silentRequestTimeoutInSeconds`, or a brief token-endpoint 5xx — immediately
ejects an actively-working user to `/login`. The IdP SSO session and refresh
token are still valid, so re-clicking **Sign In** silently re-auths and returns
to `return_to`. That is precisely the "random" (intermittent) redirect with
"back to previous screen" behavior. Intermittency is the signature of a
transient trigger, consistent with "random" in the report.

## 3. Ownership Boundary

- **Owning module:** `experience/src/features/session-continuity/sessionRenewal.ts`
  (the single shared renewal primitive used by all callers).
- The two callers (`api.ts`, `ProtectedRoute.tsx`) are **correct** to force
  reauth when renewal *genuinely* fails; they should not each grow their own
  retry logic. Fixing the shared primitive repairs every caller uniformly and
  keeps the change DRY and minimal.
- Out of scope for a defect fix: re-enabling `automaticSilentRenew` / proactive
  pre-expiry renewal (a durable design improvement → follow-up, see §6).

## 4. Design Constraints

- Preserve existing renewal contract: in-flight **coalescing**, the 5s
  **loop guard**, `bypassLoopGuard` for user-initiated renewal, and the
  `mapRenewalError` cause taxonomy + success telemetry.
- Retry **only** transient (`idp_unreachable`) failures; terminal causes
  (`refresh_revoked`, `refresh_expired`, `renewal_loop_detected`) must still
  fail fast so a genuinely dead session forces reauth without added latency.
- Bounded retry (no unbounded loops; the loop guard already prevents
  renewal storms).

## 5. Fix Strategy (smallest correct fix)

Add a **bounded single retry with a short backoff** for transient failures
inside `renewSessionForExpiredToken`:

- New `renewWithTransientRetry(startedAt)` wraps `performSilentRenewal` in a
  loop that retries once (`MAX_TRANSIENT_RENEWAL_RETRIES = 1`) after
  `RENEWAL_TRANSIENT_RETRY_DELAY_MS = 400` ms **iff** the mapped cause is
  `idp_unreachable`; otherwise it rethrows immediately.
- Coalescing, loop guard, telemetry, and the `finally` cleanup are unchanged.
- No caller changes. `api.ts` and `ProtectedRoute.tsx` benefit automatically.

This reduces spurious redirects caused by momentary blips without weakening the
hard-failure path. It is a mitigation, not a cure for sustained IdP outages —
those still (correctly) force reauth after the retry is exhausted.

## 6. Risk Assessment

| Risk | Assessment |
|------|------------|
| Masking a genuinely dead session | Low. Only `idp_unreachable` retries, once. Terminal causes are unaffected. |
| Added latency on the failure path | Bounded: at most one 400 ms backoff before forcing reauth. |
| Renewal storms | Unchanged: coalescing + 5s loop guard still apply across requests. |
| Regression to existing tests | None expected: existing tests use success / terminal (`invalid_grant`) / loop-guard paths, none of which trigger the new transient retry. Verified by reasoning + transpile check; full vitest run escalated to CI (env blocker). |
| Blast radius | Single shared module; both callers exercise the same primitive. |

## 7. Conflict Resolution Notes (per contract)

- **Local fix vs redesign:** the durable fix is proactive renewal
  (`automaticSilentRenew` / pre-expiry refresh), but that is a feature-scope
  design change. Per the contract ("use the smallest correct local fix unless
  the redesign is needed to prevent recurrence"), the bounded transient retry is
  the correct defect-scope fix; proactive renewal is recorded as a follow-up to
  prevent recurrence more comprehensively — not implemented here.
- **No feature promotion:** `ALLOW_FEATURE_PROPOSAL=false`; the proactive-renewal
  improvement is logged as an open follow-up, not created as a feature.

## 8. Follow-ups (not implemented this run)

1. **Durable hardening:** re-enable proactive silent renewal (`automaticSilentRenew`
   or a pre-expiry refresh scheduler) so renewals do not race the live request
   path at all. This is the recurrence-preventing change and is feature-scoped.
2. Consider classifying the silent-renew **timeout** explicitly (today it maps to
   `idp_unreachable` via the default branch) and possibly widening the
   `silentRequestTimeoutInSeconds` budget.
3. Optional telemetry: count transient-retry successes vs exhaustions to size the
   real-world impact.
