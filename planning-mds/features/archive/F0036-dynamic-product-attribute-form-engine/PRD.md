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
**Status:** Plan complete (A1+B2 approved); plan-review rework applied for both rounds (`aaa8bd7c`, `378ac7da`), pending re-confirmation

> **Folder note:** the folder slug remains `F0036-dynamic-product-attribute-form-engine` for link stability. The feature was broadened on 2026-05-25 from product-attributes-only to also close the F0035 form-preservation gap on the hand-rolled CRUD forms, so the title now reads "Form Engine and Form-State Preservation." The 2026-05-27 scope refinement narrowed Workstream B from a field-state-library rewrite to a controlled-form preservation adapter — the CRUD forms stay controlled; only the dynamic product-attribute engine uses RHF (see Phase A clarification → 2026-05-27 scope refinement).

## Feature Statement

**As a** product, underwriting operations, and frontend platform team
**I want** the schema-driven LOB product-attribute form to use React Hook Form + AJV + a widget registry, and **every** in-scope mutation form — including the existing hand-rolled CRUD forms — registered with F0035 form-state preservation
**So that** product attributes (Cyber first, then any LOB) render and validate from governed schema bundles, and **all** in-scope mutation forms keep unsaved input across a forced re-auth (fully closing the F0035 form-preservation gap) **without rewriting the existing CRUD forms**

This feature has two workstreams:

- **Workstream A — Dynamic product-attribute engine (full ADR-021):** RHF + AJV + schema-driven widget registry for LOB product attributes; Cyber pilot.
- **Workstream B — CRUD form preservation via controlled-form adapter:** keep the hand-rolled CRUD forms as controlled components (no field-state-library rewrite), and register them with F0035 through a small **controlled-form dirty-tracker adapter** so their dirty state is preserved too. RHF stays scoped to the dynamic engine where its complexity is load-bearing; fixed-shape CRUD forms gain preservation without a rewrite.

## Business Objective

