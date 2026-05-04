---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0005: Document detail view with preview and provenance

**Story ID:** F0020-S0005
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Document detail view with preview, version history, and provenance
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, coordinator, broker, or MGA contact
**I want** to inspect a single document with an inline preview, full metadata, version history, and a provenance trail
**So that** I can understand exactly what I am looking at, who uploaded each version, and what changed without downloading anything

## Context & Background

The detail view is where the sidecar JSON's full value surfaces — version history, classification, parent linkage, uploader identity, audit timestamps, provenance (e.g., template source for materialized template copies), and the small inline preview. This story owns the read shape and rendering of a single document; binary download is owned by S0006 and replace/version by S0007.

## Acceptance Criteria

**Happy Path — Detail returned:**
- **Given** a user with `document:read` access (parent + classification per S0009)
- **When** they request `GET /api/documents/{documentId}`
- **Then** the response includes the sidecar JSON shape with `documentId`, `logicalName`, `parent`, `classification`, `type`, `versions[]` (with status), `events[]`, `provenance`, and `previewUrls` (one per available version) for renderable types
- **And** the UI renders the small preview frame inline for the latest available version

**Preview support:**
- **Given** the latest available version is `pdf | png`
- **When** the detail page loads
- **Then** the preview frame renders inline using a built-in renderer (no third-party service)
- **Given** the latest available version is `docx | xlsx | csv`
- **When** the detail page loads
- **Then** the preview frame shows a placeholder ("Preview not available — download to view") and the download action remains visible

**Provenance:**
- **Given** the document was materialized from a template (S0012)
- **When** the detail view loads
- **Then** the Provenance section names the source template and the user who materialized it

**Quarantined / failed-promote:**
- **Given** the latest version is `quarantined` or `failed_promote`
- **When** the detail is requested
- **Then** the response includes `versions[]` and metadata but `previewUrls` is empty for those versions and the UI shows the badge instead of preview

**Forbidden:**
- **Given** the user lacks access (parent or classification)
- **When** they request the detail
- **Then** the response is HTTP 403 `{code: "document_access_denied"}` and no metadata is leaked beyond the bare error

**Alternative Flows / Edge Cases:**
- Document with one version → version history list shows a single row.
- Document with N > 10 versions → version history paginates client-side after the latest 10 with a "show more" affordance.
- Sidecar JSON event row references a deleted user → event row shows the original `byUserId` and a `(user removed)` indicator instead of a name.

**Checklist:**
- [ ] Endpoint at `GET /api/documents/{id}` returns sidecar JSON shape augmented with `previewUrls`
- [ ] Inline preview renders for `pdf, png` only in MVP
- [ ] Version history shows latest N versions with download links per version (download owned by S0006)
- [ ] Provenance block visible when `provenance.source` is set
- [ ] HTTP 403 on access denied; no metadata leaked
- [ ] Detail response surfaces the sidecar JSON `events[]` audit / timeline trail in the Provenance section so users can see who uploaded, replaced, classified, or downloaded each version (read-only; this story does not append events itself — mutations happen in S0001/S0006/S0007/S0008)

## Data Requirements

**Response augmentations beyond raw sidecar JSON:**
- `previewUrls`: array aligned with `versions[]` — `null` for non-renderable versions or non-available status.

**Validation Rules:**
- Detail responses are always computed from the live sidecar JSON; no cache layer in MVP.

## Role-Based Visibility

**Roles that can view detail:**
- Any role granted `document:read` on the parent + classification gate from S0009.

**Data Visibility:**
- Same gating as the list view; restricted documents are 403 to disallowed users (not 404, since user already navigated from a list that excluded them).

## Non-Functional Expectations

- Performance: Detail render < 1.5 s on a developer laptop (excludes preview render which depends on file size).
- Security: Preview rendering must never exfiltrate via third-party CDN; in-process renderer only.
- Reliability: Detail view degrades gracefully when preview fails (placeholder + download still works).

## Dependencies

**Depends On:**
- F0020-S0001, S0003 — Sidecar JSON shape and statuses.
- F0020-S0009 — Classification gating.

**Related Stories:**
- F0020-S0006 — Download links rendered here.
- F0020-S0007 — Replace action launched from this view.
- F0020-S0008 — Metadata edit launched from this view.
- F0020-S0012 — Provenance section reflects template source.

## Business Rules

1. **No preview for office formats in MVP:** `docx, xlsx, csv` show a placeholder. Adding a real Office renderer is Future scope.
2. **No third-party preview CDN:** Renderer is in-process to keep `confidential` and `restricted` data inside Nebula's trust boundary.
3. **Detail renders the live sidecar JSON:** Edits made by S0008 are visible on the next refresh without cache invalidation.

## Out of Scope

- Office-format previews (Future).
- Side-by-side version comparison (Future).
- Comments / annotations (Future).

## UI/UX Notes

- Screens involved: Document Detail (Desktop layout in PRD ASCII; narrow variant stacks zones in `Preview → Metadata → Version History → Provenance` order).
- Key interactions: open from list row, switch versions in history, launch Replace and Edit Metadata, download per version.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Inline preview for `pdf` and `png` is enough demo value at MVP.
- Provenance is read-only; users cannot edit it.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (parent + classification)
- [ ] Audit/timeline logged (N/A — read-only view; download events handled by S0006)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
