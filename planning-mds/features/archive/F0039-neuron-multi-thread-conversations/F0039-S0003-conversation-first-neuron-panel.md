---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0003 — Conversation-First Neuron Panel

## Story Header

**Story ID:** F0039-S0003
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Conversation-first companion panel with server-rehydrated transcripts and thread management UX
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** the companion to open on my conversations — resuming my last thread, letting me switch/rename/delete
threads, and showing Day-at-a-Glance as a message in the transcript
**So that** the companion feels like a durable place my work lives, not a dashboard that forgets me on reload.

## Context & Background

F0038's shell is dashboard-first with a browser-memory transcript. This story flips it to **conversation-first**:
the panel resumes the last visible thread (or offers a picker), the transcript is **server-owned** (S0002
history), and Day-at-a-Glance becomes a **persisted assistant "Daily Brief" envelope** with structured app
parts rather than a detached local-only view. Thread list UX (new/switch/rename/delete) plus loading/empty/
failure states make it production-shaped. See PRD `## Screen Layouts (ASCII)` for desktop + narrow layouts.

## Acceptance Criteria

**Happy Path:**
- **Given** a returning user with existing threads
- **When** the panel opens
- **Then** it **resumes the last visible thread** or offers a thread picker, and the transcript is
  **rehydrated from server-owned history** (not local-only appended state).

- **Given** a user sends a message or reloads the page
- **When** the transcript renders
- **Then** it reflects server-owned ordered history; a reload does not lose or reorder messages.

- **Given** the Daily Brief
- **When** it is shown
- **Then** it renders from a **persisted assistant envelope with structured app parts**, replayable like any
  other assistant message.

**Behavior / Edge Cases:**
- Thread **switch, rename, delete** work from the list; **loading, empty ("no conversations yet"),
  switching, and send-failure** states are all covered (send-failure keeps the drafted message and offers
  retry).
- **Suggested prompts** can begin a conversational request **without** a mandatory Day-at-a-Glance load
  first.
- Narrow viewport (<768px): the thread list moves to a drawer (☰); selecting a thread closes the drawer and
  re-hydrates the transcript.
- A redirect or clarify result renders as a bounded CRM bubble (copy is application-owned, not model prose).

## Interaction Contract

- **Entry point:** Neuron companion panel — thread list (new/switch/rename/delete) + composer (Send).
- **Action → editable state:** rename/delete call the S0002 API; send calls the message endpoint; switching
  loads a different thread's server history.
- **Save result / persistence evidence:** after reload, the resumed thread, renamed titles, deletions, and
  message order all reflect server state — not browser memory.
- **Roles/status rules:** the panel only ever shows the current user's own threads (S0002 owner-scoping).
- **Validation failure:** send failure shows an inline retry without losing the message; rename validation
  errors surface inline.
- **Audit/timeline:** UI actions map to owner-scoped API calls recorded server-side (S0002).

## Data Requirements

**Required:**
- S0002 thread list + history APIs; the versioned message envelope; structured app-part rendering for the
  Daily Brief assistant envelope.

**Validation Rules:**
- Transcript state is derived from server history, not local-only appends.
- Only owner-visible threads are listed.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution — each sees only their own threads; the panel grants no additional CRM
  authorization.

**Data Visibility:**
- InternalOnly: the user's own conversations.
- ExternalVisible: none.

## Non-Functional Expectations

- **Performance:** thread switch re-hydrates within an interactive budget (skeleton while loading); no full
  reload required to switch threads.
- **Reliability:** reload/restart preserves the transcript (server-owned); send-failure is recoverable.
- **Accessibility:** thread list and transcript are keyboard-navigable; states have visible, non-color-only
  cues (per the frontend UX audit ruleset applied at feature time).

## Dependencies

**Depends On:**
- F0039-S0002 — thread/history API (list/switch/rename/delete/resume).
- F0038 — versioned message envelope, existing companion panel surface.

**Related Stories:**
- F0039-S0007 — dispatcher persists assistant envelopes (incl. Daily Brief) the panel renders.

## Business Rules

1. **Conversation-first:** the panel opens on conversations, not a mandatory dashboard load.
2. **Server-owned transcript:** rendered history comes from the server, not browser memory.
3. **Daily Brief is a message:** it is a persisted assistant envelope, replayable in the transcript.
4. **Application-owned copy:** redirect/clarify text is app-owned, never model-generated prose.

## Out of Scope

- The intent model itself and routing behavior — F0039-S0004+.
- Thread sharing / search (Later).
- Second live zone/head — F0040.

## UI/UX Notes

- Screens: Neuron companion panel (desktop split list+transcript; narrow drawer) — PRD ASCII layouts.
- Key interactions: new / switch / rename / delete; send; suggested prompts; Daily Brief bubble.

## Questions & Assumptions

**Open Questions:**
- [ ] (Frontend Developer, feature) Exact thread-switch latency budget and skeleton behavior on slow history
  loads.

**Assumptions (to be validated):**
- Resuming the last visible thread (vs. always showing a picker) is the preferred default open behavior.

## Definition of Done

- [ ] Acceptance criteria met (resume, server-rehydrated transcript, Daily Brief envelope, thread UX)
- [ ] Edge cases handled (loading/empty/switching/send-failure; narrow drawer; suggested prompts)
- [ ] Permissions enforced — only owner threads shown; no added CRM access (documented)
- [ ] Audit/timeline logged — UI actions map to recorded owner-scoped API calls
- [ ] Tests prove reload preserves ordered transcript, thread switch/rename/delete, and Daily Brief replay
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
