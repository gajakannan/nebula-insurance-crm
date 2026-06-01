# ADR-024: Session Continuity and Token Refresh Architecture

**Status:** Accepted
**Date:** 2026-05-23
**Owners:** Architect
**Related Features:** F0035
**Related ADRs:** ADR-006 (authentik IdP migration), ADR-007 (F0009 login + broker scope), ADR-008 (Casbin enforcer), ADR-Auth-Token-Storage, ADR-Authentication-Strategy
**Related Schema Bundles:** `planning-mds/schemas/session-continuity-event.schema.json`, `planning-mds/schemas/problem-details.schema.json`

## Context

F0009 left silent token renewal explicitly out of scope. The result is that any `401` clears the session and redirects the user to `/login`. With more high-API-count pages live (Policy 360, document panels, dynamic LOB attribute panels per F0034), normal short-lived access-token expiration is increasingly disruptive: users are bumped to login mid-workflow even when authentik still holds a valid upstream session.

F0035 Phase A defined the product behaviour: silent renewal where recoverable, idle warning with grace period, route + form state preservation across forced re-auth, semantic distinction between auth-error classes, and MVP telemetry. This ADR governs HOW.

Phase A operator clarifications (plan run 2026-05-23-41109356):

- Idle warning modal at 25 min inactivity, 5 min grace, auto-redirect at 30 min
- Restore route + dirty form state; do **not** auto-replay user-initiated mutations; reads auto-replay silently
- Telemetry events emitted to existing F0033 Serilog baseline; no dashboard in MVP
- Active session bounds: 4-hour rolling window, 8-hour hard cap

## Decision Drivers

- Continuity must not weaken authentication or authorization boundaries set by F0009 (broker scope, Casbin ABAC, `InternalOnly` field stripping all preserved unchanged).
- Mutation idempotency is unsolved across the existing endpoint surface; auto-replay is unsafe. Reads are idempotent and safe to auto-replay after renewal.
- The discriminator between recoverable token expiry and unrecoverable auth failure must be deterministic at the wire level — never inferred from heuristics.
- Telemetry must be observable to measure success without creating new PII boundaries.
- Operationally simple: fits the existing `oidc-client-ts` frontend integration (F0005) and ASP.NET Core JWT bearer middleware (F0009 baseline).

## Decision

F0035 introduces five coordinated mechanisms:

### 1. Auth-Error Discriminator (Backend → Frontend Contract)

Backend protected endpoints classify auth failures into three closed classes, each surfaced via paired `WWW-Authenticate` header and ProblemDetails `type` URI:

