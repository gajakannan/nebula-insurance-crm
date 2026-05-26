# Story F0036-S0007: Shared Preservation Registration Helper + Migrate CRUD Forms to RHF

## Story Header

**Story ID:** F0036-S0007
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Shared Preservation Registration Helper + Migrate Hand-Rolled CRUD Forms to React Hook Form
**Priority:** High
**Phase:** MVP
**Workstream:** B — CRUD form RHF migration + preservation

## User Story

**As a** Frontend Platform Engineer
**I want** the hand-rolled CRUD forms moved onto React Hook Form behind one shared registration helper
**So that** every mutation form in the app reports its dirty state the same way and is ready to plug into form-state preservation

## Context & Background

Outside product attributes, Nebula's mutation forms are plain controlled components. Because none is RHF-managed, F0035 preservation has nothing to attach to. This story migrates the confirmed CRUD inventory to RHF (field state only — no AJV/widget-registry engine; they are fixed-shape) and introduces one shared registration helper, built on F0035's `useSessionRestorableForm`, that both the product-attribute form (S0006) and the CRUD forms (S0008) use to register a stable `form_key` and expose `isDirty`/`getValues`/`getDirtyFieldPaths`.

**Confirmed Workstream B inventory (verified to exist 2026-05-25):**
- Broker: `EditBrokerModal.tsx`, `CreateBrokerPage.tsx`
- Account: `CreateAccountPage.tsx`
- Contact: `ContactFormModal.tsx`
- Task: `TaskCreateModal.tsx`
- Submission native fields: `CreateSubmissionPage.tsx` (native fields only; its embedded attribute panel is Workstream A)

`CreatePolicyPage.tsx` hosts the product-attribute panel (Workstream A) and is not a separate CRUD migration target in this story.

## Acceptance Criteria

**Happy Path — Shared registration helper:**
- **Given** the F0035 registry API (`useSessionRestorableForm`, `consumeFormSnapshot`)
- **When** the shared helper lands
- **Then** any RHF form can register by passing its `useForm` result and a stable `form_key`, and the helper builds the `DirtyFormRegistration` (`isDirty`, `getValues`, `getDirtyFieldPaths` flattened from RHF `formState.dirtyFields`) and consumes a snapshot on mount
- **And** the helper is the single place that owns `form_key` shape and the dirty-path contract for both workstreams

**Happy Path — CRUD forms migrated to RHF:**
- **Given** the confirmed inventory
- **When** each form is migrated
- **Then** each of `EditBrokerModal`, `CreateBrokerPage`, `CreateAccountPage`, `ContactFormModal`, `TaskCreateModal`, and the native fields of `CreateSubmissionPage` uses RHF for field state and its validation/submit lifecycle
- **And** existing field-level validation behavior and submit semantics are preserved (same required fields, same error messages, same successful-create/edit result)

**Happy Path — Per-form create/edit parity:**
- **Given** a per-form create/edit regression test captured before migration
- **When** the migrated form replaces the hand-rolled one
- **Then** the create and edit flows produce the same persisted result and the same entity timeline event as before (regression-asserted)

**Edge Case — Validation and error paths preserved:**
- **Given** a migrated form submitted with invalid or missing required fields
- **When** the user submits
- **Then** the same validation errors block submit as before migration (no weaker or stronger validation), and a server-side error (e.g. HTTP 400/409 conflict) surfaces the same user-visible message as today

**Edge Case — One form at a time:**
- **Given** the broad regression surface
- **When** forms are migrated
- **Then** each form is migrated independently behind its own regression test (no big-bang), so a regression in one form cannot be masked by another

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Each migrated CRUD form (create page or edit modal) | Fill fields and submit (create or update) | Editable per existing rules | RHF manages field state; on submit the existing mutation/API call runs and persists exactly as before | Per-form regression: create/edit, submit, reload → persisted record shown and entity timeline event present, identical to pre-migration | Roles/lifecycle states each form already enforces (unchanged) |

