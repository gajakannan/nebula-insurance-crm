---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0002: Renewal Detail View with Policy Context and Outreach History

**Story ID:** F0007-S0002
**Feature:** F0007 — Renewal Pipeline
**Title:** Renewal detail view with policy context and outreach history
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or underwriter
**I want** to see a renewal's full context — linked policy details, account, broker, outreach history, and current status
**So that** I can make informed decisions about how to advance the renewal and what actions are needed next

## Context & Background

The renewal detail view is where users spend time understanding and acting on a specific renewal. It must surface the linked policy (expiration date, carrier, LOB, premium), the account and broker context, the history of outreach and transitions, and the current workflow state. This view is the launch point for status transitions and ownership changes.

## Acceptance Criteria

**Happy Path:**
- **Given** a user navigates to a renewal detail view
- **When** the page loads
- **Then** they see:
  - Renewal header: status badge, assigned user, LOB, overdue/approaching indicator
  - Policy section: policy number, carrier, LOB, effective date, expiration date, premium
  - Account section: account name, industry, primary state (links to Account 360)
  - Broker section: broker name, license number, state (links to Broker 360)
  - Timeline section: chronological list of all renewal-related events (transitions, activities)
  - Action bar: available status transition buttons based on current state and user role

- **Given** the renewal is in Outreach state and the user is a distribution user
- **When** they view the detail page
- **Then** the action bar shows "Advance to InReview" button and the timeline shows prior outreach events

- **Given** the renewal is in Quoted state and the user is an underwriter
- **When** they view the detail page
- **Then** the action bar shows "Complete" and "Mark as Lost" buttons

**Alternative Flows / Edge Cases:**
- Policy data temporarily unavailable → Policy section shows "Policy data unavailable" with retry prompt; renewal data still displays
- Renewal in terminal state (Completed/Lost) → Action bar shows no transition buttons; timeline is read-only
- Completed renewal → Shows bound policy link or renewal submission link in outcome section
- Lost renewal → Shows reason code and reason detail in outcome section
- User lacks transition permission → Transition buttons are hidden (not disabled)

**Checklist:**
- [ ] Renewal header shows: status, assigned user, LOB, overdue/approaching flag, renewal ID
- [ ] Policy section shows: policy number, carrier, LOB, effective date, expiration date, premium
- [ ] Account section shows: account name with link to Account 360
- [ ] Broker section shows: broker name with link to Broker 360
- [ ] Timeline section shows all ActivityTimelineEvent records for this renewal, newest first
- [ ] Action bar shows only transitions valid for current state AND current user's role
- [ ] Completed renewals show outcome: bound policy ID link or renewal submission link
- [ ] Lost renewals show outcome: reason code and detail text
- [ ] Back navigation returns to pipeline list with preserved filter state

## Data Requirements

**Required Fields:**
- Renewal: Id, CurrentStatus, AssignedToUserId, LineOfBusiness, PolicyExpirationDate, TargetOutreachDate
- Policy: PolicyNumber, Carrier, LineOfBusiness, EffectiveDate, ExpirationDate, Premium
- Account: Name, Industry, PrimaryState
- Broker: LegalName, LicenseNumber, State
- Timeline: EventType, EventPayloadJson, ActorUserId, OccurredAt

**Optional Fields:**
- LostReasonCode, LostReasonDetail: Shown for Lost renewals
- BoundPolicyId, RenewalSubmissionId: Shown for Completed renewals

**Validation Rules:**
- Detail view requires read access to the renewal per ABAC policy
- Policy, account, and broker data are read from their respective modules

## Role-Based Visibility

**Roles that can view renewal detail:**
- Distribution User — Own assigned renewals and team renewals within ABAC scope
- Distribution Manager — All renewals within ABAC scope
- Underwriter — Renewals assigned to them in InReview/Quoted, plus read access to prior stages
- Relationship Manager — Renewals for their accounts/brokers (read-only)
- Program Manager — Renewals within their programs (read-only)
- Admin — All renewals

**Data Visibility:**
- InternalOnly content: All renewal detail data
- ExternalVisible content: None

## Non-Functional Expectations

- Performance: Detail page loads in < 1.5s including policy and timeline data
- Security: ABAC enforcement on renewal read; cross-module data (policy, account, broker) respects each module's access rules
- Reliability: Graceful degradation if cross-module data is unavailable (show renewal data with "unavailable" placeholders)

## Dependencies

**Depends On:**
- F0007-S0001 — Pipeline list provides navigation to this view
- F0007-S0007 — Timeline data powers the timeline section
- F0018 — Policy data displayed in the policy section

**Related Stories:**
- F0007-S0003 — Status transitions triggered from this view's action bar
- F0007-S0004 — Ownership assignment triggered from this view

## Out of Scope

- Inline editing of policy data (owned by F0018)
- Document attachments on the renewal (deferred to F0020 integration)
- Communication/notes entry beyond timeline events (deferred to F0021)
- Renewal-specific task creation UI (uses existing Task create from F0004)

## UI/UX Notes

- Screens involved: Renewal Detail
- Key interactions: View policy/account/broker context, scroll timeline, click transition buttons, assign/reassign owner
- Layout: Header card (status, owner, LOB) → Policy/Account/Broker sections (card or accordion) → Timeline (scrollable feed) → Sticky action bar at bottom

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Policy section pulls data from F0018 API at render time (not cached on the renewal record)
- Timeline events include both workflow transitions and general activity events (outreach logged, notes added)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC scoping on renewal read)
- [ ] Audit/timeline logged: No (read-only view; transitions logged by F0007-S0003)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
