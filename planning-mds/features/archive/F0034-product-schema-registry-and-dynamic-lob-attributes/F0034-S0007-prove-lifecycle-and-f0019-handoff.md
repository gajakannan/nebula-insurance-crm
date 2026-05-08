# F0034-S0007 - Prove lifecycle integration and F0019 handoff

**Story ID:** F0034-S0007
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Prove lifecycle integration and F0019 handoff
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** underwriting manager
**I want** Cyber product attributes to work across the submission, policy, endorsement, and renewal handoff path
**So that** F0019 and later workflow features can consume product data without adding hardcoded Cyber fields

## Context & Background

F0034 is valuable only if it changes how downstream features capture product-specific data. The final pilot story proves the lifecycle path and records an explicit guardrail for F0019 quote/proposal work.

## Acceptance Criteria

**Happy Path:**
- **Given** Cyber `1.0.0` is active and the dynamic panel is available
- **When** a distribution user creates or triages a Cyber submission with valid attributes
- **Then** the submission stores the attributes, pins Cyber `1.0.0`, and appends audit/timeline evidence
- **And** an underwriter can review the same pinned attributes on submission detail
- **And** policy, endorsement, and renewal surfaces can display or capture product attributes through their carrier paths
- **And** F0019 quote/proposal planning references F0034 for product-specific Cyber data

**Alternative Flows / Edge Cases:**
- User tries to advance a null-LOB submission to Cyber without required Cyber attributes -> write is rejected with normalized field errors
- User opens a pre-registry Cyber row -> product attributes render read-only through the legacy path
- User attempts to edit legacy-pinned attributes during endorsement without migration -> write is rejected
- F0019 adds a fixed Cyber field in quote/proposal scope -> review blocks the F0019 change and points back to F0034
- Dynamic panel save succeeds but audit/timeline event is missing -> story fails

**Checklist:**
- [ ] Create or triage Cyber submission path passes end to end
- [ ] Submission detail displays pinned Cyber attributes
- [ ] Policy current-version view displays product attributes through PolicyVersion
- [ ] Endorsement path can validate changed product attributes or reject legacy edits
- [ ] Renewal detail can display or capture renewal-stage product attributes
- [ ] Policy Detail entered from the policy list allows active Cyber attributes to be edited and saved through the policy update or endorsement path, depending on policy status
- [ ] Renewal Detail entered from the renewal list allows active Cyber attributes to be edited and saved through the renewal LOB-attribute update path
- [ ] Legacy Cyber rows have an explicit first-capture path to active Cyber `1.0.0`; they are not permanently read-only when a governed lifecycle write supplies valid Cyber attributes
- [ ] F0019 handoff note links to F0034
- [ ] E2E coverage includes legacy read, validation error, and pin-on-open behavior

## Data Requirements

**Required Fields:**
- Submission id
- Policy id where applicable
- PolicyVersion id where applicable
- Renewal id where applicable
- `lobProductVersionId`
- `attributes`
- Audit/timeline event id

**Optional Fields:**
- Deprecated-version warning
- Legacy read reason
- F0019 handoff link

**Validation Rules:**
- Cyber submission writes require valid Cyber attributes before moving from null LOB to Cyber
- Product attribute writes must use the lifecycle carrier's approved mutation path
- Policy reads product attributes from PolicyVersion
- Renewal detail attribute writes require row-version concurrency and append timeline evidence
- Issued policy attribute edits must use endorsement semantics; pending policy attribute edits may update the current PolicyVersion through the policy update path
- F0019 product-specific quote/proposal attributes must route through F0034 product attributes

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Distribution User - creates and triages submissions
- Underwriter - reviews and updates authorized product attributes
- Product Operations Lead - verifies F0019 handoff
- Quality Engineer - validates E2E coverage
- Code Reviewer - reviews hardcoded-field guardrail
- Security Reviewer - reviews authorization and data visibility

**Data Visibility:**
- InternalOnly content: Cyber attributes, validation errors, audit/timeline evidence
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Reliability: lifecycle surfaces render historical rows against pinned product versions
- Performance: E2E tests meet the frontend performance target for the dynamic panel
- Security: unauthorized users receive 401 or 403 without product attribute payload leakage

## Dependencies

**Depends On:**
- F0034-S0002 - registry foundation
- F0034-S0003 - lifecycle carrier pinning
- F0034-S0004 - validation parity
- F0034-S0005 - dynamic panel
- F0034-S0006 - Cyber bundle

**Related Stories:**
- F0019 downstream submission workflow stories consume F0034 for product-specific quote/proposal attributes

## Business Rules

1. **F0019 consumes F0034:** Product-specific Cyber quote/proposal data belongs in schema-pinned attributes, not fixed Cyber fields.
2. **Lifecycle path proves value:** F0034 acceptance requires at least one Cyber flow through submission and one downstream carrier surface.
3. **Audit is mandatory:** Every successful product attribute mutation appends audit/timeline evidence.

## Out of Scope

- Completing all F0019 quoting, proposal, approval, and bind functionality
- Rolling Cyber attributes to broker-facing external capture
- Migrating every historical Cyber row to active Cyber `1.0.0`

## UI/UX Notes

- Screens involved: Submission Create/Triage, Submission Detail, Policy 360, Endorsement Flow, Renewal Detail
- Key interactions: create/triage with attributes, validate, save, review, read legacy, inspect F0019 handoff

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- F0019 remains planned and does not start implementing product-specific fixed fields before F0034 approval.
- Existing workflow state machines remain the authority for allowed lifecycle transitions.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
