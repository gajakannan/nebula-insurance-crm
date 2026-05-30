# ADR-021: Dynamic Form Engine With RHF, AJV, and shadcn Widget Registry

**Status:** Accepted (Amended 2026-05-25; §3–§4 reworked 2026-05-26 after plan-review PR-C1/PR-H1 — see Plan Amendment)
**Date:** 2026-05-06
**Amended:** 2026-05-25 (F0036 — reconciled with the shipped F0034 implementation; plan run `2026-05-25-51ff2a92`); 2026-05-26 (§3 parity + §4 conditional gating reworked after plan-review `2026-05-26-aaa8bd7c`)
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

### 3. Client/backend validation parity scope — REWORKED 2026-05-26 (plan-review PR-C1)

The 2026-05-25 version of this clause said the client reaches parity by "evaluating `rules.json` per ADR-023." Plan-review `2026-05-26-aaa8bd7c` established that this is **not implementable**: the backend validates Cyber attributes **and** the cross-field rules in **hardcoded C#** (`engine/src/Nebula.Application/Services/LobAttributeService.cs`), `rules.json` is loaded by no code and does not conform to ADR-023's JsonLogic envelope, and ADR-022/023's portable schema/rules-driven validators are Accepted-but-unimplemented. F0036 is frontend-only ("no backend change"), so realizing ADR-023 is out of its scope. Reworked decision — two validation layers with distinct ownership:

1. **Structural (data-schema) layer — client-validated, parity-checked.** The client validates the form with AJV against the bundle's `data-schema.json` for immediate pre-submit feedback. This is the schema-driven core: the bundle's data-schema is the client's source of truth.
2. **Cross-field / contextual rules — backend-authoritative, displayed by the client.** The known Cyber rules (`mfa_required_for_high_record_count`, `minimum_retention_not_met`) and any future rules are evaluated by the **backend**; the client surfaces them from the `LobValidationProblemDetails.lobErrors[]` response (the ADR-022 envelope), binding each error to its field via `pointer`. The client does **not** maintain an authoritative duplicate of the rule logic.

**Parity is measured against the actual backend, not a shared rules artifact.** Per **ADR-022**, parity = multiset equality on normalized `(code, pointer)` over the Cyber published examples, asserted by a CI fixture matrix that runs each example through (a) the client validator and (b) the real backend endpoint (or recorded backend responses). This proves the client's schema-driven validation agrees with the backend's (currently hardcoded) validation **for the data-schema layer the client owns**. For cross-field rules there is no client evaluation to diverge — the backend is the sole evaluator and the client displays its `lobErrors[]`.

**Optional client pre-check (not a parity requirement):** the client MAY mirror the small known Cyber cross-field rule set purely for pre-submit UX, but only behind the same parity harness; the backend stays authoritative. This is explicitly a UX nicety, not a correctness dependency.

**F0036 does not depend on ADR-023.** ADR-023's portable JsonLogic platform (and a backend refactor to evaluate the bundle instead of hardcoded C#) remains the aspirational future for true client/backend rule sharing — a separate, larger initiative. F0036's KG binding is corrected to drop `adr:023` / `capability:lob-rules-governance`; it retains `capability:validator-equivalence` / **ADR-022**, which the parity harness directly realizes on the client side.

### 4. Conditional field gating — REWORKED 2026-05-26 (plan-review PR-H1)

The MFA-maturity conditional ("mfaMaturity required/enabled only when mfaEnabled") is hardcoded today in both the panel (`DynamicAttributePanel.tsx:135-136`) and the backend (`LobAttributeService.cs:210-211`), and **cannot** be expressed in the bundle: **ADR-022 forbids `if/then/else` and `dependentRequired` in bundle data schemas**, and the bundle is frozen ("no bundle change"). The earlier "governed engine convention derived from schema relationships" was unworkable because the relationship is not present in any schema. Reworked decision — split the conditional into its two halves:

