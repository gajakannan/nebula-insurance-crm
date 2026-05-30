# Story F0036-S0001: Form Engine Skeleton, Dependencies, and Widget-Registry Contract

## Story Header

**Story ID:** F0036-S0001
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Form Engine Skeleton, Dependencies, and Widget-Registry Contract
**Priority:** High
**Phase:** MVP
**Workstream:** A — Dynamic product-attribute engine

## User Story

**As a** Frontend Platform Engineer
**I want** one governed form-engine foundation with a clear widget-registry contract in place
**So that** every later product-attribute and CRUD form is built on the engine ADR-021 decided, instead of bespoke panels

## Context & Background

ADR-021 (Accepted 2026-05-06) decided Nebula uses React Hook Form for field state, AJV for client validation, and an explicit shadcn-style widget registry. F0034 shipped none of it: `experience/package.json` carries no `react-hook-form`, `ajv`, `ajv-formats`, or `ajv-errors`, and `DynamicAttributePanel.tsx` is a hardcoded Cyber panel. This story lays the foundation — pinned dependencies and the registry contract type surface — that S0002–S0008 build on. It is an enabling/infrastructure story: it ships the engine skeleton and registry interface, not yet a rendered Cyber form (that is S0003/S0005).

## Acceptance Criteria

**Happy Path — Dependencies adopted:**
- **Given** the frontend workspace
- **When** the engine foundation lands
- **Then** `react-hook-form`, `ajv`, `ajv-formats`, and `ajv-errors` are present in `experience/package.json` as exact, non-caret versions (no `^`/`~`)
- **And** the lockfile resolves and `pnpm --dir experience build` succeeds

**Happy Path — Widget-registry contract exists:**
- **Given** the engine skeleton
- **When** a developer inspects the engine module
- **Then** a typed `WidgetRegistry` contract exists that maps a widget name to a component plus its option type
- **And** an engine entry component (the future `DynamicAttributePanel` internals) accepts a pinned bundle (data-schema + ui-schema) and a registry, and renders nothing unmapped

**Edge Case — Unknown widget fails closed:**
- **Given** a registry lookup for a widget name that is not registered
- **When** the engine attempts to resolve it
- **Then** the engine raises a developer-visible error (does not silently render a fallback input), matching ADR-021 "bundle activation fails on unknown widget"
- **And** a unit test asserts the unknown-widget path throws rather than rendering

**Edge Case — No behavioral regression yet:**
- **Given** the engine skeleton is merged but not yet wired into screens
- **When** the five consuming screens render
- **Then** they behave exactly as before (the hardcoded panel is still in use until S0005); this story adds no user-visible change

## Interaction Contract

N/A — read-only / infrastructure story. This story introduces no user-facing mutation; it ships dependencies and an engine/registry contract surface. Mutation behavior is delivered by S0003 (validation), S0005 (panel swap), and S0006 (preservation).

## Data Requirements

**Engine inputs (contract, not user data):**
- Pinned bundle reference: `(productVersionId, stage)` plus the bundle's `data-schema.json` and `ui-schema.json`
- A `WidgetRegistry` map: `widgetName -> { component, optionSchema }`

**Validation Rules:**
- Dependency versions are exact (non-caret), consistent with the F0034 assembly-plan intent that was never executed.
- The registry is the single source of widget resolution; there is no inline widget fallback.

## Role-Based Visibility

**Roles that can use this:**
- Frontend Platform Engineer — consumes the engine API. No end-user-facing surface in this story.

**Data Visibility:**
- No InternalOnly/ExternalVisible product data is rendered in this story. Authorization is unchanged; the engine inherits the existing route-level authentication of its host screens (no new public surface).

## Non-Functional Expectations

- **Performance:** Engine bundle additions keep `pnpm --dir experience build` output within the existing size budget; no measurable change to first-render of the unchanged panel.
- **Security:** No new network surface; AJV validation is client-side only and never authoritative (backend remains authoritative on submit). No tokens or PII handled by the engine skeleton.
- **Reliability:** Unknown-widget resolution fails closed (throws), never renders an unverified input.

## Dependencies

**Depends On:**
- F0034 — provides the bundle structure (`LobSchemaBundle`, `data-schema.json`, `ui-schema.json`) the engine consumes.
- ADR-021 — the decision this story begins realizing.

**Related Stories:**
- F0036-S0002 — consumes the registry contract to register concrete widgets.
- F0036-S0003 — adds AJV validation on top of the skeleton.

## Business Rules

1. **Exact dependency pinning.** Engine dependencies are non-caret to keep validation/render behavior deterministic across installs.
2. **Explicit registry (ADR-021).** Every rendered widget must resolve through the registry; unknown widget/option/layout primitives fail closed.

## Out of Scope

- Concrete widget implementations (S0002).
- Rendering the Cyber form or AJV validation (S0003).
- Swapping the live `DynamicAttributePanel` (S0005).
- F0035 preservation wiring (S0006).

## UI/UX Notes

- No user-visible UI in this story. The engine entry component is a drop-in target for S0005.

## Questions & Assumptions

**Resolved (Phase B preflight):**
- The Cyber bundle file is named `ui-schema.json` (hyphen), not `ui.schema.json` as ADR-021 prose says. Phase B amends ADR-021 to the shipped filename; the engine reads `ui-schema.json`.

**Assumptions (to be validated):**
- The existing shadcn/Tailwind component system supplies the primitives the widgets will wrap (confirmed by S0002).

## Definition of Done

- [ ] Acceptance criteria met (deps pinned, registry contract, unknown-widget throws, no regression)
- [ ] Edge cases handled (unknown widget fails closed)
- [ ] Permissions enforced (N/A — no new auth surface; host-screen auth unchanged)
- [ ] Audit/timeline logged (N/A — infrastructure story, no entity mutation)
- [ ] Tests pass (unit: registry resolution + unknown-widget throw; build green)
- [ ] Documentation updated (GETTING-STARTED notes the engine module + registry contract)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
