# Story F0036-S0008: Register Migrated CRUD Forms With F0035 Preservation and Close the Canonical Contact-Edit Scenario

## Story Header

**Story ID:** F0036-S0008
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Register Migrated CRUD Forms With F0035 + Restore on Mount; Close F0035 S0003 Contact-Edit Scenario
**Priority:** High
**Phase:** MVP
**Workstream:** B — CRUD form RHF migration + preservation

## User Story

**As a** Broker Relationship Manager editing a contact's notes
**I want** my unsaved edits on any form to come back after a forced sign-in
**So that** a session boundary never throws away work I had typed but not yet saved

## Context & Background

S0007 migrated the CRUD forms to RHF and built the shared registration helper. This story registers those forms with F0035 preservation and proves the end-to-end forced-re-auth restore — including the exact canonical scenario F0035 documented but could not deliver: Contact Edit → type into "Notes" → forced re-auth → values restored → explicit re-save. Closing this scenario fully resolves F0035 finding #1 (preservation wired to zero forms). The no-auto-replay mandate from F0035 (plan run 2026-05-23-41109356) applies to every CRUD form.

## Acceptance Criteria

**Happy Path — Canonical Contact-Edit scenario (F0035 S0003):**
- **Given** a Broker Relationship Manager is on Contact Edit (`ContactFormModal`) for a contact, has typed new content into the "Notes" field (form dirty), and clicks Save
- **When** the Save returns 401 and silent renewal fails (or is skipped because the request was a mutation, per F0035 S0001)
- **Then** the contact form's values snapshot to sessionStorage (keyed by `(user_id, route, form_key)`) before redirect to `/login?reason=session_expired`
- **And** after sign-in the user lands on the same Contact Edit URL with the "Notes" content (and other fields) restored, the form marked dirty, and Save enabled
- **And** the Save is NOT auto-replayed — the user clicks Save explicitly, and only then does the contact-update timeline event fire

**Happy Path — All migrated forms preserved:**
- **Given** any of the migrated CRUD forms (broker create/edit, account create, contact create/edit, task create, submission native fields) is dirty
- **When** a forced re-auth occurs
- **Then** that form's values snapshot and restore on return via the shared helper, with Save enabled and no auto-replay

**Edge Case — Cross-user / TTL / sign-out / oversize:**
- **Given** a stored CRUD snapshot
- **When** a different user signs in, OR > 1 hour passes, OR the user signs out, OR the snapshot exceeds 256 KB
- **Then** the snapshot is discarded (cross-user/TTL/sign-out) or skipped to route-only with a `form-snapshot-skipped` event (oversize/storage_unavailable), per the F0035 rules — never rehydrated into the wrong user's or wrong form's fields

**Edge Case — Concurrent forms, correct targeting:**
- **Given** more than one migrated form has a snapshot (distinct `form_key`s)
- **When** the user returns from forced re-auth
- **Then** each form rehydrates only from its own `form_key` snapshot (no cross-form contamination)

**Edge Case — Non-form mutation:**
- **Given** the interrupted action was a confirm-dialog/toggle rather than a form (no RHF form dirty)
- **When** forced re-auth occurs
- **Then** route-only preservation applies and no form restore is attempted (matches F0035 S0003 non-form behavior)

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Any migrated CRUD form, dirty, then 401 + silent-renewal-fail | (implicit) redirect; then sign-in | Values snapshotted, then editable on return | Original create/update is NOT performed; user re-clicks Save on return | After return, the form shows restored values and Save enabled; explicit Save runs the existing mutation and the entity create/update timeline event fires; cross-user sign-in shows no restored values | All authenticated roles that can use each form |

Required checks:
- [x] Render-only behavior cannot satisfy: values must rehydrate AND explicit Save must persist — both asserted, including the canonical Contact-Edit Notes case.
- [x] Save path validation: restored form runs the same validation as S0007 before the explicit Save.
- [x] Audit/timeline: only the explicit re-save emits the entity create/update event; no `mutation-auto-replayed` event exists.
- [x] Tests prove the canonical Contact-Edit scenario end-to-end, plus same-user-only rehydrate and per-form_key targeting.

