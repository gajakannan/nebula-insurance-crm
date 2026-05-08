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
**Status:** Phase A draft pending approval

## Feature Statement

**As a** product and underwriting operations team
**I want** product-specific insurance attributes to be defined by governed JSON Schema bundles and rendered through dynamic forms
**So that** Nebula can support different insurance products and lines of business without repeated frontend, backend, and database rewrites

## Business Objective

- **Goal:** Establish the product-attribute foundation before quoting, proposal, coverage, and reporting features add product-specific data.
- **Metric:** A new Cyber attribute that fits the approved widget vocabulary can be added by publishing a schema-bundle version, updating examples, and passing parity tests without adding an ordinary relational column or custom form component.
- **Baseline:** Submission, policy, renewal, and policy-version models rely on mostly fixed columns. Product-specific underwriting data would require coordinated DTO, validation, UI, persistence, and test changes.
- **Target:** Cyber becomes the first product pilot. It proves schema-pinned attributes, dynamic rendering, frontend/backend validation parity, legacy-read compatibility, and the F0019 handoff path.

## Personas

- **Schema Steward**
  - Primary job: review, approve, activate, deprecate, and retire product schema bundles.
  - Success metric: every activated bundle has signed examples, parity fixtures, activation evidence, and an audit trail.

- **Underwriter**
  - Primary job: capture and review product-specific risk attributes during submission, quote, policy, endorsement, and renewal workflows.
  - Success metric: product-specific fields appear in the right workflow surface with clear validation errors and no duplicate custom forms.

- **Distribution User**
  - Primary job: triage submissions and maintain the attributes needed for underwriting handoff.
  - Success metric: a null-LOB submission can move to Cyber with required product attributes in one intentional triage write.

- **Product Operations Lead**
  - Primary job: evolve product definitions without waiting for platform column and form rewrites for every compatible data-schema change.
  - Success metric: additive Cyber data-schema changes remain deployable through the governed schema-bundle process when the current widget vocabulary covers them.

- **Quality Engineer**
  - Primary job: prove browser and backend validation decisions match for every pilot fixture.
  - Success metric: valid, invalid, and rule-case fixtures produce identical accept/reject outcomes and identical `(code, pointer)` error multisets across both layers.

## Problem Statement

### Current State

Nebula has static `lineOfBusiness` classification and a document metadata schema renderer from F0020, but it does not yet have a governed product schema registry for lifecycle product attributes. Without F0034, F0019 quote/proposal work and later coverage/reporting work would introduce hardcoded product fields across frontend forms, backend validators, DTOs, schemas, and database columns.

### Desired State

Product-specific lifecycle attributes live in governed JSON payloads pinned to immutable product schema versions. The same schema bundle drives client validation, backend validation, dynamic form rendering, examples, and projection declarations. Core operational columns remain stable and keep platform workflows, audit, ABAC, and reporting reliable.

### Business Impact

F0034 prevents product-attribute drift from spreading into F0019 and later CRM release features. It lets Nebula evolve from generic LOB classification toward product-aware underwriting while preserving traceability and operational controls.

## Scope & Boundaries

### In Scope

- Stage 0 decision lock for product-attribute architecture, form engine, validation parity, and rules governance before implementation begins.
- Filesystem-canonical product schema bundle governance for MVP, with runtime serving and cache behavior defined by architecture in Phase B.
- Attribute-carrier requirements for Submission, PolicyVersion, PolicyEndorsement, and Renewal.
- Policy parent boundary: policy-level product attributes come from the current PolicyVersion; Policy does not own an independent attribute payload in the first slice.
- Schema version pinning on every attribute-bearing row.
- Existing-record compatibility through `_unspecified/0.0.0` for null-LOB Submission/Renewal rows and per-LOB legacy sentinels for pre-registry non-null rows.
- Frontend/backend validation parity using shared fixtures and normalized LOB validation errors.
- Dynamic form rendering with a governed widget vocabulary and pin-on-open editing behavior.
- Cyber `1.0.0` as the first product pilot.
- F0019 guardrail: quote/proposal work consumes this foundation instead of adding hardcoded Cyber product fields.

### Out of Scope

