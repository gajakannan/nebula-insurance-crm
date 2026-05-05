---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0012: Document templates library

**Story ID:** F0020-S0012
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Document templates library (broker boilerplate templates with parent-record linking)
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** broker, MGA, distribution user, or coordinator
**I want** to maintain a library of document templates (blank ACORD forms, loss-run spreadsheets, proposal covers) and apply a template to a parent record
**So that** new submissions and renewals start from the right boilerplate without me hunting for the latest version each time

## Context & Background

Templates are document boilerplates kept in a shared corner of the document repository. Applying a template *materialises* a copy of the template binary into the parent's documents folder, recording provenance (source template id) on the new document's sidecar JSON. Templates use the same upload + quarantine + sidecar JSON model — they are not a parallel storage system.

## Acceptance Criteria

**Library upload:**
- **Given** an authorised user (admin, distribution-user, broker-user, mga-user) with `document:create:template`
- **When** they upload a template via `POST /api/document-templates`
- **Then** the binary follows the same allowed-types, size cap, and quarantine + promote rules as S0001/S0003
- **And** the template's sidecar JSON is colocated under `<docroot>/templates/{templateId}/{logicalName}.json`
- **And** the sidecar `type = "template"` and `classification` is one of `public, confidential, restricted` (defaults to `public`)

**Library list:**
- **Given** an authorised user
- **When** they request `GET /api/document-templates?type={t}&classification={c}`
- **Then** the response paginates templates with `templateId, logicalName, type, classification, lastUsedAt, useCount`
- **And** templates the user is forbidden to read by classification (S0009) are excluded

**Materialise to a parent:**
- **Given** a user with `document:create` on the parent
- **When** they call `POST /api/document-templates/{templateId}/link?parent={parentRef}`
- **Then** a copy of the template binary is materialised into the parent's documents folder via the upload-quarantine-promote pipeline
- **And** the new document's sidecar JSON `provenance: { source: "template:{templateId}", materializedAt, byUserId }`
- **And** the template's `useCount` increments and `lastUsedAt` updates
- **And** the response is `202 Accepted` with the new `documentId`

**Parent classification check:**
- **Given** a template at `confidential` classification
- **When** a user without `document:create:confidential` on the parent attempts to link it
- **Then** the request is rejected with HTTP 403 and the template is not materialised

**Templates are versioned like documents:**
- **Given** a user with `document:replace:template`
- **When** they replace the template binary
- **Then** S0007 semantics apply (immutable supersedes lineage on the template's sidecar JSON)

**Templates are subject to retention:**
- **Given** the retention YAML (`perType.template`) sets a retention window
- **When** the sweeper runs (S0011)
- **Then** templates older than their retention are removed (subject to the same hard 10-day MVP ceiling)

**Forbidden:**
- Listing without `document:read:template` returns HTTP 403.
- Linking without parent `document:create` returns HTTP 403.

**Alternative Flows / Edge Cases:**
- A template with `useCount > 0` is removed by retention sweep → already-materialised copies are unaffected (they live on parent records and have their own retention timers).
- Template name collision (two templates with same logical name) → both stored under distinct `templateId`s; the list view shows both with their templateIds.

**Checklist:**
- [ ] Templates stored under `<docroot>/templates/{templateId}/`
- [ ] Same upload + quarantine + promote pipeline as S0001/S0003
- [ ] `POST /api/document-templates`, `GET /api/document-templates`, `POST /api/document-templates/{id}/link?parent=...`
- [ ] `provenance.source = "template:{id}"` on materialised copies
- [ ] Templates respect S0009 classification gates (separate `template` ops in YAML)
- [ ] `useCount` and `lastUsedAt` maintained on every link
- [ ] Sidecar JSON audit / timeline trail records the operation: a `materialised` event on the new parent-side document and a `linked` event on the source template's sidecar JSON (both carry `at`, `byUserId`, and the cross-reference)

## Data Requirements

**Template sidecar JSON additions over a normal document:**
- `templateId` (alias of `documentId` to make cross-references clear)
- `useCount` (int)
- `lastUsedAt` (timestamp)

**Materialised-copy sidecar JSON additions:**
- `provenance.source = "template:{templateId}"`
- `provenance.materializedAt`
- `provenance.byUserId`

**Validation Rules:**
- Template type must be `template`.
- Materialise call requires `parent` reference; rejects with 400 if missing.

## Role-Based Visibility

**Roles that can upload templates:**
- `document:create:template` — granted to admin, distribution-user, broker-user, mga-user, coordinator.

**Roles that can replace / declassify templates:**
- `document:replace:template` — admin and the original uploader's role.

**Roles that can materialise:**
- Any user with `document:create` on the parent and S0009 classification access for the template.

## Non-Functional Expectations

- Performance: Library list p95 ≤ 500 ms for ≤ 50 templates; materialise call returns within 1 s after byte copy completes (excluding the 60 s quarantine wait).
- Security: Templates respect classification just like regular documents; an external broker can never see a `confidential` template.
- Reliability: Failure during materialise leaves no partial parent-side document.

## Dependencies

**Depends On:**
- F0020-S0001, S0003 — Sidecar JSON, quarantine + promote.
- F0020-S0009 — Classification gating.
- F0020-S0011 — Retention.

**Related Stories:**
- F0020-S0005 — Detail view shows `provenance.source = "template:{id}"` for materialised copies.

## Business Rules

1. **Templates are documents:** They use the same on-disk shape, sidecar JSON, quarantine pipeline, classification gates, and retention rules.
2. **Materialise creates an independent copy:** Once linked, the parent-side document has its own version chain, audit log, and retention timer. The template can change or be deleted without affecting the materialised copy.
3. **Provenance is mandatory on materialised copies:** The new document's sidecar JSON always records the source template id.
4. **Templates have a separate `type` taxonomy entry:** They are not categorised as `acord, loss-run, …`; the `type` field on a template's sidecar JSON is literally `template` and the original taxonomy classification (e.g., "ACORD blank") lives in `tags`.

## Out of Scope

- Template parameterisation / variable substitution (Future).
- Per-tenant template inheritance (Future).
- Template approval workflow (Future).

## UI/UX Notes

- Screens involved: Templates Library (Desktop ASCII layout in PRD).
- Key interactions: upload template, list with filters, "link to record" affordance from a parent record's Documents List.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Templates living under the same document repository (just a different top-level folder) is acceptable.
- `useCount` and `lastUsedAt` are sufficient template-usage telemetry for MVP.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (template ops + parent ops + classification)
- [ ] Audit/timeline logged (`materialised` event on the parent-side doc; `linked` event on the template's sidecar JSON)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0012-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