| Class | HTTP | `WWW-Authenticate` (error=) | ProblemDetails `type` URI | Frontend action |
|-------|------|------------------------------|----------------------------|-----------------|
| Token expired | 401 | `invalid_token` (error_description: "The access token expired") | `https://nebula.local/problems/auth/token-expired` | Silent renewal (#2) |
| Token invalid (sig, audience, issuer) | 401 | `invalid_token` (error_description: cause) | `https://nebula.local/problems/auth/invalid-token` | Forced re-auth (#4) |
| Upstream session revoked | 401 | `invalid_token` (error_description: "session revoked") | `https://nebula.local/problems/auth/session-revoked` | Forced re-auth (#4) |
| Authorization denied | 403 | (none — RFC 7235 reserves WWW-Authenticate for 401) | `https://nebula.local/problems/authz/forbidden` | F0009 permission-safe in-page message; **no renewal, no redirect** |

Defensive default: a `401` lacking both signals is treated as `auth_token_invalid` (force re-auth). Validators on every protected endpoint enforce the contract.

### 2. Silent Token Renewal with Coalescing

Frontend API client intercepts `401` responses pre-UI. On `auth_token_expired`:

1. A coalescing semaphore ensures exactly **one** in-flight refresh-token call per expiry burst.
2. All concurrent `401-token-expired` responses queue on the semaphore.
3. On refresh success: the new token replaces stored token; queued requests retry exactly once each.
4. On refresh failure (`refresh_revoked`, `refresh_expired`, `idp_unreachable`, `renewal_loop_detected`): fall through to forced re-auth (#4) with cause.
5. **Mutation rule:** if the original queued request is a mutation (`POST`/`PUT`/`PATCH`/`DELETE`), it is **not** auto-replayed after renewal. A non-blocking inline notification surfaces; user explicitly re-confirms.

Renewal-loop throttle: if a previous renewal succeeded < 5 seconds ago, the next `401-token-expired` is treated as forced re-auth with `cause: renewal_loop_detected` (suspected misconfiguration).

Refresh-token transport: direct authentik OIDC token endpoint via `oidc-client-ts` (the existing F0005-S0003 integration). No backend-mediated refresh.

### 3. Idle Warning Modal

Frontend activity listener resets a monotonic-clock-based idle timer on `mousedown`, `keydown`, `touchstart`, route change, and form input. At `IDLE_THRESHOLD_MS` (1_500_000, 25 min) the modal renders with a 5:00 countdown (`GRACE_PERIOD_MS = 300_000`).

- **Stay signed in** → invokes #2 silent renewal, dismisses modal, resets idle timer.
- **Sign out** → clears local session, redirects to `/login?reason=signed_out` (NOT `?reason=session_expired`).
- No action for 5 min → forced re-auth (#4) with `cause: idle_timeout`.

Modal is a singleton; backgrounded-tab returns reflect real elapsed time (monotonic clock guarantee). WCAG 2.1 AA: `role="alertdialog"`, focus trap, `aria-live="polite"` announcements at 60-second intervals + final 10 seconds.

### 4. Forced Re-Auth with Route + Form State Preservation

When forced re-auth is triggered (renewal failure, hard cap reached, idle grace expired):

1. Frontend captures `return_to` URL.
2. If any React Hook Form on the current route has `formState.isDirty === true`, snapshot the form values to `sessionStorage` keyed by `nebula.session-restore.v1.<user_id>.<form_key>`. TTL 1 hour. Size cap 256 KB serialised; oversize triggers route-only preservation + telemetry.
3. Redirect to `/login?reason=session_expired&return_to=<encoded>`.
4. After successful OIDC callback (F0009-S0002, **amended in this ADR** to consume `return_to`): navigate to the preserved route.
5. On route mount, restore-hook reads matching `sessionStorage` entry, validates `user_id` matches current session, deletes the entry, hydrates the form, re-marks dirty fields, surfaces inline notification.
6. The original mutation is **not** invoked by restore; user explicitly re-clicks Save/Submit/Approve.

Cross-user safety: snapshot keys include `user_id`. Different-user sign-in clears any prior user's snapshots before restore runs. Explicit sign-out clears the current user's pending snapshots.

### 5. Telemetry Event Emission

Frontend emits structured events through a new internal ingest endpoint (#6 API contract); backend writes them to F0033 Serilog under category `Nebula.Session.Continuity`. Event shape governed by `planning-mds/schemas/session-continuity-event.schema.json`.

Event taxonomy:

| Event name | Emitted by |
|------------|-----------|
| `silent-renewal-success` | #2 silent renewal happy path |
| `silent-renewal-fail` | #2 renewal failure (with `cause` enum) |
| `forced-redirect` | #4 forced re-auth trigger (with `cause` enum) |
| `idle-warning-shown` | #3 modal appearance |
| `idle-warning-accepted` | #3 "Stay signed in" click |
| `idle-warning-dismissed` | #3 "Sign out" click OR grace-period expired (distinguished by `dismissal_action` field) |
| `auth-classifier-fallback` | #1 401 lacked both signals; defensive default applied |
| `auth-classifier-conflict` | #1 WWW-Authenticate vs ProblemDetails type disagreed; ProblemDetails wins, conflict logged |
| `form-snapshot-skipped` | #4 oversize snapshot; route-only preservation used |

PII boundary (HARD RULE, enforced at emit and validate time): event payloads carry only stable internal `UserId` (per ADR-006), opaque `session_id`, operationally-meaningful fields. **No** email/name/IP/raw tokens/broker_tenant_id/role list/form contents/query strings.

Emission is fire-and-forget; bounded in-memory buffer (50 events), exponential backoff (max 3 retries then drop with a single WARN log). Telemetry failures never affect session-continuity behaviour (strict one-way coupling).

#### 5a. Failure-Class Event Durability (Deferred-Emit Buffer)

The ingest endpoint (#6) requires an authenticated bearer session, but four event classes are emitted precisely when authentication is failing or has failed:

- `silent-renewal-fail`
- `forced-redirect`
- `auth-classifier-fallback`
- `auth-classifier-conflict`

A revoked/expired session combined with the forced-redirect that follows can drop these events on the ingest network call (401 from the endpoint, or no transport opportunity before redirect) — undermining the success metric exactly when it matters most. The deferred-emit buffer closes this hole:

1. **Persist-before-emit (synchronous).** Before the normal fire-and-forget emit attempt, each failure-class event is written synchronously to `localStorage` at key `nebula.telemetry-defer.v1.<user_id>.<event_uuid>` with the full event JSON payload. The `<event_uuid>` is a fresh v4 UUID per event. This write completes before any redirect or session-clear in the failure path.
2. **Normal emit proceeds.** The in-memory buffer + retry logic from §5 runs unchanged.
3. **Acknowledge.** On HTTP 202 from the ingest endpoint, the matching `localStorage` entry is deleted (`localStorage.removeItem`).
4. **Persist-on-failure (passive).** On any non-202 outcome (network error, 401, 4xx, 5xx, timeout, ingest unavailable), the `localStorage` entry remains.
5. **Drain on next successful auth bootstrap.** The OIDC callback completion path (F0009-S0002, the same hook §4 uses to consume `return_to`) invokes `drainDeferredEvents(user_id)`: enumerate all `nebula.telemetry-defer.v1.<user_id>.*` entries, batch in groups of up to 10, POST each batch to the ingest endpoint; on 202 delete each entry from the batch; on non-202 leave the batch and stop (will retry on next successful bootstrap).
6. **Bounds.** Maximum 100 stored entries per user (LRU eviction on overflow — oldest deleted on new write). Per-entry TTL 7 days from event `timestamp`; the drain step purges any entry older than TTL before POSTing (purged entries are dropped silently with a single aggregated WARN at drain time including the dropped count).
7. **Per-user isolation.** The `<user_id>` segment in the key prevents cross-user leak on shared browsers. `drainDeferredEvents` enumerates only entries matching the *current* session's `user_id`; entries from prior users sitting in localStorage are not read by the new user's drain. Explicit sign-out clears the current user's deferred entries (same hook that clears form snapshots per §4).
8. **Failure-mode containment.** `localStorage.setItem` exceptions (quota exceeded, storage disabled, private mode) are caught and silently dropped. The original failure-path code does not branch on telemetry-write success; the strict one-way coupling from session-continuity to telemetry is preserved.

Non-failure-class events (`silent-renewal-success`, `idle-warning-*`, `form-snapshot-skipped`) remain in the existing in-memory bounded buffer only — they coincide with healthy sessions where the normal §5 path is sufficient, and adding `localStorage` writes there would mean a write per protected request burst (excessive I/O for marginal benefit).

The trade-off accepted is a single synchronous `localStorage.setItem` call (microsecond cost) on the failure path. Privacy is preserved because the payload is the same PII-bounded event already governed by `session-continuity-event.schema.json` (UserId + session_id only; no email/name/IP/raw tokens/broker_tenant_id/role list/form contents). The 7-day TTL is reasonable for recovery-window diagnostics without indefinite local accumulation.

### 6. Internal Telemetry Ingest Endpoint

New endpoint added to `nebula-api.yaml`:

```yaml
POST /internal/telemetry/session-continuity
Auth: bearerAuth (existing)
Body: { events: [SessionContinuityEvent, ...] }   # batch of 1..10 events
Responses:
  202: Accepted (fire-and-forget; events queued for Serilog write)
  400: ProblemDetails — validation_error
  401: ProblemDetails — auth/* per #1
  403: ProblemDetails — authz/forbidden  (should not occur for authenticated users; safety net)
```

Backend validates each event against `session-continuity-event.schema.json`; invalid events are rejected with `400 + validation_error` ProblemDetails. The endpoint is **not** publicly addressable — same authenticated boundary as other protected APIs.

The endpoint accepts batches from two sources: (a) the normal in-memory buffer (§5) under healthy session conditions, and (b) the deferred-emit drain (§5a) on OIDC bootstrap. Both paths use the same batch shape and same authentication. The endpoint cannot tell the two apart (intentionally — they are operationally equivalent from the ingest side); drained events carry their original `timestamp`, so age-of-event is recoverable from the payload.

### 7. Active-Session Bounds

Two timers run in parallel (both monotonic-clock based):

- **Rolling activity window:** 4 hours (`14_400_000 ms`). Any meaningful user input resets it.
- **Absolute hard cap:** 8 hours (`28_800_000 ms`) from sign-in; never reset by activity.

Reaching either triggers forced re-auth (#4) with `cause: rolling_window_exceeded` or `hard_cap_reached`. The hard cap takes precedence (cannot be bypassed by activity).

## Options Considered

### Refresh transport

1. **Frontend-mediated (oidc-client-ts → authentik directly)** [chosen]
   - ✅ Mirrors F0005-S0003 existing pattern; minimal new surface
   - ✅ Refresh token kept in browser session storage (F0009 baseline; no new persistence)
   - ❌ Refresh token exposed in browser memory (already true today; not a regression)

2. **Backend-mediated (frontend posts to `/auth/refresh`, backend exchanges with authentik)**
   - ✅ Refresh token confined to backend HttpOnly cookie
   - ❌ Significant new endpoint surface, new cookie boundary, refactor of F0005-S0003
   - ❌ Defers the cookie-vs-storage debate to a separate ADR (out of F0035 scope)

Chosen #1 for scope discipline. Future security tightening can promote to #2 in a follow-up.

### Mutation auto-replay after silent renewal

1. **No auto-replay; user re-confirms** [chosen, operator-mandated]
   - ✅ Zero risk of duplicate writes; safest default
   - ❌ Mild UX cost: user clicks Save twice in rare expiry-during-save scenario

2. **Auto-replay with idempotency keys**
   - ✅ Seamless UX
   - ❌ Requires idempotency keys on every mutation endpoint; current surface lacks them
   - ❌ Idempotency-window correctness under coalescing is non-trivial
   - ❌ A botched idempotency window produces silent duplicate writes — the exact failure mode we are protecting against

Chosen #1. Operator decision; documented in F0035 PRD Resolved Open Questions.

### Idle warning behaviour

1. **Modal with grace period (25 + 5 = 30 min)** [chosen, operator-mandated]
2. **Silent renewal while IdP session valid; no warning**
3. **Aggressive timeout (15 min total)**

Chosen #1 per operator decision; matches enterprise CRM convention and gives users agency.

### Form state preservation storage

1. **sessionStorage keyed by (user_id, form_key)** [chosen]
   - ✅ Per-tab isolation; survives the redirect/return cycle
   - ✅ Not persisted across browser restarts (security-positive)
   - ❌ Sensitive form fields are present in browser session storage briefly

2. **Server-side draft persistence**
   - ✅ Survives tab close; sharable across devices in principle
   - ❌ Significant new entity surface (`Draft`), retention policy, ABAC; out of F0035 scope
   - ❌ Cross-user risk if not carefully scoped

3. **In-memory only (lost on redirect)**
   - ❌ Fails the operator's mandated "preserve form state" requirement

Chosen #1. Field-level encryption deferred unless security review identifies need (`Phase 2 Candidates` in F0035 STATUS.md).

### Telemetry transport

1. **New internal ingest endpoint POST /internal/telemetry/session-continuity** [chosen]
   - ✅ Authenticated boundary; no public surface
   - ✅ Batched ingestion; backend validates schema before writing to Serilog
   - ✅ Decouples frontend emit from log infrastructure

2. **Direct browser-to-Serilog/Seq HTTP**
   - ❌ Requires public log-ingest endpoint or exposes log-store credentials to browser
   - ❌ No backend-side validation/redaction layer

Chosen #1.

## Pros / Cons

**Overall approach (#1–#7 combined)**

- ✅ Authentication boundary preserved (F0009 rules unchanged for `403`, broker scope, `InternalOnly` field stripping).
- ✅ Coalescing eliminates the cascading-redirect failure mode on high-API-count pages.
- ✅ No mutation auto-replay → zero risk of duplicate writes.
- ✅ Telemetry one-way coupling → renewal/redirect logic never blocks on telemetry health.
- ✅ Discriminator is contract-level, not heuristic-driven.
- ❌ Sensitive form fields are present in sessionStorage during the redirect/return cycle (mitigated by per-user namespacing, TTL, and explicit signout cleanup; full encryption deferred).
- ❌ The auth-error classification adds a backend conformance test per protected endpoint (one-time cost; ongoing as new endpoints arrive).
- ❌ Refresh token remains in browser session storage (no regression vs F0009 baseline; future tightening possible).

## Consequences

**Development:**

- Backend: extend the existing JWT bearer authentication failure handler to emit the ProblemDetails type registry per failure class. Add `/internal/telemetry/session-continuity` endpoint.
- Frontend: add an interceptor layer above `oidc-client-ts` for the classifier + coalescing semaphore + telemetry emitter + idle hook + restore hook. Single-tab scoping (multi-tab synchronization deferred to Phase 2).
- QE: per-endpoint backend contract conformance test (one new fixture matrix); coalescing simulation tests; full forced-re-auth E2E with dirty form.
- DevOps preflight: confirm authentik OIDC client has refresh-token issuance enabled. Register `Nebula.Session.Continuity` Serilog category.
- Security review: F0035 expected required signoff per STATUS.md (auth boundary changes + sessionStorage form snapshot scope).

**Operations:**

- Admin can query Serilog `Nebula.Session.Continuity` events for silent-renewal success rate, forced-redirect cause distribution, idle-warning effectiveness.
- No new infrastructure (the ingest endpoint runs in the existing API host; Serilog target is the existing F0033 sink).
- Phase 2 follow-up: visualisation dashboard once event volume establishes baseline.

**Risks and mitigations:**

- **Risk:** Misconfigured authentik client (refresh-token issuance disabled) causes 100% renewal failure on first deploy.
  - Mitigation: DevOps preflight checklist item; smoke test on deploy.
- **Risk:** A form classifier (sensitive-field scrubbing for snapshots) is not implemented; sensitive content lands in sessionStorage.
  - Mitigation: Phase B explicitly defers full classifier; STATUS.md documents this with a Phase 2 candidate; Security review may upgrade to MVP-required in B2.
- **Risk:** Renewal-loop throttle (5s hard-coded) too aggressive for some pages.
  - Mitigation: telemetry on `silent-renewal-fail{cause: renewal_loop_detected}` provides early signal; Architect may parameterize in Phase 2.

## Security & Compliance Notes

- The auth-error response classification must not constitute an information leak distinguishing valid-user-with-expired-token from invalid-user-with-malformed-token to an external attacker. Both surface as `401 + WWW-Authenticate: Bearer error="invalid_token"`; the ProblemDetails `type` distinguishes them but does not name the user or token state in user-meaningful terms.
- The `WWW-Authenticate` `error_description` strings are bounded to the closed set ("The access token expired", "session revoked", or specific signature/audience/issuer failure). They never include token contents, claim values, or user identifiers.
- Telemetry events MUST NOT contain PII beyond stable internal `UserId`. Schema validator enforces `additionalProperties: false` on event payload subobjects. Code review checks emit-site payload construction.
- sessionStorage form snapshots include `user_id` in key; cross-user-on-same-browser is mitigated by clearing previous user's snapshots at sign-in. Snapshots are never copied to `localStorage`.
- Refresh-token transport remains TLS-only via existing `oidc-client-ts` integration (no change).
- F0009's `InternalOnly` field boundary, broker scope, Casbin ABAC, and `policy.csv` remain unchanged. F0035 does not modify any authorization artifact.

## Invalid After This ADR

- Any `401` from a protected endpoint that lacks both `WWW-Authenticate` and a recognised `auth/*` ProblemDetails type (treated as backend-contract violation; logged server-side).
- Frontend auto-replay of mutation requests after silent renewal — must remain prohibited.
- Telemetry event payloads carrying any PII beyond `UserId` and `session_id`.
- New `401`-emitting code paths in `engine/` that bypass the central authentication-failure handler.
- Failure-class telemetry events (`silent-renewal-fail`, `forced-redirect`, `auth-classifier-fallback`, `auth-classifier-conflict`) that skip the §5a persist-before-emit step — must remain mandatory so the metric survives the very auth failures it measures.
- Adding non-failure-class events to the §5a `localStorage` buffer — those events coincide with healthy sessions; using the durability buffer there is excess I/O for marginal benefit and risks crowding out the failure-class slots.

## References

- F0035 PRD: `planning-mds/features/archive/F0035-session-continuity-and-token-refresh/PRD.md`
- F0035 stories: `F0035-S0001` through `F0035-S0005`
- F0009 baseline: `planning-mds/features/archive/F0009-authentication-and-role-based-login/`
- F0005 IdP: `planning-mds/features/archive/F0005-idp-migration/`
- F0033 Serilog: `planning-mds/features/archive/F0033-structured-logging-and-qe-toolchain-activation/`
- ADR-006 (IdP migration), ADR-007 (F0009 login + broker scope), ADR-008 (Casbin), ADR-Auth-Token-Storage, ADR-Authentication-Strategy
- Plan run evidence: `planning-mds/operations/evidence/runs/2026-05-23-41109356/`

## Follow-up Actions

- [x] Add `planning-mds/schemas/session-continuity-event.schema.json`
- [x] Update `planning-mds/api/nebula-api.yaml` to add `POST /internal/telemetry/session-continuity`
- [x] Update `planning-mds/knowledge-graph/canonical-nodes.yaml` (new capabilities, endpoint, events, schemas, adr:024 + rationale)
- [x] Enrich `planning-mds/knowledge-graph/feature-mappings.yaml` F0035 entry
- [ ] Phase 2: evaluate refresh-token transport tightening (backend-mediated HttpOnly cookie)
- [ ] Phase 2: form classifier for sensitive-field scrubbing in snapshots
- [ ] Phase 2: parameterize renewal-loop throttle if telemetry warrants
- [ ] Phase 2: session-continuity dashboard once event-volume baseline established
- [ ] Phase 2: multi-tab session-state synchronization
