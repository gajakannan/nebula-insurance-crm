# F0024-S0002: View service cases in workspace and 360 context

**Story ID:** F0024-S0002
**Feature:** F0024 — Claims & Service Case Tracking
**Title:** View service cases in workspace and 360 context
**Priority:** High
**Phase:** MVP+

## User Story

**As a** service, relationship, or underwriting user
**I want** to view service cases from a workspace and from account/policy 360 views
**So that** I can find active service issues without leaving customer or policy context

## Context & Background

After intake exists, users need a read path for daily triage and contextual account/policy review. This story keeps the first visibility slice read-only.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized internal user has service cases in their visible account/policy scope
- **When** they open the Service Cases workspace
- **Then** they can see case summary, account, policy, status, priority, owner, due date, and claim-reference indicator
- **And** they can filter by status, owner, priority, due date, account, and policy
- **And** opening an account or policy 360 view shows linked service cases in a service rail
- **And** viewing and filtering cases does not write timeline or audit events because this story is read-only

**Alternative Flows / Edge Cases:**
- No cases shows an empty state with a create action only where the user can create.
- Unauthorized cases do not appear in list counts, rails, filters, or search results.
- Closed cases are hidden by default but available through an explicit closed/recently closed filter.
- Missing policy link displays account-only case context.

## Interaction Contract

N/A — read-only story.

## Data Requirements

**Required Fields:**
- Case ID, summary, account, status, priority, owner, due date.

**Optional Fields:**
- Policy, claim-reference indicator, last communication timestamp, closed date.

**Validation Rules:**
- Filters accept only known status, priority, owner, account, and policy values.
- Results are authorization-filtered before counts are returned.

## Role-Based Visibility

**Roles that can view:**
- Internal roles with account/policy visibility.

**Data Visibility:**
- InternalOnly content: full service-case details and claim-reference indicators.
- ExternalVisible content: none in MVP+.

## Non-Functional Expectations

- Performance: bounded lists and filters should follow existing CRM list-view expectations.
- Security: unauthorized rows, counts, and filter values are not surfaced.
- Reliability: empty/error states are explicit and recoverable.

## Dependencies

**Depends On:**
- F0024-S0001 — created service cases.

**Related Stories:**
- F0024-S0006 — permission-safe history.

## Out of Scope

- Operational reporting dashboards.
- Manager rollups beyond workspace filters.
- External portal views.

## UI/UX Notes

- Screens involved: Service Cases workspace, Account 360 service rail, Policy 360 service rail.
- Key interactions: filter, sort, open case detail, return to source context.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- The first workspace can use existing CRM list/filter patterns.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only story)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0024-S0002-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
