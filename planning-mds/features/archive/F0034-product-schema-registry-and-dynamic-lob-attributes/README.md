# F0034 - Product Schema Registry and Dynamic LOB Attributes

**Status:** Done and archived (2026-05-07)
**Archived:** 2026-05-07
**Priority:** Critical
**Phase:** Platform Foundation / CRM Release MVP Enabler

## Overview

F0034 establishes Nebula's product-attribute foundation: governed JSON Schema bundles, schema-pinned lifecycle attributes, frontend/backend validation parity, dynamic forms, legacy-read compatibility, and a Cyber pilot. The feature exists to keep F0019 quote/proposal work and later coverage/reporting work from adding product-specific fixed fields and custom forms.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Phase A requirements, personas, workflows, screen layouts, acceptance criteria |
| [STATUS.md](./STATUS.md) | Planning and implementation tracker |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Planning context and handoff notes |
| [ADR-020](../../../architecture/decisions/ADR-020-lob-extensible-attribute-architecture.md) | Product schema registry, attribute carriers, sentinels, invariants |
| [ADR-021](../../../architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md) | Dynamic form engine and widget registry |
| [ADR-022](../../../architecture/decisions/ADR-022-validator-equivalence-restricted-profile.md) | Restricted JSON Schema profile and normalized LOB errors |
| [ADR-023](../../../architecture/decisions/ADR-023-rules-governance-jsonlogic.md) | JsonLogic rule governance |
| [OpenAPI projection matrix](../../../architecture/openapi-30-projection-matrix.md) | JSON Schema 2020-12 to OpenAPI 3.0.3 compatibility |
| [Validation perf baseline](../../../architecture/validation-perf-baseline.md) | Required F0034 performance measurements and gates |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0034-S0001](./F0034-S0001-lock-product-attribute-decision-set.md) | Lock product-attribute decision set | Done |
| [F0034-S0002](./F0034-S0002-establish-product-schema-registry-foundation.md) | Establish product schema registry foundation | Done |
| [F0034-S0003](./F0034-S0003-pin-attributes-on-lifecycle-carriers.md) | Pin attributes on lifecycle carriers | Done |
| [F0034-S0004](./F0034-S0004-prove-validator-equivalence.md) | Prove frontend and backend validator equivalence | Done |
| [F0034-S0005](./F0034-S0005-render-dynamic-attribute-panel.md) | Render dynamic attribute panel from schema metadata | Done |
| [F0034-S0006](./F0034-S0006-activate-cyber-product-bundle.md) | Activate Cyber product bundle | Done |
| [F0034-S0007](./F0034-S0007-prove-lifecycle-and-f0019-handoff.md) | Prove lifecycle integration and F0019 handoff | Done |

**Total Stories:** 7
**Completed:** 7 / 7

## Key Dependencies

- F0006 Submission Intake Workflow
- F0007 Renewal Pipeline
- F0018 Policy Lifecycle and Policy 360
- F0019 Submission Quoting, Proposal and Approval Workflow
- F0020 Document Management and ACORD Intake
- ADR-001 JSON Schema Validation
- ADR-018 Policy Aggregate Versioning and Reinstatement Window
- `planning-mds/architecture/lob-extensible-attribute-plan.md`

## Phase A Notes

- Cyber is the first product pilot per the existing LOB extensibility plan.
- Product attributes belong to Submission, PolicyVersion, PolicyEndorsement, and Renewal. Policy reads current attributes through PolicyVersion.
- F0019 must consume this foundation for product-specific quote/proposal attributes.

## Phase B Architecture Notes

- ADR-020 through ADR-023 are the accepted Stage 0 decision set for F0034.
- `planning-mds/api/nebula-api.yaml` defines `/lob-schemas/active`, `/lob-schemas/{productVersionId}/{stage}`, and `/lob-schemas/{productVersionId}/activate`.
- Static draft-07 schemas now carry the `lobAttributes` envelope on the attribute-carrier contracts; product-version-specific validation remains runtime-resolved.
- Authorization adds `lob_schema:read`, `lob_schema:resolve`, and Admin-only activation/deprecation/retirement actions.