Required checks:
- [x] Render-only behavior cannot satisfy: each form must still create/update and persist, proven by edit→submit→reload assertions.
- [x] Save path validation: existing validation preserved exactly; server errors surface unchanged.
- [x] Audit/timeline: each form's existing create/update timeline event must still fire on submit (regression-asserted), not a new event class.
- [x] Tests prove create + edit parity per form against a pre-migration baseline.

## Data Requirements

**Per form:** unchanged field sets — broker, account, contact, task, and submission native fields keep their current required/optional fields and validation rules. The migration changes the field-state mechanism (controlled → RHF), not the data contract.

**Shared helper inputs:** RHF `useForm` return (`getValues`, `formState`), a stable `form_key`, the current `route`.

**Validation Rules:**
- No change to which fields are required or how server errors map to messages.
- `form_key` is unique per form instance to prevent snapshot collisions (consumed in S0008).

## Role-Based Visibility

**Roles that can use each form:**
- Exactly the roles each form authorizes today (e.g. broker/account/contact/task create/edit per existing ABAC). This story changes field-state plumbing, not authorization. Unauthorized users are blocked by each form's existing route/permission checks (HTTP 401/403 upstream).

**Data Visibility:**
- No change. Each form shows the same fields to the same roles as before; InternalOnly/ExternalVisible treatment is unchanged.

## Non-Functional Expectations

- **Performance:** No regression in form open or submit latency; RHF reduces per-keystroke re-renders relative to fully-controlled state.
- **Security:** No new network surface; no token/PII handling added. Authorization unchanged.
- **Reliability:** Each form's error handling (validation + server error) is preserved; the shared helper degrades safely if the registry provider is absent (no crash, just no preservation).

## Dependencies

**Depends On:**
- F0035 (archived) — `useSessionRestorableForm`, `consumeFormSnapshot`, `DirtyFormRegistration` contract.
- F0036-S0001 — RHF adopted as a dependency (the helper and CRUD forms rely on it being present).

**Related Stories:**
- F0036-S0006 — the product-attribute form uses the same shared helper.
- F0036-S0008 — registers these migrated forms with preservation and proves restore.

## Business Rules

1. **Fixed-shape, no schema engine.** CRUD forms use RHF for field state only; they do NOT go through AJV/widget-registry rendering (PRD scope boundary).
2. **One helper owns the dirty contract.** A single registration helper owns `form_key` shape and `isDirty`/`getValues`/`getDirtyFieldPaths` for both workstreams.
3. **Migrate incrementally.** One form at a time, each behind its own regression test.

## Out of Scope

- Registering the migrated forms with preservation and proving restore (S0008 — this story stops at RHF migration + the helper existing).
- Putting CRUD forms through the AJV/widget-registry engine.
- Adding/removing fields or changing validation rules on any CRUD form.
- Filter-only / non-mutation forms (no in-flight state worth preserving).

## UI/UX Notes

- Screens involved: the six confirmed CRUD surfaces. The migration must be visually and behaviorally invisible to users — same layout, same validation messages.

## Questions & Assumptions

**Resolved (Phase A clarification):**
- **Is the inventory exactly these six surfaces?** Yes — confirmed against the codebase 2026-05-25. Edit variants handled by the same modal (e.g. `ContactFormModal` for create+edit) are covered by migrating that component. `CreatePolicyPage` is a Workstream A consuming screen, not a separate CRUD target.

**Assumptions (to be validated):**
- RHF is acceptable for fixed-shape CRUD forms (aligns with ADR-021's choice of RHF as Nebula's form-state library — Phase B confirms).

## Definition of Done

- [ ] Acceptance criteria met (shared helper, six forms migrated, per-form parity, validation/error preserved, incremental)
- [ ] Edge cases handled (invalid/missing fields, server errors, one-at-a-time)
- [ ] Permissions enforced (each form's existing auth unchanged; regression-verified)
- [ ] Audit/timeline logged (each form's existing create/update event still fires)
- [ ] Tests pass (per-form create/edit regression pre+post migration; unit tests for the shared helper incl. dirty-path flattening)
- [ ] Documentation updated (GETTING-STARTED documents the shared registration helper + migrated inventory)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
