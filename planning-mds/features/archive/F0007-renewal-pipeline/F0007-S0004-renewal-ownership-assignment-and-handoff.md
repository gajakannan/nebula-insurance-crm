---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0004: Renewal Ownership Assignment and Handoff

**Story ID:** F0007-S0004
**Feature:** F0007 — Renewal Pipeline
**Title:** Renewal ownership assignment and handoff
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution manager
**I want** to assign and reassign renewal ownership, and explicitly hand off renewals from distribution to underwriting
**So that** every renewal has a clear owner and the distribution-to-underwriting handoff is traceable

## Context & Background

Renewal ownership determines who is responsible for advancing the renewal. Distribution users own early stages; underwriters own later stages. Managers need the ability to assign renewals to team members, reassign when someone is overloaded or unavailable, and ensure the handoff to underwriting is explicit and auditable. The existing user search API from F0004-S0002 provides the assignee picker capability.

## Acceptance Criteria

**Happy Path — Initial Assignment:**
- **Given** a new renewal is created (F0007-S0006) without an explicit assignee
- **When** a distribution manager opens the renewal detail and selects an assignee via the user picker
- **Then** `AssignedToUserId` is set, an ActivityTimelineEvent is created ("Assigned to {user}"), and the renewal appears in the assignee's "My Renewals" list

**Happy Path — Reassignment:**
- **Given** a renewal in Outreach state assigned to distribution user A
- **When** a distribution manager reassigns the renewal to distribution user B
- **Then** `AssignedToUserId` is updated, an ActivityTimelineEvent records the reassignment (from A to B with actor = manager), and UpdatedAt/UpdatedByUserId are set

**Happy Path — Underwriting Handoff:**
- **Given** a renewal transitions from Outreach to InReview (F0007-S0003)
- **When** the renewal enters InReview
- **Then** the manager (or the distribution user performing the transition) can reassign ownership to an underwriter via the assignee picker. The handoff is recorded as an ActivityTimelineEvent.

**Alternative Flows / Edge Cases:**
- Self-assignment: Distribution user assigns a renewal to themselves → Allowed; timeline records self-assignment
- Reassign to same user → No-op; no timeline event created
- Assign to user without renewal access → HTTP 400 with validation error ("Target user does not have the required role for this renewal stage")
- Distribution user (non-manager) attempts to reassign someone else's renewal → HTTP 403
- Renewal in terminal state (Completed/Lost) → Assignment not allowed; HTTP 409

**Checklist:**
- [ ] Assignee picker uses existing user search API (F0004-S0002) filtered by relevant roles
- [ ] Distribution Manager and Admin can assign/reassign any renewal in their ABAC scope
- [ ] Distribution User can self-assign unassigned renewals
- [ ] Distribution User cannot reassign another user's renewal (only manager/admin can)
- [ ] Assignment to a terminal-state renewal is rejected
- [ ] Every assignment/reassignment creates an ActivityTimelineEvent
- [ ] Timeline event records: previous assignee, new assignee, actor, timestamp
- [ ] Handoff from distribution to underwriting is recorded when ownership changes to an underwriter

## Data Requirements

**Required Fields:**
- `assignedToUserId` (uuid): Target user for assignment

**Validation Rules:**
- Target user must exist and have an active UserProfile
- Target user must have a role compatible with the renewal's current stage (distribution roles for Identified/Outreach; underwriter for InReview/Quoted)
- Renewal must not be in a terminal state

## Role-Based Visibility

**Roles that can assign/reassign:**
- Distribution Manager — Assign/reassign any renewal in their ABAC scope (any stage)
- Distribution User — Self-assign unassigned renewals only
- Admin — Assign/reassign any renewal

**Data Visibility:**
- Assignment history visible in timeline to all roles that can read the renewal

## Non-Functional Expectations

- Performance: Assignment completes in < 500ms
- Security: ABAC enforcement; only authorized managers/admins can reassign
- Reliability: Optimistic concurrency on assignment update

## Dependencies

**Depends On:**
- F0004-S0002 — User search API for assignee picker
- F0007-S0006 — Renewals must exist

**Related Stories:**
- F0007-S0003 — Outreach→InReview transition often paired with handoff
- F0007-S0007 — Assignment events appear in timeline

## Out of Scope

- Queue-based automatic assignment (deferred to F0022)
- Workload balancing across assignees
- Out-of-office / backup coverage routing
- Bulk reassignment

## UI/UX Notes

- Screens involved: Renewal Detail
- Key interactions: Click "Assign" or "Reassign" → user search picker opens → select user → confirm → timeline updates
- Assignee picker: same component pattern as F0004 task assignee picker, filtered to show only users with compatible roles for the renewal's current stage

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Handoff to underwriting is an explicit reassignment action, not automatic upon InReview transition
- The assignee picker filters users by role compatibility with the renewal's current stage

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (manager/admin for reassignment; self-assign for distribution user)
- [ ] Audit/timeline logged (ActivityTimelineEvent for every assignment change)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
