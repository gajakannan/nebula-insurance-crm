# F0036 — Acceptance Criteria Checklist

Applies the framework acceptance-criteria checklist (`agents/templates/acceptance-criteria-checklist.md`) to F0036's eight stories. Completed at Phase A (plan run `2026-05-25-51ff2a92`). This is the PM-owned quality gate over the story acceptance criteria; story-level reviewer signoff lives in `STATUS.md`.

## Per-Story Coverage Matrix

| Story | Clarity & Testable | Happy + ≥1 edge/error | AuthZ specified | Data validation | Error handling | Nav/Feedback | Audit/Timeline | Out-of-scope |
|-------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| S0001 Engine skeleton + deps | ✅ | ✅ (unknown widget fails closed) | N/A (no new auth surface) | ✅ (exact pins, registry) | ✅ (throws, not silent) | N/A (no UI) | N/A (infra) | ✅ |
| S0002 Widget vocabulary | ✅ | ✅ (unknown option fails closed) | Inherited from host | ✅ (options from enum; money-minor round-trip) | ✅ (inline error slot) | ✅ (error/required affordance) | N/A (presentational) | ✅ |
| S0003 Schema render + AJV parity | ✅ (parity = 0 disagreements) | ✅ (cross-field rules, required empty) | ✅ (host auth; 401/403 upstream) | ✅ (data-schema + rules.json) | ✅ (inline AJV msgs; backend authoritative) | ✅ (blocked submit) | ✅ (backend event on host save) | ✅ |
| S0004 Pin-during-edit | ✅ (no rebind on activation) | ✅ (activation race; unresolvable version) | N/A (not an auth control) | ✅ (immutable pinned tuple) | ✅ (controlled error, no silent fallback) | ✅ | ✅ (version recorded on save) | ✅ |
| S0005 Replace panel (5-screen) | ✅ (per-screen baseline parity) | ✅ (read-only; conditional gating) | ✅ (per-screen auth unchanged) | ✅ (parity contract) | ✅ (bundle-load failure controlled) | ✅ | ✅ (existing event still fires) | ✅ |
| S0006 Attr-form preservation | ✅ (no auto-replay; pinned rebind) | ✅ (oversize/skip; cross-user/TTL) | ✅ (per-user isolation; 401-only) | ✅ (256 KB cap; dirty-path flatten) | ✅ (route-only fallback + message) | ✅ (inline restore notice) | ✅ (only explicit re-save emits event) | ✅ |
| S0007 CRUD RHF migration + helper | ✅ (per-form parity) | ✅ (invalid/missing; one-at-a-time) | ✅ (each form auth unchanged) | ✅ (no field/validation change) | ✅ (server 400/409 surfaces same msg) | ✅ | ✅ (existing create/update event) | ✅ |
| S0008 CRUD preservation/restore | ✅ (canonical Contact-Edit) | ✅ (cross-user/TTL/sign-out/oversize; concurrent forms; non-form) | ✅ (per-user isolation; 401-only) | ✅ (per-form_key targeting) | ✅ (route-only fallback) | ✅ (inline restore notice) | ✅ (only explicit re-save emits event) | ✅ |

## Checklist Dimensions (feature-level)

### 1) Clarity & Testability
- [x] Each criterion is specific and measurable (parity = 0 disagreements; snapshot ≤ 256 KB; TTL = 1h; latencies quantified).
- [x] No vague terms in acceptance criteria (validator `validate-stories.py` passes; "fast" removed from S0003 AC).
- [x] Pass/fail unambiguous (Given/When/Then throughout).

### 2) Coverage
- [x] Happy path covered for every story.
- [x] At least one error/edge case per story (unknown widget/option, activation race, oversize, cross-user, server errors, non-form mutation).
- [x] AuthZ behavior specified or explicitly N/A per story; forced re-auth triggers only on `401-auth-failed`, not `403`.
- [x] Role-based visibility addressed per story (host-screen inheritance documented; no new exposure).

### 3) Data Validation
- [x] Required fields enforced (Cyber data-schema required set; CRUD existing required sets).
- [x] Formats/constraints specified (enums, money-minor minor units, snapshot record shape).
- [x] Duplicates/conflicts handled (per-form_key snapshot isolation; server 409 conflict surfaces existing message).

### 4) Error Handling
- [x] Error messages actionable (inline AJV messages via `ajv-errors`; F0035 "unable to preserve" fallback message).
- [x] System errors have a user-safe message (controlled bundle-load / product-unavailable errors).

### 5) Navigation & Feedback
- [x] Post-action navigation specified (forced re-auth `/login?reason=session_expired` → return route).
- [x] Success/failure feedback specified (inline restore notice; blocked submit on invalid).

### 6) Non-Functional
- [x] Performance expectations quantified per story (render < 100ms; AJV < 50ms; snapshot write < 100ms; rehydrate < 200ms).
- [x] Security expectations stated (no tokens/PII in snapshots; sessionStorage isolation boundary; backend authoritative).
- [x] Reliability expectations stated (fail-closed widgets; graceful oversize/TTL).

### 7) Audit & Timeline
- [x] Mutations map to the existing backend entity create/update timeline events (no new event classes introduced by F0036).
- [x] No-auto-replay invariant: no `mutation-auto-replayed` event exists; only explicit re-save emits the entity event (S0006, S0008).

### 8) Out of Scope
- [x] Explicit non-goals listed per story and at the feature level (no schema engine for CRUD; no heavy widgets; no new LOBs; no backend/bundle changes; no F0035 behavior change; no filter-only forms).

## Open Items Deferred to Phase B (Architecture)

These are architecture decisions, not unresolved PM requirements:

1. **Widget derivation source** — widgets derive from `data-schema.json` (ui-schema carries only sections + labels). Phase B records the type→widget table in the amended ADR-021.
2. **AJV parity scope** — client parity must include `rules.json` cross-field rules to claim 0 disagreements. Phase B fixes the client rule-evaluation contract.
3. **Conditional gating mechanism** — MFA-maturity-enabled-when-MFA-enabled is not in the shipped bundle; Phase B decides the engine convention vs a bundle extension (S0005 preserves observable behavior regardless).
4. **`ui-schema.json` filename** — shipped name is hyphenated; Phase B amends ADR-021 prose.
