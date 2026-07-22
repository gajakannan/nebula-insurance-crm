---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0007 — Dispatcher, Persistence, and Provenance Integration

## Story Header

**Story ID:** F0039-S0007
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Wire durable persistence and the direct resolver into the message dispatcher with full traceability
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** every message and every companion response — routed, clarified, redirected, or failed — to be
persisted in my thread and traceable end to end
**So that** my conversation is complete and auditable on reload, and only validated routes ever reach a
specialist head.

## Context & Background

This story integrates S0001–S0006 into the live `MessageDispatcher`: persist the inbound message in the
owner-scoped thread, run the direct resolver **before** any downstream head dispatch, persist every assistant
envelope (clarification / redirect / failure / routed result), and make the whole chain traceable (parent
resolver run → logical scope/intent stages → optional downstream head run). The **engine remains the sole
authorization boundary — unchanged**. It replaces F0038's browser-memory turn handling with server-owned,
persisted, resolver-gated dispatch.

## Acceptance Criteria

**Happy Path:**
- **Given** an inbound message
- **When** the dispatcher handles it
- **Then** the message is **persisted in its owner-scoped thread** (S0001/S0002) **before** routing, the
  **direct resolver (S0006) runs before any head dispatch**, and downstream heads execute **only after a
  validated route**.

- **Given** each possible outcome
- **When** the turn completes
- **Then** the assistant **clarification, redirect, failure, and routed-result envelopes are all persisted**
  as replayable messages in the thread.

**Traceability:**
- **Given** a completed turn
- **When** provenance is inspected
- **Then** the **parent resolver run, logical scope/intent stages, and optional downstream head run are
  traceable** (A2A-shaped run/records), with model/prompt/schema/catalog provenance recorded and **no raw
  prompt content** in general telemetry.

**Authorization unchanged:**
- **Given** a routed action that reads/writes CRM data
- **When** the head's tools call the engine
- **Then** the **engine performs authorization and business validation exactly as before** — no classifier
  decision grants access, and `renewals.mock_send` still requires explicit confirmation.

**Behavior / Edge Cases:**
- A persistence failure on the inbound message fails safe **before** resolution/routing (S0001).
- A resolver failure persists a bounded failure/redirect envelope; no head dispatch, no engine call.
- Duplicate client-message keys are idempotent (no duplicate turn); the Daily Brief envelope is idempotent by
  its key.

## Interaction Contract

- **Entry point:** `POST /v1/messages` → dispatcher.
- **Action → editable state:** appends user + assistant message rows (envelopes) to the owner-scoped thread;
  a validated route may trigger an engine-authorized CRM read/write via the head.
- **Save result / persistence evidence:** the full turn (user message + assistant envelope) replays in order
  after reload; routed CRM writes are the engine's own persisted state + timeline event (unchanged).
- **Roles/status rules:** owner-scoped throughout; engine authorization governs CRM data access; confirmation
  required for confirmation-flagged actions.
- **Validation failure:** any resolver/schema/registry failure persists a bounded assistant envelope and stops
  before the engine.
- **Audit/timeline:** A2A-shaped runs + operation metadata recorded; engine emits its own CRM timeline events
  for routed writes.

## Data Requirements

**Required:**
- S0001/S0002 store + APIs; S0006 resolver; A2A-shaped run/task records; §32 telemetry fields
  (request_id, thread_id, owner_user_id_hash, agent_run_id, model_call_id, stage, outcome, reason_code, …).

**Validation Rules:**
- Inbound message persisted before routing; heads dispatched only after a validated route; every outcome
  envelope persisted; no raw prompt text in telemetry.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution — turns are owner-scoped; engine authorization unchanged for any CRM data
  access a routed head performs.

**Data Visibility:**
- InternalOnly: resolver/stage traces, run records, operation metadata.
- ExternalVisible: persisted assistant envelopes the user sees in their own transcript.

## Non-Functional Expectations

- **Security:** engine remains the only authorization boundary; owner-scoping throughout; no raw prompt
  content in telemetry.
- **Reliability:** fail-safe-before-routing on persistence failure; bounded failure envelopes on resolver
  failure; idempotent turns.
- **Observability:** end-to-end traceability across resolver, logical stages, and downstream head runs.

## Dependencies

**Depends On:**
- F0039-S0001, S0002 — durable store + owner-scoped threads.
- F0039-S0006 — direct resolver runs before dispatch.
- F0038 — message envelope, scope-guard seam, specialist-head dispatch, mock-send workflow.

**Related Stories:**
- F0039-S0003 — panel renders the persisted envelopes.
- F0039-S0008 — shadow-mode comparison hooks into this dispatch path.

## Business Rules

1. **Persist first, route second:** the inbound message is stored before resolution.
2. **Resolve before dispatch:** heads run only after a validated route.
3. **Everything is persisted:** clarify/redirect/failure/routed envelopes are all replayable.
4. **Engine authorization unchanged:** no classifier decision grants CRM access; confirmations preserved.

## Out of Scope

- Shadow-mode/evaluation orchestration — F0039-S0008 (this story provides the dispatch hook).
- Contextual adjudication — F0039-S0009.
- New specialist heads — F0040.

## UI/UX Notes

- No new screens; the persisted envelopes are what S0003 rehydrates and renders.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect, feature) Exact A2A run/record shape for the parent-resolver → logical-stage → head-run
  chain and how it maps onto existing operation-store records.

**Assumptions (to be validated):**
- The existing F0038 head-dispatch + mock-send workflow can be gated behind the new resolver without changing
  engine authorization.

## Definition of Done

- [ ] Acceptance criteria met (persist-first, resolve-before-dispatch, all envelopes persisted, traceable)
- [ ] Edge cases handled (persistence-failure fail-safe; resolver-failure bounded envelope; idempotent turns)
- [ ] Permissions enforced — engine authorization unchanged; owner-scoped; confirmations preserved (documented)
- [ ] Audit/timeline logged — A2A runs + operation metadata; engine timeline events for routed writes; no raw text
- [ ] Tests prove: persist-before-route, resolve-before-dispatch, envelope persistence for each outcome,
      end-to-end traceability, unchanged engine authorization
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
