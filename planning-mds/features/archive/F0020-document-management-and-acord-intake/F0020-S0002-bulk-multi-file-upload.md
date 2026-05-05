---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0002: Bulk multi-file upload to a parent record

**Story ID:** F0020-S0002
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Bulk multi-file upload (drag and drop) to a parent record
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** broker, MGA contact, distribution user, or coordinator
**I want** to drag and drop multiple insurance documents at once onto a parent record
**So that** I can attach a full submission packet (ACORD form, loss runs, financials, supporting attachments) without uploading them one at a time

## Context & Background

Brokers send packets, not single files. Bulk upload reuses the upload contract from S0001 — every dropped file produces its own sidecar JSON and goes through the same quarantine + promote pipeline. Bulk is a UI affordance and a request batching layer; it does not introduce a new on-disk shape.

## Acceptance Criteria

**Happy Path — Bulk accepted:**
- **Given** an authenticated user with `document:create` for the target parent
- **And** 2-N files dropped where every file is `pdf | png | docx | xlsx | csv` and each ≤ 5 MB
- **When** they submit a multi-part `POST /api/documents` with the batch
- **Then** every file follows the S0001 contract independently
- **And** the response is `202 Accepted` with `documents: [{ documentId, logicalName, status: "quarantined" }, ...]`
- **And** the list view shows all N files with the ⏳ scanning… badge until S0003 promotes each

**Mixed Validity — Partial accept:**
- **Given** a batch where some files violate the type or size rule
- **When** the batch is submitted
- **Then** valid files are accepted and quarantined as in the happy path
- **And** invalid files are reported per-file in the response under `rejected: [{ index, logicalName, code, detail }]`
- **And** the response status is `207 Multi-Status` when at least one file was accepted and at least one was rejected

**Rejected — All invalid:**
- **Given** a batch where every file fails validation
- **When** the batch is submitted
- **Then** the response is `400 Bad Request` with `rejected[]` listing each failure and no documents created

**Throttle / Cap:**
- **Given** a batch larger than 25 files in a single request
- **When** the batch is submitted
- **Then** the request is rejected with HTTP 413 `{code: "batch_too_large", limit: 25}`

**Alternative Flows / Edge Cases:**
- Two files in the same batch with the same logical name → both are accepted with deterministic disambiguation suffixes (no collision).
- Network interruption mid-batch → already-buffered files that completed remain accepted; client must retry the missing files (server does not retain partial-batch transaction state).
- A user drops a folder → only files at the top level of the folder are processed; nested folders are ignored with a per-entry `rejected[].code = "folder_entry_unsupported"`.

**Checklist:**
- [ ] Drag-and-drop on the Documents List opens the Upload Dialog with files preloaded
- [ ] Per-file progress visible in the dialog
- [ ] Server returns 207 on partial success, 400 on total rejection, 202 on full success
- [ ] Each accepted file results in its own sidecar JSON under the parent
- [ ] Batch cap enforced at 25 files per request
- [ ] Each accepted file appends an `uploaded` audit event to its sidecar JSON `events[]` (the audit/timeline trail per file is identical to S0001 and is required for every accepted member of the batch)

## Data Requirements

**Per-file inputs (same as S0001):**
- `parent`, `classification` (default-to-batch unless overridden), `type` (default `auto-detect` per file with manual override)

**Validation Rules:**
- Per-file rules from S0001 apply.
- Batch size: 1 < N ≤ 25.
- Sum of binary sizes per request ≤ 50 MB to keep request handlers bounded.

## Role-Based Visibility

**Roles that can bulk upload:**
- Same as S0001 — `document:create` on the parent. `restricted` classification requires `document:create:restricted`.

**Data Visibility:**
- Same as S0001.

## Non-Functional Expectations

- Performance: Front-end queue handles up to 25 files concurrently with bounded parallelism; per-file UI progress updates ≤ 250 ms after each chunk.
- Security: Per-file content-type and extension validation; one bad file does not poison sibling files.
- Reliability: Failure of one file's quarantine write does not roll back already-accepted siblings; the response makes the partial outcome explicit.

## Dependencies

**Depends On:**
- F0020-S0001 — Single-file upload contract.
- F0020-S0009 — Classification gate on `restricted` uploads.

**Related Stories:**
- F0020-S0003 — Promotes each accepted binary out of quarantine independently.

## Business Rules

1. **Per-file independence:** Each file in a batch is its own document; one validation failure never affects sibling files.
2. **Default classification per batch:** The dialog applies a single default classification to the batch; users can override per file before submitting.
3. **Cap is structural, not heuristic:** The 25-file batch cap and 50 MB total request cap are enforced server-side and not adjustable by the client.

## Out of Scope

- Resumable uploads (Future).
- Background continuation after the user navigates away (Future).
- Folder traversal (only top-level files in a dropped folder are accepted).

## UI/UX Notes

- Screens involved: Documents List (drop zone), Upload Dialog (queue view).
- Key interactions: drag and drop, per-file progress bars, per-file classification override, "Cancel queued" action before submit, post-submit toast summarising accepted vs. rejected counts.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- 25 files per batch is sufficient for a typical broker submission packet.
- Concurrent quarantine timers (one per file) are acceptable; the system does not need to deduplicate timers across the batch.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged per file in each sidecar JSON
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
