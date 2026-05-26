# Story F0036-S0003: Schema-Driven Rendering and AJV Client Validation with Backend Parity

## Story Header

**Story ID:** F0036-S0003
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Schema-Driven Rendering and AJV Client Validation with Backend Parity (Cyber)
**Priority:** High
**Phase:** MVP
**Workstream:** A — Dynamic product-attribute engine

## User Story

**As a** Cyber Underwriter
**I want** the Cyber attributes form to build itself from the governed product definition and tell me immediately when an entry is invalid
**So that** I can correct mistakes before submitting and trust that what passes on screen is what the system will accept

## Context & Background

This story makes the engine render a real Cyber form from the pinned bundle and validate it in the browser with AJV. The bundle's `data-schema.json` defines fields, types, enums, and constraints; `ui-schema.json` defines section grouping and labels. The engine derives each field's widget (S0002) from the data-schema and orders/labels it from the ui-schema. AJV validates against the same `data-schema.json` the backend enforces, so client and backend agree on the published examples. The bundle also ships `rules.json` (cross-field rules) — see the parity decision below.

## Acceptance Criteria

**Happy Path — Schema-driven render:**
- **Given** the pinned Cyber `cyber/1.0.0` bundle
- **When** the engine renders the attribute form
- **Then** the rendered fields, sections, order, and labels come entirely from `data-schema.json` + `ui-schema.json` (sections `exposure`, `controls`, `terms`), with no Cyber-specific field list in component code
- **And** widgets resolve from the data-schema: `revenueBand`→select, `recordsHeld`→number, `controls.*Enabled`→checkbox, `controls.mfaMaturity`/`trainingFrequency`→select, `requestedLimit`/`requestedRetention`→money-minor

**Happy Path — AJV validation feedback:**
- **Given** the rendered form
- **When** the user enters a value that violates the data-schema (e.g. a negative `recordsHeld`, or an out-of-enum `revenueBand`)
- **Then** AJV reports the violation and the offending widget shows an inline, human-readable message (via `ajv-errors`)
- **And** the form blocks its own submit affordance while data-schema-invalid

**Happy Path — Backend parity:**
- **Given** the Cyber bundle's published example payloads
- **When** each example is validated by client AJV and by the backend validator
- **Then** the two agree on every example — a parity fixture matrix records ≥ 1 row per example with 0 disagreements
- **And** backend validation remains authoritative on submit (client validation is an immediate-feedback layer, never a substitute)

**Edge Case — Cross-field rules (rules.json):**
- **Given** the Cyber bundle ships `rules.json` cross-field rules (`mfa_required_for_high_record_count`: `recordsHeld >= 1000000` requires `controls.mfaEnabled`; `minimum_retention_not_met`: `requestedRetention` ≥ 1% of `requestedLimit`)
- **When** the form is filled in a way that violates a cross-field rule but passes the plain JSON Schema
- **Then** the client surfaces the same error class the backend would (the engine evaluates `rules.json` in addition to the data-schema), keeping the 0-disagreement parity claim true for rule-bearing examples
- **And** if a published example does not exercise a cross-field rule, that is recorded explicitly in the parity matrix as "schema-only"

**Edge Case — Required and error states:**
- **Given** a required field (`revenueBand`, `recordsHeld`, `controls`, `requestedLimit`, `requestedRetention`) left empty
- **When** validation runs
- **Then** the field shows a required error and submit is blocked until resolved

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Cyber attribute form embedded in a host screen | Enter/select attribute values; attempt submit | Editable in create/edit contexts; read-only in read-only contexts (host-driven) | Client AJV gates the form's own validity; the actual persistence is the host screen's save (S0005), which calls the existing F0034 backend write path; backend re-validates and is authoritative | A test enters invalid data → inline error + blocked submit; enters valid data → submit allowed; on host save, reload shows persisted attributes and the entity timeline event from the backend | Roles/states allowed by the host screen (create on draft submission/policy, etc.) |

Required checks:
- [x] Render-only behavior cannot satisfy: validation must actually block invalid submit and clear on correction, proven by interaction tests.
- [x] Save path validation: AJV (data-schema + rules.json) on the client; backend authoritative on submit.
- [x] Audit/timeline: the entity timeline event (e.g. attribute update) is emitted by the backend on the host form's save; this story asserts it persists on reload but does not introduce a new event class.
- [x] Tests prove invalid→error, valid→submit-allowed, and client/backend parity on examples.

