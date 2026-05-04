---
template: feature
version: 1.1
applies_to: product-manager
---

# F0020: Document Management & ACORD Intake

**Feature ID:** F0020
**Feature Name:** Document Management & ACORD Intake
**Priority:** Critical
**Phase:** CRM Release MVP

## Feature Statement

**As an** underwriter, coordinator, distribution user, broker, or MGA contact
**I want** insurance documents stored, classified, versioned, and discoverable in Nebula
**So that** submissions, accounts, policies, and renewals carry complete supporting evidence and teams stop chasing files through email

## Business Objective

- **Goal:** Make documents a first-class part of the Nebula workflow with consistent storage, classification, and retrieval rules.
- **Metric:** Submission completeness coverage, document retrieval latency (p95), and document attachment rate per submission/policy/renewal.
- **Baseline:** Critical insurance files are scattered across email, shared drives, and broker portals.
- **Target:** Required and supporting documents are uploaded, discoverable on the parent record, and accessible within performance budgets.

## Problem Statement

- **Current State:** Users lose time gathering ACORD forms, loss runs, financials, and proposals from disparate sources.
- **Desired State:** Nebula stores binaries with sidecar JSON metadata, organizes documents by classification and parent record, and exposes a soft completeness signal others reuse.
- **Impact:** Faster underwriting, cleaner intake, defensible auditability, and a foundation other CRM workflows can rely on.

## Scope & Boundaries

**In Scope (MVP):**
- Single-file and bulk multi-file upload of insurance workflow documents to a parent record (account, submission, policy, renewal).
- Allowed types: `pdf`, `png`, `docx`, `xlsx`, `csv`. Maximum binary size: 5 MB per file.
- Quarantine-then-promote ingest workflow with a mocked 60-second scanner (binary stays in `quarantine/` for 60 s before promoting to its target folder).
- Sidecar JSON metadata colocated next to each binary (same basename + `.json`), single per logical document, holding history of metadata changes, version chain, classification, parent links, uploader identity, audit timestamps, and provenance.
- Versioning by binary: same logical document keeps colocated `-v1`, `-v2`, … suffix files; JSON sidecar carries `supersedes` lineage.
- Document classification tiers: `public`, `confidential`, `restricted` — combined with parent-record permissions for access.
- Listing documents on a parent record with classification filters and pagination.
- Document detail view with binary preview frame (small inline preview for supported types), version history, provenance, and download for current + historical versions.
- Metadata-only edits (classification, type, tags) without creating a new binary version; recorded in JSON sidecar history.
- Soft document-completeness signal endpoint (counts by category and classification) for other features to consume — no hard gate.
- Document templates library: brokers and MGAs can upload boilerplate templates and link them to parent records as starting artifacts.
- Repository configuration folder: a reserved `configuration/` directory inside the document repository holds canonical YAML for taxonomy, retention policies, and Casbin role mapping for document operations.
- Retention policy YAML driving a scheduled cleanup; MVP retention ceiling = 10 days per document type. Longer retention is Future scope.

**Out of Scope (MVP):**
- OCR and AI-driven extraction.
- External e-signature workflows.
- Outbound document generation (lives in F0027).
- Real malware scanner integration (MVP uses the 60-second quarantine mock).
- Multi-region or object-storage providers (storage abstraction is designed in but MVP is local filesystem; Architect decides binding in Phase B).

## Acceptance Criteria Overview

- [ ] Authorized users can upload single and multiple documents to any supported parent record using only the allowed file types within the 5 MB cap.
- [ ] Every uploaded binary is held in the quarantine folder for 60 seconds before being promoted to its target folder; only promoted binaries are listable, downloadable, and previewable.
- [ ] Each document has exactly one sidecar JSON colocated with its binary; the JSON tracks version chain, classification, parent links, uploader identity, audit timestamps, and metadata-change provenance.
- [ ] Replacing a document creates a new colocated `-v{N+1}` binary and updates the sidecar JSON `supersedes` chain without mutating the previous binary.
- [ ] Listing a parent record's documents returns p95 ≤ 500 ms for ≤ 10 documents; classification filters narrow results to `public`, `confidential`, or `restricted`.
- [ ] Document detail view exposes a small preview frame, full metadata, version history, provenance, and download links for current + prior versions; download starts within 1 s of click.
- [ ] Metadata-only edits update the sidecar JSON history without creating a new binary version.
- [ ] Classification-based access control enforces `public ∪ confidential ∪ restricted` rules layered on top of parent-record ABAC; users without the right combined access cannot list, preview, download, or replace the document.
- [ ] Document completeness signal endpoint returns a structured count summary by category and classification; consumers (e.g., F0006) treat it as a soft signal.
- [ ] Document templates library lets users upload boilerplates and link them to a parent record as initial artifacts.
- [ ] Retention policy YAML in `configuration/` drives a scheduled cleanup that purges sidecar JSON + binaries past their per-type retention; MVP cap = 10 days; cleanup is auditable.
- [ ] All audit events for upload, promote-from-quarantine, replace, metadata edit, classify, download, and delete are appended to the sidecar JSON history with actor and timestamp.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Documents List (on parent record) | Show all documents linked to a parent record | Filter by classification, paginate, open detail, upload, bulk-upload |
| Document Detail | Inspect a single document with preview, history, provenance | Preview, download current/prior version, replace, edit metadata, classify |
| Upload Dialog | Drag-and-drop ingestion for single or multi-file | Choose target classification, choose document type, attach to parent |
| Templates Library | Browse uploadable boilerplates and link to records | Upload template, list, link to parent |
| Quarantine Indicator | Inline badge on the documents list signalling pending-scan items | Auto-resolves after 60 s; shows time-remaining hint |

