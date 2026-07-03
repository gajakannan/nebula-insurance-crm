---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0002 — Day-at-a-Glance Shell + Zone-Dispatch + Multi-Part Message/Component Envelope

## Story Header

**Story ID:** F0038-S0002
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Day-at-a-Glance multi-zone shell with zone-dispatch and component envelope
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** to open the companion and see a Day-at-a-Glance shell that lays out my work in distinct zones
**So that** I have one place that shows me what is happening across my day without leaving the CRM

## Context & Background

This is the frame that makes the multi-head pathway real on day one: the orchestrator enumerates registered specialist heads and dispatches per zone; each zone result is assembled into a slot of the Day-at-a-Glance component. This is **assembly, not composition** — zones are independent; there is no cross-zone ranking or "what matters most" logic (that is the deferred Day-at-a-Glance *brain*). The shell also establishes the **versioned, multi-part message envelope** and the **component/action registry** rendering contract the whole companion uses.

## Acceptance Criteria

**Happy Path:**
- **Given** the Underwriter opens the companion panel (`experience/src/features/neuron`)
- **When** the Day-at-a-Glance view loads
- **Then** the shell renders one zone slot per registered specialist head: Renewals (live, content from F0038-S0003) plus the inert stub zones (F0038-S0004), each in its own slot

**Behavior:**
- **Given** the orchestrator builds the glance
- **When** it dispatches to each registered head
- **Then** the Renewals head returns a content payload and stub heads return a typed `inactive` payload, and the shell assembles each into its slot independently (no zone reads or ranks another zone's data)

- **Given** a Neuron response is returned
- **Then** it uses the versioned multi-part message envelope with part types `text | app | status | sources | actions`, carries `thread_id`, and the frontend renders only **registered component identifiers with validated props** — no model-generated markup or numbers are rendered

- **Given** the glance thread
- **Then** it is a single domain-level/free-form thread auto-titled "Day at a Glance" / "Renewals" (single-thread v1, no switcher; thread management is F0039)

**Alternative Flows / Edge Cases:**
- A head fails or times out → its slot shows a typed error/empty state; other zones still render (one zone failing does not blank the shell).
- A response references an unregistered component identifier → the frontend refuses to render it and shows a safe fallback; nothing executable is rendered (denied by the registry).
- Unauthorized/expired session → the panel surfaces an auth-required state rather than partial data.

## Interaction Contract

N/A — read-only assembly/rendering. The glance shell renders zone results and the message envelope; it performs no CRM mutation. (Mutations are F0038-S0005 / F0038-S0006.)

## Data Requirements

**Required (read/render):**
- Registered specialist-head list (from the registry, F0038-S0001).
- Per-zone payload: Renewals content payload (S0003) and stub `inactive` payloads (S0004).
- Message envelope: `thread_id`, ordered parts (`text|app|status|sources|actions`), component identifier + validated props + action payloads.

**Validation Rules:**
- The envelope schema is versioned so persisted threads replay correctly as the `app`-part schema evolves.
- Only registry-known component identifiers render; unknown identifiers are rejected.

## Role-Based Visibility

**Roles that can view the glance:**
- Underwriter (primary) and Distribution (assist) — both authenticated CRM users; data within each zone is permission-respecting via the engine (the shell renders only what the user's token authorizes).

**Data Visibility:**
- InternalOnly: the glance thread is private to the creating user.
- ExternalVisible: none — no external/portal surface in v1.

## Non-Functional Expectations

- Performance: the shell renders zone slots progressively; a slow zone must not block the others from rendering.
- Security: rendering is restricted to registered components with validated props; no executable markup path exists; authorization is enforced engine-side via the forwarded user token.
- Reliability: per-zone error isolation (one zone's failure is contained to its slot).

## Dependencies

**Depends On:**
- F0038-S0001 — runtime, head registry, orchestration assets, message envelope plumbing.

**Related Stories:**
- F0038-S0003 — Renewals zone content.
- F0038-S0004 — stub zone inactive payloads.
- F0038-S0007 — out-of-scope redirect responses also use this envelope.

## Business Rules

1. **Assembly, not composition:** zones are independent; no cross-zone ranking/merge in v1 (intake guardrail).
2. **Zone-dispatch is the F0040 extension point:** stubbed heads return a typed `inactive` payload; flipping one to live is an F0040 concern.
3. **Component registry rendering (intake L1):** the model picks component + data; React renders pre-built registered components; the model never emits markup/numbers.
4. **Thread id keys conversation state (intake seam 1):** the envelope carries `thread_id` from day one even with a single thread.

## Out of Scope

- Cross-zone composition/ranking (Day-at-a-Glance brain — Later).
- Multi-thread management UI / switcher (F0039).
- Any zone other than Renewals returning live content.
- MCP-UI / external-host sandboxed resource rendering.

## UI/UX Notes

- Screens involved: Neuron companion panel — Day-at-a-Glance view (see PRD `## Screen Layouts (ASCII)`).
- Key interactions: open companion → glance renders zone slots; Renewals slot is live, others show "not yet active".

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect, Phase B) Exact envelope schema versioning and component/action registry contract.

**Assumptions (to be validated):**
- A single glance thread is sufficient for v1 (full thread UX deferred to F0039).

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (zone failure isolation, unknown component rejection, auth-required state)
- [ ] Permissions enforced (engine-side via forwarded token; per-zone data respects authorization)
- [ ] Audit/timeline logged — N/A (read-only assembly; no CRM mutation in this story)
- [ ] Tests prove zone-dispatch assembles live + inactive payloads and the envelope renders only registered components
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
