# F0034-S0006 - Activate Cyber product bundle

**Story ID:** F0034-S0006
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Activate Cyber product bundle
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** schema steward
**I want** Cyber `1.0.0` activated as the first governed product bundle
**So that** Nebula proves product-specific attributes with a bounded commercial P&C pilot

## Context & Background

The existing LOB extensibility plan selects Cyber as the first pilot. Cyber has a useful but bounded attribute set: revenue band, records held, controls posture, prior incidents, requested limit, and requested retention. It proves schema bundles, shared primitives, rules, projections, examples, and dynamic rendering without requiring the heaviest future LOB widgets.

## Acceptance Criteria

**Happy Path:**
- **Given** the schema registry foundation exists
- **When** the Cyber `1.0.0` bundle is authored and activated
- **Then** it includes data schema, UI schema, rules, projections, examples, and README evidence
- **And** it uses shared primitives for money, currency, TIV, percent, US state, and effective date where applicable
- **And** activation records Cyber `1.0.0` as tenant-available and Active
- **And** FE and backend parity evidence passes for valid, invalid, and rule-case fixtures

**Alternative Flows / Edge Cases:**
- Any required Cyber bundle artifact is missing -> activation is rejected
- A fixture rejects in only one validation layer -> activation is rejected
- A Cyber projection targets a carrier not declared by the projection -> activation is rejected
- A pre-registry Cyber row remains pinned to `_unspecified/0.0.0` after legacy backfill -> activation is rejected
- A legacy-pinned Cyber row opens after activation -> it renders through the legacy read path and stays read-only for product attributes

**Checklist:**
- [ ] Cyber data schema includes `revenueBand`
- [ ] Cyber data schema includes `recordsHeld`
- [ ] Cyber data schema includes `controls` with MFA, EDR, backup, and training fields
- [ ] Cyber data schema includes `priorIncidents`
- [ ] Cyber data schema includes `requestedLimit`
- [ ] Cyber data schema includes `requestedRetention`
- [ ] Cyber rules include records-held requiring MFA and retention minimum relative to requested limit
- [ ] Cyber projections include records-held count, requested-limit amount, and MFA enabled
- [ ] Cyber valid, invalid, and rule-case examples exist

## Data Requirements

**Required Fields:**
- `revenueBand`
- `recordsHeld`
- `controls.mfaEnabled`
- `controls.mfaMaturity`
- `controls.edrEnabled`
- `controls.edrMaturity`
- `controls.backupEnabled`
- `controls.trainingFrequency`
- `priorIncidents`
- `requestedLimit.amountMinor`
- `requestedLimit.currency`
- `requestedRetention.amountMinor`
- `requestedRetention.currency`

**Optional Fields:**
- `priorIncidents[].description`
- UI section labels
- Projection query hints

**Validation Rules:**
- `recordsHeld` must be an integer
- `priorIncidents` must contain no more than 20 entries
- `recordsHeld > 1000000` requires `controls.mfaEnabled = true`
- `requestedRetention.amountMinor` must be at least one percent of `requestedLimit.amountMinor`
- Money amounts use integer minor units

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Schema Steward - authors and activates the Cyber bundle
- Underwriter - reviews the Cyber attribute vocabulary
- Product Operations Lead - accepts the pilot scope
- Quality Engineer - validates fixtures and parity
- Security Reviewer - reviews product data visibility
- Authorization review confirms only steward-approved activation paths can expose Cyber `1.0.0` as Active

**Data Visibility:**
- InternalOnly content: Cyber risk attributes, bundle fixtures, activation evidence
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Reliability: Cyber records render against their pinned version after future Cyber versions activate
- Performance: Cyber validation and render budgets match F0034 feature-level targets
- Security: active Cyber bundle listing respects tenant availability

## Dependencies

**Depends On:**
- F0034-S0002 - registry foundation
- F0034-S0004 - parity harness
- F0034-S0005 - dynamic widget vocabulary

**Related Stories:**
- F0034-S0007 - lifecycle integration uses Cyber `1.0.0`

## Business Rules

1. **Cyber is first pilot:** F0034 proves the end-to-end product-attribute path with Cyber before remaining LOB rollout.
2. **Examples are release evidence:** Activation requires valid, invalid, and rule-case examples.
3. **Projection declarations are explicit:** Projection entities and stages are declared per projection and are not inferred globally.

## Out of Scope

- Remaining LOB activation
- Heavy custom widgets for Commercial Auto and Property
- Broker-facing Cyber capture

## UI/UX Notes

- Screens involved: Dynamic Attribute Panel on lifecycle screens
- Key interactions: Cyber controls posture grid, records-held presentation, requested limit and retention inputs

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Cyber field names and constraints remain aligned with the existing LOB extensibility plan.
- The first tenant availability set includes Cyber.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
