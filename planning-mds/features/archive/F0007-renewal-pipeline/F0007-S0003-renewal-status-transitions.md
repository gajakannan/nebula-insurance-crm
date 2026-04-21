---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0003: Renewal Status Transitions

**Story ID:** F0007-S0003
**Feature:** F0007 — Renewal Pipeline
**Title:** Renewal status transitions
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or underwriter
**I want** to advance a renewal through its workflow states (Identified → Outreach → InReview → Quoted → Completed / Lost)
**So that** renewal progress is tracked, auditable, and enforces the correct handoff between distribution and underwriting

## Context & Background

The renewal workflow has six states with defined transitions and role-based gating. Distribution users own the early stages (Identified → Outreach → InReview handoff), while underwriters own the later stages (InReview → Quoted → Completed/Lost). Terminal states require additional data: Lost requires a reason code, and Completed requires a link to the bound policy or renewal submission. Every transition is recorded as an immutable WorkflowTransition and ActivityTimelineEvent.

## Acceptance Criteria

**Happy Path — Distribution Outreach:**
- **Given** a renewal in Identified state assigned to a distribution user
- **When** the distribution user triggers the "Advance to Outreach" transition
- **Then** the renewal status changes to Outreach, a WorkflowTransition (Identified→Outreach) is appended, an ActivityTimelineEvent is created, and UpdatedAt/UpdatedByUserId are set

**Happy Path — Handoff to Underwriting:**
- **Given** a renewal in Outreach state
- **When** a distribution user triggers "Advance to InReview"
- **Then** the renewal status changes to InReview, transition records are appended, and the renewal is now visible to underwriters for review

**Happy Path — Underwriter Quotes:**
- **Given** a renewal in InReview state assigned to an underwriter
- **When** the underwriter triggers "Advance to Quoted"
- **Then** the renewal status changes to Quoted with transition records appended

**Happy Path — Completion:**
- **Given** a renewal in Quoted state
- **When** the underwriter triggers "Complete" and provides a `boundPolicyId`
- **Then** the renewal status changes to Completed, `BoundPolicyId` is set, and transition records are appended

**Happy Path — Lost:**
- **Given** a renewal in InReview or Quoted state
- **When** the underwriter triggers "Mark as Lost" and provides `reasonCode=CompetitiveLoss`
- **Then** the renewal status changes to Lost, `LostReasonCode` is set, and transition records are appended

**Alternative Flows / Edge Cases:**
- Invalid transition (e.g., Identified → Quoted) → HTTP 409 with `code=invalid_transition`
- Missing reason code on Lost transition → HTTP 409 with `code=missing_transition_prerequisite`, detail: "reasonCode is required for Lost transition"
- Missing bound policy/submission link on Completed transition → HTTP 409 with `code=missing_transition_prerequisite`, detail: "boundPolicyId or renewalSubmissionId is required"
- `reasonCode=Other` without `reasonDetail` → HTTP 409 with `code=missing_transition_prerequisite`, detail: "reasonDetail is required when reasonCode is Other"
- User lacks role permission for transition → HTTP 403
- Renewal is soft-deleted → HTTP 404
- Concurrent transition (optimistic concurrency conflict) → HTTP 409 with `code=concurrency_conflict`

**Checklist:**
- [ ] Allowed transitions enforced: Identified→Outreach, Outreach→InReview, InReview→Quoted, InReview→Lost, Quoted→Completed, Quoted→Lost
- [ ] Role gating: Distribution User/Manager for Identified→Outreach and Outreach→InReview; Underwriter for InReview→Quoted/Lost and Quoted→Completed/Lost
- [ ] Lost transition requires `reasonCode` from known set (NonRenewal, CompetitiveLoss, BusinessClosed, CoverageNoLongerNeeded, PricingDeclined, Other)
- [ ] Lost with `reasonCode=Other` requires non-empty `reasonDetail`
- [ ] Completed transition requires `boundPolicyId` or `renewalSubmissionId`
- [ ] Each transition appends one WorkflowTransition record (FromState, ToState, Reason, ActorUserId, OccurredAt)
- [ ] Each transition appends one ActivityTimelineEvent record
- [ ] Invalid transitions return HTTP 409 with RFC 7807 ProblemDetails
- [ ] Unauthorized transitions return HTTP 403
- [ ] Optimistic concurrency enforced (xmin)

## Data Requirements

**Required Fields (transition request):**
- `toState` (string): Target state
- `reason` (string, optional): Transition reason/comment

**Conditional Fields:**
- `reasonCode` (string): Required when `toState=Lost`
- `reasonDetail` (string): Required when `reasonCode=Other`
- `boundPolicyId` (uuid): Required when `toState=Completed` (unless `renewalSubmissionId` provided)
- `renewalSubmissionId` (uuid): Required when `toState=Completed` (unless `boundPolicyId` provided)

**Validation Rules:**
- Transition must be in the allowed transition set
- Actor must have the required role for the transition
- Conditional fields validated per target state
- Optimistic concurrency check on renewal record

## Role-Based Visibility

**Roles that can transition:**
- Distribution User — Identified→Outreach, Outreach→InReview
- Distribution Manager — Identified→Outreach, Outreach→InReview (for any renewal in scope)
- Underwriter — InReview→Quoted, InReview→Lost, Quoted→Completed, Quoted→Lost
- Admin — All transitions

**Data Visibility:**
- Transition history is visible to all roles that can read the renewal
- WorkflowTransition records are immutable

## Non-Functional Expectations

- Performance: Transition completes in < 500ms
- Security: Role-based transition gating enforced server-side; ABAC scoping on the renewal
- Reliability: Optimistic concurrency prevents conflicting transitions; transition + audit record creation is atomic (single DB transaction)

## Dependencies

**Depends On:**
- F0007-S0006 — Renewals must exist before they can be transitioned

**Related Stories:**
- F0007-S0002 — Transition buttons on detail view
- F0007-S0007 — Timeline records created by each transition

## Out of Scope

- Compensating transitions (undo/revert) — corrections are forward-only in MVP
- Transition approval workflows (multi-step approval before advancing)
- Automated transitions triggered by external events (carrier response, etc.)

## UI/UX Notes

- Screens involved: Renewal Detail (action bar)
- Key interactions: Click transition button → confirmation dialog (for terminal states Lost/Completed) with required fields → submit → status badge updates, timeline refreshes
- Lost dialog: Dropdown for reason code, text field for reason detail (visible when Other selected)
- Completed dialog: Picker for bound policy or renewal submission

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Transitions are triggered from the Renewal Detail action bar (not from the list view)
- Distribution Manager can perform all distribution-role transitions on any renewal in their ABAC scope

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (role-based transition gating + ABAC)
- [ ] Audit/timeline logged (WorkflowTransition + ActivityTimelineEvent per transition)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
