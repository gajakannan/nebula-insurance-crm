---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0006 — Mock-Send (Commit Identified → Outreach Transition, Fake SMTP)

## Story Header

**Story ID:** F0038-S0006
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Mock-send commits the real renewal workflow transition without dispatching email
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** a Send action on my reviewed draft that advances the renewal and records that outreach happened
**So that** my pipeline reflects that I have begun broker outreach, even while real email delivery is still simulated

## Context & Background

Mock-send is the deliberate v1 boundary: it **commits the real `Identified → Outreach` workflow transition** (emitting a `WorkflowTransition` + an `ActivityTimelineEvent`) but **fakes the SMTP delivery** — no real email leaves the system. So the pipeline state change is real; only the transport is simulated. This is **not** gated on F0021; the system migrates to a real Communication Hub draft + real send Later.

## Acceptance Criteria

**Happy Path:**
- **Given** an Underwriter with a reviewed/edited draft (from F0038-S0005) on a renewal in `Identified`
- **When** the Underwriter clicks `[Send]` (mock-send)
- **Then** the renewal transitions `Identified → Outreach`, a `WorkflowTransition` is recorded, a "sent (simulated)" `ActivityTimelineEvent` is emitted, and **no real email is dispatched**

- **Given** a completed mock-send
- **When** the Underwriter reloads the renewal / thread
- **Then** the renewal shows `Outreach` state and the timeline shows both the draft event and the "sent (simulated)" event with provenance (actor, timestamp, model, prompt id/version, content hash)

**Alternative Flows / Edge Cases:**
- A user **without** `renewal:draft_outreach` attempts mock-send → engine returns forbidden/denied (403); no transition, no events.
- Renewal not in `Identified` (e.g., already `Outreach` or a terminal/invalid state) → the transition is rejected as an invalid transition; state is unchanged and the user sees a typed reason.
- Engine/transition failure mid-commit → no partial state: the transition + both events commit together or not at all (no "sent" event without the transition).
- Under no circumstance is a real email dispatched in v1.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| In-chat reviewed draft → `[Send]` (mock-send) affordance | Click `[Send]` | Draft text is finalized at send; the transition itself is not user-editable | Renewal transitions `Identified → Outreach`; a `WorkflowTransition` + a "sent (simulated)" `ActivityTimelineEvent` are committed; SMTP delivery is faked (no real email) | Reload renewal → state is `Outreach`; timeline shows the "sent (simulated)" event with provenance; no message in any real outbound mailbox | Underwriter **only** (`renewal:draft_outreach`); renewal must be in `Identified` (valid `Identified → Outreach` transition) |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — a real `WorkflowTransition` and timeline event are committed and survive reload.
- [ ] The mutation has validation (valid-transition + role check) and error behavior (invalid transition / forbidden are typed, atomic-rollback on failure).
- [ ] The mutation emits audit/timeline events (transition + simulated-send, with provenance).
- [ ] Tests prove the transition + events persist after reload, that no real email is dispatched, that invalid-state and unauthorized attempts are rejected.

## Data Requirements

**Required Fields:**
- Renewal reference + current workflow state (must be `Identified`).
- Finalized draft content (from S0005).
- `WorkflowTransition` record (`Identified → Outreach`, actor, timestamp).
- "Sent (simulated)" `ActivityTimelineEvent` with provenance (actor, timestamp, model, prompt id/version, content hash).

**Validation Rules:**
- Transition must be a valid `Identified → Outreach` move per the renewal state machine.
- SMTP/transport is mocked; the system must guarantee no real send path is invoked.

## Role-Based Visibility

**Roles that can mock-send:**
- Underwriter — holds `renewal:draft_outreach`.
- Distribution — cannot mock-send in v1.

**Data Visibility:**
- InternalOnly: the simulated-send event and provenance are internal audit data.
- ExternalVisible: none — no external communication is produced (delivery is faked).

## Non-Functional Expectations

- Performance: mock-send completes within an interactive latency budget.
- Security: authorization enforced engine-side (`renewal:draft_outreach`) via forwarded user token; unauthorized denied (403).
- Reliability: transition + both events are atomic (all-or-nothing); guaranteed no real email dispatch.

## Dependencies

**Depends On:**
- F0038-S0005 — the reviewed/edited draft being sent.
- F0007 Renewal Pipeline — the `Identified`/`Outreach` states and transition machinery.

**Related Stories:**
- F0038-S0008 — mock-send count + the "draft-ready/sent" timestamp feed telemetry.

## Business Rules

1. **Mock-send commits the real transition (intake decision B / §6):** `Identified → Outreach` with `WorkflowTransition` + `ActivityTimelineEvent`; only SMTP delivery is faked.
2. **Underwriter-only (intake decision C):** `renewal:draft_outreach` gates send.
3. **No real outbound (intake §3.4):** v1 never dispatches a real email.
4. **Not gated on F0021 (intake §6):** interim now; real Comms Hub + real send Later.
5. **Atomicity:** never a "sent" event without the committed transition.

## Out of Scope

- Real SMTP/email delivery.
- Communication Hub (F0021) integration.
- Any transition other than `Identified → Outreach`.
- Re-sending / send scheduling / retries beyond a single mock-send.

## UI/UX Notes

- Screens involved: in-chat draft `[Send]` affordance + renewal timeline (see PRD `## Screen Layouts (ASCII)`).
- Key interactions: review draft → `[Send]` → renewal advances + "sent (simulated)" appears on the timeline.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect, Phase B) The mock-send engine endpoint shape that commits the transition without dispatching email.

**Assumptions (to be validated):**
- The F0007 renewal state machine permits `Identified → Outreach` for the Underwriter role.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (invalid transition, unauthorized denied, atomic rollback, never real send)
- [ ] Permissions enforced (`renewal:draft_outreach`, engine-side)
- [ ] Audit/timeline logged (`WorkflowTransition` + "sent (simulated)" event with provenance)
- [ ] Tests prove transition + events persist after reload, no real email is dispatched, invalid/unauthorized rejected
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