- Full rollout to every LOB in the first implementation slice.
- A no-code product administration console.
- Replacing core operational columns such as account, broker, status, dates, premium summaries, workflow state, authorization fields, or audit fields.
- Replacing the F0020 document metadata schema registry.
- External broker self-service product capture.
- Carrier rating integration, policy issuance accounting, and billing workflows.
- Advanced widgets for Commercial Auto, Property COPE, D&O towers, or other future LOB-specific layouts.
- Applying JSON attribute payloads to Account, Broker, Contact, Carrier, UserProfile, Task, or Activity.

## Product Decisions

1. **Pilot LOB:** Cyber is the first product pilot because the existing LOB plan identifies it as the smallest useful attribute footprint and it does not require the heavy custom widgets needed by Commercial Auto or Property.
2. **First lifecycle carriers:** Submission, PolicyVersion, PolicyEndorsement, and Renewal are the attribute carriers. Policy reads current product attributes through its current PolicyVersion.
3. **Compatibility posture:** Existing records continue to render and accept core-only edits when their pinned legacy product version stays unchanged; new or changed product attributes require a tenant-available active product version.
4. **Validation contract:** Acceptance depends on FE/BE decision parity and stable normalized error codes plus JSON pointers, not on matching human-readable error strings.
5. **Form contract:** Dynamic forms are pinned to the product version opened by the user for the full editing session. A newly activated schema version applies to the next form open.
6. **F0019 dependency:** F0019 must not add Cyber quote/proposal product attributes as fixed fields when those fields belong in the F0034 product-attribute contract.

## Success Criteria

- Stage 0 architecture decisions are accepted before implementation stories proceed.
- Attribute-bearing create/update requests can carry `lobProductVersionId` and `attributes` where the relevant lifecycle carrier accepts product attributes.
- Cyber `1.0.0` has valid, invalid, and rule-case fixtures with FE/BE parity evidence.
- Dynamic forms render Cyber submission, policy, endorsement, and renewal attributes from schema metadata.
- Product attribute writes pin the schema/product version used for validation and rendering.
- Existing records without pilot attributes remain readable and operational for core-only workflow actions.
- F0019 planning and implementation reference F0034 for product-specific quote/proposal fields.

## Release Slicing

| Slice | Stories | Outcome |
|-------|---------|---------|
| Decision Lock | F0034-S0001 | Architecture and governance decisions are binding before runtime work begins. |
| Registry Foundation | F0034-S0002, F0034-S0003 | Product versions, sentinels, and lifecycle attribute carriers are available. |
| Validation and Forms | F0034-S0004, F0034-S0005 | Browser/backend parity and dynamic panel rendering are available. |
| Cyber Pilot | F0034-S0006, F0034-S0007 | Cyber bundle, lifecycle integration, and F0019 guardrail are proven end to end. |

## User Workflows

### Schema Steward Activation

1. Schema Steward reviews a product bundle with data schema, UI schema, rules, projections, examples, and narrative.
2. The bundle passes profile linting and FE/BE parity fixtures.
3. The steward activates the bundle with a reason.
4. Nebula records the activation event with actor, product version, prior state, new state, timestamp, and reason.
5. Authenticated app sessions receive active tenant-available bundles on bootstrap or via ETag-backed fetch.

### Cyber Submission Triage

1. Distribution user opens a submission that does not yet have a LOB.
2. User selects Cyber and supplies the Cyber product attributes required for submission triage.
3. Nebula validates the attributes against Cyber `1.0.0`.
4. The write sets `lineOfBusiness = Cyber`, pins the Cyber product version, stores attributes, and appends audit/timeline evidence.
5. The submission can advance through the approved intake and F0019 downstream workflow states without custom Cyber columns.

### Legacy Cyber Read

1. Underwriter opens a pre-registry Cyber record.
2. Nebula resolves the legacy Cyber sentinel pinned to the row.
3. The product-attribute panel renders legacy attributes read-only or empty, with a visible legacy badge.
4. Core-only workflow actions remain available when authorization and workflow state allow them.
5. Attribute edits require a governed migration or active product version path.

## Screen Responsibilities

| Screen / Zone | Responsibility | Primary Personas |
|---------------|----------------|------------------|
| Submission Create / Triage | Select LOB/product version and capture Cyber submission attributes when moving out of null-LOB intake. | Distribution User, Underwriter |
| Submission Detail | Display pinned schema version, render Cyber attributes, show normalized validation errors, and preserve pin-on-open editing. | Distribution User, Underwriter |
| Policy 360 / Current Version | Display current PolicyVersion product attributes without making Policy an independent attribute carrier. | Underwriter, Product Operations Lead |
| Endorsement Flow | Capture changed product attributes against the endorsed PolicyVersion path and show migration prompts for legacy pins. | Underwriter |
| Renewal Detail | Capture renewal-stage product attributes from the pinned bundle and preserve existing renewal workflow context. | Distribution User, Underwriter |
| Schema Steward Evidence View | Phase B may expose this as docs/runbook evidence rather than an app screen in the first slice; no no-code admin console is included. | Schema Steward, Quality Engineer |

