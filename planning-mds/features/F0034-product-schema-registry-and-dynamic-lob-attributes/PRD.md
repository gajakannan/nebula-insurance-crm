---
template: feature
version: 1.1
applies_to: product-manager
---

# F0034: Product Schema Registry and Dynamic LOB Attributes

**Feature ID:** F0034
**Feature Name:** Product Schema Registry and Dynamic LOB Attributes
**Priority:** Critical
**Phase:** Platform Foundation / CRM Release MVP Enabler

## Feature Statement

**As a** product and underwriting operations team
**I want** product-specific attributes to be defined by governed JSON Schemas and rendered through dynamic forms
**So that** Nebula can support different insurance products and lines of business without repeated frontend, backend, and database rewrites

## Business Objective

- **Goal:** Establish the product-attribute foundation before quoting, proposal, coverage, and reporting features add more product-specific data.
- **Metric:** New product attributes can be added through schema changes without adding ordinary relational columns or custom form code when existing widgets and validation rules are sufficient.
- **Baseline:** Current submission, policy, renewal, and policy-version models are mostly fixed-column. Product-specific underwriting data would require coordinated DTO, validation, UI, and persistence changes.
- **Target:** A first product pilot proves schema-pinned attributes, dynamic rendering, and frontend/backend validation parity.

## Problem Statement

- **Current State:** Nebula has static LOB classification and a document metadata schema renderer, but it does not yet have a product schema registry for lifecycle product attributes.
- **Desired State:** Product-specific attributes are stored as governed JSON, pinned to a product schema version, validated consistently across boundaries, and rendered through reusable dynamic form components.
- **Impact:** Reduces feature-by-feature schema drift and prevents hardcoded product attributes from accumulating as technical debt in F0019 and later coverage/reporting work.

## Scope & Boundaries

**In Scope for Product Manager Planning:**
- Define the minimal product schema registry foundation.
- Define the dynamic form experience and widget vocabulary needed for the first pilot.
- Define how frontend validation with AJV and backend validation use the same JSON Schema contract and shared fixtures.
- Define persistence for product attributes on the correct lifecycle carriers without replacing core operational columns.
- Define a first product/LOB pilot, expected to be Cyber unless planning identifies a better candidate.
- Define compatibility behavior for existing records that have only static `lineOfBusiness`.
- Define acceptance criteria for preventing F0019 from adding hardcoded product-attribute debt.

**Out of Scope for the First Implementation Slice:**
- Full rollout for every LOB.
- Full no-code product administration UI.
- Replacing core operational columns such as account, broker, status, dates, premium summaries, or audit fields.
- Replacing the F0020 document metadata schema registry.
- Building every advanced widget needed by future LOBs.

## Success Criteria

- Product-specific attributes are modeled through JSON Schema rather than new fixed columns for the pilot.
- Dynamic forms render the pilot product attributes from schema metadata.
- Frontend and backend validation outcomes are proven equivalent for the pilot fixtures.
- Product attribute writes pin the schema/product version used for validation and rendering.
- Existing submission/policy/renewal workflows continue to operate for records without pilot attributes.
- F0019 can consume this foundation for quote/proposal product details instead of introducing hardcoded product fields.

## Risks & Assumptions

- **Risk:** Overbuilding the registry delays CRM MVP delivery.
- **Mitigation:** Ship a foundation plus one pilot product before rolling out all LOBs.
- **Risk:** Under-scoping creates another partial schema renderer that cannot support quoting and coverage workflows.
- **Mitigation:** Product Manager planning must include submission, policy-version, endorsement, renewal, and F0019 integration implications even if implementation starts with one vertical slice.
- **Assumption:** AJV remains the frontend validation engine for JSON Schema validation.
- **Assumption:** Backend validation should use a real JSON Schema validator rather than ad hoc schema walking.

## Dependencies

- F0006 Submission Intake Workflow
- F0007 Renewal Pipeline
- F0018 Policy Lifecycle and Policy 360
- F0019 Submission Quoting, Proposal and Approval Workflow
- F0020 Document Management and ACORD Intake
- ADR-001 JSON Schema Validation
- ADR-018 Policy Aggregate Versioning and Reinstatement Window
- `planning-mds/architecture/lob-extensible-attribute-plan.md`

## Product Manager Planning Handoff

The full planning pass should turn this minimal PRD into:

- A complete PRD with personas, workflows, and release slicing.
- Story files for the smallest implementation sequence.
- A status tracker with required signoff roles.
- API/schema/data-model boundaries for the first vertical slice.
- Explicit acceptance criteria that keep F0019 from adding product-specific hardcoded fields.

## Related User Stories

- To be defined during product-manager planning.
