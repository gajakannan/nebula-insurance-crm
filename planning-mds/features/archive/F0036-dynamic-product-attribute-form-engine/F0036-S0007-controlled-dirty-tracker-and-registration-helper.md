# Story F0036-S0007: Controlled-Form Dirty-Tracker + Shared Preservation Registration Helper

## Story Header

**Story ID:** F0036-S0007
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Controlled-Form Dirty-Tracker (`useControlledDirtyTracker`) + Library-Agnostic Shared Preservation Registration Helper
**Priority:** High
**Phase:** MVP
**Workstream:** B — CRUD form preservation via controlled-form adapter

## User Story

**As a** Frontend Platform Engineer
**I want** a small dirty-tracker hook for the existing controlled CRUD forms, plus one library-agnostic shared helper that both the RHF product-attribute form and the controlled CRUD forms register through
**So that** every mutation form in the app reports its dirty state in the F0035 contract and is ready to plug into form-state preservation — without rewriting any CRUD form's field-state mechanism

## Context & Background

Outside product attributes, Nebula's mutation forms are plain controlled components. F0035 preservation has nothing to attach to today not because RHF is missing but because no form holds its dirty bookkeeping in the F0035 `DirtyFormRegistration` shape — three plain function pointers (`isDirty`/`getValues`/`getDirtyFieldPaths`) defined in `experience/src/features/session-continuity/dirtyFormRegistryContext.ts`. RHF is not a precondition.

This story:

1. Ships **`useControlledDirtyTracker(values, initialValues, options?)`** for plain controlled forms. Given the current values object and a stable initial-values reference, it returns the F0035 triple (`isDirty()`, `getValues()`, `getDirtyFieldPaths()`) by deep-diffing current vs. initial values. No field-state library is introduced.
2. Ships one **library-agnostic shared registration helper** built on F0035's `useSessionRestorableForm`. The helper consumes a `DirtyFormRegistration`-shaped source from either backend: an **RHF adapter** (used by the product-attribute form, Workstream A) or the **controlled-form tracker** (used by every CRUD form in the inventory below). Both call `useSessionRestorableForm` and `consumeFormSnapshot` through the same code path; the helper owns the `form_key` shape.
3. Wires the in-scope CRUD inventory through the helper using the controlled tracker. The CRUD forms **remain controlled** — no `useForm`, no `Controller` wrapping, no validation reshape, no submit reshape.

### Scope refinement (2026-05-27)

Earlier F0036 planning briefly considered changing Workstream B's field-state library. On 2026-05-27 the operator narrowed Workstream B: RHF was intended only for the dynamic LOB engine, and inspection confirmed F0035's contract is library-agnostic. The story now ships the controlled-form tracker + library-agnostic helper instead of changing CRUD field-state mechanisms. The ~11-component inventory and the F0035 finding #1 closure goal are unchanged; only the per-form mechanism on the CRUD side changes (controlled stays controlled). See ADR-021 §6 (reworked 2026-05-27) and PRD Phase A clarification item 4 for the decision record.

### Workstream B inventory — exhaustive sweep (updated 2026-05-26, resolves plan-review PR-M1)

A full sweep of `experience/src` (all `useMutation` write paths + every `<form>`/`onSubmit` + editable-field component) was performed and each dirty-able mutation form classified. The original 2026-05-25 list (6) was **create-centric and missed nearly every edit surface**; the corrected in-scope set is:

_Create forms:_
- Broker create — `pages/CreateBrokerPage.tsx`
- Account create — `pages/CreateAccountPage.tsx`
- Submission create (native fields) — `pages/CreateSubmissionPage.tsx` (attribute panel is Workstream A)
- **Policy create (native fields) — `pages/CreatePolicyPage.tsx`** _(added via sweep: `useState<PolicyCreateForm>` + `useCreatePolicy`; it hosts the attribute panel too, but its native policy fields are a hand-rolled form exactly like CreateSubmissionPage)_
- **Renewal create — `pages/RenewalsPage.tsx` create modal** _(added: `useState<RenewalCreateForm>` + `useCreateRenewal`)_
- Task create — `features/tasks/components/TaskCreateModal.tsx`
- Contact create — `features/brokers/components/ContactFormModal.tsx`

