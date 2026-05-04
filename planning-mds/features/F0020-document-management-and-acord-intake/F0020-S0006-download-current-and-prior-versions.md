---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0006: Download a document for current and prior versions

**Story ID:** F0020-S0006
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Download a document for the current version and any prior version
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, coordinator, broker, or MGA contact
**I want** to download the current or any prior version of a document
**So that** I can attach the exact ACORD form, loss run, or financial spreadsheet that an underwriting decision relied on

## Context & Background

Download is the only operation that streams binary content out of the document subsystem. It must respect the same parent + classification ABAC as detail and list, must record an audit event in the sidecar JSON, and must remain quick enough for routine use.

## Acceptance Criteria

**Happy Path — Current version:**
- **Given** a user with `document:read` access (parent + classification)
- **When** they request `GET /api/documents/{id}/versions/latest/binary`
- **Then** the server resolves `versions[]` to the latest `available` version
- **And** the response is `200 OK` with `Content-Type` matching the binary's recorded MIME, `Content-Disposition: attachment; filename="{logicalName}-v{N}.{ext}"`, and the binary body
- **And** an `events: [{kind: "downloaded", at, byUserId, version: N}]` row is appended to the sidecar JSON

**Happy Path — Specific version:**
- **Given** a `versionNumber` referencing an `available` version
- **When** the user requests `GET /api/documents/{id}/versions/{versionNumber}/binary`
- **Then** that exact version is streamed
- **And** the audit event records the requested `version` number

**Quarantined or failed-promote:**
- **Given** the requested version is in `quarantined` or `failed_promote` status
- **When** the user attempts to download
- **Then** the response is HTTP 409 `{code: "version_not_available", status}`

**Forbidden:**
- **Given** the user lacks access (parent or classification)
- **When** they attempt to download any version
- **Then** the response is HTTP 403 `{code: "document_access_denied"}` and no audit event is recorded for the unauthorised user

**Performance:**
- p95 time-to-first-byte ≤ 1 s on a developer laptop / typical CI runner for a 5 MB file.

**Alternative Flows / Edge Cases:**
- Range requests (HTTP `Range` header) → MVP does not support partial content; server responds `200 OK` with the full body and ignores the header.
- Concurrent downloads of the same version → served independently; each emits its own audit event.
- Sidecar JSON missing on disk between request validation and stream → respond `404 Not Found` with `{code: "document_not_found"}` and emit no audit event.

**Checklist:**
- [ ] Endpoint exists at `GET /api/documents/{id}/versions/{ref}/binary` where `ref` is `latest` or an integer
- [ ] Streams from disk; never buffers the full file in memory
- [ ] Sets `Content-Type` and `Content-Disposition: attachment` with deterministic filename
- [ ] Appends `downloaded` event to sidecar JSON for authorised requests only
- [ ] Returns 409 for quarantined / failed_promote, 403 for denied, 404 for missing

## Data Requirements

**Sidecar JSON additions:**
- `events[]`: append `{kind: "downloaded", at, byUserId, version}` row.

**Validation Rules:**
- `version` query/path must be `latest` or a positive integer that exists in `versions[]`.

## Role-Based Visibility

**Roles that can download:**
- Any role granted `document:read` on the parent + classification gate from S0009. Restricted documents require both `document:read:restricted` on the parent and the user's role policy in `configuration/casbin-document-roles.yaml`.

**Data Visibility:**
- External brokers / MGAs can only download `public` versions on parents they can read.

## Non-Functional Expectations

- Performance: Time-to-first-byte ≤ 1 s p95 for a 5 MB file.
- Security: Path resolution must confirm the binary path lives inside the parent's directory before streaming.
- Reliability: Streaming must terminate cleanly on client disconnect without leaving partial state.

## Dependencies

**Depends On:**
- F0020-S0003 — Status states (only `available` versions are downloadable).
- F0020-S0009 — Classification ABAC.

**Related Stories:**
- F0020-S0005 — Detail view exposes per-version download buttons.

## Business Rules

1. **Audit only on success:** `downloaded` event is appended only when the response will actually stream bytes. 403/404/409 do not write events.
2. **Path safety:** Binaries are resolved through `documentId → sidecar JSON → version filename`; no client-supplied path is ever concatenated into the filesystem path.
3. **No content rewriting:** The bytes streamed are byte-for-byte identical to the stored binary; no transformation, redaction, or watermark is applied in MVP.

## Out of Scope

- Range / resumable downloads (Future).
- Watermarking, redaction, or DRM (Future).
- Bulk zip download of multiple documents (Future).

## UI/UX Notes

- Screens involved: Document Detail per-version download buttons; Documents List row "view" action that opens detail (no inline download from the list in MVP).
- Key interactions: click triggers a browser download; the toast shows file name and size on success; error toasts surface 403/404/409 with friendly copy.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Browser download dialogs are acceptable UX for MVP; an in-app download manager is Future.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (`downloaded` event on success)
- [ ] Tests pass (including 5 MB stream perf test)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
