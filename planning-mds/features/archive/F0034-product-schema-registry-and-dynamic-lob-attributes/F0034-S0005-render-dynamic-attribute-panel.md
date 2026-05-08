# F0034-S0005 - Render dynamic attribute panel from schema metadata

**Story ID:** F0034-S0005
**Feature:** F0034 - Product Schema Registry and Dynamic LOB Attributes
**Title:** Render dynamic attribute panel from schema metadata
**Priority:** Critical
**Phase:** Platform Foundation

## User Story

**As a** underwriter
**I want** product-specific attributes rendered from the pinned schema bundle inside lifecycle screens
**So that** I can capture Cyber risk details without a custom hardcoded form for every product version

## Context & Background

The Cyber pilot needs a dynamic attribute panel that consumes registry-served schema metadata, supports a governed widget vocabulary, caches active bundles, pins the opened version for the edit session, and displays normalized errors.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user opens an attribute-bearing lifecycle screen for a Cyber record
- **When** the screen loads the pinned Cyber product version
- **Then** the dynamic attribute panel renders fields from the product bundle metadata
- **And** the panel snapshots the product version for the full editing session
- **And** validation errors appear at the field mapped by normalized JSON pointer
- **And** saving valid attributes persists the payload and product version through the approved lifecycle write path

**Alternative Flows / Edge Cases:**
- User opens a record pinned to a deprecated version -> the panel renders the pinned version and displays the warning returned by the server
- A new product version is activated while a form is open -> the open form continues using the original pinned version
- The user opens a legacy-pinned row -> product attributes render read-only with a legacy state message
- Active bundle bootstrap misses the pinned version -> the panel uses ETag-backed lazy fetch for that exact product version and stage
- The schema references a widget not in the registry -> bundle activation fails before users can open it
- A user lacks record access -> no product attributes are returned to the client

**Checklist:**
- [ ] Dynamic panel supports string, number, integer, boolean, enum/select, date, nested object, and array-of-object widget paths required by the pilot
- [ ] Cyber controls posture grid renders the controls object
- [ ] Cyber magnitude-banded slider renders records-held presentation
- [ ] Error mapping uses JSON pointer to target the field
- [ ] Light and dark visual smoke coverage exists for the Cyber panel
- [ ] Legacy state is read-only for product attributes
- [ ] Policy Detail and Renewal Detail expose enabled edit/save/cancel controls for Cyber attributes when the current lifecycle state and caller role allow mutation
- [ ] Policy Detail and Renewal Detail tests prove a user can edit at least one Cyber field, save it, and see the persisted value after reload/query invalidation
- [ ] Read-only panel state is tested separately from editable state so a rendered panel cannot satisfy this story by display-only behavior

## Data Requirements

**Required Fields:**
- Product version id
- Stage
- Bundle ETag
- UI schema section order
- Widget name
- Widget options
- Attributes payload

**Optional Fields:**
- Deprecated-version warning
- Read-only reason
- Lazy-fetch cache state

**Validation Rules:**
- Widget names and options must conform to the approved UI schema meta-contract
- The panel must not mutate the product version after form open
- Unknown widgets block activation before runtime
- Field errors must map by normalized pointer

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Distribution User - capture authorized submission-stage Cyber attributes
- Underwriter - capture authorized submission, policy, endorsement, and renewal Cyber attributes
- Frontend Developer - implements dynamic panel and widget registry
- Quality Engineer - validates visual, accessibility, and parity behavior
- Security Reviewer - reviews record access and bundle visibility

**Data Visibility:**
- InternalOnly content: Cyber product attributes and validation errors
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Performance: dynamic form initial render target is at most 300 ms for a representative Cyber submission on a mid-tier laptop
- Accessibility: labels, errors, keyboard order, and focus behavior meet the existing frontend quality gate
- Reliability: cached bundles are keyed by product version, stage, and ETag

## Dependencies

**Depends On:**
- F0034-S0002 - registry foundation
- F0034-S0004 - normalized validation errors
- F0034-S0006 - Cyber bundle metadata

**Related Stories:**
- F0034-S0007 - lifecycle flow embeds the panel in target screens

## Business Rules

1. **Pin-on-open:** The panel never changes schema version during an active edit session.
2. **Schema governs display:** Product-specific fields render from bundle metadata, not feature-specific JSX for Cyber fields.
3. **Legacy is read-only for attributes:** Legacy-pinned product attributes cannot be edited through the ordinary panel.

## Out of Scope

- No-code product administration UI
- Heavy non-Cyber custom widgets
- External broker-facing product attribute capture

## UI/UX Notes

- Screens involved: Submission Create/Triage, Submission Detail, Policy 360, Endorsement Flow, Renewal Detail
- Key interactions: bootstrap active bundles, render fields, validate, save, show field-level errors, show pinned-version and legacy states
- Policy 360 and Renewal Detail are not display-only surfaces for active Cyber rows. The story is incomplete unless the page provides a clear edit affordance and submits through the approved lifecycle mutation path.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- The initial Cyber panel uses the existing app shell and shadcn/Tailwind visual system.
- The widget vocabulary is finalized in Phase B before frontend implementation.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
