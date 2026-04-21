---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0006: Create Renewal from Expiring Policy

**Story ID:** F0007-S0006
**Feature:** F0007 — Renewal Pipeline
**Title:** Create renewal from expiring policy
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user
**I want** to create a renewal record linked to an expiring policy
**So that** I can begin tracking renewal outreach and workflow for that policy before it expires

## Context & Background

Renewals are always created in the context of an expiring policy. The renewal inherits key data from the policy (account, broker, LOB, expiration date) and is linked to exactly one policy. This is the entry point for the entire renewal pipeline — without this story, no renewals exist to be tracked. In MVP, renewal creation is manual (user-initiated from policy context). Automated creation via Temporal scheduled workflows is Future scope.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user is viewing a policy that has an expiration date in the future
- **When** they click "Create Renewal" on the policy detail or from the renewal pipeline
- **Then** a new Renewal record is created with:
  - `CurrentStatus` = Identified
  - `PolicyId` = the expiring policy
  - `AccountId` = the policy's account
  - `BrokerId` = the policy's broker
  - `LineOfBusiness` = the policy's LOB (if present)
  - `PolicyExpirationDate` = the policy's expiration date
  - `TargetOutreachDate` = computed from expiration date minus LOB outreach target days
  - `AssignedToUserId` = the creating user (self-assignment by default)
  - `CreatedByUserId` = the creating user
- And an ActivityTimelineEvent is created ("Renewal created for policy {policyNumber}")
- And a WorkflowTransition is created (null → Identified)

**Happy Path — From Pipeline List:**
- **Given** a distribution user is on the pipeline list
- **When** they click "Create Renewal" and search/select an expiring policy
- **Then** the same creation flow occurs with a policy picker step

**Alternative Flows / Edge Cases:**
- Policy already has an active renewal → HTTP 409 with `code=duplicate_renewal`, detail: "An active renewal already exists for this policy"
- Policy is expired (expiration date in the past) → Allowed — user may create a renewal for a recently expired policy to track recovery outreach
- Policy does not exist or is soft-deleted → HTTP 400 with validation error
- Policy has no LOB → Renewal created with null LOB; default timing thresholds used
- User lacks access to the policy's account/broker scope → HTTP 403
- Distribution Manager creates renewal and assigns to a team member → `AssignedToUserId` set to the specified user instead of self

**Checklist:**
- [ ] Renewal links to exactly one expiring policy via `PolicyId`
- [ ] One active (non-deleted, non-terminal) renewal per policy enforced
- [ ] Renewal inherits AccountId, BrokerId, LineOfBusiness, PolicyExpirationDate from policy
- [ ] TargetOutreachDate computed from PolicyExpirationDate minus LOB-specific outreach target days
- [ ] Default assignment to creating user; managers can assign to another user at creation
- [ ] Initial status is Identified
- [ ] ActivityTimelineEvent created for renewal creation
- [ ] WorkflowTransition record created (null → Identified)
- [ ] Region alignment validated: Account.Region must be in broker's BrokerRegion set (consistent with existing submission creation validation)
- [ ] Returns HTTP 201 with renewal resource on success

## Data Requirements

**Required Fields (creation request):**
- `policyId` (uuid): The expiring policy to create a renewal for

**Optional Fields (creation request):**
- `assignedToUserId` (uuid): Override default self-assignment (managers only)
- `lineOfBusiness` (string): Override if policy LOB is null or needs correction

**Auto-populated Fields:**
- `accountId`: From policy
- `brokerId`: From policy
- `policyExpirationDate`: From policy
- `targetOutreachDate`: Computed from expiration date and LOB thresholds
- `currentStatus`: Identified
- `createdByUserId`: Authenticated user
- Standard audit fields (CreatedAt, UpdatedAt)

**Validation Rules:**
- `policyId` must reference a valid, non-deleted policy
- No other active renewal exists for this policy
- Region alignment between account and broker
- If `assignedToUserId` is provided, target user must exist and have a distribution role

## Role-Based Visibility

**Roles that can create renewals:**
- Distribution User — Can create and self-assign
- Distribution Manager — Can create and assign to any team member
- Admin — Can create and assign to any user

**Data Visibility:**
- Renewal creation visible in timeline to all roles with renewal read access

## Non-Functional Expectations

- Performance: Creation completes in < 500ms
- Security: ABAC enforcement on policy access and account/broker scope
- Reliability: Duplicate renewal check is enforced at the database level (unique constraint on PolicyId for non-deleted, non-terminal renewals)

## Dependencies

**Depends On:**
- F0018 — Policy entity must exist with expiration dates for renewal creation
- ADR-009 — WorkflowSlaThreshold for TargetOutreachDate computation

**Related Stories:**
- F0007-S0001 — Newly identified renewals appear in the pipeline list
- F0007-S0003 — Identified renewal can be transitioned
- F0007-S0007 — Creation event appears in timeline

## Out of Scope

- Automated renewal creation from batch policy expiration scan (Future — Temporal workflow)
- Bulk renewal creation for multiple expiring policies
- Renewal creation from submission (renewals are created from policies, not submissions)
- Editing inherited fields (account, broker) after creation — these are derived from the linked policy

## UI/UX Notes

- Screens involved: Policy Detail (F0018), Renewal Pipeline List
- Key interactions:
  - From Policy Detail: "Create Renewal" button visible when policy has a future expiration and no active renewal. Click → confirmation → renewal created, navigate to renewal detail.
  - From Pipeline List: "Create Renewal" button → policy search/picker → select policy → confirmation → renewal created.
- Confirmation dialog shows: policy number, account name, broker name, expiration date, computed outreach target date

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Renewal creation from an already-expired policy is allowed (for recovery/late-discovery scenarios)
- The "one active renewal per policy" constraint excludes terminal renewals (Completed/Lost) and soft-deleted renewals — a new renewal can be created after a Lost renewal for the same policy
- TargetOutreachDate is computed at creation time and stored (not dynamically recomputed)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC on policy access, role check for creation)
- [ ] Audit/timeline logged (ActivityTimelineEvent + WorkflowTransition for creation)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
