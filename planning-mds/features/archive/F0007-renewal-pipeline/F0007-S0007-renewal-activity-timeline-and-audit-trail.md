---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0007: Renewal Activity Timeline and Audit Trail

**Story ID:** F0007-S0007
**Feature:** F0007 — Renewal Pipeline
**Title:** Renewal activity timeline and audit trail
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, or distribution manager
**I want** a complete, chronological timeline of all activity on a renewal — status transitions, assignments, outreach events, and notes
**So that** I can understand the full history of a renewal and explain why it was or was not advanced on time

## Context & Background

The renewal timeline is the audit backbone of the renewal pipeline. Every system mutation — status transition, ownership change, and creation — generates an immutable ActivityTimelineEvent record. The timeline is displayed on the Renewal Detail view and serves as the answer to "what happened with this renewal?" for compliance, management review, and daily operational context. (User-authored notes are out of scope for MVP and deferred to F0021.)

## Acceptance Criteria

**Happy Path — Timeline Display:**
- **Given** a renewal with multiple events (creation, assignment, transition to Outreach, transition to InReview)
- **When** a user views the Renewal Detail timeline section
- **Then** all events are displayed in reverse chronological order (newest first) with: event type icon, event description, actor name, and timestamp

**Happy Path — Transition Events:**
- **Given** a renewal transitions from Outreach to InReview
- **When** the timeline refreshes
- **Then** a new event appears: "Status changed from Outreach to InReview by {actor}" with timestamp

**Happy Path — Assignment Events:**
- **Given** a renewal is reassigned from User A to User B by a manager
- **When** the timeline refreshes
- **Then** a new event appears: "Reassigned from {User A} to {User B} by {manager}" with timestamp

**Happy Path — Lost/Completed Outcome Events:**
- **Given** a renewal transitions to Lost with reasonCode=CompetitiveLoss
- **When** the timeline refreshes
- **Then** a new event appears: "Marked as Lost — Competitive Loss" with actor and timestamp

- **Given** a renewal transitions to Completed with boundPolicyId
- **When** the timeline refreshes
- **Then** a new event appears: "Completed — linked to policy {policyNumber}" with actor and timestamp

**Alternative Flows / Edge Cases:**
- Renewal with no events beyond creation → Timeline shows only the creation event
- Large timeline (50+ events) → Paginated or "load more" pattern; initial display shows most recent 20 events
- Actor user has been deactivated → Display stored display name (not a live lookup that fails)
- Timeline API returns empty → Display "No activity recorded"

**Checklist:**
- [ ] Timeline displays all ActivityTimelineEvent records where EntityType=Renewal and EntityId matches
- [ ] Events include: creation, status transitions, assignment changes
- [ ] Each event shows: event type icon/label, description, actor display name, timestamp
- [ ] Events are sorted reverse chronologically (newest first)
- [ ] WorkflowTransition data (fromState, toState, reason) is surfaced in transition event descriptions
- [ ] Lost events show reason code and detail
- [ ] Completed events show linked policy/submission reference
- [ ] Timeline is read-only (no edit/delete of events)
- [ ] Initial load shows most recent 20 events; "Load more" for older events
- [ ] Timeline is immutable — no events can be modified or deleted after creation

## Data Requirements

**Required Fields (timeline event):**
- `id` (uuid): Event identifier
- `entityType` (string): "Renewal"
- `entityId` (uuid): Renewal ID
- `eventType` (string): Type of event (StatusTransition, Assignment, Creation)
- `eventPayloadJson` (json): Structured event data (varies by type)
- `actorUserId` (uuid): Who performed the action
- `occurredAt` (datetime): When the event happened

**Event Payload Structures:**
- StatusTransition: `{ fromState, toState, reason, reasonCode?, reasonDetail?, boundPolicyId?, renewalSubmissionId? }`
- Assignment: `{ previousAssignee, newAssignee, assignedBy }`
- Creation: `{ policyId, policyNumber, accountName, brokerName }`

**Validation Rules:**
- Timeline events are append-only; no update or delete operations
- All renewal mutations must create a corresponding ActivityTimelineEvent (enforced in application layer)

## Role-Based Visibility

**Roles that can view the timeline:**
- All roles that have read access to the renewal can view its timeline
- Timeline data visibility follows the same ABAC rules as renewal read access

**Data Visibility:**
- InternalOnly: All timeline data is internal-only
- ExternalVisible: None

## Non-Functional Expectations

- Performance: Initial 20 events load in < 500ms; subsequent pages in < 300ms
- Security: Timeline read access tied to renewal ABAC scope
- Reliability: Timeline is append-only and immutable — no data loss from concurrent operations

## Dependencies

**Depends On:**
- F0007-S0003 — Status transitions generate timeline events
- F0007-S0004 — Assignment changes generate timeline events
- F0007-S0006 — Renewal creation generates the first timeline event
- TimelineAudit module — Existing append-only infrastructure

**Related Stories:**
- F0007-S0002 — Timeline section on renewal detail view

## Out of Scope

- Filtering/searching within the timeline
- Timeline export
- Timeline events from external systems (carrier responses, email tracking)
- Structured communication entries (deferred to F0021 — Communication Hub)
- Timeline comparison or diff views

## UI/UX Notes

- Screens involved: Renewal Detail (timeline section)
- Key interactions: Scroll through events; "Load more" button for pagination; click on policy/user references to navigate
- Layout: Vertical feed with event cards, each showing icon (by event type), description, actor, and relative timestamp ("2 hours ago") with absolute timestamp on hover
- Event type icons: arrows for transitions, person icon for assignments, plus icon for creation, flag for terminal outcomes

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Timeline uses the existing ActivityTimelineEvent and WorkflowTransition tables — no new tables needed
- Actor display name is stored in the event payload or resolved from UserProfile at query time (with fallback to "Unknown User" for deleted profiles)
- MVP does not include user-authored notes/comments on the timeline (outreach is tracked via status transitions and future F0021 communication hub)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC on timeline read via renewal access)
- [ ] Audit/timeline logged: N/A (this story IS the timeline display)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0007-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
