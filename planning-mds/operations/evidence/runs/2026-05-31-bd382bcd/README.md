# Defect Run README — 2026-05-31-bd382bcd

> Ad hoc defect/bugfix run (base run contract). **Not** feature-completion
> evidence: no `latest-run.json`, no `evidence-manifest.json`, no signoff ledger,
> no feature closeout. `Lifecycle Authority = none`.

## Run Summary

**Defect:** random redirect to the login screen during active use; clicking
**Sign In** returns to the previous screen.

> **NOTE:** An earlier version of this run shipped a transient-retry change to
> `sessionRenewal.ts` based on a wrong (static) hypothesis. The bug reproduced; live
> logs revealed the true cause below. That change was **reverted**. The actual fix is a
> one-line authentik blueprint change. See `architect-analysis.md` → FINAL ROOT CAUSE.

**Root cause (confirmed via runtime logs):** The authentik provider
(`docker/authentik/blueprints/nebula-dev.yaml`) omits the **`offline_access`** scope
mapping, so authentik **never issues a refresh token** (despite `refresh_token_validity:
days=30` and the SPA requesting `offline_access`). Access tokens expire every **5
minutes**; with no refresh token, `signinSilent()` falls back to a hidden **iframe** to
the IdP, which the app **CSP `frame-src` blocks** (`Framing 'http://localhost:9000/'
violates ... frame-src`). Result: `forced_reauth` → `/login` on every ~5-min expiry,
app-wide. Backend log confirms the API request itself returned **200** (no server-side
401 — the bounce is purely client-side).

**Fix (smallest correct):** add the built-in `offline_access` scope mapping to the
provider's `property_mappings` in `docker/authentik/blueprints/nebula-dev.yaml`.
authentik then issues a refresh token, and `signinSilent()` renews via the
**refresh-token grant** (`fetch` to the token endpoint, already allowed by CSP
`connect-src`) — no iframe, no CSP/`X-Frame-Options` change. One line, aligned with the
intended design (prod CSP deliberately excludes the IdP from `frame-src`).

**Separate bug (not the redirect):** `/internal/telemetry/session-continuity` returns
**500** — the backend DTO expects `user_id` as a `Guid`, but `sub_mode: user_username`
makes the OIDC `sub` a username string the SPA sends as `user_id`. This drops all
session-continuity telemetry. Tracked as a follow-up.

## Status

**Bug fixed (code + regression tests committed); runtime test execution
ESCALATED to CI.** Static validation green. The `vitest`/`lint`/`build`
toolchain could not run in this WSL session due to a pnpm `node_modules` linking
blocker on `/mnt/c` (unrelated to the fix; one repair cycle attempted).

**Defect run CLOSED — fix shipped; F0037 not pursued.** The proactive-renewal
recommendation was initially approved, then **withdrawn** after reviewing F0035's
PRD: the reported bug is a defect *within* F0035's reactive renewal (fixed here),
and proactive renewal is optional hardening — not a separate feature. No
feature/PRD/`REGISTRY.md` artifacts were created. See `feature-recommendation.md`
(Final Disposition) and the `PROMOTION DECISION (REVISED)` row in
`gate-decisions.md`.

## Evidence Index

| Artifact | Purpose |
|----------|---------|
| `action-context.md` | Defect scope, run identity, affected paths (D0) |
| `architect-analysis.md` | Root cause, ownership boundary, fix strategy, risk, follow-ups (D1–D2) |
| `frontend-fix-report.md` | Implemented change, regression tests, validation (D3–D4) |
| `feature-recommendation.md` | PM recommendation (operator-requested) to promote proactive renewal as feature F0037 — promotion pending operator decision |
| `gate-decisions.md` | D0–D5 verdicts |
| `artifact-trace.md` | Read / created / generated artifacts, omissions |
| `commands.log` | Shell command audit (JSONL, incl. failed commands) |
| `lifecycle-gates.log` | Validator/test invocation audit |
| `artifacts/diffs/sessionRenewal-fix.diff` | The code change (+121 / -32) |
| `artifacts/test-results/d4-transpile-check.log` | TS transpile: 0 errors |
| `artifacts/test-results/d4-logic-simulation.{mjs,log}` | Control-flow sim: 8/8 PASS |
| `artifacts/test-results/d4-env-blocker.md` | Toolchain blocker + CI escalation steps |
| `artifacts/test-results/d1-baseline-sessionRenewal.log` | vitest startup failure capture |

## Validation Summary

| Check | Result |
|-------|--------|
| TypeScript transpile (both edited files) | ✅ 0 errors |
| Control-flow logic simulation (8 assertions) | ✅ 8/8 PASS |
| `vitest` regression suite | ⏭️ NOT RUN — escalated to CI (env blocker) |
| `pnpm lint` / `pnpm build` | ⏭️ NOT RUN — escalated to CI (env blocker) |

Changed code paths: `experience/src/features/session-continuity/sessionRenewal.ts`,
`experience/src/features/session-continuity/tests/sessionRenewal.test.ts`.

## Open Follow-ups

1. **Run the regression suite + lint + build in CI / a Linux-native checkout** to
   confirm green (commands in `d4-env-blocker.md`). This closes the only
   outstanding validation gap.
2. **Optional future hardening (not tracked as a feature):** proactive pre-expiry
   renewal (`automaticSilentRenew` / scheduler) would push forced redirects from
   F0035's ≤5% target toward ~0%, but it is **not required** to close this report
   and was **not** promoted to a feature (operator decision 2026-05-31). If pursued
   later, fold it into an **F0035 follow-on**, not a new F0037. The bug itself was
   a defect in F0035's reactive renewal, resolved by this run.
3. Optionally classify the silent-renew **timeout** explicitly and/or widen
   `silentRequestTimeoutInSeconds`; add telemetry counting transient-retry
   successes vs exhaustions to size real-world impact.
4. Fix the local WSL dev environment (`node_modules` installed natively on Linux,
   not on `/mnt/c`) so the toolchain runs locally.
