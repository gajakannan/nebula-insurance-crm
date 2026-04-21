---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0006: Submission Ownership Assignment and Underwriting Handoff

**Story ID:** F0006-S0006
**Feature:** F0006 — Submission Intake Workflow
**Title:** Submission ownership assignment and underwriting handoff
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or distribution manager
**I want** to assign or reassign a submission to an underwriter
**So that** the submission has clear ownership and the handoff to underwriting is explicit and traceable

## Context & Background

Assignment is a critical step in the intake workflow. The submission is initially assigned to the distribution user who created it. During triage, the distribution user or manager assigns an underwriter as the new owner. This assignment is a prerequisite for advancing to ReadyForUWReview (the completeness check in F0006-S0005 verifies that AssignedToUserId references a user with the Underwriter role). Assignment changes are recorded in the activity timeline for audit purposes.

In MVP, assignment is manual — rule-based queue routing (F0022) is deferred. Distribution managers can assign or reassign any submission in their ABAC scope. Distribution users can assign submissions they own.

## Acceptance Criteria

**Happy Path — Assign Underwriter:**
- **Given** a submission in Triaging state owned by a distribution user
- **When** the distribution user assigns the submission to an underwriter via the user picker
- **Then** `AssignedToUserId` is updated to the selected underwriter, an ActivityTimelineEvent (`SubmissionAssigned`) is appended with old and new assignee, and UpdatedAt/UpdatedByUserId are set

**Happy Path — Manager Reassignment:**
- **Given** a submission in any non-terminal intake state, assigned to Distribution User A
- **When** a distribution manager reassigns to Underwriter B
- **Then** `AssignedToUserId` is updated, a timeline event records the reassignment with old and new assignee and the manager as actor

**Happy Path — Reassign Between Underwriters:**
- **Given** a submission in ReadyForUWReview assigned to Underwriter A
- **When** a distribution manager reassigns to Underwriter B
- **Then** assignment is updated and timeline event recorded

**Alternative Flows / Edge Cases:**
- Assign to self → allowed (distribution user self-assigns during triage)
- Assign to a non-existent or soft-deleted user → HTTP 400 with `code=invalid_assignee`
- Assign to a user without Underwriter role when submission is in ReadyForUWReview → HTTP 400 with `code=invalid_assignee`, detail: "Submission in ReadyForUWReview must be assigned to a user with Underwriter role"
- Caller lacks route/action-level `submission:assign` permission → HTTP 403
- Distribution user attempts to reassign a submission they do not own, or the submission is otherwise outside resolved entity scope → HTTP 404
- Assign to the same user already assigned → no-op (no timeline event created; return success)
- Submission is soft-deleted → HTTP 404
- Concurrent assignment (stale `If-Match` / rowVersion) → HTTP 412 with `code=precondition_failed`

**Checklist:**
- [ ] `AssignedToUserId` updated to target user
- [ ] Target user must be a valid, active internal user
- [ ] When submission is in ReadyForUWReview, target must have Underwriter role
- [ ] ActivityTimelineEvent (`SubmissionAssigned`) appended with: old assignee, new assignee, actor, timestamp
- [ ] No-op when assigning to the already-assigned user
- [ ] ABAC enforced: Distribution User can assign submissions they own; Distribution Manager can assign any in scope; Admin can assign any
- [ ] Optimistic concurrency enforced via `rowVersion` + `If-Match`
- [ ] User picker shows active internal users with role indicator

## Data Requirements

**Required Fields (assignment request):**
- `assignedToUserId` (uuid): Target user to assign to

**Validation Rules:**
- Target user must exist, be active, and be an internal user
- When submission.CurrentStatus = ReadyForUWReview, target must have Underwriter role
- Requester must have ABAC permission `submission:assign` plus ownership or managerial scope

## Role-Based Visibility

**Roles that can assign/reassign:**
- Distribution User — Can assign submissions they own (AssignedToUserId = self)
- Distribution Manager — Can assign/reassign any submission in region scope
- Admin — Can assign/reassign any submission

**Data Visibility:**
- Assignment changes visible in timeline to all roles that can read the submission
- User picker shows display names of active internal users (no sensitive IdP fields)

## Non-Functional Expectations

- Performance: Assignment completes in < 500ms
- Security: ABAC-scoped; prevents unauthorized reassignment
- Reliability: Optimistic concurrency prevents conflicting assignments; assignment + timeline event is atomic

## Dependencies

**Depends On:**
- F0006-S0002 — Submissions must exist
- F0004-S0002 — User search API (reused for user picker)

**Related Stories:**
- F0006-S0004 — ReadyForUWReview transition requires underwriter assignment
- F0006-S0005 — Completeness check verifies AssignedToUserId is an underwriter
- F0006-S0007 — Timeline records assignment events

## Business Rules

1. **ReadyForUWReview Assignment Constraint:** When a submission is in ReadyForUWReview state, assignment changes must maintain an assignee with the Underwriter role. Assigning to a non-underwriter in this state returns HTTP 400 with `code=invalid_assignee`. This ensures the underwriting handoff is not broken by a stale reassignment.
2. **Ownership-Based Access for DistributionUser:** A DistributionUser can only assign submissions where they are the current `AssignedToUserId`. They cannot reassign another user's submissions. DistributionManager and Admin can assign any submission in their ABAC scope. Route/action-level deny returns HTTP 403; entity-scope or ownership failures may be cloaked as HTTP 404 to avoid exposing submission existence.
3. **No-Op Assignment:** Assigning to the user who is already assigned is a no-op — no timeline event is produced, no UpdatedAt change, and the operation returns success. This prevents timeline pollution from redundant UI interactions.
4. **Self-Assignment Allowed:** Distribution users may assign submissions to themselves during initial triage. This is the expected workflow when a distribution user claims a submission from the Received queue.
5. **Append-Only Audit on Assignment:** Every non-no-op assignment change atomically appends a `SubmissionAssigned` ActivityTimelineEvent recording old assignee, new assignee, actor, and timestamp. The event is created in the same DB transaction as the assignment update.
6. **Optimistic Concurrency:** Assignment enforces optimistic concurrency via `rowVersion` + `If-Match` to prevent conflicting concurrent assignments on the same submission. Stale versions return HTTP 412 with `code=precondition_failed`.

## Out of Scope

- Rule-based automatic assignment (F0022)
- Round-robin or load-balanced assignment
- Assignment notifications (push or email — deferred to F0021)
- Bulk reassignment of multiple submissions

## UI/UX Notes

- Screens involved: Submission Detail — Assignment section
- Key interactions: Current assignee displayed with "Reassign" button → user search picker dialog → select user → confirm → timeline refreshes with assignment event
- User picker: search-as-you-type for active internal users, shows display name and role
- When assigning from Triaging to an underwriter, the UI should hint that this is the "handoff" step

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Distribution users can only assign submissions they own; they cannot reassign other users' submissions
- The user picker reuses the same user search API from F0004-S0002
- Self-assignment (distribution user assigns to self) is allowed and common during initial triage

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC + ownership check for Distribution User)
- [ ] Audit/timeline logged (SubmissionAssigned event)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