- **Validation half ("mfaMaturity required when mfaEnabled")** is a cross-field rule → owned by the **backend** per §3 and surfaced via `lobErrors[]`. The engine needs no schema conditional for it.
- **Presentational half (disable the mfaMaturity widget and show its required-marker only when mfaEnabled)** is a **UI affordance, not validation**, and is therefore outside ADR-022's data-schema keyword ban. The engine consumes a small, declarative **UI-conditional map supplied at the LOB-adapter layer** — frontend engine configuration, versioned with the engine, **not** the bundle (so "no bundle change" holds). Each entry declares `{ fieldPath, enabledWhen: <predicate over current form values> }`; for the Cyber MVP this is exactly one entry (`controls.mfaMaturity` enabled when `controls.mfaEnabled === true`). The engine applies the map generically (enable/disable + required-marker), so there is **no ad-hoc per-field JSX** (satisfies the ADR's governance intent) and **no behavior regression** (it reproduces today's exact behavior).
- **Stopgap with a migration path:** the engine-level UI-conditional map is an explicit MVP stopgap. Its long-term home is a governed **bundle-level UI-conditional vocabulary** in `ui-schema` (a future ADR + a new bundle version); when that lands, the map migrates into the bundle and the engine config entry is removed. Recorded as a future-ADR candidate.

### 5. F0035 form-state preservation integration — library-agnostic (no separate companion ADR)

F0036 wires every in-scope mutation form into the F0035 preservation registry (`capability:session-context-restore`, governed by **ADR-024**) through one **library-agnostic** shared registration helper. F0035's `DirtyFormRegistration` contract (`experience/src/features/session-continuity/dirtyFormRegistryContext.ts`) is three plain function pointers — `isDirty()`, `getValues()`, `getDirtyFieldPaths()` — plus `formKey` and `route`. It is not RHF-specific.

The shared helper accepts a `DirtyFormRegistration`-shaped source from either backend:

- **RHF adapter (Workstream A — product-attribute engine):** `getValues()` → snapshot values, `formState.isDirty` → `isDirty()`, `formState.dirtyFields` flattened to string paths → `getDirtyFieldPaths()`.
- **Controlled-form tracker (Workstream B — see §6):** `useControlledDirtyTracker(values, initialValues)` returns the same three functions by deep-diffing current values against initial values; no field-state library is involved.

Both adapters consume a snapshot on mount via `consumeFormSnapshot` through the same code path, and the helper owns the `form_key` shape for both workstreams. This maps cleanly onto the existing F0035 API with **no F0035 change**. The F0035 operator-mandated **no-mutation-auto-replay** invariant is inherited unchanged. **Decision: no separate companion ADR is created** — the integration is API consumption already governed by ADR-024, and recording it here keeps the form-engine decision in one place rather than fragmenting it.

### 6. Workstream B — Controlled-form preservation adapter for CRUD forms — REWORKED 2026-05-27

**Earlier framing (superseded):** F0036 was originally scoped to also change the field-state mechanism for the hand-rolled CRUD forms (broker, account, contact, task, submission native fields, plus the ~11-component edit/create inventory in `F0036-S0007`), on the rationale that doing so would generalize the dynamic engine's RHF + preservation pattern to all in-scope mutation forms.

**Why the rework:** Two findings on 2026-05-27 made that framing the wrong default for fixed-shape forms:

1. **RHF is not required by F0035 preservation.** The `DirtyFormRegistration` contract is library-agnostic (see §5). A controlled-form dirty-tracker satisfies it with a single hook (≈30 LOC) — no `Controller` wrapping, no validation reshape, no submit-path reshape.
2. **The CRUD forms are fixed-shape and do not benefit from RHF in proportion to a rewrite's cost.** Their value lives in field count, validation rules, and submit/error mapping that already work; RHF's wins (field-level subscriptions, declarative validation surfaces, dev-tools) are real but small on a 3–8 field create modal, and changing the ~11-component inventory would be a broad regression surface (per-form pre/post regression tests, `Controller` wrappers across shadcn inputs, edit-modal `defaultValues`/`reset` semantics).

**Decision (2026-05-27):** The CRUD forms in the `F0036-S0007` Workstream B inventory **remain controlled**. F0036 ships a small `useControlledDirtyTracker(values, initialValues)` hook that produces the F0035 `DirtyFormRegistration` triple by diffing current values against initial values; each in-scope CRUD form passes this through the library-agnostic shared registration helper from §5. Field-state, validation, and submit semantics are unchanged on every CRUD form — the only added surface is the registration call and an initial-values reference.

**Why this is consistent with this ADR's RHF choice.** This ADR chose RHF as the **form-state library for the dynamic product-attribute engine** (§§1–4) where dynamic field count, AJV-validated data, and pin-during-edit lifecycle justify it. Generalizing that choice to every fixed-shape mutation form was an extrapolation made during F0036 planning, not a precondition of this ADR. The reworked §6 honors the original engine-scoped intent: RHF where dynamic complexity earns it; controlled where the form is fixed-shape.

**Scope unchanged on the F0035 closure side.** The ~11-component inventory in `F0036-S0007` and the F0035 finding #1 closure goal are unchanged. Only the mechanism on the CRUD side changes (controlled → adapter-registered, not controlled → RHF).

**Equality semantics for the tracker.** Deep value equality (not reference equality), per-field-path output, with a documented contract for arrays (element-wise), nested objects (recursive), and `undefined`/missing keys (treated equal). Sensitive-field exclusion is a path-list parameter (default-deny aligned with F0035's sensitive-field policy).

**Out of scope of this rework:** Adopting RHF later for a CRUD form that grows enough to need it is not prohibited; it would simply replace the controlled-form tracker with the RHF adapter for that form, through the same library-agnostic helper.

## Framework References

Stage 1 framework work must produce or update:
- `agents/architect/references/dynamic-form-engine.md`
- `agents/templates/screen-spec-template.md`
- `agents/templates/api-contract-template.yaml`
