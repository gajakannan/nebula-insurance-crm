---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0001 — Durable Neuron Conversation Store

## Story Header

**Story ID:** F0039-S0001
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Postgres-backed, restart-durable Neuron conversation store with server-owned ordering
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** my companion conversations and messages to be stored durably by the server
**So that** they survive restarts and reloads and always replay in the exact order they happened.

## Context & Background

F0038 shipped the `neuron.*` schema scaffold (`neuron/app/persistence/migrations/0001_neuron_schema.sql`,
six tables) but keeps transcripts in browser memory with a synchronous, non-Postgres repository. ADR-028 is
authoritative: **Neuron owns and writes `neuron.*` directly** (Postgres via the engine database, not through
the engine API). This story makes the store real and restart-durable, and gives messages a stable
server-owned order so history replay and paginated retrieval (S0002) are deterministic. It is the foundation
every other F0039 story builds on.

## Acceptance Criteria

**Happy Path:**
- **Given** the existing `0001_neuron_schema.sql` scaffold is applied in a deployed environment
- **When** a Postgres-backed `NeuronRepository` persists a thread and its messages
- **Then** the data is written to `neuron.*` in Postgres (not in-memory) and is readable after a Neuron
  process restart.

- **Given** several messages are appended to a thread
- **When** they are stored
- **Then** each receives a stable **server-assigned `BIGINT` sequence** unique per `(thread_id, sequence)`,
  and history is ordered/paged by that sequence rather than by timestamp alone.

**Idempotency:**
- **Given** the same client-message key or Daily Brief key is submitted more than once
- **When** the repository appends
- **Then** the append is **idempotent** (scoped partial unique indexes enforce it) — no duplicate row.

**Behavior / Edge Cases:**
- A forward migration (e.g. `0002_message_sequence_and_idempotency.sql`) adds the sequence, the unique
  `(thread_id, sequence)` index, nullable client-message/thread idempotency keys, and their scoped partial
  unique indexes. The existing six-table scaffold is **not** recreated.
- `updated_at` is set **transactionally** whenever a message is appended or a thread is renamed/deleted.
- **Persistence failure fails safe before routing** — if the store cannot persist the inbound message, the
  request does not proceed to intent resolution or head dispatch.
- Synchronous Postgres work must **not block the async event loop**: prefer async repository + bounded
  async pool; if conversion is phased, run sync operations in a bounded worker pool.

## Interaction Contract

- **Entry point:** any inbound `POST /v1/messages` and thread mutation (create/rename/delete via S0002).
- **Editable/persisted state:** `neuron.threads` and `neuron.messages` rows in Postgres.
- **Save result / persistence evidence:** rows survive process restart; a message read-back returns the same
  ordered sequence; duplicate idempotency keys return the original row, not a new one.
- **Roles/status rules:** writes are owner-scoped (owner enforcement detailed in S0002).
- **Validation failure:** a failed persist aborts before routing; the caller receives a bounded unavailable
  response, and the failure is recorded (no raw message text in failure telemetry).
- **Audit/timeline:** operation metadata recorded in the Neuron operation store.

## Data Requirements

**Required:**
- Postgres `neuron.threads`, `neuron.messages` (from `0001_neuron_schema.sql`) plus the `0002` forward
  migration: server `sequence BIGINT`, unique `(thread_id, sequence)`, nullable `client_message_key` /
  `thread_idempotency_key`, scoped partial unique indexes, transactional `updated_at`.

**Validation Rules:**
- Message ordering is by server sequence; `(thread_id, sequence)` is unique.
- Duplicate client-message / Daily Brief keys are rejected as duplicates (idempotent append).
- No write proceeds to routing if persistence of the inbound message fails.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution (authenticated CRM users). The store itself grants no CRM authorization;
  owner-scoping is enforced at the API layer (S0002).

**Data Visibility:**
- InternalOnly: `neuron.*` conversation data and operation metadata are internal operational data owned by
  Neuron.
- ExternalVisible: none.

## Non-Functional Expectations

- **Reliability:** thread/message data survives Neuron process restart; persistence failures fail closed
  (no routing on a failed persist).
- **Performance:** repository operations use a bounded async Postgres pool and never block the event loop.
- **Security:** no raw message text duplicated into persistence-failure telemetry.

## Dependencies

**Depends On:**
- F0038 — `0001_neuron_schema.sql` scaffold, message envelope, owner-scoped thread concept.
- ADR-028 — Neuron owns/writes `neuron.*` directly.

**Related Stories:**
- F0039-S0002 — owner-scoped thread/history API built on this repository.
- F0039-S0007 — dispatcher persists every turn through this store.

## Business Rules

1. **Neuron owns `neuron.*` (ADR-028):** persistence is direct to Postgres, not through the engine API.
2. **Server owns ordering:** messages are ordered by a server-assigned sequence, never client-supplied order.
3. **Idempotent appends:** duplicate client-message / Daily Brief keys never create duplicate rows.
4. **Fail safe before routing:** a failed inbound-message persist aborts the turn before intent resolution.

## Out of Scope

- Thread lifecycle API surface (create/list/rename/delete/history) — F0039-S0002.
- UI rendering of threads/messages — F0039-S0003.
- Any intent-model behavior — F0039-S0004+.
- Full-text search over messages (Later).

## UI/UX Notes

- No direct UI. Enables the server-owned history the conversation panel (S0003) rehydrates from.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect/AI Engineer, Phase B/feature) Exact `0002` migration DDL and whether the async conversion
  is complete or phased for the first delivery.

**Assumptions (to be validated):**
- The engine database is the correct physical home for `neuron.*` (ADR-028); a bounded async pool is
  acceptable for the initial concurrency targets.

## Definition of Done

- [ ] Acceptance criteria met (Postgres-backed, restart-durable, server-sequence ordered, idempotent)
- [ ] Edge cases handled (phased async / worker pool; transactional `updated_at`; fail-safe-before-routing)
- [ ] Permissions enforced — store grants no CRM access; owner-scoping enforced at the API layer (documented)
- [ ] Audit/timeline logged — operation metadata recorded; no raw text in failure telemetry
- [ ] Tests prove restart persistence, sequence ordering, and idempotent appends (incl. duplicate keys)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
