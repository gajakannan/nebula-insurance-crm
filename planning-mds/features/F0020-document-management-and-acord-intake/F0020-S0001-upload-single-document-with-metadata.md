---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0001: Upload single document with metadata to a parent record

**Story ID:** F0020-S0001
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Upload single document with metadata to a parent record
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, coordinator, broker, or MGA contact
**I want** to upload one insurance document at a time to an account, submission, policy, or renewal record with classification and document type
**So that** every parent record carries the supporting evidence Nebula workflows need

## Context & Background

Single-document upload is the foundational ingest path. It establishes the on-disk repository shape (binary plus colocated sidecar JSON), the quarantine-then-promote pipeline, and the contract that every later story (bulk, replace, list, detail, retention) reuses. The sidecar JSON is the single canonical metadata record per logical document — bulk uploads (S0002) and replacements (S0007) extend the same shape rather than introducing new ones.

## Acceptance Criteria

**Happy Path — Upload accepted:**
- **Given** an authenticated user with `document:create` for the target parent record
- **And** a `pdf | png | docx | xlsx | csv` file ≤ 5 MB
- **When** they submit `POST /api/documents` with `parent`, `classification`, `type`, and the binary
- **Then** the binary is written to `…/quarantine/{upload-id}` with status `quarantined`
- **And** a sidecar JSON is created at `…/{parent-type}/{parent-id}/{logical-doc-name}.json` with version `v1`, classification, parent link, uploader identity, audit timestamp, and `events: [{kind: uploaded, …}]`
- **And** the response is `202 Accepted` with `documentId` and `status: quarantined`

**Happy Path — Promote after 60 s (handed off to S0003):**
- The promotion step is owned by S0003; this story succeeds when the document and sidecar JSON exist in `quarantined` state.

**Rejected — Disallowed type:**
- **Given** a file with extension or detected content-type outside `pdf, png, docx, xlsx, csv`
- **When** the user posts the upload
- **Then** the request is rejected with HTTP 415 and structured `{code: "unsupported_type", allowed: [...]}`
- **And** no quarantine file or sidecar JSON is created

**Rejected — Oversize:**
- **Given** a file > 5 MB
- **When** the user posts the upload
- **Then** the request is rejected with HTTP 413 and structured `{code: "file_too_large", limit_bytes: 5242880}`
- **And** no quarantine file or sidecar JSON is created

**Rejected — Unauthorized parent:**
- **Given** a user without `document:create` for the parent record
- **When** they post the upload
- **Then** the request is rejected with HTTP 403 and `{code: "parent_access_denied"}`

**Alternative Flows / Edge Cases:**
- Duplicate logical filename within the same parent → server appends a deterministic disambiguation suffix in the logical name and surfaces it in the response (no overwrite).
- Empty file (0 bytes) → reject with HTTP 400 `{code: "empty_file"}`.
- Filename with path separators → reject with HTTP 400 `{code: "invalid_filename"}`; never create files outside the parent directory.

**Checklist:**
- [ ] Allowed types enforced as `pdf, png, docx, xlsx, csv`
- [ ] Size cap enforced at 5 MB (5,242,880 bytes)
- [ ] Binary written to quarantine folder, not to the target folder
- [ ] Sidecar JSON colocated at the target folder using basename + `.json`
- [ ] Sidecar JSON includes `versions[].fileName = "{name}-v1.{ext}"`, `classification`, `parent`, `uploaderId`, `auditTimestamps`, `events[]`
- [ ] Response is 202 Accepted with `documentId` and `status: quarantined`
- [ ] No data is mutated when validation fails

## Data Requirements

**Sidecar JSON (initial shape):**
- `documentId`: stable id (e.g., `doc_<ulid>`)
- `logicalName`: basename without version suffix
- `parent`: `{ type, id }`
- `classification`: `public | confidential | restricted`
- `type`: enum from `configuration/taxonomy.yaml`
- `versions[]`: `[{ n: 1, fileName, sizeBytes, sha256, status: "quarantined", uploadedAt, uploadedByUserId }]`
- `events[]`: append-only `[{ kind: "uploaded", at, byUserId, version: 1 }]`
- `provenance`: optional `{ source: "upload" | "template:<id>" }`

**Validation Rules:**
- File extension must match detected content-type (block extension spoofing).
- `classification` must be one of the three tiers.
- `type` must exist in `configuration/taxonomy.yaml`.

## Role-Based Visibility

**Roles that can upload:**
- Any role granted `document:create` on the parent record (combined parent ABAC + classification policy from S0009; in MVP, classification is set on creation and gated by S0009).

**Data Visibility:**
- A user can only target parents they can read; classification rules are applied on the parent's documents listing (S0004) and detail (S0005), not on creation itself, except `restricted` which requires `document:create:restricted` on the parent.

## Non-Functional Expectations

- Performance: Upload request handler returns within 1 s after the bytes are received for a 5 MB file on a typical broadband link (excludes the 60 s quarantine timer).
- Security: Reject extension/content-type mismatches; never write outside the resolved parent directory.
- Reliability: Atomic writes — if the sidecar JSON cannot be written, the quarantined binary is removed.

## Dependencies

**Depends On:**
- F0020-S0009 — Classification policy must exist for `restricted` uploads to be gated.

**Related Stories:**
- F0020-S0002 — Bulk variant of this story.
- F0020-S0003 — Promotes the binary out of quarantine.
- F0020-S0007 — Replaces a document, reusing the same upload contract.

## Business Rules

1. **Single sidecar JSON per logical document:** No matter how many versions exist, there is exactly one JSON file. New versions append to `versions[]` and `events[]`.
2. **Quarantine before target:** Binaries do not appear under the parent folder until the promote step (S0003) completes; intermediate state is only listed via the explicit "scanning" indicator.
3. **No silent overwrite:** A logical-name collision triggers a deterministic disambiguation suffix; the original sidecar JSON is never replaced by a new upload.
4. **Filename-derived path safety:** Server resolves and confirms the target path is within the parent directory before any write; path-traversal attempts return HTTP 400.

## Out of Scope

- Bulk multi-file upload (covered by S0002).
- Promotion semantics (covered by S0003).
- Replace / version (covered by S0007).
- Metadata-only edits (covered by S0008).

## UI/UX Notes

- Screens involved: Upload Dialog (single file mode); list shows the new doc with a ⏳ scanning… badge until S0003 promotes it.
- Key interactions: file chooser, classification dropdown defaulting to `public`, type dropdown, submit; error banners for unsupported type / oversize.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Local filesystem is acceptable as the MVP storage abstraction (Architect confirms in Phase B).
- 5 MB cap is a hard cap; per-type overrides are not in MVP.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (parent ABAC + classification gate for `restricted`)
- [ ] Audit/timeline logged in sidecar JSON `events[]`
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
