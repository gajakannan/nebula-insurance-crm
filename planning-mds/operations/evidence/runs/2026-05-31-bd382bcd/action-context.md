# Action Context — Defect Run 2026-05-31-bd382bcd

> Ad hoc defect/bugfix run under the **base run** evidence contract
> (`feature-evidence-package-standardization-plan-v2.md`, effective 2026-05-19).
> This is **not** feature-completion evidence. No `latest-run.json`, no
> `evidence-manifest.json`, no signoff ledger, no feature closeout.

## Run Identity

| Field | Value |
|-------|-------|
| `DEFECT_RUN_ID` | `2026-05-31-bd382bcd` |
| `PRODUCT_ROOT` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` |
| `DEFECT_RUN_FOLDER` | `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/2026-05-31-bd382bcd` |
| `Lifecycle Authority` | `none` |
| Session working dir | `/mnt/c/Users/gajap/sandbox/nebula/nebula-agents` |
| Run-id generation | `python3 -c "import secrets; print(secrets.token_hex(4))"` → `bd382bcd` (not uuid4) |

## Defect Scope

| Input | Value |
|-------|-------|
| `DEFECT_SUMMARY` | random redirect to login screen |
| `OBSERVED_BEHAVIOR` | While using the application it randomly redirects to the login screen, and upon clicking Sign In it goes back to the previous screen. |
| `EXPECTED_BEHAVIOR` | While using the application, the user should not be redirected to the login screen (for a still-valid session). |
| `REPRO_STEPS` | Not supplied by operator. No deterministic step provided; the report describes an intermittent ("random") redirect during active use. Live reproduction needs a running OIDC IdP + backend + intermittent renewal failure timing → discover via static triage + unit reproduction (see D1). |
| `AFFECTED_PATHS` | Empty at intake → discovered during triage (see below). |
| `AGENT_ROLES` | `architect`, `frontend-developer` (default) |
| `FEATURE_REFS` | None passed. Read-only related context only: F0035 (session continuity), F0009 (OIDC login/callback). Not feature-scoped evidence. |
| `ALLOW_FEATURE_PROPOSAL` | `false` |

## Affected Paths (discovered during triage)

Frontend (`{PRODUCT_ROOT}/experience/src`):

- `features/session-continuity/sessionRenewal.ts` — silent token renewal (**fix target**)
- `features/session-continuity/tests/sessionRenewal.test.ts` — renewal regression tests (**test target**)
- `services/api.ts` — request layer; forces reauth on renewal failure (caller, unchanged)
- `features/auth/ProtectedRoute.tsx` — route guard; forces reauth on renewal failure (caller, unchanged)
- `features/auth/oidcUserManager.ts` — `automaticSilentRenew: false` (context, unchanged)
- `features/auth/useAuthEventHandler.ts`, `features/auth/authEvents.ts` — forced_reauth → `/login?...&return_to=` (context)
- `pages/LoginPage.tsx` — restores `return_to` on Sign In (confirms symptom, unchanged)

## Roles Active This Run

- **Architect** → `architect-analysis.md` (root cause, ownership boundary, fix strategy, risk)
- **Frontend Developer** → `frontend-fix-report.md` (implemented change + test evidence)

## Constraints

- Smallest correct fix within defect scope. No feature creation (ALLOW_FEATURE_PROPOSAL=false).
- Do not modify `planning-mds/features/*`, do not write `latest-run.json` / `evidence-manifest.json` / signoff ledgers.
- Record every shell command in `commands.log`; do not hide failed commands.