**Key Workflows:**
1. **Upload + Scan + Promote:** User drops one or more files → server writes binary to `quarantine/` and sidecar JSON marked `quarantined` → 60 s later a worker promotes the binary to its target folder, updates JSON status to `available`, and emits a timeline event.
2. **Replace / Version:** User opens detail → "Replace" → new binary uploaded → quarantine + promote → sidecar JSON appends new version `-v{N+1}` with `supersedes: -v{N}`; previous binary remains.
3. **Classify / Edit Metadata:** User edits classification, type, or tags → sidecar JSON records change with actor + timestamp; binary unchanged; no new version.
4. **Bulk Upload:** User drops 2-N files → each follows the upload-scan-promote pipeline independently → list view shows progress per file.
5. **Template Use:** User opens Templates Library, picks a template, links it to the parent record → a copy is materialized into the parent's documents folder with provenance noting the template source.
6. **Retention Sweep:** Scheduled cleanup reads `configuration/document-retention-policies.yaml`, walks the repository, removes binaries + sidecar JSON older than per-type policy (≤ 10 days MVP cap), appends a sweep audit record.

## Screen Layouts (ASCII)

### Documents List (on Parent Record) — Desktop

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  ← Back to {parent}    F0020 — Documents                          [Upload ▼] │
├──────────────────────────────────────────────────────────────────────────────┤
│  FILTERS: [Classification ▼ public/confidential/restricted]  [Type ▼]  [⟳]   │
│                                                                              │
│  ┌──────────────────────────── DOCUMENTS LIST ────────────────────────────┐  │
│  │ Name                Type    Class.       Version   Uploaded   Actions │  │
│  │ ────────────────────────────────────────────────────────────────────── │  │
│  │ acord125-v2.pdf     ACORD   confidential v2        2d ago    [view]   │  │
│  │ loss-runs-v1.pdf    LossRun confidential v1        5d ago    [view]   │  │
│  │ financials-v1.xlsx  Finance restricted   v1        5d ago    [view]   │  │
│  │ broker-cover.pdf    Cover   public       v1        7d ago    [view]   │  │
│  │ scan-pending.pdf    ACORD   confidential v1     ⏳ scanning… [—]      │  │
│  │ …                                                                      │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│  PAGINATION: [‹ prev]  Page 1 of 1  [next ›]                                 │
└──────────────────────────────────────────────────────────────────────────────┘
```

Note: rows in `quarantine` show a ⏳ scanning… badge instead of action buttons; auto-promotes after 60 s.

### Documents List — Mobile / iPad

```text
┌──────────────────────────────┐
│  ← {parent}        [Upload]  │
├──────────────────────────────┤
│  Filter: [Classification ▼]  │
│                              │
│  ┌────────── CARD ─────────┐ │
│  │ acord125-v2.pdf         │ │
│  │ ACORD · confidential    │ │
│  │ v2 · 2d ago     [view]  │ │
│  └─────────────────────────┘ │
│  ┌────────── CARD ─────────┐ │
│  │ scan-pending.pdf        │ │
│  │ ACORD · ⏳ scanning…    │ │
│  └─────────────────────────┘ │
│  …                           │
└──────────────────────────────┘
```

### Document Detail — Desktop

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  ← Documents   acord125-v2.pdf   ACORD · confidential · v2     [Replace]      │
├──────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────── PREVIEW (small frame) ────────────────┐  ┌─ METADATA ────┐ │
│  │                                                       │  │ Type: ACORD   │ │
│  │   [inline preview rendered here for supported types]  │  │ Class: conf.  │ │
│  │                                                       │  │ Parent: SUB-…│ │
│  │                                                       │  │ Uploader: …  │ │
│  │                                                       │  │ Size: 2.3 MB │ │
│  └───────────────────────────────────────────────────────┘  │ Tags: [edit] │ │
│                                                             └──────────────┘ │
│                                                                              │
│  ┌─────── VERSION HISTORY (descending) ──────┐  ┌──── PROVENANCE / EVENTS ─┐ │
│  │ v2  2d ago   uploaded by Alex             │  │ uploaded   v1 5d ago     │ │
│  │ v1  5d ago   uploaded by Alex             │  │ promoted   v1 5d ago     │ │
│  │ [download v2]   [download v1]             │  │ replaced   v2 2d ago     │ │
│  └───────────────────────────────────────────┘  │ classified conf 2d ago   │ │
│                                                 └──────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Upload Dialog — Desktop

```text
┌──────────────── UPLOAD DOCUMENTS ────────────────┐
│  Drop files here OR  [ Choose files… ]            │
│                                                   │
│  ┌────────── DROP ZONE ────────────┐              │
│  │   drag & drop one or many       │              │
│  │   pdf · png · docx · xlsx · csv │              │
│  │   ≤ 5 MB each                   │              │
│  └─────────────────────────────────┘              │
│                                                   │
│  Default classification: [public ▼]               │
│  Default document type:  [auto-detect ▼]          │
│                                                   │
│  Files queued (3):                                │
│   • acord125.pdf   2.1 MB   [pending]             │
│   • loss-runs.pdf  3.4 MB   [pending]             │
│   • finance.xlsx   0.6 MB   [pending]             │
│                                                   │
│  [Cancel]                       [Upload all]      │
└───────────────────────────────────────────────────┘
```

### Templates Library — Desktop

```text
┌──────────────────────────── DOCUMENT TEMPLATES ────────────────────────────┐
│  [+ Upload template]              FILTER: [Type ▼]  [Class. ▼]  [Search…]  │
├────────────────────────────────────────────────────────────────────────────┤
│  Name                  Type        Class.        Used   Last Used  Actions │
│  ──────────────────────────────────────────────────────────────────────── │
│  acord125-template.pdf ACORD       public        47     2d ago     [link]  │
│  loss-run-blank.xlsx   LossRun     public        12     1w ago     [link]  │
│  proposal-cover.docx   ProposalCvr confidential  3      3w ago     [link]  │
│  …                                                                         │
└────────────────────────────────────────────────────────────────────────────┘
```

## Success Criteria

- Users can upload, find, and download required insurance documents in Nebula within the published latency budgets.
- Document records preserve immutable version history and stable parent linkage via colocated sidecar JSON.
- Other CRM workflows can consume the soft completeness signal without coupling to filesystem internals.
- Retention policy file is the single source of truth for how long documents live; sweeps are auditable.

## Risks & Assumptions

- **Risk:** Mock scanner gives a false sense of safety. **Mitigation:** Treat the 60 s quarantine as a structural placeholder; ADR-012 follow-up captures the contract a real scanner must meet.
- **Risk:** 10-day retention cap is far shorter than insurance recordkeeping needs. **Mitigation:** Documented as MVP-only; production retention is Future scope.
- **Assumption:** Sidecar JSON colocated with binaries is acceptable for MVP; metadata search across the corpus is not in MVP.
- **Assumption:** Local filesystem storage is the MVP target; Architect decides on the storage abstraction boundary so the same contract survives migration to object storage later.
- **Assumption:** Classifications `public`, `confidential`, `restricted` are sufficient for MVP; finer-grained sensitivity is Future scope.

## Dependencies

- F0006 Submission Intake Workflow (consumer of completeness signal)
- F0018 Policy Lifecycle & Policy 360 (parent-record source for documents)

## Personas (Primary)

| Persona | Source | F0020 needs |
|---------|--------|-------------|
| Distribution & Marketing Manager (Sarah Chen-style) | `examples/personas/nebula-personas.md` | Bulk upload to broker/account context, classification filters, completeness visibility |
| Senior Underwriter (Marcus Rodriguez-style) | `examples/personas/nebula-personas.md` | Detail view + preview, version history, classification-aware access on policy/submission docs |
| Broker Relationship Coordinator (Jennifer Lee-style) | `examples/personas/nebula-personas.md` | Templates library, bulk upload, parent-record linkage, audit visibility |
| External Broker / MGA contact | F0029 (collaboration portal) | Upload to a submission, see only `public` documents on the parent, use templates |
| Admin Archivist | TBD (Future) | Retention sweeps, repository hygiene (covered by S0011 audit, full admin UI deferred) |

## Architecture & Solution Design

### Solution Components

- Document upload service with quarantine + promote pipeline.
- Repository layout: a single document repository directory tree with a reserved top-level `configuration/` folder for canonical YAML, plus per-parent folders containing colocated binary versions and a single sidecar JSON per logical document.
- Versioning service that writes new `-v{N}` binaries and appends to the sidecar JSON `supersedes` chain.
- Classification + access-control component layering classification tiers on top of parent ABAC.
- Completeness signal read-side projection.
- Templates library service.
- Retention sweeper that reads `configuration/document-retention-policies.yaml` and applies per-type retention.

### Data & Workflow Design

- Binary on disk: `…/{parent-type}/{parent-id}/{logical-doc-name}-v{N}.{ext}`.
- Sidecar JSON: `…/{parent-type}/{parent-id}/{logical-doc-name}.json` — single file per logical document; carries `versions[]`, `classification`, `parent`, `uploaderId`, `auditTimestamps`, `events[]` (upload, promote, replace, classify, edit-metadata, download, delete), `provenance` (e.g., template source).
- Configuration folder: `…/configuration/taxonomy.yaml`, `…/configuration/document-retention-policies.yaml`, `…/configuration/casbin-document-roles.yaml`.
- Quarantine: `…/quarantine/{upload-id}` while the 60 s timer runs; promotion atomically renames binary into target folder and the JSON status flips from `quarantined` to `available`.
- Completeness signal: a per-parent function over sidecar JSON that returns `{ category: { count, classifications } }` shape — read-only.

### API & Integration Design

- `POST /api/documents` (single + bulk multipart upload with `parent`, `classification`, `type` fields).
- `GET /api/documents?parent={id}&classification={tier}` paginated list.
- `GET /api/documents/{id}` detail (sidecar JSON contents).
- `GET /api/documents/{id}/versions/{n}/binary` download for any version in the chain.
- `PUT /api/documents/{id}/replace` create new version (multipart binary).
- `PATCH /api/documents/{id}/metadata` metadata-only edit.
- `GET /api/documents/completeness?parent={id}` soft signal.
- `GET/POST /api/document-templates` template library.
- All endpoints flow through the Nebula REST API (`api:nebula-rest`); ABAC handled via Casbin enriched with the document classification rules from `configuration/casbin-document-roles.yaml`.

### Security & Operational Considerations

- Classification-based access enforces `parent-allows ∧ classification-allows`.
- Audit history is append-only inside the sidecar JSON; deletion of a sidecar JSON is forbidden during normal operation (only retention sweep can purge, and it logs a sweep audit row).
- File-size cap of 5 MB enforced at the upload boundary; reject with structured 413 error.
- Content-type allowlist enforced before the binary leaves the request handler.
- Retention sweep cap of 10 days enforced at YAML-load time; any per-type policy above 10 days is rejected with a config-load error so MVP can never silently retain too long.
- Mock scanner is observable (timeline event) so the contract is testable; replacing it with a real scanner is an interface swap, not a redesign.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Cross-Cutting Component | Shared document storage subsystem with sidecar-JSON metadata, colocated versioning, and reserved `configuration/` folder | [ADR-012](../../architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) (Proposed → must be finalized in Phase B) |
| Introduces/Standardizes: Cross-Cutting Pattern | Quarantine-then-promote ingest pattern; classification-tiered access layered on parent ABAC | [ADR-012](../../architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) (Proposed) |
| Reuses: Established Component/Pattern | Append-only audit and timeline behaviour for document actions | PRD only |
| Reuses: Established Component/Pattern | Casbin ABAC for document operations enriched with classification rule set | Architect to confirm in Phase B |

## Related User Stories

- [F0020-S0001 — Upload single document with metadata to a parent record](./F0020-S0001-upload-single-document-with-metadata.md)
- [F0020-S0002 — Bulk multi-file upload to a parent record](./F0020-S0002-bulk-multi-file-upload.md)
- [F0020-S0003 — Quarantine and mock-scan workflow](./F0020-S0003-quarantine-and-mock-scan-workflow.md)
- [F0020-S0004 — List documents on a parent record with classification filtering](./F0020-S0004-list-documents-with-classification-filtering.md)
- [F0020-S0005 — Document detail view with preview and provenance](./F0020-S0005-document-detail-with-preview-and-provenance.md)
- [F0020-S0006 — Download a document for current and prior versions](./F0020-S0006-download-current-and-prior-versions.md)
- [F0020-S0007 — Replace a document with immutable supersedes lineage](./F0020-S0007-replace-with-immutable-supersedes-lineage.md)
- [F0020-S0008 — Update document metadata without creating a new binary version](./F0020-S0008-update-metadata-without-new-version.md)
- [F0020-S0009 — Classification-based access control on document operations](./F0020-S0009-classification-based-access-control.md)
- [F0020-S0010 — Document completeness signal endpoint](./F0020-S0010-document-completeness-signal-endpoint.md)
- [F0020-S0011 — Retention policy YAML and scheduled cleanup](./F0020-S0011-retention-policy-yaml-and-scheduled-cleanup.md)
- [F0020-S0012 — Document templates library](./F0020-S0012-document-templates-library.md)
