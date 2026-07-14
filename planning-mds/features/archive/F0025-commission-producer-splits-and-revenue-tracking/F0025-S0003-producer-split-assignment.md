# F0025-S0003: Producer split assignment

**Story ID:** F0025-S0003
**Feature:** F0025 — Commission, Producer Splits & Revenue Tracking
**Title:** Producer split assignment
**Priority:** Critical
**Phase:** Brokerage Platform Expansion

## User Story

**As a** producer manager
**I want** to maintain effective-dated producer split participants and percentages for a commission record
**So that** production credit reflects the approved producer attribution basis

## Context & Background

F0017 provides effective-dated producer ownership and hierarchy context. F0025 uses that context to maintain explicit split assignments for commission attribution without changing the producer ownership source model.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authorized producer manager on a commission detail record
- **When** I add producer participants, split percentages, effective dates, and reason, then save
- **Then** the split assignment is saved for the commission record
- **And** the total split percentage equals 100 percent for the active assignment
- **And** historical split assignments remain visible after later changes

**Alternative Flows / Edge Cases:**
- Split total is below or above 100 percent -> show validation feedback and do not save.
- Producer is inactive for the effective period -> show validation feedback and do not save.
- Effective period conflicts with another active split assignment -> show conflict feedback and do not save.
- User lacks split maintenance permission -> show read-only split values and no save controls.

**Checklist:**
- [ ] Split rows include producer, percentage, effective period, and reason.
- [ ] The UI shows the F0017 ownership snapshot used for comparison.
- [ ] Historical split rows remain available for audit review.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Commission Detail -> Split Assignment Panel | Add or edit split participants, then save | Enabled only for producer manager, finance operations user, or admin with split maintenance permission | Effective-dated split assignment is saved and history remains available | Reopen detail or reload page and see saved participants, percentages, effective period, and audit summary | Internal users only; producer eligibility confirmed against F0017 context |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Producer participant: selected internal producer or producer owner.
- Split percentage: numeric percentage for the participant.
- Effective start date: first date the split applies.
- Reason: explanation for the split basis.

**Optional Fields:**
- Effective end date, notes, reference to source ownership snapshot.

**Validation Rules:**
- Active split percentages total exactly 100 percent.
- Each participant appears once per active split assignment.
- Effective start date is required.
- Reason is required for create and edit actions.

## Role-Based Visibility

**Roles that can maintain splits:**
- Producer manager, finance operations user, admin — may create or edit split assignments within ABAC scope.

**Data Visibility:**
- InternalOnly content: split participants, percentages, reasons, source ownership snapshot, and audit details.
- ExternalVisible content: none in F0025.

## Non-Functional Expectations

- Security: unauthorized users cannot infer hidden split participants or percentages.
- Reliability: validation errors preserve entered participant rows.
- Auditability: create and edit actions record actor, timestamp, changed fields, and reason.

## Dependencies

**Depends On:**
- F0025-S0001 — users open a commission detail record before maintaining splits.
- F0017 — producer ownership and hierarchy context.

**Related Stories:**
- F0025-S0004 — expected commission calculation uses split assignment.
- F0025-S0006 — rollups group revenue by producer and hierarchy context.

## Business Rules

1. Full allocation: Active split assignments must total 100 percent.
2. Historical preservation: Later split changes do not alter prior effective-period attribution.

## Out of Scope

- Producer payout execution.
- Compensation plan document generation.
- External producer self-service edits.

## UI/UX Notes

- Screens involved: Commission Detail, Split Assignment Panel.
- Key interactions: add participant, edit percentage, save, cancel, view history.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will define whether producer eligibility comes from ownership, territory, or both for the first release.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0025-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
