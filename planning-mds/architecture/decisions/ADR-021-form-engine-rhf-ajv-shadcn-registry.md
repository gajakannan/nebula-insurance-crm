# ADR-021: Dynamic Form Engine With RHF, AJV, and shadcn Widget Registry

**Status:** Accepted (Amended 2026-05-25 — see Plan Amendment)
**Date:** 2026-05-06
**Amended:** 2026-05-25 (F0036 — reconciled with the shipped F0034 implementation; plan run `2026-05-25-51ff2a92`)
**Owners:** Architect
**Related Features:** F0034, F0036
**Related ADRs:** ADR-020, ADR-022, ADR-023, ADR-024
**Related Schema Bundles:** `cyber/1.0.0`, `_unspecified/0.0.0`, `_legacy/<lob>/0.0.0`

## Context

F0034 needs dynamic product forms that render from schema metadata, validate in the browser, and stay visually consistent with the Nebula frontend. General-purpose schema form libraries would reduce initial coding but would make widget behavior, layout, theme coverage, and pin-during-edit behavior harder to govern.

## Decision

Nebula uses a custom dynamic form engine built on:
- React Hook Form for field state and controlled edit lifecycle.
- AJV for client-side JSON Schema validation.
- The existing shadcn-style component system and a local widget registry for rendering.

The engine renders `<DynamicAttributePanel>` for product attributes. The panel is embedded inside existing Submission, Policy, Endorsement, and Renewal screens; it is not a standalone product admin app.

The form binds to `(productVersionId, stage)` at open time and remains pinned to that version for the edit session. If a new product version is activated while a form is open, the current form keeps its original bundle. The user sees the new version only after reopening or starting a new workflow.

The widget registry is explicit. Each `ui.schema.json` widget name must map to a shipped frontend widget and option schema. Bundle activation fails if a bundle references an unknown widget, unknown option, or layout primitive that is not in the meta-schema.

The MVP widget vocabulary is intentionally narrow:
- text
- textarea
- number
- money-minor
- select
- multi-select
- checkbox
- date
- section
- read-only summary

Heavy domain widgets such as vehicle schedules or tower visualizers require a paired frontend deploy before a bundle can activate them.

Theme and accessibility coverage are part of the widget contract. Each widget must have light and dark theme smoke coverage and form-level keyboard/focus coverage before a bundle depending on it can be activated.

## Consequences

Positive:
- The form engine stays aligned with Nebula's existing frontend system.
- Data-schema-only product changes can ship without a frontend deploy when they use known widgets.
- Pin-during-edit avoids mid-session schema drift and validation surprises.

Negative:
- Nebula owns form rendering code and widget governance.
- New complex widgets require product, frontend, and schema-steward coordination.
- Form behavior must be covered by component, integration, and Playwright tests.

Invalid after this ADR:
- Using RJSF, JSONForms, Formily, or another schema form engine for F0034 product attributes without a replacement ADR.
- Letting activation introduce unknown widget names or UI options.
- Rebinding an open edit form to a newly activated product version.

## Plan Amendment — 2026-05-25 (F0036 reconciliation)

This ADR was **Accepted** on 2026-05-06 but never implemented: F0034 shipped a hardcoded Cyber `DynamicAttributePanel` (typed `CyberLobAttributeValues`, fixed JSX, option constants, lifted parent state) and never added `react-hook-form`/`ajv` as dependencies. F0036 realizes this ADR. The original Decision above stands; the clauses below reconcile it with the shipped bundle structure and record the integration decisions F0036's plan run surfaced. No competing engine is adopted — these are refinements, not a reversal.

### 1. `ui-schema.json` filename and shape

- **Filename:** the shipped bundle file is `ui-schema.json` (hyphenated), not `ui.schema.json` as the prose above reads. The engine reads `ui-schema.json`.
- **Shape:** the shipped Cyber `ui-schema.json` is **layout-only** — it carries `sections` (`id`, `title`, `fields[]`) and `fieldLabels`. It does **not** carry a per-field `widget` name or option schema. The "explicit widget registry rendering from `ui.schema.json`" clause is therefore refined: the **ui-schema supplies section grouping, field order, and labels**, while the **widget for each field is derived from the field's `data-schema.json` definition** (see §2). The widget registry remains explicit and fail-closed; only the *source of the widget selection* is clarified.

