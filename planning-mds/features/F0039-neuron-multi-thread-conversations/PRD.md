---
template: feature
version: 1.1
applies_to: product-manager
---

# F0039: Neuron Multi-Thread Conversations

**Feature ID:** F0039
**Feature Name:** Neuron Multi-Thread Conversations
**Epic:** Neuron Companion (AI Conversational Layer) — Next
**Priority:** Medium
**Phase:** Neuron Companion
**Status:** Provisional skeleton (Planned) — **scope firms up after F0038 lands.**

> **Provisional.** This is a reserved placeholder to show the full epic arc. The
> real persistence model, thread UX, and acceptance criteria depend on F0038's
> persistence ADR, backend-owned `neuron` operation schema, prompt/provenance
> model, A2A task/thread mapping, and the as-built message envelope. Do not
> treat this as a committed spec — it will be re-derived during F0039's own
> `plan` run.
> Epic source: [`../F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).

## Feature Statement

**As an** Underwriter / Distribution user
**I want** to keep, switch between, rename, and delete multiple companion conversations (record-anchored or free-form)
**So that** my companion work persists and stays organized across records and sessions

## Business Objective

- **Goal:** Implement the real conversation store and thread management UX on top of the seams F0038 reserved (thread_id-keyed state, persistence-home ADR, versioned envelope).
- **Metric:** TBD at plan (e.g., thread resume rate, return usage).

## Scope & Boundaries (provisional)

**Likely In Scope:**
- Expansion of the backend-owned Neuron operation persistence model established
  in F0038 (Postgres-via-engine, preferably separate `neuron` schema) for thread
  list/switch/rename/delete and replayable message history.
- Thread list / switch / rename / delete; resume an existing thread.
- Record-anchoring UX (domain-level, record-level, free-form) with auto-title; anchor fixed at creation (no re-anchoring).
- Threads private to the creating user; user-deletable; retention per `ActivityTimelineEvent` policy.

**Out of Scope (provisional):**
- New specialist heads / live zones (F0040).
- Cross-zone composition (Later).
- Thread search / share / cross-device niceties (Later).

## Dependencies

- **F0038** — backend-owned Neuron operation persistence, prompt/provenance
  model, A2A `contextId`/task mapping, versioned message envelope,
  component/action contract, and `thread_id` seam (hard dependency).
- **ADR-027** — Neuron Companion A2A-aligned orchestration foundation.

## Related User Stories

- To be defined during F0039's `plan` run, after F0038 establishes the persistence interface.