## Data Requirements

**Snapshot record (F0035 `FormSnapshotRecord`, reused):** `user_id`, `route`, `form_key` (e.g. `contact-edit:<id>`), `form_values` (≤ 256 KB), `dirty_field_paths` (flattened from RHF via the shared helper), `snapshot_timestamp`.

**form_key per form:** stable and unique per instance (e.g. `contact-edit:789`, `broker-edit:42`, `task-create`), so concurrent snapshots do not collide.

**Forbidden in snapshot:** raw tokens; F0035-classified sensitive fields (default-deny).

**Validation Rules:** 1-hour TTL; per-user namespace; consumed on successful rehydrate.

## Role-Based Visibility

**Roles that experience this:**
- All authenticated roles that use the migrated forms (BrokerUser experience is identical — any BrokerUser form that fails Save with 401 gets the same preservation). Preservation is not an authorization feature.

**Data Visibility:**
- Snapshots are browser-local, per-user-namespaced sessionStorage; may transiently hold InternalOnly fields the user was editing (accepted F0035/ADR-024 boundary). Different-user sign-in invalidates prior snapshots before rehydration. Forced re-auth triggers only on `401-auth-failed`, not `403`.

## Non-Functional Expectations

- **Performance:** Snapshot write < 100ms before redirect; rehydrate < 200ms after sign-in.
- **Security:** No tokens/PII in snapshots; per-user isolation, 1h TTL, sign-out clear, 256 KB cap — the accepted F0035 boundary; Security Reviewer may tighten at the feature-action review.
- **Reliability:** Oversize/storage-unavailable degrade to route-only with a user-visible message; TTL expiry is silent; concurrent forms never cross-contaminate.

## Dependencies

**Depends On:**
- F0036-S0007 — migrated RHF forms + the shared registration helper.
- F0035-S0003 (archived) — the forced-re-auth restore mechanism, the canonical Contact-Edit scenario, and the no-auto-replay mandate.

**Related Stories:**
- F0036-S0006 — the product-attribute analogue; together S0006 + S0008 close F0035 finding #1 for all in-scope mutation forms.

## Business Rules

1. **No mutation auto-replay (ABSOLUTE).** Inherited from F0035; explicit re-save required on every form.
2. **Per-user isolation, 1h TTL, sign-out clear.** Inherited from F0035.
3. **Finding #1 closed.** After this story there are no in-scope mutation forms that lose unsaved input on a forced re-auth.

## Out of Scope

- Changing F0035 behavior (consumed as-is).
- Server-side draft persistence.
- Preserving non-mutation/filter-only forms (no in-flight state worth preserving).

## UI/UX Notes

- Screens involved: the six migrated CRUD surfaces, with Contact Edit as the canonical demonstration.
- On return: F0035 inline notification ("We saved your edits while you signed in. Click Save when ready.") with Save enabled; oversize fallback uses the F0035 "unable to preserve" message.

## Questions & Assumptions

**Resolved (grounding / Phase A):**
- The F0035 API supports this directly (`useSessionRestorableForm` + `consumeFormSnapshot`); the shared helper from S0007 bridges RHF to the `DirtyFormRegistration` contract.

**Assumptions (to be validated):**
- Each CRUD payload is well under the 256 KB cap (they are — small fixed-shape forms).

## Definition of Done

- [ ] Acceptance criteria met (canonical Contact-Edit scenario, all forms preserved, no auto-replay, per-form_key targeting)
- [ ] Edge cases handled (cross-user, TTL, sign-out, oversize, concurrent forms, non-form mutation)
- [ ] Permissions enforced (per-user snapshot isolation; 401-only trigger)
- [ ] Audit/timeline logged (only explicit re-save emits the create/update event; no auto-replay event ever)
- [ ] Tests pass (E2E: Contact Edit → Notes → forced re-auth → restore → explicit Save; integration per migrated form; unit: per-form_key targeting)
- [ ] Documentation updated (GETTING-STARTED records F0035 finding #1 closed for all in-scope forms)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
