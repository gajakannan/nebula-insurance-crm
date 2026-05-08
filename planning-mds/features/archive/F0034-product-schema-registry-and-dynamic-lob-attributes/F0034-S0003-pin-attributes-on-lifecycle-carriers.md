# F0034-S0003 - Pin attributes on lifecycle carriers

**Story ID:** F0034-S0003
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Pin attributes on lifecycle carriers
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** underwriter
**I want** product-specific attributes pinned to the exact schema version on each lifecycle carrier
**So that** submissions, policy versions, endorsements, and renewals preserve the product contract used at write time

## Context & Background

Nebula must keep core workflow columns stable while allowing product-specific risk details to vary by LOB and product version. The first slice applies product attributes only to Submission, PolicyVersion, PolicyEndorsement, and Renewal, with Policy reading current attributes through PolicyVersion.

## Acceptance Criteria

**Happy Path:**
- **Given** a user creates or updates an attribute-bearing lifecycle record
- **When** the request includes a tenant-available Active `lobProductVersionId` and attributes valid for that product version
- **Then** Nebula stores the attributes on the lifecycle carrier
- **And** it pins the row to the requested product version
- **And** it validates that `lineOfBusiness` matches the product version's LOB
- **And** it appends audit/timeline evidence for the mutation

**Alternative Flows / Edge Cases:**
- Policy write includes product attributes -> attributes are persisted to the resulting PolicyVersion, not to an independent Policy attribute payload
- Existing legacy-pinned record receives a core-only workflow update with unchanged attributes and product version -> write may proceed when workflow and ABAC rules allow it
- Existing legacy-pinned record receives changed attributes -> write is rejected unless a governed migration path handles the version change
- Submission or Renewal moves from null LOB to Cyber -> the same write sets LOB, changes from `_unspecified/0.0.0` to Cyber `1.0.0`, and supplies valid Cyber attributes
- Existing `_legacy_cyber/0.0.0` record receives valid non-empty Cyber attributes through an approved policy or renewal lifecycle mutation -> the same write may move the carrier to active Cyber `1.0.0`
- Policy line-of-business update is attempted after create -> write is rejected

**Checklist:**
- [ ] Submission carries product attributes and product version id
- [ ] PolicyVersion carries product attributes and product version id
- [ ] PolicyEndorsement carries product attributes and product version id
- [ ] Renewal carries product attributes and product version id
- [ ] Policy has no independent first-slice product attribute payload
- [ ] Attribute mutation produces audit/timeline evidence
- [ ] LOB/product-version consistency is tested

## Data Requirements

**Required Fields:**
- `lobProductVersionId`: product version used for validation and rendering
- `attributes`: product-specific attribute payload
- `lineOfBusiness`: operational LOB classification
- `rowVersion`: concurrency token where the carrier already supports it

**Optional Fields:**
- `lobProductId`
- `lobProductVersionStatus`
- `lobSchemaVersion`

**Validation Rules:**
- `attributes` must validate against the pinned product version
- `lineOfBusiness` must match the product version LOB except the null-LOB `_unspecified/0.0.0` path
- Same-version attribute edits re-run schema, rule, domain, and authorization checks
- Product version switches require the null-LOB triage carve-out or a governed migration path
- The governed migration path for first Cyber capture must be explicit in the DB consistency trigger and service tests; otherwise legacy Cyber rows can render but never save attributes

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Distribution User - capture submission-stage attributes when authorized
- Underwriter - capture underwriting, policy, endorsement, and renewal attributes when authorized
- Schema Steward - govern product versions used by writes
- Security Reviewer - review write eligibility and attribute visibility rules

**Data Visibility:**
- InternalOnly content: product attributes and validation/audit evidence
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Reliability: historical rows render against the product version pinned on the row
- Performance: attribute validation and persistence stay within the F0034 architecture budgets
- Security: ABAC remains server-side for every mutation and read path

## Dependencies

**Depends On:**
- F0034-S0001 - accepted decision lock
- F0034-S0002 - registry foundation

**Related Stories:**
- F0034-S0006 - Cyber product bundle supplies first standard attributes
- F0034-S0007 - lifecycle integration proves the carrier path end to end

## Business Rules

1. **PolicyVersion is the policy attribute source:** Policy reads current attributes through its current PolicyVersion.
2. **Pinning preserves history:** Historical records validate and render against their pinned product version.
3. **Core columns remain core:** Account, broker, workflow state, dates, premium summaries, authorization, and audit fields do not move into product attributes.

## Out of Scope

- Attribute payloads on Account, Broker, Contact, Carrier, UserProfile, Task, or Activity
- Independent Policy attribute payload in the first slice
- Bulk product-version migration UI

## UI/UX Notes

- Screens involved: Submission Create/Triage, Submission Detail, Policy 360, Endorsement Flow, Renewal Detail
- Key interactions: product attribute panel shows the pinned product version and validation results

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Existing lifecycle workflow authorization remains the outer gate for record mutations.
- Phase B defines the exact DTO and contract names.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
