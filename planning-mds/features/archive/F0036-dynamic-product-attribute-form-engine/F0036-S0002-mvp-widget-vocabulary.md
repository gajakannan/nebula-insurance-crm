# Story F0036-S0002: MVP Widget Vocabulary with Theme and Accessibility Coverage

## Story Header

**Story ID:** F0036-S0002
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** MVP Widget Vocabulary with Theme and Accessibility Coverage
**Priority:** High
**Phase:** MVP
**Workstream:** A — Dynamic product-attribute engine

## User Story

**As a** Schema Steward
**I want** the approved set of governed input widgets to exist and render consistently
**So that** I can publish an attribute that uses any approved widget and trust it will display and behave correctly without a frontend change

## Context & Background

ADR-021 fixes a narrow MVP widget vocabulary so widget behavior, theme coverage, and accessibility stay governed rather than ad hoc. This story implements that vocabulary against the registry contract from S0001. Each widget is a thin, theme-aware wrapper over the existing shadcn component system. This is the value core of the F0034 registry: once these widgets exist and are registered, schema-only attribute changes ship without new field code.

## Acceptance Criteria

**Happy Path — Vocabulary implemented and registered:**
- **Given** the widget registry from S0001
- **When** the MVP vocabulary lands
- **Then** each of these widgets has a registry entry and renders: text, textarea, number, money-minor, select, multi-select, checkbox, date, section, read-only summary
- **And** each widget reads its value and surfaces changes through React Hook Form field binding (controlled by the engine, not lifted parent state)

**Happy Path — Widget contract per widget:**
- **Given** any registered widget
- **When** its contract tests run
- **Then** it has light-theme and dark-theme smoke coverage and form-level keyboard/focus (tab order, focus-visible) coverage, per the ADR-021 widget contract
- **And** money-minor renders and edits minor-unit integer amounts with a currency affordance (e.g. cents stored, dollars displayed)

**Edge Case — Required and invalid rendering:**
- **Given** a field marked required by the data-schema
- **When** it renders
- **Then** the widget exposes a required affordance and an error slot, and shows an inline error message when validation (S0003) reports one
- **And** a disabled/read-only widget state renders without an editable control (used by read-only summary and gated fields)

**Edge Case — Unknown option fails closed:**
- **Given** a `select`/`multi-select` widget whose ui/option configuration references an option not present in the data-schema enum
- **When** the widget resolves its options
- **Then** it fails closed (developer-visible error), consistent with ADR-021 "unknown option" governance, rather than rendering an empty or guessed list

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Any host screen embedding the engine (via S0005) | Type / select / toggle into a widget | Editable when the field is enabled; disabled when gated or read-only | Widget updates the RHF field value (no persistence in this story — save is the host form's responsibility) | Component/integration tests assert the RHF field value updates on user input and the error slot renders on invalid input | All roles that can reach the host screen (unchanged) |

Required checks:
- [x] Render-only behavior cannot satisfy: widgets must accept input and propagate value changes to RHF, proven by interaction tests, not snapshot-only tests.
- [x] Save path validation: invalid input renders an inline error slot (wired to AJV in S0003).
- [x] Audit/timeline: N/A at the widget level; entity-level timeline events are produced by the backend on the host form's save (S0005/S0006).
- [x] Tests prove the user can edit each widget and observe the value/error change.

## Data Requirements

**Per-widget inputs:**
- `value`, `onChange` bound via RHF; `disabled`/`readOnly` flags; `error` message slot; `label` (from ui-schema `fieldLabels`); options list (from data-schema enum) for select/multi-select.

**Validation Rules:**
- Widgets do not invent options; option lists derive from the data-schema enum for that field.
- money-minor stores integer minor units; display formatting must round-trip without precision loss.

## Role-Based Visibility

**Roles that can use widgets:**
- All authenticated roles that can reach a host screen — widget visibility/editability follows the host screen's existing authorization; this story adds no new role checks.

**Data Visibility:**
- A widget renders only the field it is bound to. InternalOnly vs ExternalVisible treatment is inherited from the host screen and the field's existing classification; no widget exposes data the host screen would not.

## Non-Functional Expectations

- **Performance:** A full Cyber-sized form (≈10 fields) renders in < 100ms after data is available; per-keystroke updates do not re-render the whole form (RHF isolation).
- **Security:** Widgets are presentational; no direct network calls, no token handling. Authorization is unchanged.
- **Reliability:** Each widget degrades gracefully on missing optional config (uses schema defaults); unknown options fail closed.
- **Accessibility:** Every widget meets keyboard operability and visible focus; labels are programmatically associated; error messages are announced (aria-describedby).

## Dependencies

**Depends On:**
- F0036-S0001 — the registry contract and engine skeleton.

**Related Stories:**
- F0036-S0003 — feeds AJV error messages into the widget error slots.
- F0036-S0005 — composes these widgets into the live Cyber panel.

## Business Rules

1. **Governed vocabulary only (ADR-021).** Only the ten MVP widgets are registered; heavy domain widgets require a paired ADR + deploy.
2. **Theme + a11y are part of the widget contract.** A widget is not "done" until it has light/dark smoke and keyboard/focus coverage.
3. **Fail closed on unknown option.** Matches bundle-activation governance.

## Out of Scope

- AJV validation wiring and backend parity (S0003).
- Conditional enable/disable rules between fields (S0003/S0004 decide the mechanism).
- Heavy widgets (vehicle schedules, tower visualizers).

## UI/UX Notes

- Screens involved: the widgets appear inside the Cyber panel once S0005 swaps it in; visual output should match the current hardcoded panel closely (no disruptive change for underwriters).
- `section` is a layout primitive (renders a titled group from ui-schema `sections`); `read-only summary` renders non-editable resolved values.

## Questions & Assumptions

**Resolved (Phase B):**
- Widget-to-field resolution does not come from the ui-schema (which carries only `sections` + `fieldLabels`); the widget for each field is derived from the data-schema type/enum/format. Phase B records the type→widget derivation table in the amended ADR-021.

**Assumptions (to be validated):**
- The shadcn component set covers all ten widgets without new primitive dependencies.

## Definition of Done

- [ ] Acceptance criteria met (ten widgets registered, contract coverage, required/invalid/read-only states)
- [ ] Edge cases handled (unknown option fails closed; disabled/read-only render)
- [ ] Permissions enforced (N/A — inherited from host screen)
- [ ] Audit/timeline logged (N/A — presentational widgets)
- [ ] Tests pass (per-widget unit + light/dark theme smoke + keyboard/focus a11y)
- [ ] Documentation updated (GETTING-STARTED lists the registered widgets)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
