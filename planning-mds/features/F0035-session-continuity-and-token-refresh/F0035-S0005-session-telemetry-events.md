# Story F0035-S0005: Session Continuity Telemetry Events (MVP)

## Story Header

**Story ID:** F0035-S0005
**Feature:** F0035 — Session Continuity & Token Refresh
**Title:** Session Continuity Telemetry Events (MVP)
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Nebula administrator (or operations staff member)
**I want** to see structured telemetry events for session-continuity behaviors (silent renewals, forced redirects, idle warnings)
**So that** I can measure whether the F0035 fix is reducing user-visible interruptions and diagnose individual session issues without asking users for screenshots

## Context & Background

The F0035 business metric is "number of user-visible forced login redirects during active usage." Without telemetry we cannot measure success. The operator decision (plan run 2026-05-23-41109356) is to ship MVP with structured event emission only — no dashboard build. Events flow through the F0033 Serilog structured logging baseline, where admins can query them via the existing operational tooling.

This story is the consumer/integrator of telemetry events emitted by S0001, S0002, S0003, and S0004. It defines the event schemas, the emission contract, the storage path, and the verification that events are queryable.

## Acceptance Criteria

**Event Schema Contract:**

- **Given** the events emitted by S0001/S0002/S0003/S0004 (specifically: `silent-renewal-success`, `silent-renewal-fail`, `forced-redirect`, `idle-warning-shown`, `idle-warning-accepted`, `idle-warning-dismissed`, `auth-classifier-fallback`, `auth-classifier-conflict`, `form-snapshot-skipped`)
- **When** each event is emitted
- **Then** the event payload conforms to a published schema (location: `planning-mds/schemas/session-continuity-event.schema.json` — Architect Phase B deliverable)
- **And** every event has the common fields: `event_name`, `event_version` (starts at `1`), `timestamp` (ISO 8601 with TZ), `user_id` (stable internal `UserId`), `session_id` (opaque per-session-bootstrap identifier from F0009)

**Server-Side Event Reception:**

- **Given** an event emitted by the frontend
- **When** the event reaches the backend ingest endpoint
- **Then** the backend validates the payload against the schema and rejects malformed events with HTTP `400` and a `validation_error` ProblemDetails (NOT a session-affecting `401` — telemetry failures must never look like auth failures)
- **And** valid events are written to the F0033 Serilog structured log with category `Nebula.Session.Continuity`

**PII Boundary Enforcement:**

- **Given** an event being prepared for emission
- **When** the payload is constructed
- **Then** it contains NONE of: email, full name, IP address, raw token strings, broker_tenant_id, role list, form field contents, query string parameters with potential PII
- **And** unit tests on each emitter assert the absence of these fields from the serialized payload

**Query-ability:**

- **Given** events have been emitted over a 24-hour test window
- **When** an Admin queries the Serilog store for `category = Nebula.Session.Continuity`
- **Then** all emitted events are retrievable
- **And** events can be filtered by `event_name`, `user_id`, and `timestamp` range
- **And** the field `session_id` enables correlation of all events from a single user session

**Emission Reliability:**

- **Given** the telemetry ingest endpoint is temporarily unavailable
- **When** the frontend attempts to emit an event
- **Then** the event is buffered in-memory (bounded buffer, e.g. 50 events) and retried with exponential backoff (max 3 retries, then drop)
- **And** dropped events do NOT affect user-facing behavior (session-continuity logic does not block on telemetry success)
- **And** a final-drop case is itself logged once at WARN (`Nebula.Session.Continuity.TelemetryDrop`) with the drop count

**Event Version Forward Compatibility:**

- **Given** the schema is at `event_version: 1`
- **When** a future version `2` is introduced with new optional fields
- **Then** `v1` consumers continue to function (additive evolution only)
- **And** the schema document records the evolution contract: additive-only is allowed without major bump; removing or changing types requires a version bump

**No Cross-User Correlation Without UserId:**

- **Given** events from User A and User B in the same time window
- **When** queried
- **Then** filtering by `user_id` returns only that user's events
- **And** events do NOT include cross-user correlation fields (no `device_id` shared across users, no shared session identifiers)

**Sampling / Rate Limiting:**

- **Given** the frontend emitter is healthy
- **When** events are being emitted at a normal rate (estimated < 10 events per user per session)
- **Then** all events are sent without sampling
- **And** if the per-user rate exceeds 100 events per minute (suggesting a bug / loop), the emitter drops further events for that user-session with a single WARN-level drop notice (no DoS amplification toward the ingest endpoint)

**Failure-Class Event Durability (Deferred-Emit Buffer, ADR-024 §5a):**

This block covers the four failure-class events — `silent-renewal-fail`, `forced-redirect`, `auth-classifier-fallback`, `auth-classifier-conflict` — that coincide with auth disruptions and would otherwise be lost on the in-memory + bearer-authenticated ingest path.

