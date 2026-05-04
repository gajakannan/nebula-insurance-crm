---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0008: Update document metadata without creating a new binary version

**Story ID:** F0020-S0008
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Update document metadata (classification, type, tags) without creating a new binary version
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, or coordinator
**I want** to change a document's classification, type, or tags without uploading a new binary
**So that** I can correct misclassifications and refine taxonomy assignments without polluting the version history

## Context & Background

Metadata edits are common (a doc was uploaded as `public` but should be `confidential`, a type taxonomy was selected wrong, a tag is missing). These edits do not create a new binary or a new version row — they append a `metadata_edited` event to the sidecar JSON `events[]`. The change is fully audited but does not enter the version chain.

## Acceptance Criteria

**Happy Path — Edit classification:**
- **Given** a user with `document:update:metadata` on the document (parent ABAC + classification gate)
- **When** they submit `PATCH /api/documents/{id}/metadata` with `{ classification: "confidential" }`
- **Then** the sidecar JSON `classification` field is updated
- **And** an `events: [{kind: "classified", at, byUserId, from: "public", to: "confidential"}]` row is appended
- **And** no new `versions[]` entry is created
- **And** the response is `200 OK` with the updated metadata

**Happy Path — Edit type and tags:**
- **Given** a payload `{ type: "loss-run", tags: ["renewal-2026-q1"] }`
- **When** the request is submitted
- **Then** `type` and `tags` are updated and an `events: [{kind: "metadata_edited", at, byUserId, changes: {...}}]` row is appended

**Validation:**
- `classification` must be one of `public, confidential, restricted`.
- `type` must exist in `configuration/taxonomy.yaml`.
- `tags`: 0-10 entries, each ≤ 32 characters, no commas.

**Restricted classification escalation:**
- **Given** the user does not have `document:create:restricted` on the parent
- **When** they attempt to set `classification: "restricted"`
- **Then** the request is rejected with HTTP 403 `{code: "classification_access_denied"}`
- **And** the change is not applied

**Idempotency:**
- **Given** a request that does not change any value
- **When** the request is submitted
- **Then** the response is `200 OK` with the existing metadata
- **And** no event row is appended

**Concurrency:**
- **Given** two metadata edits arriving concurrently
- **When** both pass validation
- **Then** the per-document lock from S0007 serialises them and both events land in order

**Forbidden:**
- **Given** the user lacks `document:update:metadata`
- **When** they attempt the edit
- **Then** the response is HTTP 403 `{code: "metadata_access_denied"}`

**Alternative Flows / Edge Cases:**
- Edit on a document where the latest version is `quarantined` → allowed; metadata edits are version-independent.
- Edit on a document where every version is `failed_promote` → allowed; metadata edits do not depend on availability.
- Setting `type` to a value that was removed from `taxonomy.yaml` after a previous upload used it → reject with 400 and a clear message; existing values are not auto-migrated.

**Checklist:**
- [ ] Endpoint at `PATCH /api/documents/{id}/metadata` accepting `classification`, `type`, `tags`
- [ ] No `versions[]` entry is created
- [ ] Sidecar JSON `events[]` audit / timeline trail appended with `classified` (when classification changes, carrying `from`/`to`) or `metadata_edited` (any other change, carrying the structured diff) for every accepted edit
- [ ] Idempotent when no fields change (no audit row appended when nothing changed)
- [ ] Restricted-classification escalation gated by S0009

## Data Requirements

**Sidecar JSON updates:**
- Top-level `classification`, `type`, `tags` fields.
- `events[]`: `classified` row when classification changes; `metadata_edited` row for any other change.

**Validation Rules:**
- `classification` ∈ allowed tiers.
- `type` ∈ `configuration/taxonomy.yaml`.
- `tags`: ≤ 10, ≤ 32 chars, no commas.

## Role-Based Visibility

**Roles that can edit metadata:**
- `document:update:metadata` on the document; restricted-classification escalation requires `document:create:restricted`.

**Data Visibility:**
- Edits are visible to anyone who can see the document at all (no role-specific obfuscation).

## Non-Functional Expectations

- Performance: Edit completes in < 200 ms p95.
- Security: Re-validate access on every edit; no implicit trust based on a prior list/detail call.
- Reliability: Per-document lock prevents racy event ordering.

## Dependencies

**Depends On:**
- F0020-S0001 — Sidecar JSON shape.
- F0020-S0009 — Classification gating.

**Related Stories:**
- F0020-S0005 — Edit launched from detail view.
- F0020-S0007 — Replace operation is independent of metadata.

## Business Rules

1. **Metadata edits are not versions:** They never create a `versions[]` entry; the binary stays untouched.
2. **Audit every change:** `events[]` carries before/after values for `classification`; for other fields, it carries the diff.
3. **Taxonomy is authoritative:** `type` values are bound to the live `configuration/taxonomy.yaml`; edits cannot bypass the taxonomy.
4. **Restricted is gated separately:** Setting `classification: "restricted"` always requires the elevated permission; downgrading from `restricted` requires `document:declassify` (created in S0009).

## Out of Scope

- Bulk metadata edits across many documents (Future).
- Custom per-tenant taxonomy overrides (Future).

## UI/UX Notes

- Screens involved: Document Detail metadata block — "edit" affordance opens an inline form.
- Key interactions: classification dropdown, type dropdown, tags chip input; save persists immediately and refreshes the metadata block + events list.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- The 10-tag, 32-char-per-tag limit is enough for MVP.
- `metadata_edited` event payload need not redact tag values; tags are not sensitive in MVP.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (`classified` and `metadata_edited` events)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0008-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