## Screen Layouts (ASCII)

### Desktop: Dynamic Attribute Panel in Submission Detail

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Submission SUB-1042                         Status: ReadyForUWReview         │
├───────────────────────┬──────────────────────────────────────────────────────┤
│ Core Submission        │ Product Attributes                                  │
│ Account                │ Cyber / 1.0.0                         Active        │
│ Broker                 │ Schema pinned at open: 48f5...e63c20                │
│ LOB: Cyber             │                                                      │
│ Effective Date         │ ┌ Revenue Band ─────────┐ ┌ Records Held ────────┐ │
│ Assigned UW            │ │ [10-50M v]             │ │ [ 1,250,000      ]   │ │
│ Timeline               │ └────────────────────────┘ └──────────────────────┘ │
│ Documents              │                                                      │
│ Workflow Actions       │ ┌ Controls Posture ───────────────────────────────┐ │
│                        │ │ MFA [x] Mature [Managed v]  EDR [x] [Managed v]│ │
│                        │ │ Backups [x]  Training [Quarterly v]            │ │
│                        │ └─────────────────────────────────────────────────┘ │
│                        │                                                      │
│                        │ Requested Limit        Requested Retention          │
│                        │ [ $1,000,000     ]     [ $10,000            ]       │
│                        │                                                      │
│                        │ [Validate] [Save Attributes] [Cancel]               │
└───────────────────────┴──────────────────────────────────────────────────────┘
```

### Narrow: Dynamic Attribute Panel Stacked

```text
┌──────────────────────────────┐
│ Submission SUB-1042          │
│ Status ReadyForUWReview      │
│ LOB Cyber                    │
├──────────────────────────────┤
│ Product Attributes           │
│ Cyber / 1.0.0                │
│ Pinned 48f5...e63c20         │
├──────────────────────────────┤
│ Revenue Band                 │
│ [10-50M v]                   │
│ Records Held                 │
│ [1,250,000]                  │
├──────────────────────────────┤
│ Controls                     │
│ MFA [x] [Managed v]          │
│ EDR [x] [Managed v]          │
│ Backups [x]                  │
│ Training [Quarterly v]       │
├──────────────────────────────┤
│ Requested Limit              │
│ [$1,000,000]                 │
│ Requested Retention          │
│ [$10,000]                    │
├──────────────────────────────┤
│ [Validate] [Save]            │
└──────────────────────────────┘
```

### Legacy Read State

```text
┌───────────────────────────────────────────────┐
│ Product Attributes                            │
│ Legacy Cyber / 0.0.0                 Internal │
│ This record uses the legacy read path.         │
│ Product attribute edits require migration.     │
│                                               │
│ Core workflow actions remain available when    │
│ workflow state and ABAC allow the action.      │
└───────────────────────────────────────────────┘
```

## Feature-Level Acceptance Criteria

### Governance

- [ ] Stage 0 decisions cover product-attribute architecture, dynamic form engine, validator equivalence, and rules governance.
- [ ] Every activated product bundle has data schema, UI schema, rules, projections, examples, and README evidence.
- [ ] Schema lifecycle transitions record actor, from-state, to-state, product version, timestamp, and reason.
- [ ] Tenant bootstrap returns only active tenant-available product versions; internal sentinels are excluded from the active listing.

### Validation

- [ ] FE and backend validators accept the same valid Cyber examples.
- [ ] FE and backend validators reject the same invalid Cyber examples with identical `(code, pointer)` multisets.
- [ ] JsonLogic rule fixtures produce identical pass/fail outcomes in FE and backend harnesses.
- [ ] Existing global ProblemDetails `errors` semantics remain unchanged; LOB validation uses a sibling LOB validation error shape.

### Dynamic Forms

- [ ] Cyber dynamic forms render from the pinned product version for submission, policy, endorsement, and renewal stages.
- [ ] Opening a form snapshots the product version for the full editing session.
- [ ] A new schema activation does not alter an already-open editing session.
- [ ] Unknown widget names or options block activation until the paired frontend widget registry support exists.
- [ ] Legacy-pinned rows render in a read-only product-attribute state for product attributes.

### Compatibility and F0019 Guardrail

- [ ] Null-LOB Submission/Renewal rows can pin `_unspecified/0.0.0` only with empty attributes.
- [ ] Existing non-null LOB rows pin the matching per-LOB legacy sentinel during foundation migration.
- [ ] Product attribute changes on legacy-pinned rows are rejected unless a governed migration path handles the move.
- [ ] F0019 quote/proposal requirements consume product attributes through the F0034 foundation instead of adding Cyber-specific fixed fields.

## Dependencies

- F0006 Submission Intake Workflow
- F0007 Renewal Pipeline
- F0018 Policy Lifecycle and Policy 360
- F0019 Submission Quoting, Proposal and Approval Workflow
- F0020 Document Management and ACORD Intake
- ADR-001 JSON Schema Validation
- ADR-018 Policy Aggregate Versioning and Reinstatement Window
- `planning-mds/architecture/lob-extensible-attribute-plan.md`

## Architecture Traceability

| Decision Area | Artifact |
|---------------|----------|
| Product registry, attribute carriers, sentinels, invariants | `planning-mds/architecture/decisions/ADR-020-lob-extensible-attribute-architecture.md` |
| Dynamic form engine and widget registry | `planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md` |
| Validator equivalence and LOB error envelope | `planning-mds/architecture/decisions/ADR-022-validator-equivalence-restricted-profile.md` |
| JsonLogic rules governance | `planning-mds/architecture/decisions/ADR-023-rules-governance-jsonlogic.md` |
| OpenAPI 3.0.3 projection compatibility | `planning-mds/architecture/openapi-30-projection-matrix.md` |
| Validation and form performance gates | `planning-mds/architecture/validation-perf-baseline.md` |
| Static API and schema contract | `planning-mds/api/nebula-api.yaml`, `planning-mds/schemas/lob-*.schema.json` |
| Authorization contract | `planning-mds/security/authorization-matrix.md`, `planning-mds/security/policies/policy.csv` |

## Risks & Mitigations

- **Risk:** Registry scope delays CRM MVP delivery.
  - **Mitigation:** Ship foundation rails plus Cyber first; defer no-code administration and full LOB rollout.
- **Risk:** Under-scoping creates a second partial schema renderer.
  - **Mitigation:** Require validation parity, widget governance, lifecycle pinning, and F0019 handoff acceptance in the first feature.
- **Risk:** Existing rows lose operational continuity.
  - **Mitigation:** Use internal sentinels for read compatibility and allow core-only workflow actions when the pinned product version and attributes stay unchanged.
- **Risk:** Product schemas expose authorization-sensitive fields through bootstrap.
  - **Mitigation:** Tenant filtering and ABAC are required for active bundle listing and record-level access; security review is required before closeout.

## Assumptions

- AJV remains the frontend validation engine for JSON Schema validation.
- Backend validation uses a real JSON Schema validator and a normalized error shim rather than ad hoc schema walking.
- ADR-018 remains accepted and keeps Policy line-of-business immutable after create.
- Cyber `1.0.0` is the first pilot unless the user changes that decision before Phase A approval.
- Existing F0020 document metadata schema rendering remains a precedent only; F0034 owns lifecycle product attributes.

## Related User Stories

| Story | Title | Status |
|-------|-------|--------|
| [F0034-S0001](./F0034-S0001-lock-product-attribute-decision-set.md) | Lock product-attribute decision set | Not Started |
| [F0034-S0002](./F0034-S0002-establish-product-schema-registry-foundation.md) | Establish product schema registry foundation | Not Started |
| [F0034-S0003](./F0034-S0003-pin-attributes-on-lifecycle-carriers.md) | Pin attributes on lifecycle carriers | Not Started |
| [F0034-S0004](./F0034-S0004-prove-validator-equivalence.md) | Prove frontend and backend validator equivalence | Not Started |
| [F0034-S0005](./F0034-S0005-render-dynamic-attribute-panel.md) | Render dynamic attribute panel from schema metadata | Not Started |
| [F0034-S0006](./F0034-S0006-activate-cyber-product-bundle.md) | Activate Cyber product bundle | Not Started |
| [F0034-S0007](./F0034-S0007-prove-lifecycle-and-f0019-handoff.md) | Prove lifecycle integration and F0019 handoff | Not Started |