### 2. Widget derivation from `data-schema.json`

The engine derives each field's widget from its JSON Schema definition (type + enum + format + nesting), then renders it through the registry. For the Cyber `cyber/1.0.0` bundle:

| data-schema field | JSON Schema shape | Derived widget |
|---|---|---|
| `revenueBand` | `string` + `enum` | select |
| `recordsHeld` | `integer`, `minimum:0` | number |
| `controls.mfaEnabled` / `edrEnabled` / `backupEnabled` | `boolean` | checkbox |
| `controls.mfaMaturity` | nullable `string` + `enum` | select (nullable) |
| `controls.trainingFrequency` | `string` + `enum` | select |
| `requestedLimit` / `requestedRetention` | `object {amountMinor:int, currency:enum}` | money-minor |
| `sections[]` (from ui-schema) | layout primitive | section |

Unknown/underivable shapes fail closed (the registry has no entry), preserving ADR-021 governance.

### 3. Client/backend validation parity scope (ADR-022 + ADR-023)

"AJV for client-side JSON Schema validation" is necessary but not sufficient for the F0036 parity claim. The Cyber bundle also ships `rules.json` cross-field rules (e.g. `mfa_required_for_high_record_count`, `minimum_retention_not_met`), which plain AJV over `data-schema.json` does not evaluate. To reach **0-disagreement parity** with the backend on the published examples (the contract of **ADR-022 Validator Equivalence**), the client engine must evaluate **both** the data-schema (AJV) **and** the bundle's `rules.json` rules (per **ADR-023 Rules Governance / JsonLogic**). The backend validator remains authoritative on submit; the client layer is fast feedback only.

### 4. Conditional field gating

UI conditional behavior (e.g. MFA maturity is only meaningful when MFA is enabled) is not encoded in the shipped bundle. Until a bundle-level conditional primitive exists, the engine expresses such gating as an explicit, governed engine convention derived from schema relationships (not ad-hoc per-screen logic), and F0036-S0005 must preserve the panel's current observable behavior. A bundle-level conditional vocabulary is a future ADR candidate, not part of this amendment.

### 5. F0035 form-state preservation integration (no separate companion ADR)

F0036 wires RHF forms into the F0035 preservation registry (`capability:session-context-restore`, governed by **ADR-024**). A shared registration helper adapts an RHF form to the F0035 `DirtyFormRegistration` contract: `getValues()` → snapshot values, `formState.isDirty` → `isDirty()`, and `formState.dirtyFields` flattened to string paths → `getDirtyFieldPaths()`; it consumes a snapshot on mount via `consumeFormSnapshot`. This maps cleanly onto the existing F0035 API with **no F0035 change**. The F0035 operator-mandated **no-mutation-auto-replay** invariant is inherited unchanged. **Decision: no separate companion ADR is created** — the integration is API consumption already governed by ADR-024, and recording it here keeps the form-engine decision in one place rather than fragmenting it.

### 6. Workstream B — RHF for fixed-shape CRUD forms

F0036 also migrates the hand-rolled CRUD forms (broker, account, contact, task, submission native fields) to React Hook Form for field state, and registers them with F0035 preservation. These are **fixed-shape** forms: they use RHF for field state and the validation/submit lifecycle, but **do not** go through the AJV/widget-registry schema engine. This is consistent with this ADR's choice of RHF as Nebula's form-state library; it generalizes RHF + preservation to all in-scope mutation forms without extending the schema engine beyond product attributes.

## Framework References

Stage 1 framework work must produce or update:
- `agents/architect/references/dynamic-form-engine.md`
- `agents/templates/screen-spec-template.md`
- `agents/templates/api-contract-template.yaml`