- **Given** a `silent-renewal-fail` event is about to be emitted because authentik returned `refresh_revoked`
- **When** the event is constructed
- **Then** BEFORE the normal fire-and-forget emit attempt, the full event JSON is written synchronously to `localStorage` at key `nebula.telemetry-defer.v1.<user_id>.<event_uuid>` (fresh v4 UUID per event)
- **And** the normal §5 emit proceeds
- **And** on HTTP 202 from ingest, the matching `localStorage` entry is deleted
- **And** on any non-202 outcome (including 401 because the session is now invalid), the `localStorage` entry remains

- **Given** the user completes a fresh OIDC sign-in (callback completion in F0009-S0002)
- **When** the bootstrap path runs
- **Then** a `drainDeferredEvents(user_id)` step enumerates all `nebula.telemetry-defer.v1.<current_user_id>.*` entries
- **And** entries older than 7 days (per-entry TTL from event `timestamp`) are purged silently with a single aggregated WARN log including the purged count
- **And** remaining entries are batched in groups of up to 10 and POSTed to the ingest endpoint
- **And** on 202 each entry in the batch is deleted from `localStorage`
- **And** on non-202 the batch remains in `localStorage` (drain retries on next successful bootstrap)
- **And** the drained events carry their original `timestamp` so age-of-event is recoverable from the payload (the endpoint cannot tell drained vs in-memory paths apart, intentionally)

- **Given** the per-user deferred buffer has 100 entries
- **When** an additional failure-class event is emitted
- **Then** the oldest entry (lowest `timestamp`) is evicted before the new entry is written (LRU bound)
- **And** the eviction is logged at WARN with the evicted event's `event_name` and `timestamp`

- **Given** User A's deferred entries are sitting in `localStorage` and User B signs in on the same browser
- **When** User B's OIDC bootstrap runs `drainDeferredEvents(<user_b_id>)`
- **Then** the drain reads ONLY entries matching `nebula.telemetry-defer.v1.<user_b_id>.*`
- **And** User A's entries are NOT read or POSTed by User B's drain (per-user isolation)
- **And** User A's entries remain in `localStorage` until either (a) User A signs in again and drains them, or (b) they exceed the 7-day TTL

- **Given** the explicit sign-out path runs for User X
- **When** local session is cleared
- **Then** all `nebula.telemetry-defer.v1.<user_x_id>.*` entries are deleted in the same operation (consistent with form-snapshot sign-out cleanup per S0003)

- **Given** `localStorage.setItem` throws (quota exceeded, storage disabled, private browsing)
- **When** the persist-before-emit step runs
- **Then** the exception is caught and silently dropped
- **And** the original failure-path code does NOT branch on telemetry-write success (strict one-way coupling from session-continuity to telemetry preserved)

- **Non-failure-class events** (`silent-renewal-success`, `idle-warning-shown/accepted/dismissed`, `form-snapshot-skipped`) are NOT written to the deferred-emit buffer; they coincide with healthy sessions where the existing in-memory + retry path is sufficient. Tests assert that these event classes do NOT appear in `localStorage`.

## Interaction Contract

This story is a backend-frontend contract story; the "user" is the Admin querying the logs. Direct user interaction is the Admin running an operational query.

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Telemetry ingest endpoint (backend) | (system — frontend emitter posts events) | N/A | Event persisted to Serilog `Nebula.Session.Continuity` category | Admin queries log store, retrieves events by `category`, `event_name`, `user_id`, `timestamp` | Backend endpoint requires authenticated session; rejects unauthenticated posts |
| Admin log query (existing F0033 tooling) | Run query against Serilog store | N/A — read-only | N/A | Query results visible to Admin role | Admin role only (F0009 access boundary) |

This story is read-only at the Admin level (querying logs); the only "mutation" is the emission itself, which is system-driven.

## Data Requirements

**Common Event Envelope (all events):**

- `event_name`: string, enum of recognized event names
- `event_version`: integer, starts at `1`
- `timestamp`: ISO 8601 with TZ
- `user_id`: stable internal `UserId` (per ADR-006)
- `session_id`: opaque per-session-bootstrap identifier (sourced from F0009 OIDC callback bootstrap)

**Event-specific payload fields:** see individual stories S0001 (silent-renewal-*), S0002 (idle-warning-*), S0003 (forced-redirect, form-snapshot-skipped), S0004 (auth-classifier-fallback, auth-classifier-conflict).

**Forbidden across all events:**

- email, full name, IP address (server-side may log IP separately at the ingest layer; never carried in event payload)
- raw token strings (access, refresh, ID)
- broker_tenant_id, role list, claims
- form field contents
- query string parameters
- error stack traces

**Validation Rules:**

