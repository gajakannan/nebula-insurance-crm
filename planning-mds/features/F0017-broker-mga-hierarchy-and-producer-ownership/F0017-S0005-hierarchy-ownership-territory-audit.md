---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0017-S0005 — Audit and timeline for hierarchy, ownership, and territory changes

## Story Header

**Story ID:** F0017-S0005
**Feature:** F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management
**Title:** Audit and timeline for hierarchy, ownership, and territory changes
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution & Marketing Manager
**I want** every change to hierarchy, producer ownership, and territory assignment to be recorded with who, when, and what changed
**So that** structural changes that affect accountability, future access boundaries, and reporting are traceable and trustworthy

## Context & Background

Hierarchy, ownership, and territory changes (F0017-S0001/S0003/S0004) alter
accountability and will feed future access-control, reporting, and commission
attribution. The PRD requires these changes to be auditable. This story makes
each mutation emit an immutable timeline/audit entry, consistent with the
platform's existing activity-timeline pattern (reused from F0002).

## Acceptance Criteria

**Happy Path:**
- **Given** any hierarchy/ownership/territory mutation is performed
- **When** it succeeds
- **Then** an immutable audit/timeline event is recorded capturing actor (`changedBy`), timestamp (`changedAt`), entity, change type, and old→new values, and it appears in the related entity's timeline

**Alternative Flows / Edge Cases:**
- Failed/rejected mutation (e.g., cycle, overlap) → **no** audit event is written for the rejected change (only successful mutations are recorded)
- Audit entries are append-only → an existing entry cannot be edited or deleted (attempt rejected, HTTP 403/405)
- High-volume change (bulk reparent of a subtree) → one audit event per affected node, all attributable to the same action
- Reading the timeline with no events → empty state, not an error

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| (System-emitted) entity Timeline panel | View timeline; events are produced automatically by S0001/S0003/S0004 mutations | Read-only; events are never user-editable | Audit/timeline event persisted on each successful structural mutation | Event visible in the entity's timeline after reload | Timeline readable by authenticated internal users; events emitted by the actor who made the change |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story — the audit event must actually be persisted, not just shown.
- [x] Validation: only successful mutations emit events; events are immutable.
- [x] Audit/timeline expectation is the core of this story.
- [x] Tests prove an event is persisted and visible after reload for each mutation type.

## Data Requirements

**Required Fields:**
- `eventId`, `entityRef` (node / ownership / territory), `changeType` (reparent | owner-assigned | owner-reassigned | territory-assigned | territory-created | …)
- `changedBy`, `changedAt`, `oldValue`, `newValue`

**Optional Fields:**
- `correlationId` to group events from one bulk action

**Validation Rules:**
- Events are append-only and immutable.
- Every successful S0001/S0003/S0004 mutation produces at least one event.

## Role-Based Visibility

**Roles that can read the timeline:**
- All authenticated internal users.

**Roles that can write events:**
- System only (emitted as a side effect of an authorized mutation); no direct user authoring.

**Data Visibility:**
- InternalOnly content: audit/timeline events are internal.
- Authorization note: timeline read-scoping follows the same deferred hierarchy-authorization posture (G1); unauthorized writes/edits to audit entries return HTTP 403/405.

## Non-Functional Expectations

- Performance: audit write adds < 100ms to the owning mutation; timeline read for an entity returns < 1s.
- Reliability: the audit event and the mutation commit atomically (no committed change without its event).
- Security: audit entries are tamper-evident (append-only, immutable).

## Dependencies

**Depends On:**
- F0002 — provides the activity-timeline pattern this reuses.
- F0017-S0001, F0017-S0003, F0017-S0004 — the mutations that emit events.

**Related Stories:**
- F0025 — later consumes ownership-change history for attribution (out of scope here).

## Business Rules

1. **Atomicity:** a structural mutation and its audit event commit together or not at all.
2. **Immutability:** audit/timeline events are append-only; never edited or deleted.
3. **Attribution:** every event names the actor and the precise old→new change.

## Out of Scope

- Audit export / external SIEM integration (future).
- Rollup of audit events into reporting (deferred).

## UI/UX Notes (Optional)

- Screens involved: entity Timeline panels on Broker/MGA, Account, and Territory detail.
- Key interactions: read-only timeline list with actor, time, and change summary.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking — resolved at G1.

**Assumptions (to be validated):**
- The existing F0002 activity-timeline event model can carry these change types (confirmed in Phase B architecture).

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (rejected mutation, immutability, bulk, empty)
- [ ] Permissions enforced (no direct edit/delete of audit entries)
- [ ] Audit/timeline logged (core of this story)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated

## Review Provenance

Recorded in F0017 `STATUS.md`. Minimum roles: Quality Engineer, Code Reviewer.
