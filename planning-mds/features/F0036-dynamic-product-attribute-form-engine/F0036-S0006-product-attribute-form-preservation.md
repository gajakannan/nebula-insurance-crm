# Story F0036-S0006: Wire Product-Attribute Form Into F0035 Dirty-Form Registry and Restore

## Story Header

**Story ID:** F0036-S0006
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Wire Product-Attribute Form Into F0035 Dirty-Form Registry + Restore (End-to-End Forced-Re-Auth Journey)
**Priority:** High
**Phase:** MVP
**Workstream:** A — Dynamic product-attribute engine

## User Story

**As a** Cyber Underwriter entering attributes
**I want** my in-progress attribute entries to survive a forced sign-in
**So that** a session blip in the middle of a quote does not throw away the values I just typed

## Context & Background

F0035 shipped a form-state preservation registry (`dirtyFormRegistry`, `useSessionRestorableForm`, `consumeFormSnapshot`) wired to zero forms. The engine-backed product-attribute form (S0005) is the first real RHF form, so it is the first form that can register. This story connects it: on a forced re-auth with a dirty attribute form, the values snapshot to sessionStorage before redirect and rehydrate on return — delivering the end-to-end forced-re-auth journey F0035 could not test because no RHF form existed. Per the F0035 operator mandate (plan run 2026-05-23-41109356), the in-flight mutation is never auto-replayed; the user re-saves explicitly.

The verified F0035 API: `useSessionRestorableForm<TValues>({ formKey, route, isDirty, getValues, getDirtyFieldPaths })`; `consumeFormSnapshot(userId, formKey)` returns a `FormSnapshotRecord` or null; `snapshotDirtyForm` enforces a 256 KB cap and emits skip causes `oversize` / `storage_unavailable` / `classifier_uncertain`; snapshot TTL is 1 hour; keys are namespaced `nebula.session-restore.v1.<user_id>.<form_key>`.

## Acceptance Criteria

**Happy Path — Dirty attribute form restored after forced re-auth:**
- **Given** an Underwriter has typed Cyber attribute values into the engine-backed panel (form is dirty) on a draft submission
- **When** a protected action returns 401 and silent renewal fails, triggering forced re-auth (F0035 S0001/S0003 path)
- **Then** the attribute form registers via `useSessionRestorableForm` so its dirty values snapshot to sessionStorage (keyed by `(user_id, route, form_key)`) before redirect
- **And** after successful sign-in the user returns to the same route with the attribute values rehydrated via `consumeFormSnapshot`, the form marked dirty, and Save enabled
- **And** the mutation is NOT auto-replayed — the user clicks Save explicitly (no server-side attribute change, and no entity-update timeline event, exists between the failed action and the explicit re-save)

**Happy Path — Pinned version on restore:**
- **Given** a restored attribute form
- **When** it rehydrates
- **Then** it rebinds to the snapshot's pinned `(productVersionId, stage)` (per S0004), not the currently-active version, so the restored values validate against the version actually edited

**Edge Case — Oversize / skip:**
- **Given** the attribute snapshot would exceed the 256 KB cap (or sessionStorage is unavailable)
- **When** snapshotting is attempted
- **Then** the snapshot is skipped, route-only preservation applies, and a `form-snapshot-skipped` telemetry event is emitted with the F0035 cause (`oversize` / `storage_unavailable`), carrying no form content
- **And** on return the user sees the F0035 "unable to preserve your edits" inline message

**Edge Case — Cross-user / TTL / sign-out:**
- **Given** a stored attribute snapshot
- **When** a different user signs in, OR more than 1 hour passes, OR the user explicitly signs out
- **Then** the snapshot is discarded and not used to rehydrate (per F0035 isolation, TTL, and sign-out-clear rules)

**Edge Case — getDirtyFieldPaths from RHF:**
- **Given** the engine's RHF form state
- **When** the registration adapter builds `getDirtyFieldPaths()`
- **Then** it flattens RHF `formState.dirtyFields` (including nested paths like `controls.mfaEnabled`, `requestedLimit.amountMinor`) into the string-path array the F0035 `DirtyFormRegistration` requires

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Engine-backed attribute panel with dirty values, then 401 + silent-renewal-fail | (implicit) system-driven redirect; then sign-in | Values snapshotted, then editable on return | Original attribute mutation is NOT performed; user must re-click Save on return | After return, the route reloads with snapshotted attribute values and Save enabled; explicit Save then calls the F0034 backend write path and the entity timeline event fires | All authenticated roles that can edit attributes |

