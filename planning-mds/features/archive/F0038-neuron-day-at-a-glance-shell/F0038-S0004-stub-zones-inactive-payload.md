---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0004 — Stub Zones (Inert "Not Yet Active" Payload)

## Story Header

**Story ID:** F0038-S0004
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Inert stub zones for Tasks, Pipeline, and Broker activity
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** the other Day-at-a-Glance zones to clearly show they are coming but not yet active
**So that** I understand the companion will grow to cover more of my work, while today I rely only on the live Renewals zone

## Context & Background

Per the operator's confirmed product decision (G1), the non-live zones are **shown as visible "not yet active" stubs** rather than hidden. This advertises the multi-head pathway and sets up F0040 ("flip a stub to live"). Stubs **read no data** and return a typed `inactive` zone payload through the same zone-dispatch contract as the live Renewals head. A stub agrees with anything, so it does **not** validate the head API — the head contract hardens at F0040 with the first real second consumer.

## Acceptance Criteria

**Happy Path:**
- **Given** the Underwriter opens the glance
- **When** the shell dispatches to the stub heads (Tasks, Pipeline, Broker activity)
- **Then** each returns a typed `inactive` payload and its slot renders a "not yet active" placeholder with no data

**Behavior:**
- **Given** a stub zone
- **Then** it performs **no** engine read and exposes **no** action/CTA (nothing clickable that implies live behavior)
- **Given** the set of zones
- **Then** the three stub zones (Tasks, Pipeline, Broker activity) plus the live Renewals zone are all present in the shell

**Alternative Flows / Edge Cases:**
- A stub head is somehow asked to act → it returns the typed `inactive` payload (never partial data, never an error implying it tried to read).
- Adding/removing a stub is a registry change only — the shell renders whatever registered heads return.

## Interaction Contract

N/A — read-only/inert. Stub zones have no mutation surface and no data reads.

## Data Requirements

**Required:**
- Stub-zone identity (zone name + `inactive` status) per registered stub head.

**Validation Rules:**
- The `inactive` payload type carries no business data fields.
- Stub heads must not call the engine.

## Role-Based Visibility

**Roles that can view:**
- Underwriter and Distribution — both see the "not yet active" stubs (no authorization-sensitive data is shown because stubs read nothing).

**Data Visibility:**
- InternalOnly: none beyond zone labels.
- ExternalVisible: none.

## Non-Functional Expectations

- Performance: stub zones render instantly (no I/O).
- Security: no data path exists in a stub, so no authorization decision is needed; there is no way for a stub to leak data.
- Reliability: stub rendering never blocks or fails the live Renewals zone.

## Dependencies

**Depends On:**
- F0038-S0001 — head registry + `inactive` payload type.
- F0038-S0002 — the shell renders stub slots from `inactive` payloads.

**Related Stories:**
- F0040 — flips one of these stubs to a live head (future).

## Business Rules

1. **Visible stubs are a conscious product choice (G1-confirmed):** show "not yet active" zones rather than hiding until live.
2. **Stubs read nothing (intake §3.5):** no other domain is read in v1.
3. **Thin, provisional head contract (intake guardrail):** stubs prove a slot exists; they do not validate the head API and must not be gold-plated.

## Out of Scope

- Any live data or action in Tasks, Pipeline, or Broker activity zones.
- Cross-zone composition.
- Hardening the specialist-head contract (that is F0040).

## UI/UX Notes

- Screens involved: stub zone slots in the Day-at-a-Glance shell (see PRD `## Screen Layouts (ASCII)` — "○ soon / not yet active").
- Key interactions: none — stubs are non-interactive by design.

## Questions & Assumptions

**Open Questions:**
- [ ] (Design, Phase B/feature) Exact "not yet active" placeholder copy/visual treatment.

**Assumptions (to be validated):**
- Three stub zones (Tasks, Pipeline, Broker activity) is the right v1 set, matching the intake sketch.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (stub never reads/acts; isolation from live zone)
- [ ] Permissions enforced — N/A (stubs read no data; documented why)
- [ ] Audit/timeline logged — N/A (no mutation, no read)
- [ ] Tests prove stub heads return the typed `inactive` payload and make no engine call
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
