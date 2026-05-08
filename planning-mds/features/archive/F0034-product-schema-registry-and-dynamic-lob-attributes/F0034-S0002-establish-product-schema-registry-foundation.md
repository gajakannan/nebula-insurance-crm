# F0034-S0002 - Establish product schema registry foundation

**Story ID:** F0034-S0002
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Establish product schema registry foundation
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** schema steward
**I want** a governed product schema registry with product versions, lifecycle states, sentinels, and activation evidence
**So that** Nebula can serve the exact bundle version required for product-attribute validation and rendering

## Context & Background

Product-specific attributes need a registry that separates standard active product versions from internal compatibility sentinels. The first foundation slice establishes registry concepts and write eligibility before Cyber bundle activation.

## Acceptance Criteria

**Happy Path:**
- **Given** a steward activates a standard product bundle
- **When** the bundle passes profile linting, examples, and parity checks
- **Then** the registry records product code, product kind, LOB, version, stage, status, bundle hash, ETag, and activation reason
- **And** the product version becomes available only when its status is Active and tenant availability includes the requester
- **And** the activation event records actor, from-state, to-state, product version, timestamp, and reason

**Alternative Flows / Edge Cases:**
- Bundle profile linting fails -> activation is rejected with a LOB validation error payload
- Bundle references an unknown widget or widget option -> activation is rejected until the widget registry supports it
- A client requests active bundles for a tenant without that product -> response omits the product version
- A client attempts to create against an internal sentinel -> request is rejected except for the null-LOB `_unspecified/0.0.0` carve-out

**Checklist:**
- [ ] Standard product versions have lifecycle status values Draft, Active, Deprecated, and Retired
- [ ] Internal sentinels use status Internal and product kind Unspecified, Legacy, or Bridge
- [ ] `_unspecified/0.0.0` exists for null-LOB Submission/Renewal rows with empty attributes
- [ ] Per-LOB legacy sentinels exist for pre-registry non-null LOB rows
- [ ] Active listing excludes internal sentinels
- [ ] Activation emits audit evidence

## Data Requirements

**Required Fields:**
- Product code
- Product kind
- Line of business
- Product version
- Stage
- Status
- Bundle hash
- ETag
- Tenant availability
- Activation reason

**Optional Fields:**
- Signature key id
- Deprecation warning text
- Retirement upgrade path

**Validation Rules:**
- Standard creates may use only tenant-available Active product versions
- Internal sentinels are never returned in active bundle bootstrap responses
- Deprecated versions allow same-version edits on existing rows only
- Retired versions reject writes with a conflict response and upgrade path

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Schema Steward - activate, deprecate, retire, and review bundles
- Architect - approve registry semantics
- Security Reviewer - review tenant bundle visibility and write eligibility
- DevOps - verify activation and cache behavior

**Data Visibility:**
- InternalOnly content: activation history, bundle hash, steward review notes
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Reliability: active bundle lookup returns a stable ETag for unchanged content
- Performance: authenticated bootstrap can load active bundles for each lifecycle stage within the architecture budget
- Security: tenant filtering prevents unavailable product versions from appearing in bootstrap responses

## Dependencies

**Depends On:**
- F0034-S0001 - accepted decision lock

**Related Stories:**
- F0034-S0003 - lifecycle carrier pinning consumes registry product versions
- F0034-S0005 - dynamic forms consume active and lazy-fetched bundles

## Business Rules

1. **Version pinning is immutable per product version:** A product version represents one governed bundle snapshot.
2. **Internal is not selectable:** Internal sentinels support compatibility and migration paths; users cannot choose them for ordinary creates.
3. **Activation requires evidence:** No bundle becomes Active without lint, fixture, and review evidence.

## Out of Scope

- No-code product administration console
- Full remaining-LOB rollout
- Heavy custom widgets for non-Cyber LOBs

## UI/UX Notes

- Screens involved: no dedicated registry admin screen in MVP
- Key interactions: active bundle availability appears through lifecycle dynamic attribute panels

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Phase B assigns the exact registry storage model and API route shape.
- Bundle activation is operated by internal roles only in the first slice.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