_Edit forms:_
- Broker edit — `features/brokers/components/EditBrokerModal.tsx`
- Contact edit — `features/brokers/components/ContactFormModal.tsx` (same modal serves create + edit)
- **Submission edit (native fields) — `pages/SubmissionDetailPage.tsx` edit modal** _(added: `editForm`/`editErrors` + `useUpdateSubmission`)_
- **Account edit + account-contact edit — `pages/AccountDetailPage.tsx`** _(added: `useUpdateAccount` + `useUpdateAccountContact`)_
- **Task inline edit (title / description) — `features/tasks/components/TaskDetailPanel.tsx`** _(added: inline edit state + `useUpdateTask`; lighter-weight inline edits)_

**Explicitly out of scope (with reason):**
- _Filter / search forms_ — `PoliciesPage` (`PolicyFilterToolbar`), `AccountsPage`, `BrokerListPage`, `SubmissionsPage`, and the `RenewalsPage` filter toolbar: filter-only, no in-flight mutation state worth preserving.
- _Action dialogs_ — assign (submission/renewal), status transition (incl. a short `transitionReason`), task complete/delete, broker delete/reactivate, contact delete: quick confirm/action surfaces, not editable CRUD forms (revisit only if a free-text reason proves worth preserving).
- _`pages/PolicyImportPage.tsx`_ — bulk JSON import/paste tool with an in-flight payload, but belongs to the data-import/migration domain (F0031 territory), not F0036's CRUD set. Cross-feature follow-up.
- _Document upload_ (`features/documents/hooks.ts`) — file upload, not a text CRUD form.

**Scope note (operator):** the sweep grows Workstream B from 6 to ~11 components. This is required to truthfully claim "F0035 finding #1 closed for **all** in-scope mutation forms." The operator confirmed the full set with no deferrals on 2026-05-27.

## Acceptance Criteria

**Happy Path — `useControlledDirtyTracker` semantics:**
- **Given** initial values `I` and current values `C`
- **When** the hook is called as `useControlledDirtyTracker(C, I)`
- **Then** it returns `{ isDirty, getValues, getDirtyFieldPaths }` where:
  - `isDirty()` returns `true` iff `C` is not deeply equal to `I`
  - `getValues()` returns `C`
  - `getDirtyFieldPaths()` returns the flattened JSON-path list of every leaf-or-array-or-object path whose value differs between `C` and `I` (e.g. `["notes", "address.line1", "phones[0]"]`)
