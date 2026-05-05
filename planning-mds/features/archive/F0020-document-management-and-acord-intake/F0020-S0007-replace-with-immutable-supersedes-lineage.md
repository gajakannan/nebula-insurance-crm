---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0007: Replace a document with immutable supersedes lineage

**Story ID:** F0020-S0007
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Replace a document creating an immutable new version with supersedes lineage
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** broker, MGA, distribution user, or coordinator
**I want** to replace an existing document with a new binary while keeping the previous version intact
**So that** corrections to ACORD forms, loss runs, or financials never erase the audit history Nebula relies on

## Context & Background

Replace is the versioning operation. It writes a new binary as `-v{N+1}` colocated next to the existing versions and appends to the sidecar JSON's `versions[]` and `events[]`. Previous binaries are never deleted by replacement; only the retention sweep (S0011) can remove them, and only when a per-type retention policy elapses.

## Acceptance Criteria

**Happy Path — Replace accepted:**
- **Given** a user with `document:replace` on the document (parent + classification gate from S0009)
- **And** a `pdf | png | docx | xlsx | csv` file ≤ 5 MB
- **When** they submit `PUT /api/documents/{id}/replace` with the new binary
- **Then** the new binary is written to `…/quarantine/{upload-id}` first
- **And** the sidecar JSON `versions[]` appends `{ n: N+1, fileName: "{logicalName}-v{N+1}.{ext}", status: "quarantined", uploadedAt, uploadedByUserId, supersedes: N }`
- **And** an `events: [{kind: "replaced", at, byUserId, fromVersion: N, toVersion: N+1}]` row is appended
- **And** the response is `202 Accepted` with `version: N+1, status: "quarantined"`
- **And** S0003's promotion worker promotes the new binary after 60 s, flipping `versions[N].status` to `available`

**Type / size enforcement:**
- Same enforcement as S0001 (415 / 413 / 400). Replacement does not bypass any upload-side validation.

**Classification preserved:**
- **Given** the existing document has `classification = confidential`
- **When** a user without `document:create:confidential` attempts to replace it
- **Then** the request is rejected with HTTP 403 `{code: "classification_access_denied"}`
- **And** classification is never silently changed by the replace operation (use S0008 to change classification)

**Concurrent replace:**
- **Given** two users issue replacements concurrently
- **When** both pass validation
- **Then** the server serialises them via a per-document lock; both versions land sequentially with `n` increasing monotonically
- **And** both emit their own `replaced` events with distinct `fromVersion / toVersion` rows

**Cannot replace a quarantined version:**
- **Given** `versions[N].status = quarantined`
- **When** a user attempts to replace `v{N}`
- **Then** the request is rejected with HTTP 409 `{code: "version_not_available"}`

**Cannot mutate prior versions:**
- **Given** any existing `versions[m]` where `m < N`
- **When** any operation runs
- **Then** the binary file for `versions[m]` is never modified or deleted by anything except the retention sweep (S0011)

**Alternative Flows / Edge Cases:**
- Replace with the same byte-for-byte content (idempotent) → still creates a new version row; users are not protected from "wasted" versions in MVP.
- Replace with a different MIME type than the original (e.g. PDF → PNG) → allowed; new version inherits the parent's `type` taxonomy entry but the rendered preview switches accordingly on detail.

**Checklist:**
- [ ] Endpoint at `PUT /api/documents/{id}/replace`
- [ ] New version written via the same quarantine + promote pipeline as upload
- [ ] `versions[]` appended with `supersedes` pointing to the previous version `n`
- [ ] `events[]` audit / timeline trail appended with a `replaced` row (`fromVersion`, `toVersion`, `byUserId`, `at`)
- [ ] Per-document lock serialises concurrent replaces
- [ ] Prior binaries never modified by replace
- [ ] Classification cannot change via replace

## Data Requirements

**Sidecar JSON updates:**
- `versions[]`: append next entry with `supersedes`.
- `events[]`: append `replaced` row.

**Validation Rules:**
- All upload-side rules from S0001.
- Replacement of a `quarantined` or `failed_promote` version is forbidden.

## Role-Based Visibility

**Roles that can replace:**
- `document:replace` granted by parent ABAC + classification gate from S0009.

**Data Visibility:**
- Replacement does not change who can see existing prior versions. S0006 download access is unchanged.

## Non-Functional Expectations

- Performance: Same upload performance budget as S0001 (handler completes within 1 s after byte receipt for a 5 MB file).
- Security: Path resolution must confirm the new binary is colocated next to existing versions and within the parent directory.
- Reliability: Per-document lock prevents lost-update of `versions[]`.

## Dependencies

**Depends On:**
- F0020-S0001 — Upload contract.
- F0020-S0003 — Quarantine and promote.
- F0020-S0009 — Classification gating.

**Related Stories:**
- F0020-S0005 — Replace launched from detail view.
- F0020-S0006 — Prior versions remain downloadable.
- F0020-S0011 — Retention sweep is the only way prior versions are removed.

## Business Rules

1. **Versioning is colocated and immutable:** Each version has its own binary file; previous binaries are never overwritten or deleted by replacement.
2. **Sidecar JSON carries the chain:** `versions[N+1].supersedes = N` is the canonical link; the file system layout merely backs it up.
3. **Replace does not reclassify:** Classification changes are explicit (S0008); replace is binary-only.
4. **Per-document lock is structural:** Concurrent replace is allowed but linearised; lost updates are not acceptable.

## Out of Scope

- Manual deletion of a specific version (Future — only retention sweep removes versions in MVP).
- Reverting to a prior version as the "current" version (Future — clients can already download any prior version; promoting an older version to "latest" is a Future operation).

## UI/UX Notes

- Screens involved: Document Detail "Replace" action.
- Key interactions: file picker constrained to allowed types, classification displayed read-only with a hint to use the metadata edit (S0008) to change it, success toast names the new version number.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- "Promote an older version to latest" is not needed in MVP — users can re-upload as a new version if they want a current copy of an older payload.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (`replaced` event)
- [ ] Tests pass (including concurrency + immutability tests)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0007-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