- The backend validates incoming events against the JSON Schema strictly (additionalProperties: false at the top level; payload subobjects per event type).
- The frontend emitter validates against the same schema in dev mode (catches schema drift early).

## Role-Based Visibility

**Roles affected:**

- All authenticated roles emit events through their sessions.
- Only Admin role can query the Serilog log store (existing F0009/F0033 boundary).

**Data Visibility:**

- InternalOnly content: telemetry events are server-side; not exposed in any user-facing UI.
- BrokerVisible content: BrokerUser sessions emit events with the same shape and same PII boundary as Internal users; Admin sees the same fields.

## Non-Functional Expectations

- **Performance:**
  - Event emission: < 50ms p95 (async fire-and-forget; never blocks user-facing operations).
  - Ingest endpoint: < 100ms p95 for a single event POST.
  - Buffered batch emission (if implemented): up to 10 events per POST, < 200ms p95.
- **Security:**
  - Ingest endpoint requires authenticated session (rejects unauthenticated POSTs with `401`).
  - Endpoint is NOT publicly addressable (same auth boundary as other protected APIs).
  - PII boundary enforced at emit time (frontend) AND validate time (backend); double-check defense.
- **Reliability:**
  - Drop policy is bounded (3 retries, then drop with WARN); never infinite retry.
  - Telemetry failures NEVER affect session-continuity behavior (one-way coupling).
  - Buffered events are bounded (50 in-memory); overflow drops oldest with WARN.

## Dependencies

**Depends On:**

- F0033-S0001 — Establish Serilog Structured Logging Baseline (provides the structured log infrastructure)
- F0009-S0002 — OIDC Callback and Session Bootstrap (provides `session_id` source)
- F0005-S0004 — Principal Key Data Model (provides stable internal `UserId` per ADR-006)
- F0035-S0001, S0002, S0003, S0004 — emitters of the events this story specifies

**Related Stories:** None outside this feature.

## Business Rules

1. **One-way coupling.** Session-continuity logic emits events fire-and-forget. Failures in telemetry never affect user-facing behavior. Tests enforce this.
2. **No PII.** The event payload PII boundary is hard-rule; any future event must conform. Code review checks.
3. **Schema is the contract.** Frontend emitter and backend validator share the same schema document; mismatches are caught in dev mode.
4. **Admin-only read.** Querying the telemetry stream is an Admin operation; no in-app surface exposes these events to other roles.

## Out of Scope

- Dashboards, charts, or visualizations of telemetry data (deferred; admins query via F0033 baseline tooling).
- Real-time alerting on event patterns (deferred; future feature once baseline data establishes thresholds).
- Long-term retention policy for events beyond F0033's existing retention contract (no new retention rules).
- Event sourcing or replay capability (events are diagnostic, not authoritative).
- Per-event encryption (events are non-PII by design; ingest endpoint TLS is sufficient).

## UI/UX Notes

- Screens involved: NONE. Admin uses existing operational query tooling from F0033.
- Key interactions: none user-facing in this story.

## Questions & Assumptions

**Resolved by ADR-024 (Phase B):**

- [x] Endpoint location? — **Resolved (ADR-024 §6 + `planning-mds/api/nebula-api.yaml` `SessionTelemetry` tag):** new endpoint `POST /internal/telemetry/session-continuity`. Authenticated bearer; 202 Accepted; batch of 1–10 events.
- [x] Serilog category? — **Resolved (ADR-024 §5):** category `Nebula.Session.Continuity` explicitly registered as part of DevOps preflight (Required Signoff Roles in F0035 STATUS.md). F0033's setup tolerates additive categories.
- [x] Schema location? — **Resolved (ADR-024 §5):** `planning-mds/schemas/session-continuity-event.schema.json`, alongside other API schemas as suggested.

**Open Questions:** (none — all closed by ADR-024)

**Assumptions (to be validated):**

- F0033's Serilog setup tolerates additive event categories without configuration changes.
- The frontend can perform a fire-and-forget POST with sub-50ms p95 reliably (typical case is very fast on a healthy backend).
- 50-event in-memory buffer with 3-retry exponential backoff is sufficient; if real traffic exceeds this we'll observe and tune.

## Definition of Done

- [ ] Acceptance criteria met (schema contract, server-side reception, PII boundary, query-ability, emission reliability, version compatibility, isolation, rate limiting)
- [ ] Edge cases handled (ingest unavailable, buffer overflow, malformed events)
- [ ] Permissions enforced (Admin-only read; authenticated emission only)
- [ ] Audit/timeline logged (telemetry events themselves serve as the audit; drop notices logged at WARN)
- [ ] Tests pass (schema conformance per emitter; backend rejection of malformed events; PII assertion tests; rate-limit/drop tests)
- [ ] Documentation updated (GETTING-STARTED notes the event schema location and query examples; STATUS.md provenance)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
