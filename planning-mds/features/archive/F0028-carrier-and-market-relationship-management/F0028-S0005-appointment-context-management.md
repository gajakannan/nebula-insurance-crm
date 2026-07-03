# F0028-S0005: Appointment context management

**Story ID:** F0028-S0005
**Feature:** F0028 — Carrier & Market Relationship Management
**Title:** Appointment context management
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** distribution leader
**I want** to manage appointment context for carriers and markets
**So that** teams understand where the organization has market access and who owns it

## Context & Background

Appointments and market-access context affect placement strategy. F0028 records status, scope, owner, and effective dates; it does not automate carrier appointment workflows.

## Acceptance Criteria

**Happy Path:**
- **Given** an active market profile and an authorized user
- **When** the user creates or updates appointment context
- **Then** the workspace shows appointment status, jurisdiction/geography, LOB scope, owner, effective dates, and notes
- **And** appointment summary appears in the market directory
- **And** the mutation appends an audit/timeline event

**Alternative Flows / Edge Cases:**
- Missing status, scope, owner, or effective date -> rejected with field-level validation.
- End date before effective date -> rejected.
- Unauthorized actor attempts mutation -> `403 Forbidden`.
- Expired appointment -> visible as expired, not deleted.
- Profile archived -> appointment context becomes read-only.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Market Workspace → Appointments → Add appointment | Enter appointment fields and Save | Enabled for active/monitoring profiles | Appointment record created | Reload shows appointment row and directory summary | Distribution leader/manager, admin |
| Market Workspace → Appointments → Edit appointment | Update status/scope/dates and Save | Enabled for current appointment records | Appointment record updated | Reload shows updated appointment and timeline event | Distribution leader/manager, admin; archived profiles read-only |

Required checks:
- [ ] Render-only behavior cannot satisfy this story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutations append audit/timeline events.
- [ ] Tests prove create/update/expire and persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Appointment status: appointed, pending, suspended, terminated, or unknown.
- Geography/jurisdiction scope.
- Line of business or market segment scope.
- Owner.
- Effective date.

**Optional Fields:**
- End date, appointment number/reference, notes, related contact.

**Validation Rules:**
- End date, when supplied, must be on or after effective date.
- Appointment status must use controlled values.
- Unknown status requires notes explaining why status is unknown.

## Role-Based Visibility

**Roles that can manage appointments:**
- Distribution leader, distribution manager, admin.

**Data Visibility:**
- InternalOnly content: appointment status, scope, owner, reference identifiers, and notes.
- ExternalVisible content: none in F0028.

## Non-Functional Expectations

- Security: appointment context is internal market-access intelligence and requires explicit manage permission.
- Reliability: expired and terminated records remain historically visible.

## Dependencies

**Depends On:**
- F0028-S0002 — carrier/market profile exists.

**Related Stories:**
- F0028-S0001 — appointment summary appears in directory filters and rows.
- F0028-S0006 — appointment changes appear in activity history.

## Business Rules

1. **Appointment context is recorded:** F0028 stores market-access facts; it does not file appointment requests with carriers.
2. **Historical visibility:** expired or terminated appointment records remain visible for context.

## Out of Scope

- Carrier appointment submission workflow.
- Licensing/compliance filing automation.
- Producer compensation or commission setup.

## UI/UX Notes

- Screens involved: Market Relationship Workspace appointments tab/panel.
- Key interactions: add appointment, edit appointment, view expired/terminated context.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Distribution leader/manager and admin are the only appointment mutation roles in MVP+.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0028-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
