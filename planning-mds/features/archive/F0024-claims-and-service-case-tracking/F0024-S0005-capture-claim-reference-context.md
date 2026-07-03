# F0024-S0005: Capture claim-reference context on a service case

**Story ID:** F0024-S0005
**Feature:** F0024 — Claims & Service Case Tracking
**Title:** Capture claim-reference context on a service case
**Priority:** Medium
**Phase:** MVP+

## User Story

**As a** service, relationship, or underwriting user
**I want** to capture reference-only claim context on a service case
**So that** customer service follow-up has useful claim context without replacing the carrier claims system

## Context & Background

Users need enough claim-adjacent context to service an account or policy. Nebula must not become the adjudication, reserve, or payment system of record.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized internal user opens a service case
- **When** they add or update carrier claim number, date of loss, claimant/contact reference, loss summary, or carrier contact reference
- **Then** the claim-reference section saves as reference-only context
- **And** reloading the case shows the updated reference values
- **And** a timeline/audit event records the claim-reference update

**Alternative Flows / Edge Cases:**
- Claim-reference fields may be blank for non-claim service cases.
- Date of loss cannot be later than the current date unless Phase B explicitly approves a future-date exception.
- Financial fields such as reserves, payments, recoveries, or coverage determination are not available.
- Unauthorized update attempts are rejected and do not modify claim-reference values.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Service Case Detail claim-reference section | Edit reference fields and Save | Enabled for authorized internal users on non-closed cases | Updates reference-only claim context and emits audit/timeline event | Reference values persist after reload; history shows update | Internal roles with service-case update permission; external users denied |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- None specific to claim-reference context.

**Optional Fields:**
- Carrier claim number.
- Date of loss.
- Claimant/contact reference.
- Loss summary.
- Carrier contact reference.

**Validation Rules:**
- Financial claim fields are not part of this story.
- Date of loss cannot be in the future unless Phase B authorizes an exception.
- Carrier claim number is not required because early servicing may precede carrier assignment.

## Role-Based Visibility

**Roles that can update:**
- Underwriter, Distribution User, Distribution Manager, Admin.

**Data Visibility:**
- InternalOnly content: all claim-reference fields.
- ExternalVisible content: none in MVP+.

## Non-Functional Expectations

- Performance: save completes within normal CRM mutation expectations.
- Security: claim-adjacent fields are treated as sensitive CRM content.
- Reliability: claim-reference update and audit/timeline event are committed consistently.

## Dependencies

**Depends On:**
- F0024-S0001 — service case exists.

**Related Stories:**
- F0024-S0006 — permission-safe audit history.

## Out of Scope

- Coverage determination.
- Reserve/payment tracking.
- Carrier claim feed synchronization.
- Claim document generation.

## UI/UX Notes

- Screens involved: Service Case Detail.
- Key interactions: edit claim-reference section, save, reload, view history.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Claim-reference fields are intentionally sparse in the first slice.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0024-S0005-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