## Data Requirements

**Required fields (from Cyber `data-schema.json`):** `revenueBand` (enum), `recordsHeld` (integer ≥ 0), `controls` (object: `mfaEnabled`, `edrEnabled`, `backupEnabled`, `trainingFrequency` required; `mfaMaturity` nullable enum), `requestedLimit`/`requestedRetention` (object: `amountMinor` integer, `currency` enum `USD`).

**Validation Rules:**
- Data-schema constraints (types, enums, minimums) enforced client-side by AJV with `ajv-formats`.
- Cross-field `rules.json` evaluated client-side for parity (severity `error`).
- Backend validation is authoritative on submit; client never bypasses it.

## Role-Based Visibility

**Roles that can edit Cyber attributes:**
- The same roles the host screen already authorizes for create/edit (e.g. Underwriter on a draft submission/policy). This story does not change authorization; it renders within the host's existing permission and lifecycle constraints.

**Data Visibility:**
- Attribute values follow the host entity's existing visibility. No new InternalOnly/ExternalVisible exposure is introduced; unauthorized users never reach the editable form (host route auth, HTTP 401/403 handled upstream).

## Non-Functional Expectations

- **Performance:** AJV validation of a Cyber-sized payload runs in < 50ms; inline error feedback appears within one frame of the triggering change.
- **Security:** Client validation is advisory; the backend re-validates every submit and remains authoritative (no trust in client-passed data). No tokens/PII handled by the validator.
- **Reliability:** A malformed or unexpected bundle shape surfaces a controlled error rather than rendering a partial/guessed form.

## Dependencies

**Depends On:**
- F0036-S0001 — engine skeleton + registry.
- F0036-S0002 — widgets to render and show errors.
- F0034 — Cyber bundle (`data-schema.json`, `ui-schema.json`, `rules.json`) and the backend validator that defines parity truth.

**Related Stories:**
- F0036-S0004 — pins the bundle version this story validates against.
- F0036-S0005 — embeds this validated form into the five screens.

## Business Rules

1. **Backend is authoritative.** Client AJV is fast feedback; the backend re-validates and wins on submit.
2. **Parity is measured, not assumed.** A fixture matrix over the published examples proves 0 disagreements; rule-only gaps are recorded explicitly.
3. **Schema-driven only.** No Cyber field list, option list, or layout is hardcoded in component code.

## Out of Scope

- Pin-during-edit session binding (S0004).
- Swapping the live panel on the five screens (S0005).
- F0035 preservation (S0006).
- Non-Cyber LOB activation (engine is LOB-agnostic but only Cyber is validated here).

## UI/UX Notes

- Inline validation messages come from AJV + `ajv-errors` and must read in plain language aligned with backend semantics.
- The PRD's ASCII layout is illustrative; the bundle's `ui-schema.json` is authoritative for section grouping (note: it groups `requestedLimit`/`requestedRetention` under a `terms` section, not `exposure`).

## Questions & Assumptions

**Resolved (Phase B):**
- **Does client parity include `rules.json`?** Yes — to claim 0-disagreement parity the engine must evaluate `rules.json` cross-field rules client-side, because plain AJV against `data-schema.json` does not cover them. Recorded in the amended ADR-021 / companion ADR.
- **`ui-schema.json` carries no widget map** — widgets are derived from the data-schema (decision recorded in S0002/ADR amendment).

**Assumptions (to be validated):**
- The Cyber bundle's published examples are available to both client fixtures and the backend validator for the parity matrix.

## Definition of Done

- [ ] Acceptance criteria met (schema-driven render, AJV feedback, backend parity, rules.json parity, required states)
- [ ] Edge cases handled (cross-field rule violations, required/empty, malformed bundle)
- [ ] Permissions enforced (host-screen auth unchanged; no new exposure)
- [ ] Audit/timeline logged (backend entity event persists on host save; asserted on reload)
- [ ] Tests pass (unit: widget derivation; integration: validation feedback; parity fixture matrix 0 disagreements)
- [ ] Documentation updated (GETTING-STARTED documents the parity fixture matrix location)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
