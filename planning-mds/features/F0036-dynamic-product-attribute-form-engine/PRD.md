---
template: feature
version: 1.0
applies_to: product-manager
---

# F0036: Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)

**Feature ID:** F0036
**Feature Name:** Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)
**Priority:** High
**Phase:** Platform Foundation / CRM Release MVP Enabler
**Status:** Phase A draft pending approval

> **Folder note:** the folder slug remains `F0036-dynamic-product-attribute-form-engine` for link stability. The feature was broadened on 2026-05-25 from product-attributes-only to also migrate the hand-rolled CRUD forms onto React Hook Form, so the title now reads "Form Engine and Form-State Preservation."

## Feature Statement

**As a** product, underwriting operations, and frontend platform team
**I want** Nebula forms — both schema-driven LOB product attributes and the hand-rolled CRUD forms — to be managed by React Hook Form (with AJV + a widget registry for product attributes), and registered with F0035 form-state preservation
**So that** product attributes (Cyber first, then any LOB) render and validate from governed schema bundles, the existing CRUD forms behave consistently, and **all** in-scope mutation forms keep unsaved input across a forced re-auth (fully closing the F0035 form-preservation gap)

This feature has two workstreams:

- **Workstream A — Dynamic product-attribute engine (full ADR-021):** RHF + AJV + schema-driven widget registry for LOB product attributes; Cyber pilot.
- **Workstream B — CRUD form RHF migration + preservation:** migrate the existing hand-rolled CRUD forms to RHF (fixed-shape forms; no schema engine) and register them with F0035 so their dirty state is preserved too.

## Business Objective

