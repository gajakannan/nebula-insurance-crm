# Story F0036-S0004: Pin-During-Edit Binding to (productVersionId, stage)

## Story Header

**Story ID:** F0036-S0004
**Feature:** F0036 — Form Engine and Form-State Preservation
**Title:** Pin-During-Edit Binding to (productVersionId, stage)
**Priority:** High
**Phase:** MVP
**Workstream:** A — Dynamic product-attribute engine

## User Story

**As a** Cyber Underwriter
**I want** the attribute form I opened to keep the same product definition for my whole editing session
**So that** a product update published by someone else mid-edit does not silently change my fields or my validation under me

## Context & Background

ADR-021 mandates pin-during-edit: the form binds to `(productVersionId, stage)` at open time and stays pinned for the session; activating a newer product version while a form is open must not rebind the open form. The user only sees a newer version after reopening or starting a new workflow. This protects against mid-session schema drift and validation surprises. This story makes the engine's binding explicit and stable.

## Acceptance Criteria

**Happy Path — Binding at open:**
- **Given** an Underwriter opens an attribute form for a product at `(productVersionId=V1, stage=Quote)`
- **When** the form initializes
- **Then** the engine records the pinned `(productVersionId, stage)` and loads exactly the V1 bundle (data-schema, ui-schema, rules)
- **And** validation and rendering for the session use V1 only
- **And** when the form is later saved, the host entity's existing update timeline event records the product version used (no new event class)

**Happy Path — No rebind on activation:**
- **Given** the V1 form is open and dirty
- **When** a newer product version V2 is activated elsewhere
- **Then** the open form keeps rendering and validating against V1 (no field add/remove, no validation change)
- **And** no remount or value loss occurs as a result of the activation

**Edge Case — New workflow sees new version:**
- **Given** V2 is now active
- **When** the user starts a new attribute form (reopen / new workflow)
- **Then** the new form binds to V2
- **And** the prior V1 session, if still open in another context, remains on V1

**Edge Case — Missing/invalid pinned version:**
- **Given** a pinned `(productVersionId, stage)` that cannot be resolved to a bundle
- **When** the form attempts to open
- **Then** the engine surfaces a controlled "product definition unavailable" error rather than falling back to a different version silently

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Attribute form opened from a host screen | Edit fields over a session during which a new version activates | Editable against the pinned version only | The pinned `(productVersionId, stage)` is part of what the host save submits, so the persisted attributes are interpreted against the version the user actually edited | A test opens V1, activates V2, asserts the open form still renders/validates V1 fields and values; on save, the submitted payload carries the pinned version | Host-screen roles/states (unchanged) |

Required checks:
- [x] Render-only behavior cannot satisfy: the test must prove the open form does NOT change when a new version activates (a behavioral, not snapshot, assertion).
- [x] Save path validation: the pinned version travels with the save; backend validates against it.
- [x] Audit/timeline: the host entity's existing save event records which product version was used (no new event class here).
- [x] Tests prove pin holds across an activation event and that a new form binds to the new version.

## Data Requirements

**Required fields:**
- `productVersionId`: the pinned product version identifier
- `stage`: the workflow stage the form was opened in
- The pinned bundle triplet (data-schema, ui-schema, rules) resolved once at open

**Validation Rules:**
- The pinned tuple is immutable for the session; re-resolution only happens on a fresh open.
- Unresolvable pinned version → controlled error, never a silent fallback.

## Role-Based Visibility

**Roles that experience this:**
- All roles that can open an attribute form (Underwriter primarily). Pinning behavior is identical across roles; it is not an authorization feature.

**Data Visibility:**
- No change to data visibility; pinning governs which schema version renders, not who can see what. Unauthorized access is still blocked upstream by host-route auth.

## Non-Functional Expectations

- **Performance:** Bundle resolution happens once per open (< 100ms with a warm cache); activation events do not trigger re-fetch for open forms.
- **Security:** Pinning has no auth impact; it prevents validation drift, not access. No tokens/PII involved.
- **Reliability:** An activation race (version flips during open) resolves deterministically to the version captured at open.

## Dependencies

**Depends On:**
- F0036-S0003 — the schema-driven, validated form whose version must be pinned.
- F0034 — the product version + bundle activation model that defines `(productVersionId, stage)`.

**Related Stories:**
- F0036-S0006 — a preserved/restored form must rehydrate against the same pinned version.

## Business Rules

1. **Pin at open (ADR-021).** The form binds to `(productVersionId, stage)` at open and never rebinds for the session.
2. **New version only on new workflow.** Reopening or starting a new workflow is the only way to pick up a newer version.
3. **No silent fallback.** An unresolvable pinned version is an explicit error.

## Out of Scope

- Cross-session/cross-tab coordination of the pinned version.
- Notifying the user that a newer version exists (informational banners are a future enhancement).
- The activation mechanism itself (owned by F0034 backend).

## UI/UX Notes

- No new primary UI; pinning is invisible in the happy path. An optional, non-blocking note ("A newer product version is available; reopen to use it") may be added later but is out of scope here.

## Questions & Assumptions

**Resolved (Phase B):**
- The pinned version must also survive an F0035 forced-re-auth restore (S0006) — the restored form rebinds to the snapshot's pinned version, not the currently-active one. Recorded in S0006 and the ADR.

**Assumptions (to be validated):**
- The host screens can supply `(productVersionId, stage)` at form open from existing F0034 context.

## Definition of Done

- [ ] Acceptance criteria met (bind at open, no rebind on activation, new workflow sees new version, unresolvable error)
- [ ] Edge cases handled (activation race, missing version)
- [ ] Permissions enforced (N/A — pinning is not an auth control; host auth unchanged)
- [ ] Audit/timeline logged (host save records the product version used)
- [ ] Tests pass (integration: open V1 → activate V2 → form stays V1; reopen → V2)
- [ ] Documentation updated (GETTING-STARTED notes the pin-during-edit contract)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
