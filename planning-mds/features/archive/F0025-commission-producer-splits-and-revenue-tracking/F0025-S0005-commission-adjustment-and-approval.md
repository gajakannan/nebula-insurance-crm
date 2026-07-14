# F0025-S0005: Commission adjustment and approval

**Story ID:** F0025-S0005
**Feature:** F0025 — Commission, Producer Splits & Revenue Tracking
**Title:** Commission adjustment and approval
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** finance operations user
**I want** to request and approve commission adjustments with a reason and audit trail
**So that** authorized corrections are visible without changing source policy facts

## Context & Background

Some expected commission records will need manual correction because source context is missing, delayed, or contract-specific. F0025 captures adjustment decisions with audit evidence while keeping payment and accounting settlement out of scope.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authorized finance operations user on a commission detail record
- **When** I enter an adjustment amount, reason, effective date, and submit for approval
- **Then** the adjustment request is recorded with pending status
- **And** an authorized approver can approve or reject the request with a decision note
- **And** approved adjustments appear in the commission detail and rollup context
- **And** request, approval, and rejection actions appear in audit history with actor and timestamp

**Alternative Flows / Edge Cases:**
- Missing adjustment amount, reason, or effective date -> show validation feedback and do not submit.
- User attempts to approve their own request when Phase B disallows same-user approval -> show authorization feedback and keep pending status.
- Rejected adjustment -> preserve decision note and do not change approved adjustment total.
- Source record is archived -> adjustment actions are read-only unless Phase B defines a reopen path.

**Checklist:**
- [ ] Adjustment request captures amount, reason, effective date, requester, and timestamp.
- [ ] Approval decision captures approver, decision, note, and timestamp.
- [ ] Adjustment history remains visible after later decisions.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Commission Detail -> Adjustment Review Panel | Submit adjustment request | Enabled for finance operations user or admin with adjustment request permission | Pending adjustment request is saved | Reopen detail or reload page and see pending request plus audit summary | Internal users only; archived records are read-only unless Phase B defines reopen |
| Commission Detail -> Adjustment Review Panel | Approve or reject request | Enabled for authorized approver with adjustment approval permission | Decision is saved and approved adjustment affects visible adjusted amount | Reopen detail or reload page and see decision, approver, note, and adjusted amount if approved | Approver constraints confirmed in Phase B |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Adjustment amount: currency amount or adjustment basis defined in Phase B.
- Reason: explanation for the adjustment.
- Effective date: date the adjustment applies.
- Decision note: explanation for approve or reject decision.

**Optional Fields:**
- Attachment/reference note, related exception state, source context note.

**Validation Rules:**
- Amount, reason, and effective date are required for request submission.
- Decision note is required for approve and reject decisions.
- Pending adjustment can receive one terminal decision.

## Role-Based Visibility

**Roles that can request or approve adjustments:**
- Finance operations user and admin — may request adjustments within ABAC scope.
- Authorized approver role defined in Phase B — may approve or reject adjustments within ABAC scope.

**Data Visibility:**
- InternalOnly content: adjustment amount, requester, approver, reason, decision note, and audit history.
- ExternalVisible content: none in F0025.

## Non-Functional Expectations

- Security: unauthorized users cannot see or infer hidden adjustment amounts or decision notes.
- Reliability: failed requests preserve entered values for correction.
- Auditability: request, approval, and rejection actions record actor, timestamp, amount, reason, and decision.

## Dependencies

**Depends On:**
- F0025-S0004 — expected commission calculation review.

**Related Stories:**
- F0025-S0006 — approved adjustments appear in revenue rollup context.

## Business Rules

1. Source preservation: Adjustments do not mutate policy premium, carrier schedule, or producer split source data.
2. Decision history: Adjustment decisions remain visible after later corrections.

## Out of Scope

- Payment authorization.
- Journal entries.
- Reconciliation matching.
- Producer statement generation.

## UI/UX Notes

- Screens involved: Commission Detail, Adjustment Review Panel.
- Key interactions: submit adjustment, approve, reject, view history.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will define whether same-user request and approval is allowed.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0025-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
