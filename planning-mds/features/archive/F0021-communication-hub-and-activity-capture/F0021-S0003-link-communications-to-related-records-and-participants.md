# F0021-S0003: Link communications to related records and participants

**Story ID:** F0021-S0003
**Feature:** F0021 — Communication Hub & Activity Capture
**Title:** Link communications to related records and participants
**Priority:** High
**Phase:** MVP

## User Story

**As a** relationship manager or underwriter
**I want** one communication event linked to its records and participants
**So that** context appears in approved workspaces without duplicating the communication

## Context & Background

A single communication can involve an account, broker, submission, and policy. F0021 uses a primary record for ownership plus related records for contextual visibility.

## Acceptance Criteria

**Happy Path:**
- **Given** a user is capturing a communication event from a primary record
- **When** they add related records and participants
- **Then** the communication event saves with one primary record, zero or more related records, and zero or more participants
- **And** the communication appears on approved related-record communication panels
- **And** participant labels are visible in the event detail

**Alternative Flows / Edge Cases:**
- Adding the same related record twice is prevented.
- Related record types are limited to approved CRM record types for MVP.
- A related record that the user cannot access cannot be linked.
- External participants may be captured as display name and contact value without creating a Contact record.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Add Communication drawer | Add related record or participant before Save | Editable during initial capture for users allowed to capture communication | Saves related-record links and participant rows/details with the communication event | Event detail and related-record panels show links/participants on reload | User must have access to all linked records |
| Communication detail | Add/remove related link if Phase B permits post-create edits | Pending Phase B; default is read-only on create completion except correction/redaction story | If allowed, appends audit-preserving link update | Link changes visible on reload and audit event exists | Architect to decide post-create edit scope |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state on reload/query invalidation.

## Data Requirements

**Required Fields:**
- Primary entity type/id.

**Optional Fields:**
- Related entity type/id.
- Participant type, display name, contact detail, linked contact/user id when available.

**Validation Rules:**
- Exactly one primary entity.
- No duplicate related entity links.
- Related entity link is available only with access to that entity.
- External participant contact value is optional unless Phase B sets stricter rules.

## Role-Based Visibility

**Roles that can link:**
- Internal users who can capture communication and read the linked records.

**Data Visibility:**
- Participant details follow communication visibility; they are not exposed externally in MVP.

## Non-Functional Expectations

- Security: related links cannot be used to leak communication details into unauthorized contexts.

## Dependencies

**Prerequisite Stories:**
- None — can be planned and tested as part of the communication capture contract.

**Related Stories:**
- F0021-S0001 — communication event capture.
- F0021-S0002 — contextual visibility.

## Out of Scope

- Automatic participant creation as Contact records.
- Connector-based participant matching.

## UI/UX Notes

- Screens involved: Add Communication drawer and communication detail.
- Key interactions: add/remove chips for related records and participants before save.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must confirm whether related links are editable following initial capture.

**Assumptions (to be validated):**
- Initial MVP can require related links to be set during capture only.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0021-S0003-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