- **Goal:** (A) Realize ADR-021's accepted dynamic form engine in code — F0034 shipped `DynamicAttributePanel` as a hardcoded Cyber panel with no RHF, AJV, or widget registry; and (B) fully close the F0035 form-preservation gap by registering every in-scope mutation form with F0035 preservation through a library-agnostic registration helper — the product-attribute form via its RHF adapter, the existing controlled CRUD forms via a controlled-form dirty-tracker adapter (no CRUD field-state rewrite).
- **Metric:** (A) A new attribute that fits the approved widget vocabulary can be added to a LOB by publishing a schema-bundle version (data schema + `ui.schema.json`) and passing parity tests, with **no hand-written field components** added to `DynamicAttributePanel`. (B) 100% of in-scope mutation forms are registered with the F0035 dirty-form registry and survive a forced re-auth with values restored — without rewriting any CRUD form's field-state mechanism.
- **Baseline:** `experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx` is a hardcoded Cyber panel using controlled `value`/`onChange`/`errors` props and lifted parent state. `react-hook-form`, `ajv`, `ajv-formats`, and `ajv-errors` are not dependencies. The hand-rolled CRUD forms (broker, account, submission, policy, renewal, contact, task — exhaustive inventory in S0007) are plain controlled components. F0035 form-state preservation (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`) is wired to **zero** forms, so it preserves nothing for any form today.
- **Target:**
  - 100% of LOB product-attribute rendering for the Cyber pilot is driven by the schema bundle through the widget registry (no hardcoded field list).
  - Client-side AJV validation reaches parity with backend validation on the Cyber bundle's published examples (≥ 1 parity fixture matrix, 0 disagreements).
  - Every in-scope mutation form — the product-attribute form **and** the existing controlled CRUD forms — is registered with the F0035 dirty-form registry, so a forced re-auth on any dirty in-scope form restores the in-flight values on return (no auto-replay, per F0035).
  - F0035's S0003 canonical scenario (Contact Edit → "Notes" → forced re-auth → values restored) passes end-to-end.

## Problem Statement

- **Current State:** ADR-021 ("Dynamic Form Engine With RHF, AJV, and shadcn Widget Registry", **Accepted** 2026-05-06) decided that `<DynamicAttributePanel>` would use React Hook Form for field state, AJV for client validation, and an explicit shadcn-style widget registry rendering from `ui.schema.json`. F0034 instead shipped a hardcoded Cyber panel: typed `CyberLobAttributeValues`, fixed JSX fields, option constants in `lib/cyber.ts`, and a `useCyberSchemaBundle` call used only to display a status string. None of the ADR-021 engine exists in the frontend. Separately, the app's other mutation forms (broker, account, submission, policy, renewal, contact, task — exhaustive inventory in S0007) are hand-rolled controlled components. F0035's form-state preservation registry is wired to zero forms, so it preserves nothing for **any** form today — including F0035's own S0003 canonical example (Contact Edit → "Notes"). F0035's `DirtyFormRegistration` contract is three plain functions (`isDirty`/`getValues`/`getDirtyFieldPaths`), so RHF is not a precondition for preservation; it was wired to no forms simply because no form yet held its own dirty bookkeeping in a registry-compatible shape.
- **Desired State:** (A) A schema-driven form engine renders product attributes from the governed bundle via the widget registry, manages field state with React Hook Form, validates in the browser with AJV against the same data schema the backend enforces, and pins to `(productVersionId, stage)` for the edit session. (B) The hand-rolled CRUD forms **stay controlled** and gain a small `useControlledDirtyTracker` adapter that exposes the F0035 `DirtyFormRegistration` contract. (A) and (B) both register through a single library-agnostic registration helper, so unsaved input on any in-scope form survives a forced re-auth.
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

_Workstream B — CRUD form preservation via controlled-form adapter:_

- A small `useControlledDirtyTracker` hook for plain controlled forms: given `(values, initialValues)`, it produces `isDirty()`, `getValues()`, and `getDirtyFieldPaths()` (via a shallow/recursive diff against `initialValues`) — the exact `DirtyFormRegistration` contract F0035 expects, with no field-state library involved.
- A **library-agnostic** shared registration helper used by both workstreams: Workstream A passes an RHF adapter (`isDirty`/`getValues`/dirty-paths sourced from `useForm`); Workstream B passes the controlled-form tracker. Both call `useSessionRestorableForm` and rehydrate via `consumeFormSnapshot` on mount through the same code path.
- Wiring each in-scope CRUD form to register through the helper. The **exhaustive, classified inventory** (with explicit out-of-scope exclusions) lives in `F0036-S0007` → *Workstream B inventory*: broker (`EditBrokerModal`, `CreateBrokerPage`), account create + edit (`CreateAccountPage`, `AccountDetailPage` incl. account-contact edit), contact (`ContactFormModal`), task create + inline edit (`TaskCreateModal`, `TaskDetailPanel`), submission create + edit native fields (`CreateSubmissionPage`, `SubmissionDetailPage`), policy create native fields (`CreatePolicyPage`), and renewal create (`RenewalsPage` modal) — ~11 components. The 2026-05-26 PR-M1 sweep expanded the original create-centric list of 6 to ~11 by adding the missed edit surfaces. **The forms remain controlled; only their registration is added.** No field-state library change, no validation reshape, no submit-path reshape.

_Both workstreams:_

- End-to-end F0035 form-state preservation: a forced re-auth on any dirty in-scope form restores values on return (no auto-replay), including F0035's S0003 Contact Edit canonical scenario.

**Out of Scope:**

- **Changing the CRUD forms' field-state library.** They stay controlled — RHF is not required by the F0035 contract and brings no benefit proportional to the rewrite cost on fixed-shape forms. RHF is scoped to Workstream A.
- Putting CRUD forms through the schema-driven AJV/widget-registry engine — they are fixed-shape.
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
| Frontend Platform Engineer | When I build new product forms, I want one governed engine (RHF + AJV + widget registry) rather than bespoke panels; and for the existing fixed-shape CRUD forms I want preservation without a rewrite. | A single engine is what ADR-021 decided for product attributes; for fixed-shape CRUD, a thin controlled-form adapter avoids a rewrite while still closing the F0035 gap. |
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
- The hand-rolled CRUD mutation forms — the exhaustive ~11-component create+edit inventory in S0007 (broker create/edit, account create/edit incl. account-contact, contact create/edit, task create + inline edit, submission create/edit native fields, policy create native fields, renewal create) — remain controlled components and are registered with the F0035 dirty-form registry through the controlled-form dirty-tracker adapter, with no behavior regression and no field-state library change in their create/edit flows.
- F0035 form-state preservation works end-to-end for the CRUD forms: a forced re-auth on any dirty in-scope CRUD form restores values on return. F0035's S0003 canonical scenario (Contact Edit → "Notes" dirty → forced re-auth → values restored → explicit re-save) passes.
- After F0036, F0035 finding #1 is fully closed: there are no in-scope mutation forms that lose unsaved input on a forced re-auth.
- The shared registration helper is library-agnostic: the RHF adapter (Workstream A) and the controlled-form tracker (Workstream B) plug into the same `useSessionRestorableForm` call path, so future forms can pick either backend without churning the registry contract.

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

10. **Controlled-Form Dirty-Tracker Adapter (Workstream B)**
    - Provide a `useControlledDirtyTracker` hook for plain controlled forms. Given `(values, initialValues)` (and an optional path-list for sensitive-field exclusion), it produces `isDirty()`, `getValues()`, and `getDirtyFieldPaths()` — the exact `DirtyFormRegistration` contract from F0035 — by shallow/recursive diff against `initialValues`. The hook must work for the full Workstream B inventory: broker (`CreateBrokerPage`, `EditBrokerModal`), account (`CreateAccountPage`, `AccountDetailPage` incl. account-contact edit), contact (`ContactFormModal` create+edit), task (`TaskCreateModal`, `TaskDetailPanel` inline edit), submission native fields (`CreateSubmissionPage`, `SubmissionDetailPage`), policy native fields (`CreatePolicyPage`), and renewal create (`RenewalsPage` modal) — the ~11-component exhaustive inventory in `F0036-S0007` (the 2026-05-26 PR-M1 sweep expanded the original 6 to ~11; operator confirmed the full set with no deferrals on 2026-05-27). **Forms remain controlled; no field-state library is introduced, no validation is reshaped, and no submit semantics change.**

11. **Library-Agnostic Shared Registration Helper (both workstreams)**
    - Provide one shared registration helper (built on F0035's `useSessionRestorableForm`) that consumes a `DirtyFormRegistration`-shaped source from **either** backend: an RHF adapter (Workstream A — `getValues()`, `formState.isDirty`, `formState.dirtyFields` flattened to paths) or the controlled-form tracker (Workstream B — Requirement 10). The helper owns the `form_key` shape, the dirty-path contract, and snapshot rehydration via `consumeFormSnapshot` on mount, for both workstreams. A forced re-auth on any dirty in-scope form restores values; the mutation is never auto-replayed (F0035 contract); oversize/skip cases follow F0035's `form-snapshot-skipped` behavior.

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
- **Risk:** The controlled-form dirty-tracker (Workstream B) might disagree with RHF on edge cases — e.g. an array reordered to the same contents, a number typed and re-typed, a "touched but reset to initial" field — and either over-snapshot or miss dirtiness. **Mitigation:** Define equality semantics explicitly (deep value equality, not reference equality) and exercise them on every form in the inventory through a per-form dirty/clean state matrix (typed-and-cleared, edited-then-reset, array-reorder, identical-object-replacement); the matrix is the unit-test contract for `useControlledDirtyTracker`.
- **Risk:** Adding registration to a CRUD form accidentally changes its render or submit behavior (e.g. a stale `initialValues` ref re-marks the form dirty after restore, or registration unmounts/remounts at the wrong time). **Mitigation:** The registration helper is render-side only (no submit/network change); each in-scope CRUD form gets a smoke regression test that creates and edits an entity end-to-end before and after the registration is added.
- **Risk:** CRUD forms and the product-attribute form need a consistent `form_key` and dirty-state contract, or restore mis-targets a form. **Mitigation:** One shared registration helper (Requirement 11) owns key shape and the `isDirty`/`getValues`/dirty-path contract for both workstreams; cross-user keying already handled by F0035.
- **Assumption:** The backend Cyber bundle already carries (or can carry) a `ui.schema.json` sufficient for the MVP widget vocabulary. To be confirmed in Phase B preflight.
- **Assumption:** No backend or database change is required; this is a frontend feature consuming existing F0034 and F0035 contracts.
- **Assumption:** A controlled-form dirty-tracker can faithfully reproduce the `DirtyFormRegistration` contract for the fixed-shape CRUD forms — i.e. F0035's contract is library-agnostic (three function pointers in `dirtyFormRegistryContext.ts`) and does not require RHF. Verified by inspection 2026-05-27; the contract is `{ formKey, route, isDirty, getValues, getDirtyFieldPaths }`.

## Related User Stories (finalized at Phase A — plan run `2026-05-25-51ff2a92`)

> All eight stories are colocated in this folder as `F0036-S####-{slug}.md` and pass `validate-stories.py`.


_Workstream A — Dynamic product-attribute engine (full ADR-021):_

- F0036-S0001 — Adopt RHF + AJV dependencies and engine skeleton with widget-registry contract
- F0036-S0002 — Implement MVP widget vocabulary with theme + a11y coverage
- F0036-S0003 — Schema-driven rendering + AJV client validation with backend parity (Cyber)
- F0036-S0004 — Pin-during-edit binding to `(productVersionId, stage)`
- F0036-S0005 — Replace hardcoded Cyber `DynamicAttributePanel` with the engine (five-screen regression)
- F0036-S0006 — Wire product-attribute form into F0035 dirty-form registry + restore (end-to-end forced-re-auth journey)

_Workstream B — CRUD form preservation via controlled-form adapter:_

- F0036-S0007 — Controlled-form dirty-tracker (`useControlledDirtyTracker`) + library-agnostic shared registration helper; wire the ~11-component CRUD inventory (broker, account incl. account-contact, contact, task create + inline edit, submission native, policy create native, renewal create) to register through it — **without changing the CRUD field-state mechanism**
- F0036-S0008 — Register the controlled CRUD forms with F0035 preservation + restore on mount through the adapter; close F0035 S0003 Contact Edit canonical scenario end-to-end

## Phase A Clarification Resolution (plan run `2026-05-25-51ff2a92`)

Requirements clarification gate (G1) outcome. The PRD was already broadened by the operator on 2026-05-25; the items below were confirmed against the codebase or explicitly deferred to Phase B architecture (deferral is recorded, not left ambiguous).

**Resolved at Phase A (PM):**

1. **Workstream B inventory.** _(Phase A locked 6 create-centric surfaces; **superseded by the 2026-05-26 PR-M1 exhaustive sweep** — the authoritative in-scope inventory is the ~11-component create+edit set in `F0036-S0007` → *Workstream B inventory*, operator-confirmed with no deferrals 2026-05-27.)_ The original Phase A list (broker `EditBrokerModal`/`CreateBrokerPage`, account `CreateAccountPage`, contact `ContactFormModal`, task `TaskCreateModal`, `CreateSubmissionPage` native fields) was create-centric and missed the edit surfaces; the sweep added policy create native, renewal create, submission edit, account/account-contact edit, and task inline edit, and confirmed `CreatePolicyPage`'s native policy fields as an in-scope CRUD form (its attribute panel stays Workstream A). Edit variants served by the same component (e.g. `ContactFormModal` for create+edit) are covered by wiring registration into that component.
2. **F0035 API confirmed compatible.** The shipped F0035 surface exports `useSessionRestorableForm`, `consumeFormSnapshot`, and a `DirtyFormRegistration` requiring `formKey`/`route`/`isDirty`/`getValues`/`getDirtyFieldPaths`. RHF supplies `getValues()` and `formState.dirtyFields` (flattened to paths) for Workstream A; a controlled-form dirty-tracker (Requirement 10) supplies the same contract for Workstream B without a field-state-library rewrite — closing PRD Risk #3. No F0035 change required; both adapters live in the shared registration helper (S0007).
3. **No-auto-replay invariant inherited.** F0035's operator-mandated no-mutation-auto-replay rule applies to every preserved form (S0006, S0008); the user always re-saves explicitly.
4. **Workstream B scope refinement (2026-05-27).** Workstream B was originally framed as a broad CRUD field-state-library rewrite. On 2026-05-27 the operator pushed back: RHF was intended only for the dynamic LOB engine; fixed-shape CRUD forms do not need that complexity, and inspection of `experience/src/features/session-continuity/dirtyFormRegistryContext.ts` confirmed F0035's contract is three plain function pointers — not RHF-specific. Workstream B was narrowed to a controlled-form dirty-tracker adapter that lets the existing controlled CRUD forms register with F0035 without a field-state-library rewrite. The ~11-component inventory and the F0035 finding #1 closure goal are unchanged; only the mechanism on the CRUD side changes (controlled stays controlled). ADR-021 §6 reworked the same day to record the controlled-form adapter decision; the earlier rewrite framing is superseded by that rework.

**Deferred to Phase B (Architecture) — these are design decisions, not open PM requirements:**

1. **Widget derivation source.** The shipped Cyber `ui-schema.json` is layout-only (`sections` + `fieldLabels`) — it carries no per-field widget map, unlike ADR-021's prose. Phase B records the data-schema type/enum/format → widget derivation in the amended ADR-021.
2. **AJV parity scope.** _(Resolved in Phase B — ADR-021 §3, reworked 2026-05-26.)_ The deferred question was whether the client must evaluate `rules.json` to reach 0-disagreement parity. Phase B decided **no**: the client validates the **data-schema** with AJV and parity is measured against the **actual backend** (ADR-022 `(code, pointer)` multiset equality over the Cyber examples), while the cross-field rules (`mfa_required_for_high_record_count`, `minimum_retention_not_met`) stay **backend-authoritative** and are surfaced via `lobErrors[]`. F0036 does not depend on ADR-023.
3. **Conditional gating mechanism.** MFA-maturity-enabled-when-MFA-enabled is not encoded in the shipped bundle; Phase B decides the engine convention (S0005 preserves today's observable behavior regardless).
4. **`ui-schema.json` filename.** Shipped name is hyphenated; ADR-021 prose says `ui.schema.json`. Phase B amends the ADR to the shipped name.

## Architecture Traceability (Architect Phase B)

- Primary governing decision: **ADR-021** (`planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md`). Phase B must reconcile ADR-021's "Accepted" status with reality — either confirm F0036 implements it as written or amend it where F0034 intentionally diverged — and decide whether a companion ADR is needed for the F0035 form-preservation integration contract. Phase B should also record that Workstream B keeps the CRUD forms as controlled components and uses a controlled-form dirty-tracker adapter to satisfy F0035's library-agnostic `DirtyFormRegistration` contract — RHF stays scoped to Workstream A.
- Consumes F0034 contracts (`LobSchemaBundle`, `lob-schema-bundle.schema.json`, Cyber bundle) and F0035 contracts (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`).
- The `feature-assembly-plan.md` is owned by the feature action at Step 0 (per `agents/actions/plan.md` Deliverables Contract), not by Phase A/B.

### Phase B Outcome (Architect — plan run `2026-05-25-51ff2a92`)

- **ADR-021 amended** (2026-05-25, reconciliation section; §6 reworked 2026-05-27): the ADR was Accepted-but-unimplemented; F0036 realizes it. The amendment records (1) the shipped `ui-schema.json` filename + layout-only shape, (2) the data-schema→widget derivation table, (3) the parity scope (client AJV over `data-schema.json` measured against the **actual backend** per ADR-022; cross-field rules **backend-authoritative** via `lobErrors[]`, **no client `rules.json` evaluation**, no ADR-023 dependency — §3 reworked 2026-05-26), (4) the conditional-gating convention, (5) the F0035 preservation integration adapter (library-agnostic), and (6) Workstream B controlled-form dirty-tracker adapter (§6 reworked 2026-05-27 — supersedes the earlier CRUD rewrite framing; the CRUD forms stay controlled and gain registration via the adapter).
- **No separate companion ADR.** The F0035 form-preservation integration is API consumption already governed by **ADR-024**; recording it inside the ADR-021 amendment avoids ADR fragmentation.
- **Governing ADRs:** ADR-021 (primary, amended + §3/§4 reworked 2026-05-26), ADR-020 (LOB extensible attributes), ADR-022 (validator equivalence — defines parity; realized client-side by the parity harness), ADR-024 (session continuity / form preservation). **ADR-023 removed** from F0036's governing set by the 2026-05-26 rework (its JsonLogic rules platform is unimplemented and F0036 does not realize it; cross-field rules stay backend-authoritative).
- **No backend, schema, or bundle change.** The Cyber bundle and `lob-schema-bundle.schema.json` are consumed as-is; backend validation stays authoritative. No new OpenAPI contract or JSON Schema is introduced by F0036.
- **Ontology bindings (updated 2026-05-26 rework):** `feature-mappings.yaml` `feature:F0036` lives in `features[]` with `affects` (`capability:dynamic-attribute-panel`, `capability:dynamic-lob-attributes`, `capability:validator-equivalence`, `capability:session-context-restore`), `governed_by` (adr:020/021/022/024), `uses_schema`, `uses_api_contract` (`api:nebula-rest`), and `depends_on` (F0034, F0035, F0019). `adr:023`/`capability:lob-rules-governance` were **dropped** (PR-C1 consequence). **No new canonical nodes** — F0036 reuses existing shared semantics.

### Plan Review Findings (run `2026-05-26-aaa8bd7c` — NOT READY)

A read-only plan-review audit returned **NOT READY** and corrected two of the Phase B Outcome claims above against the shipped code. Both were **resolved by an Architect Phase B rework on 2026-05-26** (ADR-021 §3/§4 reworked). Full report: `planning-mds/operations/evidence/runs/2026-05-26-aaa8bd7c/plan-review-report.md`; tracker in `STATUS.md` → Plan Review Findings.

- **✅ PR-C1 (Critical) — RESOLVED (ADR-021 §3 reworked).** Parity is split into two layers: the **client** validates the **data-schema** with AJV (parity measured against the *actual backend* per ADR-022 — multiset equality on `(code, pointer)` over the Cyber examples, live endpoint or recorded responses), and **cross-field rules stay backend-authoritative**, surfaced via `LobValidationProblemDetails.lobErrors[]` bound to fields by `pointer`. The client keeps no authoritative rule duplicate (an optional UX pre-check is allowed behind the parity harness). No backend change; **F0036 no longer depends on ADR-023** (the binding drops `adr:023`/`capability:lob-rules-governance`, retains ADR-022/`capability:validator-equivalence`).
- **✅ PR-H1 (High) — RESOLVED (ADR-021 §4 reworked).** The MFA-maturity conditional is split: the **presentational** enable/disable + required-marker is owned by the engine's declarative **UI-conditional map** (LOB-adapter-layer frontend config, not the bundle → "no bundle change"; applied generically → no ad-hoc JSX); the **validation** half is backend-authoritative via `lobErrors[]`. ADR-022's keyword ban applies to bundle data schemas, not this UI-layer map.
- All `aaa8bd7c` findings (PR-C1/PR-H1/PR-M1/PR-M2/PR-L1) were resolved by the 2026-05-26 rework, and the follow-up `2026-05-26-378ac7da` plan-review findings (PR-H1–H4/M1/L1) were resolved by the 2026-05-26/27 rework. Both are tracked in `STATUS.md` → Plan Review Findings.
