---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0002 — Owner-Scoped Thread and History API

## Story Header

**Story ID:** F0039-S0002
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Owner-scoped create/list/get/rename/delete thread API with paginated resumable history
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** to create, list, switch between, rename, and delete my own companion conversations and resume any
of them with full history
**So that** my companion work stays organized and private to me across records and sessions.

## Context & Background

With the durable store in place (S0001), this story exposes the owner-scoped thread lifecycle and history
retrieval over HTTP so the panel (S0003) and dispatcher (S0007) can manage conversations. Threads are
**private to the creating user**; anchors (free-form / domain / record) are **fixed at creation**. Delete is
a **soft-delete** with retention per the `ActivityTimelineEvent` policy. History is cursor-paginated and
ordered by the server sequence from S0001 so resume is deterministic.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user
- **When** they call create / list / get / rename / delete
- **Then** each operates only on **their own** threads, and rename/delete update `updated_at`
  transactionally.

- **Given** a thread with message history
- **When** the user retrieves history
- **Then** they receive messages in **server-sequence order**, cursor-paginated, and can **resume** the
  thread with history intact.

**Owner-scoping / Security:**
- **Given** user A's thread id
- **When** user B attempts get / rename / delete / history on it
- **Then** the request **fails closed** (404/deny) and reveals nothing about the thread's existence or
  contents; a cross-user access test asserts this.

**Behavior / Edge Cases:**
- Anchor is immutable — no re-anchoring endpoint; attempts are rejected.
- Rename validation: empty or over-length title → validation error (ProblemDetails).
- Delete is soft; deleted threads disappear from the list after reload; retention behavior is explicit and
  documented.
- Listing a user with no threads returns an empty list (drives the panel empty state).
- Pagination: a stable cursor; the last page returns fewer than a full page without error.

## Interaction Contract

- **Entry point:** Neuron thread endpoints (create/list/get/rename/delete/history) called from the panel.
- **Action → editable state:** rename edits `neuron.threads.title`; delete sets the soft-delete marker;
  create inserts an owner-scoped thread with immutable anchor + auto-title.
- **Save result / persistence evidence:** after reload, renamed titles persist, deleted threads are absent,
  and resumed threads replay ordered history.
- **Roles/status rules:** every read and write is owner-scoped by `owner_user_id`; no role widens another
  user's visibility.
- **Validation failure:** invalid title / unknown thread / cross-user access → ProblemDetails (400/404),
  never a silent success.
- **Audit/timeline:** thread lifecycle operations recorded in the Neuron operation store; retention per
  `ActivityTimelineEvent` policy.

## Data Requirements

**Required:**
- `neuron.threads` (owner_user_id, anchor {kind, ref}, title, timestamps, soft-delete marker),
  `neuron.messages` (server sequence, envelope), from S0001.
- Cursor-pagination parameters for history retrieval.

**Validation Rules:**
- All queries filter by `owner_user_id`; cross-user access is impossible (fail closed).
- Anchor immutable after creation.
- Title non-empty and within max length.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution — each sees and manages **only their own** threads; the API grants no CRM
  data authorization (CRM reads still go through the engine with the user token).

**Data Visibility:**
- InternalOnly: a user's own thread/message data; no cross-user visibility.
- ExternalVisible: none.

## Non-Functional Expectations

- **Security:** owner-scoping enforced server-side on every operation; cross-user access denied and tested.
- **Performance:** paginated history retrieval bounded per page; ordered by indexed server sequence.
- **Reliability:** soft-delete + retention are explicit; resume replays complete ordered history.

## Dependencies

**Depends On:**
- F0039-S0001 — durable store, server sequence, transactional `updated_at`.

**Related Stories:**
- F0039-S0003 — panel consumes this API for list/switch/rename/delete/resume.
- F0039-S0007 — dispatcher opens/resumes owner-scoped threads and persists turns.

## Business Rules

1. **Private by owner:** threads/messages are visible and mutable only to the creating user.
2. **Immutable anchor:** the anchor (free-form/domain/record) is fixed at creation; no re-anchoring.
3. **Soft-delete + retention:** delete is reversible-by-retention-policy, not a hard purge.
4. **Fail closed:** cross-user or unknown-thread access denies without leaking existence.

## Out of Scope

- Thread sharing, cross-user visibility, full-text thread search (Later).
- UI/UX rendering — F0039-S0003.
- Intent-model behavior — F0039-S0004+.

## UI/UX Notes

- Screens involved: Neuron conversation panel — thread list + transcript (PRD `## Screen Layouts (ASCII)`).
- Key interactions: new / switch / rename / delete / resume drive the panel's list and transcript states.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect, Phase B) Exact retention window semantics for soft-deleted threads relative to the
  `ActivityTimelineEvent` policy.

**Assumptions (to be validated):**
- Cursor pagination (not offset) is acceptable for history; a single owner per thread (no shared threads).

## Definition of Done

- [ ] Acceptance criteria met (create/list/get/rename/delete + paginated resumable history)
- [ ] Edge cases handled (immutable anchor, title validation, soft-delete/retention, empty list, last page)
- [ ] Permissions enforced — owner-scoping on every op; cross-user access fails closed (tested)
- [ ] Audit/timeline logged — thread lifecycle recorded; retention explicit
- [ ] Tests prove owner-scoping (cross-user denied), ordered resumable history, rename/delete persistence
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