- **And** the equality contract holds for the documented matrix: scalar typed-and-cleared (equal to initial → not dirty), edited-then-reset (equal again → not dirty), array reorder of identical contents (not dirty if contents match; dirty if order is semantically significant per opt-in), nested-object replacement with structurally-equal contents (not dirty), `undefined`/missing key (treated equal to absent), Date and Number primitives compared by value
- **And** a `sensitiveFieldPaths` option excludes named paths from `getValues()` and `getDirtyFieldPaths()` (default-deny aligned with F0035's sensitive-field policy)

**Happy Path — Library-agnostic shared helper:**
- **Given** the F0035 registry API (`useSessionRestorableForm`, `consumeFormSnapshot`)
- **When** the shared helper lands
- **Then** any source of the `DirtyFormRegistration` triple — the RHF adapter (Workstream A) or the controlled tracker (Workstream B) — can register by passing it and a stable `form_key`, and the helper consumes a snapshot on mount via `consumeFormSnapshot`
- **And** the helper is the single place that owns `form_key` shape and the dirty-path contract for both workstreams
- **And** a unit test demonstrates both backends registering and being snapshotted/restored equivalently through the same helper

**Happy Path — All CRUD forms registered via the controlled tracker:**
- **Given** the confirmed ~11-component inventory
- **When** each form is wired
- **Then** every in-scope form in the exhaustive inventory above (all create **and** edit surfaces) holds a stable `initialValues` reference and calls the shared registration helper with `useControlledDirtyTracker(values, initialValues)` and a unique `form_key`
- **And** the forms remain controlled — no `useForm`, no `Controller` wrappers, no validation reshape, no submit-path reshape
- **And** existing field-level validation behavior and submit semantics are unchanged (same required fields, same error messages, same successful-create/edit result, same entity timeline event)

**Edge Case — Validation and error paths preserved:**
- **Given** a wired form submitted with invalid or missing required fields
- **When** the user submits
- **Then** the same validation errors block submit as before wiring (no weaker or stronger validation), and a server-side error (e.g. HTTP 400/409 conflict) surfaces the same user-visible message as today — the registration call is render-side only and does not touch the submit path

**Edge Case — Stable `initialValues` reference:**
- **Given** a CRUD edit form that loads its initial values asynchronously
- **When** the entity finishes loading and `initialValues` is set
- **Then** `initialValues` is captured once (e.g. via `useRef` set on first non-null value, or a derived memo keyed by entity id) so that subsequent re-renders do not re-mark the form dirty
- **And** restoring a snapshot after a forced re-auth uses the restored values as the new dirty baseline (per S0008 behavior)

**Edge Case — One form at a time:**
- **Given** the broad registration surface
- **When** forms are wired
- **Then** each form is wired independently behind its own regression test (no big-bang), so a regression in one form cannot be masked by another

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Each wired CRUD form (create page or edit modal) | Fill fields and submit (create or update) | Controlled, unchanged | Existing mutation/API call runs and persists exactly as before; registration call is render-side only | Per-form regression: create/edit, submit, reload → persisted record shown and entity timeline event present, identical to pre-wiring | Roles/lifecycle states each form already enforces (unchanged) |

Required checks:
- [x] Render-only behavior cannot satisfy: each form must still create/update and persist, proven by edit→submit→reload assertions.
- [x] Save path validation: existing validation preserved exactly; server errors surface unchanged.
- [x] Audit/timeline: each form's existing create/update timeline event must still fire on submit (regression-asserted), not a new event class.
- [x] Tests prove create + edit parity per form against a pre-wiring baseline.

## Data Requirements

**Per form:** unchanged field sets — broker, account (+ account-contact), contact, task, submission native fields, policy native fields, and renewal-create keep their current required/optional fields and validation rules. The story changes nothing about the data contract or the field-state mechanism; it adds a registration call and an `initialValues` reference.

**Tracker inputs:** `(values: T, initialValues: T, options?: { sensitiveFieldPaths?: string[] })`.

**Shared helper inputs:** a `DirtyFormRegistration`-shaped source (`isDirty`, `getValues`, `getDirtyFieldPaths`), a stable `form_key`, the current `route`.

**Validation Rules:**
- No change to which fields are required or how server errors map to messages.
- `form_key` is unique per form instance to prevent snapshot collisions (consumed in S0008).
- Deep equality is the contract; reference equality on identical contents must not produce false dirtiness.

## Role-Based Visibility

**Roles that can use each form:**
- Exactly the roles each form authorizes today (e.g. broker/account/contact/task create/edit per existing ABAC). This story adds registration plumbing, not authorization. Unauthorized users are blocked by each form's existing route/permission checks (HTTP 401/403 upstream).

**Data Visibility:**
- No change. Each form shows the same fields to the same roles as before; InternalOnly/ExternalVisible treatment is unchanged. `sensitiveFieldPaths` is the explicit hook for excluding sensitive fields from snapshots (S0008 enforces).

## Non-Functional Expectations

- **Performance:** No regression in form open or submit latency. The dirty-tracker runs a memoized deep-diff per render; for the 3–8 field CRUD forms this is negligible. Forms larger than 50 leaves should pass an opt-in `equals` strategy if measured cost matters; not expected for this inventory.
- **Security:** No new network surface; no token/PII handling added. Authorization unchanged. `sensitiveFieldPaths` provides the hook for excluding sensitive values from snapshots.
- **Reliability:** Each form's error handling (validation + server error) is preserved; the shared helper and tracker degrade safely if the registry provider is absent (no crash, just no preservation).

## Dependencies

**Depends On:**
- F0035 (archived) — `useSessionRestorableForm`, `consumeFormSnapshot`, `DirtyFormRegistration` contract.
- F0036-S0001 — RHF adopted as a dependency for Workstream A's adapter (the controlled tracker has no RHF dependency).

**Related Stories:**
- F0036-S0006 — the product-attribute form uses the same shared helper through its RHF adapter.
- F0036-S0008 — registers the wired CRUD forms with preservation and proves restore.

## Business Rules

1. **Fixed-shape, no schema engine.** CRUD forms stay as plain controlled components; they do NOT use AJV/widget-registry rendering and they do NOT change field-state library (PRD scope boundary; ADR-021 §6 reworked 2026-05-27).
2. **One helper owns the dirty contract.** A single library-agnostic registration helper owns `form_key` shape and the `isDirty`/`getValues`/`getDirtyFieldPaths` contract for both workstreams.
3. **Wire incrementally.** One form at a time, each behind its own regression test. No big-bang.
4. **No submit-path change.** Registration is render-side only. Validation, error mapping, and the mutation call are unchanged on every CRUD form.

## Out of Scope

- **Changing any CRUD form's field-state library.** Workstream B keeps CRUD controlled; RHF stays scoped to Workstream A (ADR-021 §6 reworked 2026-05-27).
- Registering the wired forms with preservation and proving restore (S0008 — this story stops at the tracker + helper existing + every CRUD form passing the tracker through the helper).
- Putting CRUD forms through the AJV/widget-registry engine.
- Adding/removing fields or changing validation rules on any CRUD form.
- Filter-only / non-mutation forms (no in-flight state worth preserving).

## UI/UX Notes

- Screens involved: the in-scope CRUD surfaces in the exhaustive inventory above (~11 components, create + edit). Wiring must be visually and behaviorally invisible to users — same layout, same validation messages, same submit behavior.

## Questions & Assumptions

**Resolved (Phase A clarification + 2026-05-27 scope refinement):**
- **Is the inventory exactly these six surfaces?** _(Superseded by the 2026-05-26 PR-M1 exhaustive sweep — see Context → "Workstream B inventory".)_ The original 2026-05-25 name-confirmation listed six create-centric surfaces and treated `CreatePolicyPage` as Workstream-A-only; the sweep corrected this to the exhaustive ~11-component create+edit inventory, in which `CreatePolicyPage`'s **native policy fields** are an in-scope hand-rolled CRUD form (its attribute panel remains Workstream A). Edit variants served by the same modal (e.g. `ContactFormModal` for create+edit) are covered by wiring registration into that component. Operator confirmed the full set with no deferrals (2026-05-27).
- **Does Workstream B require a field-state-library rewrite?** _(Resolved 2026-05-27 — no.)_ F0035's `DirtyFormRegistration` contract (`dirtyFormRegistryContext.ts`) is library-agnostic; a controlled-form dirty-tracker satisfies it without RHF. The CRUD forms stay controlled. RHF remains scoped to Workstream A. ADR-021 §6 reworked the same day to record the decision.

**✅ Plan Review Finding PR-M1 (Medium) — RESOLVED 2026-05-26 by the exhaustive sweep above (Context → "Workstream B inventory").**
- The exhaustive `experience/src` sweep classified every dirty-able mutation form. `PoliciesPage`'s `onSubmit` was confirmed a **filter form** (out of scope). Five previously-missed editable forms were added to scope (policy create, renewal create, submission edit, account/account-contact edit, task inline edit); filters, action dialogs, bulk import, and document upload were explicitly excluded with reasons. Inventory grows 6 → ~11 components; operator confirmed full set with no deferrals (2026-05-27). Origin: `planning-mds/operations/evidence/runs/2026-05-26-aaa8bd7c/plan-review-report.md`.

**Assumptions (validated 2026-05-27 by source inspection):**
- F0035's `DirtyFormRegistration` is `{ formKey, route, isDirty, getValues, getDirtyFieldPaths }` — three plain function pointers, library-agnostic. Verified at `experience/src/features/session-continuity/dirtyFormRegistryContext.ts:4-10`. A controlled-form dirty-tracker satisfies it without an RHF dependency.

## Definition of Done

- [ ] Acceptance criteria met (`useControlledDirtyTracker` semantics with the equality matrix; library-agnostic shared helper with both backends exercised; all in-scope inventory forms wired; per-form parity; validation/error preserved; incremental)
- [ ] Edge cases handled (stable `initialValues` reference, invalid/missing fields, server errors, one-at-a-time, sensitive-field exclusion)
- [ ] Permissions enforced (each form's existing auth unchanged; regression-verified)
- [ ] Audit/timeline logged (each form's existing create/update event still fires; no new event class)
- [ ] Tests pass (per-form create/edit regression pre+post wiring; unit tests for `useControlledDirtyTracker` covering the documented equality matrix and `sensitiveFieldPaths`; unit test for the shared helper exercising both RHF and controlled backends)
- [ ] Documentation updated (GETTING-STARTED documents the controlled-form tracker, the library-agnostic shared registration helper, and the wired inventory)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