Required checks:
- [x] Render-only behavior cannot satisfy: values must actually rehydrate AND explicit Save must be required to persist — both asserted.
- [x] Save path validation: restored form still runs AJV (S0003) before the explicit Save.
- [x] Audit/timeline: only the explicit re-save produces the backend entity-update timeline event; no `mutation-auto-replayed` event exists (its presence in logs is a bug).
- [x] Tests prove snapshot-across-redirect, same-user-only rehydrate, pinned-version rebind, and explicit-Save requirement.

## Data Requirements

**Snapshot record (F0035 `FormSnapshotRecord`, reused as-is):** `user_id`, `route`, `form_key`, `form_values` (the attribute value object, ≤ 256 KB), `dirty_field_paths` (flattened from RHF), `snapshot_timestamp` (ISO 8601).

**form_key:** stable per attribute form instance, e.g. `cyber-attributes:<entityType>:<entityId>` so concurrent forms do not collide.

**Forbidden in snapshot:** raw access/refresh tokens; any field F0035 classifies as sensitive (default-deny per F0035/ADR-024 if unsure).

**Validation Rules:** 1-hour TTL; per-user namespacing; consumed (deleted) on successful rehydrate.

## Role-Based Visibility

**Roles that experience this:**
- All authenticated roles that can edit Cyber attributes. Preservation behavior is identical across roles; it is not an authorization feature.

**Data Visibility:**
- Snapshots are browser-local, per-user-namespaced sessionStorage (not localStorage), and may transiently contain InternalOnly attribute values the user was already editing — consistent with the F0035/ADR-024 accepted boundary. Different-user sign-in invalidates prior snapshots before any rehydration runs. Forced re-auth itself follows F0035's 401-auth-failed semantics (not 403).

## Non-Functional Expectations

- **Performance:** Snapshot write before redirect < 100ms; rehydrate on return < 200ms after sign-in completes.
- **Security:** No tokens/PII in the snapshot; sessionStorage isolation + per-user namespace + 1h TTL + 256 KB cap is the accepted boundary (Security Reviewer may tighten at the feature-action review). Forced re-auth triggers only on `401-auth-failed`, not `403`.
- **Reliability:** Oversize and storage-unavailable cases degrade to route-only preservation with a user-visible message; TTL expiry is silent.

## Dependencies

**Depends On:**
- F0036-S0005 — the engine-backed RHF attribute form that can register.
- F0036-S0004 — pinned version that the restored form must rebind to.
- F0035-S0003 (archived) — the forced-re-auth + restore mechanism and the no-auto-replay mandate this story consumes.
- F0036-S0007 — the shared registration helper (this story uses it; if sequenced after S0007, it consumes the helper directly).

**Related Stories:**
- F0036-S0008 — the CRUD-form analogue of this story.

## Business Rules

1. **No mutation auto-replay (ABSOLUTE).** Inherited from F0035 operator mandate; the user re-saves explicitly.
2. **Per-user snapshot isolation + 1h TTL + sign-out clear.** Inherited from F0035.
3. **Restore rebinds to the pinned version.** The restored form validates against the version the user edited, not the now-active one.

## Out of Scope

- Changing F0035 session-continuity behavior (consumed as-is).
- Server-side draft persistence (snapshots are browser-local).
- CRUD-form preservation (S0008).

## UI/UX Notes

- On return: the F0035 inline notification ("We saved your edits while you signed in. Click Save when ready.") appears near the panel; Save is enabled.
- Oversize fallback uses the F0035 "unable to preserve your edits" message.

## Questions & Assumptions

**Resolved (Phase B / grounding):**
- **Does `DirtyFormRegistration` map cleanly onto RHF?** Yes — RHF `getValues()` and `formState.dirtyFields` provide `getValues`/`isDirty`/`getDirtyFieldPaths` (after flattening). No F0035 change needed; the adapter lives in the engine's registration hook (resolves PRD Risk #3).

**Assumptions (to be validated):**
- A typical Cyber attribute payload is far below the 256 KB cap (it is — ~10 fields).

## Definition of Done

- [ ] Acceptance criteria met (restore journey, pinned-version rebind, oversize/skip, cross-user/TTL/sign-out, dirty-path flattening)
- [ ] Edge cases handled (oversize, storage-unavailable, cross-user, TTL, sign-out)
- [ ] Permissions enforced (per-user snapshot isolation; 401-only trigger)
- [ ] Audit/timeline logged (only explicit re-save emits the entity event; no auto-replay event ever)
- [ ] Tests pass (unit: dirty-path flattening, form_key; integration: 401→redirect→rehydrate; E2E: full forced-re-auth journey with a dirty Cyber form)
- [ ] Documentation updated (GETTING-STARTED notes the attribute-form registration + restore)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
