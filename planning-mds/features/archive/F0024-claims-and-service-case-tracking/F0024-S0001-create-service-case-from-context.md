# F0024-S0001: Create a service case from account or policy context

**Story ID:** F0024-S0001
**Feature:** F0024 — Claims & Service Case Tracking
**Title:** Create a service case from account or policy context
**Priority:** High
**Phase:** MVP+

## User Story

**As a** service, relationship, or underwriting user
**I want** to create a service case from an account or policy
**So that** post-bind service issues are captured where the customer relationship is managed

## Context & Background

F0016 and F0018 provide account and policy anchors. This story creates the first service-case intake path and keeps claims/service work inside Nebula without adding claims adjudication.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user with access to an account or policy
- **When** they select "Add Service Case" from the account or policy service rail
- **Then** a drawer opens with account, optional policy, summary, type, priority, owner, due date, and optional claim-reference fields
- **And** saving creates the service case in Intake status
- **And** reloading the account or policy shows the new case in the service rail
- **And** a timeline/audit event records the case creation

**Alternative Flows / Edge Cases:**
- Missing summary, type, priority, owner, or due date shows inline validation and does not save.
- A policy selected from a different account is rejected.
- Unauthorized users cannot open the create action or save through a direct request.
- Archived or deleted account/policy contexts are read-only unless Phase B explicitly permits intake.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Account 360 or Policy 360 service rail | Click Add Service Case, complete drawer, Save | Enabled for authorized internal roles; disabled/read-only for unauthorized or archived context | Creates one service case in Intake status linked to account and optional policy | Case appears in service rail and service workspace after reload/query invalidation; timeline/audit event exists | Internal roles with account/policy access; external users denied |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Account: required customer context.
- Summary: short user-facing case description.
- Type: service request, claim support, documentation support, billing inquiry, or other Phase B-approved type.
- Priority: low, medium, high, or urgent.
- Owner: internal user responsible for the case.
- Due date: next follow-up target.

**Optional Fields:**
- Policy link.
- Claim-reference fields.
- Initial communication reference.

**Validation Rules:**
- Policy must belong to the selected account.
- Due date must be present for active cases.
- Claim-reference values are optional and reference-only.

## Role-Based Visibility

**Roles that can create:**
- Underwriter, Distribution User, Distribution Manager, Admin.

**Data Visibility:**
- InternalOnly content: service summary, claim-reference details, owner/follow-up notes.
- ExternalVisible content: none in MVP+.

## Non-Functional Expectations

- Performance: create and reload should match normal CRM form expectations.
- Security: account/policy authorization is enforced before save.
- Reliability: service case and timeline/audit event are committed consistently.

## Dependencies

**Depends On:**
- F0016 — account context.
- F0018 — policy context.

**Related Stories:**
- F0024-S0002 — contextual visibility.
- F0024-S0005 — claim-reference enrichment.

## Out of Scope

- Carrier claim submission.
- Claims adjudication.
- Payments, reserves, or coverage determination.

## UI/UX Notes

- Screens involved: Account 360 service rail, Policy 360 service rail, Add Service Case drawer.
- Key interactions: open drawer, validate fields, save, reload, view created case.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Case type values can be refined by Phase B without changing the MVP+ scope.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0024-S0001-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
