# F0034-S0001 - Lock product-attribute decision set

**Story ID:** F0034-S0001
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Lock product-attribute decision set
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** product operations lead
**I want** the product-attribute decision set accepted before runtime implementation starts
**So that** teams build the registry, validation, form, and compatibility rails from the same governed contract

## Context & Background

The LOB extensibility plan contains binding choices for core versus extension attributes, schema lifecycle, validator parity, dynamic form rendering, rule governance, sentinels, and Cyber as the first pilot. This story turns those choices into approved architecture handoff artifacts before backend and frontend implementation stories proceed.

## Acceptance Criteria

**Happy Path:**
- **Given** F0034 planning has Phase A approval
- **When** the architecture decision-lock package is prepared
- **Then** it identifies the accepted decisions for product-attribute architecture, form engine, validator equivalence, and rules governance
- **And** it confirms ADR-018 remains accepted with immutable Policy line-of-business behavior
- **And** it identifies the OpenAPI projection matrix and validation performance baseline artifacts required before implementation
- **And** it records Cyber as the first product pilot
- **And** it blocks runtime implementation stories until the decision-lock package is accepted

**Alternative Flows / Edge Cases:**
- An ADR decision conflicts with the LOB extensibility plan -> halt Phase B and reconcile the raw artifacts before implementation work starts
- ADR-018 no longer preserves immutable Policy line-of-business behavior -> reject the decision-lock package
- OpenAPI projection produces semantic loss for an allowed bundle keyword -> require an ADR decision to forbid that keyword or migrate the contract path

**Checklist:**
- [ ] Product-attribute architecture decision documented
- [ ] Dynamic form engine decision documented
- [ ] Validator equivalence decision documented
- [ ] Rules governance decision documented
- [ ] OpenAPI projection matrix named as a required artifact
- [ ] Validation performance baseline named as a required artifact
- [ ] Cyber pilot decision recorded

## Data Requirements

**Required Fields:**
- Decision topic: unique name for each decision area
- Decision state: proposed, accepted, rejected
- Approver: human or role that accepted the decision
- Evidence path: raw artifact that contains the accepted decision

**Optional Fields:**
- Superseded decision reference
- Follow-up implementation owner

**Validation Rules:**
- Every accepted decision must link to an artifact under `planning-mds/`
- No accepted decision may rely only on notes in a feature story
- Decision evidence must not point to `agents/` as the product-specific source of truth

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Product Manager
- Architect
- Schema Steward
- Quality Engineer
- Security Reviewer
- Authorization review confirms only approved planning and architecture roles can accept decision-lock evidence

**Data Visibility:**
- InternalOnly content: planning decisions, architecture evidence, and implementation gate notes
- ExternalVisible content: none

## Non-Functional Expectations

- Traceability: every implementation story can point to the decision artifact it depends on
- Reliability: conflicting decisions block the feature before code or contract work starts
- Security: governance decisions include schema activation and tenant bundle visibility constraints

## Dependencies

**Depends On:**
- ADR-018 - Policy aggregate versioning and reinstatement window
- `planning-mds/architecture/lob-extensible-attribute-plan.md` - source decision plan

**Related Stories:**
- F0034-S0002 - registry foundation depends on accepted lifecycle and sentinel decisions
- F0034-S0004 - parity harness depends on accepted validator equivalence decisions

## Business Rules

1. **Decision lock gates implementation:** Runtime stories cannot start until the decision-lock package is accepted.
2. **Raw artifacts win:** If a story and decision artifact disagree, halt and repair the feature plan or decision artifact before continuing.
3. **Cyber pilot is binding for this feature:** Any pilot change requires Phase A re-approval.

## Out of Scope

- Implementing registry tables or schema activation runtime behavior
- Creating frontend form components
- Publishing Cyber schema bundle files

## UI/UX Notes

- Screens involved: none
- Key interactions: decision review occurs through planning artifacts and approval gates

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- ADR numbering is assigned during Phase B.
- Product-specific decision evidence remains under `planning-mds/`.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - planning decision story)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