- **Goal:** (A) Realize ADR-021's accepted dynamic form engine in code — F0034 shipped `DynamicAttributePanel` as a hardcoded Cyber panel with no RHF, AJV, or widget registry; and (B) fully close the F0035 form-preservation gap by migrating the hand-rolled CRUD forms to RHF and registering all in-scope mutation forms with F0035 preservation.
- **Metric:** (A) A new attribute that fits the approved widget vocabulary can be added to a LOB by publishing a schema-bundle version (data schema + `ui.schema.json`) and passing parity tests, with **no hand-written field components** added to `DynamicAttributePanel`. (B) 100% of in-scope mutation forms are RHF-managed and registered with the F0035 dirty-form registry, and survive a forced re-auth with values restored.
- **Baseline:** `experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx` is a hardcoded Cyber panel using controlled `value`/`onChange`/`errors` props and lifted parent state. `react-hook-form`, `ajv`, `ajv-formats`, and `ajv-errors` are not dependencies. The hand-rolled CRUD forms (broker, account, submission, contact, task) are plain controlled components. F0035 form-state preservation (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`) is wired to **zero** forms, so it preserves nothing for any form today.
- **Target:**
  - 100% of LOB product-attribute rendering for the Cyber pilot is driven by the schema bundle through the widget registry (no hardcoded field list).
  - Client-side AJV validation reaches parity with backend validation on the Cyber bundle's published examples (≥ 1 parity fixture matrix, 0 disagreements).
  - Every in-scope mutation form — the product-attribute form **and** the migrated CRUD forms — is registered with the F0035 dirty-form registry, so a forced re-auth on any dirty in-scope form restores the in-flight values on return (no auto-replay, per F0035).
  - F0035's S0003 canonical scenario (Contact Edit → "Notes" → forced re-auth → values restored) passes end-to-end.

## Problem Statement

- **Current State:** ADR-021 ("Dynamic Form Engine With RHF, AJV, and shadcn Widget Registry", **Accepted** 2026-05-06) decided that `<DynamicAttributePanel>` would use React Hook Form for field state, AJV for client validation, and an explicit shadcn-style widget registry rendering from `ui.schema.json`. F0034 instead shipped a hardcoded Cyber panel: typed `CyberLobAttributeValues`, fixed JSX fields, option constants in `lib/cyber.ts`, and a `useCyberSchemaBundle` call used only to display a status string. None of the ADR-021 engine exists in the frontend. Separately, the app's other mutation forms (broker, account, submission, contact, task) are hand-rolled controlled components. Because **no** form is RHF-managed, F0035's form-state preservation registry has nothing to attach to and preserves nothing for **any** form — including F0035's own S0003 canonical example (Contact Edit → "Notes").
- **Desired State:** (A) A schema-driven form engine renders product attributes from the governed bundle via the widget registry, manages field state with React Hook Form, validates in the browser with AJV against the same data schema the backend enforces, and pins to `(productVersionId, stage)` for the edit session. (B) The hand-rolled CRUD forms are migrated to React Hook Form (fixed-shape; no schema engine). (A) and (B) both register with the F0035 dirty-form registry so unsaved input on any in-scope form survives a forced re-auth.
- **Impact:** Today, adding or changing a product attribute requires hand-editing `DynamicAttributePanel`, defeating the purpose of the F0034 registry; the ADR-021 record is "Accepted" but unimplemented, which already misled F0035 planning; and users lose unsaved input on **every** mutation form on any session boundary — the exact symptom F0035 was meant to remove.

## Scope & Boundaries

**In Scope:**

_Workstream A — Dynamic product-attribute engine (full ADR-021):_

- Adopting `react-hook-form`, `ajv`, `ajv-formats`, and `ajv-errors` as pinned (non-caret) frontend dependencies.
- A schema-driven dynamic form engine that renders product attributes from a bundle's data schema + `ui.schema.json` through an explicit widget registry.
- The ADR-021 MVP widget vocabulary: text, textarea, number, money-minor, select, multi-select, checkbox, date, section, read-only summary.
- React Hook Form field-state management (`useForm`, `formState.isDirty`, `getValues`, dirty-field tracking) for the product-attribute form.
- Client-side AJV validation with backend parity on the Cyber bundle examples.
- Pin-during-edit: the form binds to `(productVersionId, stage)` at open and stays pinned for the session (ADR-021).
- Replacing the hardcoded Cyber `DynamicAttributePanel` with the engine, with no behavior regression on the five consuming screens (Create Submission, Create Policy, Policy Detail, Renewal Detail, Submission Detail).

_Workstream B — CRUD form RHF migration + preservation:_

- Migrating the hand-rolled CRUD mutation forms to React Hook Form: broker (`EditBrokerModal`, `CreateBrokerPage`), account (`CreateAccountPage`), contact (`ContactFormModal`), task (`TaskCreateModal`), and the native (non-attribute) fields on `CreateSubmissionPage`. Final form inventory confirmed at Phase A. These are fixed-shape forms migrated to RHF for field state only — **no AJV/widget-registry schema engine**.
- A shared registration helper so any RHF form (product-attribute or CRUD) registers with the F0035 dirty-form registry (`useSessionRestorableForm`) and rehydrates via snapshot restore (`consumeFormSnapshot`) on mount.

_Both workstreams:_

- End-to-end F0035 form-state preservation: a forced re-auth on any dirty in-scope form restores values on return (no auto-replay), including F0035's S0003 Contact Edit canonical scenario.

**Out of Scope:**

- Putting CRUD forms through the schema-driven AJV/widget-registry engine — they are fixed-shape and use RHF for field state only.
- Heavy domain widgets beyond the ADR-021 MVP vocabulary (vehicle schedules, tower visualizers).
- A standalone product/admin form builder UI.
- Changing the backend schema-bundle registry, the `LobSchemaBundle` entity, or `lob-schema-bundle.schema.json` (consumed as-is; backend validation remains authoritative).
- Adding new LOBs beyond Cyber (the engine must be LOB-agnostic, but only Cyber is activated and validated in this feature).
- Changing F0035 session-continuity behavior; F0036 only consumes its registry/restore API.
- Non-mutation/filter-only forms (e.g., list filters) where there is no in-flight state worth preserving — confirmed during the Phase A form inventory.

## Personas & Jobs

| Persona | Job To Be Done | Why It Matters |
|---------|----------------|----------------|
| Underwriter | When I enter Cyber attributes on a submission or policy, I want the form to behave like the rest of Nebula and not lose my input if my session blips. | Product-attribute entry is the core of the F0034/F0019 workflow; lost input and inconsistent behavior erode trust. |
| Schema Steward | When I publish a new attribute that uses an approved widget, I want it to render without a frontend code change. | This is the entire value proposition of the F0034 registry; the hardcoded panel currently blocks it. |
| Frontend Platform Engineer | When I build new product forms, I want one governed engine (RHF + AJV + widget registry) rather than bespoke panels. | A single engine is what ADR-021 decided; divergence creates maintenance and governance drift. |
| Architect | When an ADR is "Accepted", I want the code to match it, so downstream features can safely plan on it. | ADR-021 drift already caused F0035 to build form preservation against a non-existent engine. |

## Success Criteria

- The Cyber product-attribute form renders entirely from the schema bundle through the widget registry; no Cyber-specific field JSX or option constants remain in the rendering path.
- Each widget in the ADR-021 MVP vocabulary has a registry entry, light/dark theme smoke coverage, and keyboard/focus coverage (widget contract per ADR-021).
- Client AJV validation agrees with backend validation on the Cyber bundle's published examples (parity fixture matrix, 0 disagreements).
- The form is React Hook Form-managed: `formState.isDirty` and `getValues()` are real, and dirty-field paths are available for snapshot restore.
- Pin-during-edit holds: activating a new product version while a form is open does not rebind the open form.
- The five consuming screens (Create Submission, Create Policy, Policy Detail, Renewal Detail, Submission Detail) show no regression in create/edit/read-only behavior.
- F0035 form-state preservation works end-to-end for the Cyber form: a forced re-auth on a dirty product-attribute form restores the in-flight values on return, and the user must explicitly re-save (no auto-replay, per F0035).
- Bundle activation still fails closed on unknown widget, unknown option, or unknown layout primitive (ADR-021 governance preserved).
- The hand-rolled CRUD mutation forms (broker, account, contact, task, submission native fields) are React Hook Form-managed and registered with the F0035 dirty-form registry, with no behavior regression in their create/edit flows.
- F0035 form-state preservation works end-to-end for the CRUD forms: a forced re-auth on any dirty in-scope CRUD form restores values on return. F0035's S0003 canonical scenario (Contact Edit → "Notes" dirty → forced re-auth → values restored → explicit re-save) passes.
- After F0036, F0035 finding #1 is fully closed: there are no in-scope mutation forms that lose unsaved input on a forced re-auth.

## Product Requirements

1. **Engine Dependencies**
   - Add `react-hook-form`, `ajv`, `ajv-formats`, `ajv-errors` as exact, non-caret versions, consistent with the F0034 assembly-plan intent that was never executed.

2. **Schema-Driven Rendering via Widget Registry**
   - The engine renders product attributes from the bundle data schema + `ui.schema.json`. Each `ui.schema.json` widget name maps to exactly one shipped widget and its option schema. Unknown widget/option/layout primitives fail closed (bundle activation already enforces this; the frontend must not silently render around an unknown widget).

3. **MVP Widget Vocabulary**
   - Implement: text, textarea, number, money-minor, select, multi-select, checkbox, date, section, read-only summary. Each has a registry entry and meets the widget contract (theme + a11y coverage).

4. **React Hook Form Field State**
   - The product-attribute form uses React Hook Form for field state and the controlled edit lifecycle. `formState.isDirty`, `getValues()`, and dirty-field paths are exposed for F0035 snapshotting.

5. **Client-Side AJV Validation with Backend Parity**
   - AJV validates the form against the bundle data schema in the browser. Validation outcomes match the backend validator on the Cyber bundle examples. Backend validation remains authoritative on submit.

6. **Pin-During-Edit**
   - The form binds to `(productVersionId, stage)` at open and stays pinned for the session. A newly activated product version does not rebind an open form (ADR-021).

7. **Replace Hardcoded Cyber Panel Without Regression**
   - `DynamicAttributePanel` is reimplemented on the engine. Create, edit, and read-only behavior on the five consuming screens is preserved. Cyber field semantics (revenue band, records held, requested limit/retention, MFA enabled + maturity gating, EDR, offline backups, training frequency) are expressed through the schema bundle, not hardcoded JSX.

8. **F0035 Form-State Preservation Integration**
   - The product-attribute form registers with F0035's dirty-form registry via `useSessionRestorableForm` and consumes snapshots on mount via `consumeFormSnapshot`. A forced re-auth with a dirty Cyber form restores values on return; the mutation is not auto-replayed (F0035 contract). Oversize/skip cases follow F0035's `form-snapshot-skipped` behavior.

9. **Governance & Test Coverage**
   - Component, integration, and Playwright coverage per ADR-021, including: widget-registry rendering, AJV/backend parity, pin-during-edit, the five-screen regression surface, and the F0035 forced-re-auth dirty-form restore journey (the end-to-end test F0035 could not provide because no RHF form existed) — for both a product-attribute form and a CRUD form.

10. **CRUD Form RHF Migration (Workstream B)**
    - Migrate the hand-rolled CRUD mutation forms to React Hook Form for field state: broker (`EditBrokerModal`, `CreateBrokerPage`), account (`CreateAccountPage`), contact (`ContactFormModal`), task (`TaskCreateModal`), and the native fields on `CreateSubmissionPage`. The final inventory is confirmed at Phase A. These remain fixed-shape forms — RHF for field state and validation lifecycle only; they do **not** go through the AJV/widget-registry schema engine. Existing validation behavior and submit semantics must be preserved.

11. **Shared Preservation Registration (Workstream B)**
    - Provide one shared registration helper (built on F0035's `useSessionRestorableForm`) used by both the product-attribute form and the migrated CRUD forms so each registers a stable `form_key`, exposes `isDirty`/`getValues`/dirty-field paths, and rehydrates via `consumeFormSnapshot` on mount. A forced re-auth on any dirty in-scope form restores values; the mutation is never auto-replayed (F0035 contract); oversize/skip cases follow F0035's `form-snapshot-skipped` behavior.

## Screen Layouts (ASCII)

The engine replaces the rendering internals of the existing `DynamicAttributePanel`; the panel's placement inside Submission/Policy/Renewal screens is unchanged. Layout is driven by `ui.schema.json` `section` primitives.

### Dynamic Product Attribute Panel — Cyber (schema-driven)

```
+--------------------------------------------------------------+
| Cyber attributes                         Bundle 1.0.0 · Active|
+--------------------------------------------------------------+
| [section: Exposure]                                          |
|   Revenue band      [ select v ]   Records held   [ number ] |
|   Requested limit   [ money     ]  Requested ret. [ money  ] |
|                                                              |
| [section: Controls]                                          |
|   MFA enabled       [x]            MFA maturity   [ select v]|  <- maturity enabled only when MFA enabled
|   EDR enabled       [ ]            Offline backups[ ]        |
|   Training freq.    [ select v ]                             |
|                                                              |
|   (validation messages render inline per field via AJV)      |
+--------------------------------------------------------------+
```

Rendering, field order, sections, and which widgets appear are all determined by the pinned bundle's `ui.schema.json`, not by component code.

## UX Notes

- Visual output for the Cyber pilot should match the current panel closely enough that underwriters see no disruptive change; the difference is that the form is now schema-driven and RHF-managed underneath.
- Inline validation messages come from AJV (with `ajv-errors` for friendly messages) and must align with backend validation semantics.
- Conditional enable/disable (e.g., MFA maturity gated on MFA enabled) is expressed via schema/`ui.schema.json` rules, not ad-hoc component logic.
- The form is the F0035 snapshot surface: when dirty and a forced re-auth occurs, values are preserved and restored on return; the Save button returns enabled, and the user must explicitly re-save.

## Dependencies

- **F0034 Product Schema Registry and Dynamic LOB Attributes** — provides the backend schema-bundle registry, `LobSchemaBundle`, `lob-schema-bundle.schema.json`, and the Cyber `cyber/1.0.0` bundle this engine renders.
- **ADR-021 Dynamic Form Engine (RHF + AJV + shadcn widget registry)** — the accepted decision F0036 realizes.
- **F0035 Session Continuity & Token Refresh** — provides `dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`; F0036 wires real forms into it.
- **F0019 Submission Quoting, Proposal & Approval** (consumer; in `Now`) — depends on dependable product-attribute entry.

## Risks & Assumptions

- **Risk:** Schema-driven rendering changes Cyber field behavior subtly and regresses the five consuming screens. **Mitigation:** Express the current Cyber semantics exactly in the bundle; add a regression test per screen before swapping the panel internals.
- **Risk:** Client AJV and backend validator diverge. **Mitigation:** Parity fixture matrix on the published Cyber examples; backend remains authoritative on submit.
- **Risk:** F0035 `DirtyFormRegistration` shape assumes RHF idioms (`getDirtyFieldPaths`) — confirm it maps cleanly onto the RHF engine. **Mitigation:** Adapter in the engine's registration hook; Architect to confirm in Phase B.
- **Risk:** ADR-021 is "Accepted" but unbuilt; realizing it may surface decisions ADR-021 left implicit (e.g., exact `ui.schema.json` conditional-rule semantics). **Mitigation:** Architect updates/extends ADR-021 (or adds a companion ADR) in Phase B rather than improvising in code.
- **Risk:** Migrating the CRUD forms to RHF (Workstream B) is a broad regression surface across broker/account/contact/task/submission create-and-edit flows. **Mitigation:** Migrate one form at a time behind existing tests; add a per-form create/edit regression test before and after migration; keep submit/validation semantics identical.
- **Risk:** CRUD forms and the product-attribute form need a consistent `form_key` and dirty-state contract, or restore mis-targets a form. **Mitigation:** One shared registration helper (Requirement 11) owns key shape and the `isDirty`/`getValues`/dirty-path contract for both workstreams; cross-user keying already handled by F0035.
- **Assumption:** The backend Cyber bundle already carries (or can carry) a `ui.schema.json` sufficient for the MVP widget vocabulary. To be confirmed in Phase B preflight.
- **Assumption:** No backend or database change is required; this is a frontend feature consuming existing F0034 and F0035 contracts.
- **Assumption:** RHF is an acceptable field-state library for the fixed-shape CRUD forms (not only product attributes); this aligns with ADR-021's choice of RHF as Nebula's form-state library.

## Related User Stories (finalized at Phase A — plan run `2026-05-25-51ff2a92`)

> All eight stories are colocated in this folder as `F0036-S####-{slug}.md` and pass `validate-stories.py`.


_Workstream A — Dynamic product-attribute engine (full ADR-021):_

- F0036-S0001 — Adopt RHF + AJV dependencies and engine skeleton with widget-registry contract
- F0036-S0002 — Implement MVP widget vocabulary with theme + a11y coverage
- F0036-S0003 — Schema-driven rendering + AJV client validation with backend parity (Cyber)
- F0036-S0004 — Pin-during-edit binding to `(productVersionId, stage)`
- F0036-S0005 — Replace hardcoded Cyber `DynamicAttributePanel` with the engine (five-screen regression)
- F0036-S0006 — Wire product-attribute form into F0035 dirty-form registry + restore (end-to-end forced-re-auth journey)

_Workstream B — CRUD form RHF migration + preservation:_

- F0036-S0007 — Shared preservation registration helper + migrate the CRUD mutation forms to React Hook Form (broker, account, contact, task, submission native fields)
- F0036-S0008 — Register migrated CRUD forms with F0035 preservation + restore on mount; close F0035 S0003 Contact Edit canonical scenario end-to-end

## Phase A Clarification Resolution (plan run `2026-05-25-51ff2a92`)

Requirements clarification gate (G1) outcome. The PRD was already broadened by the operator on 2026-05-25; the items below were confirmed against the codebase or explicitly deferred to Phase B architecture (deferral is recorded, not left ambiguous).

**Resolved at Phase A (PM):**

1. **Workstream B inventory locked (6 surfaces).** Verified to exist 2026-05-25: broker (`EditBrokerModal`, `CreateBrokerPage`), account (`CreateAccountPage`), contact (`ContactFormModal`), task (`TaskCreateModal`), and the native fields of `CreateSubmissionPage`. `CreatePolicyPage` hosts the Workstream A attribute panel and is **not** a separate CRUD-migration target. Edit variants served by the same component (e.g. `ContactFormModal` for create+edit) are covered by migrating that component.
2. **F0035 API confirmed compatible.** The shipped F0035 surface exports `useSessionRestorableForm`, `consumeFormSnapshot`, and a `DirtyFormRegistration` requiring `formKey`/`route`/`isDirty`/`getValues`/`getDirtyFieldPaths`. RHF supplies `getValues()` and `formState.dirtyFields` (flattened to paths), so it maps cleanly — closing PRD Risk #3. No F0035 change required; the adapter lives in the shared registration helper (S0007).
3. **No-auto-replay invariant inherited.** F0035's operator-mandated no-mutation-auto-replay rule applies to every preserved form (S0006, S0008); the user always re-saves explicitly.

**Deferred to Phase B (Architecture) — these are design decisions, not open PM requirements:**

1. **Widget derivation source.** The shipped Cyber `ui-schema.json` is layout-only (`sections` + `fieldLabels`) — it carries no per-field widget map, unlike ADR-021's prose. Phase B records the data-schema type/enum/format → widget derivation in the amended ADR-021.
2. **AJV parity scope includes `rules.json`.** Plain AJV over `data-schema.json` does not cover the bundle's cross-field rules (`mfa_required_for_high_record_count`, `minimum_retention_not_met`). To claim 0-disagreement backend parity the client must also evaluate `rules.json`; Phase B fixes that contract.
3. **Conditional gating mechanism.** MFA-maturity-enabled-when-MFA-enabled is not encoded in the shipped bundle; Phase B decides the engine convention (S0005 preserves today's observable behavior regardless).
4. **`ui-schema.json` filename.** Shipped name is hyphenated; ADR-021 prose says `ui.schema.json`. Phase B amends the ADR to the shipped name.

## Architecture Traceability (Architect Phase B)

- Primary governing decision: **ADR-021** (`planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md`). Phase B must reconcile ADR-021's "Accepted" status with reality — either confirm F0036 implements it as written or amend it where F0034 intentionally diverged — and decide whether a companion ADR is needed for the F0035 form-preservation integration contract. Phase B should also confirm that Workstream B (RHF for fixed-shape CRUD forms, no schema engine) is consistent with ADR-021's choice of RHF as Nebula's form-state library, and record that CRUD forms deliberately do not use AJV/widget-registry rendering.
- Consumes F0034 contracts (`LobSchemaBundle`, `lob-schema-bundle.schema.json`, Cyber bundle) and F0035 contracts (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`).
- The `feature-assembly-plan.md` is owned by the feature action at Step 0 (per `agents/actions/plan.md` Deliverables Contract), not by Phase A/B.

### Phase B Outcome (Architect — plan run `2026-05-25-51ff2a92`)

- **ADR-021 amended** (2026-05-25, reconciliation section): the ADR was Accepted-but-unimplemented; F0036 realizes it. The amendment records (1) the shipped `ui-schema.json` filename + layout-only shape, (2) the data-schema→widget derivation table, (3) the parity scope (AJV over `data-schema.json` **plus** client evaluation of `rules.json`), (4) the conditional-gating convention, (5) the F0035 preservation integration adapter, and (6) Workstream B RHF-for-CRUD.
- **No separate companion ADR.** The F0035 form-preservation integration is API consumption already governed by **ADR-024**; recording it inside the ADR-021 amendment avoids ADR fragmentation.
- **Governing ADRs:** ADR-021 (primary, amended), ADR-020 (LOB extensible attributes), ADR-022 (validator equivalence — defines parity), ADR-023 (rules governance / JsonLogic — `rules.json`), ADR-024 (session continuity / form preservation).
- **No backend, schema, or bundle change.** The Cyber bundle and `lob-schema-bundle.schema.json` are consumed as-is; backend validation stays authoritative. No new OpenAPI contract or JSON Schema is introduced by F0036.
- **Ontology bindings completed:** `feature-mappings.yaml` `feature:F0036` moved from `coverage.excluded_features` into `features[]` with `affects` (`capability:dynamic-attribute-panel`, `capability:dynamic-lob-attributes`, `capability:validator-equivalence`, `capability:lob-rules-governance`, `capability:session-context-restore`), `governed_by` (adr:020/021/022/023/024), `uses_schema`, `uses_api_contract` (`api:nebula-rest`), and `depends_on` (F0034, F0035, F0019). **No new canonical nodes** — F0036 reuses and generalizes existing shared semantics.
