# F0021-S0005: Correct or redact communication content with audit

**Story ID:** F0021-S0005
**Feature:** F0021 — Communication Hub & Activity Capture
**Title:** Correct or redact communication content with audit
**Priority:** High
**Phase:** MVP

## User Story

**As a** distribution manager, compliance-aware operator, or authorized internal user
**I want** to correct mistakes or redact sensitive communication content without deleting audit history
**So that** the CRM record remains accurate, governed, and defensible

## Context & Background

Communication free text can contain mistakes or sensitive content. MVP must avoid hard deletes while still allowing governed correction and redaction.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized user views a communication detail
- **When** they submit a correction with a reason
- **Then** the corrected view becomes the current display value
- **And** audit/timeline evidence records who corrected it, when, and why

- **Given** an authorized user views a communication detail containing sensitive content
- **When** they redact free-text content with a reason
- **Then** the communication remains visible as metadata with redacted content hidden
- **And** audit/timeline evidence records the redaction

**Alternative Flows / Edge Cases:**
- Redaction requires a reason.
- Correction requires a reason.
- Unauthorized correction/redaction returns forbidden.
- Redaction does not remove primary entity, occurred-at, type, actor, or audit metadata.
- Redacted content is not visible in contextual lists or detail views.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Communication detail | Click Correct, update allowed fields, enter reason, Save | Enabled only for roles authorized by Phase B policy | Appends correction metadata and updates current display fields | Corrected values appear after reload; audit/timeline event exists | Security-sensitive; authorization required |
| Communication detail | Click Redact, enter reason, Confirm | Enabled only for roles authorized by Phase B policy | Marks free-text content redacted while preserving metadata | Body/summary hidden after reload; audit/timeline event exists | Security-sensitive; authorization required |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Correction/redaction reason.
- Actor.
- Timestamp.
- Communication event id.

**Optional Fields:**
- Corrected subject/body values, depending on Phase B policy.

**Validation Rules:**
- Reason is required.
- Redacted content cannot be returned in read models.
- Metadata remains available for audit.

## Role-Based Visibility

**Roles that can correct/redact:**
- Phase B must define exact roles; default expectation is manager/admin or equivalent privileged internal role.

**Data Visibility:**
- Redacted free-text content hidden from all normal read surfaces.
- Audit metadata remains visible to authorized internal users.

## Non-Functional Expectations

- Security: redacted content must not leak through list, detail, timeline, search, or related-record views.
- Reliability: correction/redaction audit is append-only.

## Dependencies

**Depends On:**
- F0021-S0001 — communication event capture.
- F0021-S0002 — display redacted/corrected state.

**Related Stories:**
- F0021-S0003 — related-record visibility must honor redaction.

## Out of Scope

- Hard delete of communication events.
- Legal hold workflow.
- External privacy request workflow.

## UI/UX Notes

- Screens involved: communication detail.
- Key interactions: correct action, redact action, required reason, confirmation.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must define exact roles and policy names for correction/redaction.

**Assumptions (to be validated):**
- MVP redaction hides free-text content but preserves non-sensitive metadata.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0021-S0005-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
