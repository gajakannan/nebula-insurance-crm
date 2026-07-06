# F0027 Architecture — COI, ACORD & Outbound Document Generation

**Feature:** F0027
**Phase:** B
**Status:** Proposed; pending G5 Phase B approval
**Architectural owners:** Architect for contracts, shared semantics, KG bindings; Product Manager for requirements and tracker state

## Architecture Summary

F0027 adds an outbound document generation layer on top of the F0020 document subsystem. It does not create a separate document repository or relational document table. Templates remain ADR-012 documents under `<docroot>/templates/`; issued artifacts are ordinary parent-linked documents with generated-document metadata, immutable binary versions, sidecar events, and source-record timeline events.

The first release supports COI, ACORD, and reusable proposal templates. Preview is ephemeral and non-audited unless it fails validation; Issue is explicit, auditable, and stores an immutable generated artifact. Regeneration creates a new issued artifact linked to the prior generated artifact rather than mutating the historical issued binary.

F0019 remains the owner of submission quote/proposal packet state, approval, bind, rating boundary, and workflow transitions. F0027 may read a recorded F0019 packet as proposal source data; it must not compute premium, derive coverage terms, approve packets, transition submissions, or change bind readiness.

## Component Design

| Component | Responsibility | Owner |
|-----------|----------------|-------|
| OutboundDocumentGenerationService | Coordinates source-data load, template validation, render preview, issue, and regenerate. | Application |
| OutboundMergeDataAssembler | Reads account, policy, submission, broker, and recorded F0019 packet facts into a render context. | Application |
| TemplateGovernanceService | Applies F0027 artifact-family rules and Admin-only template publish/replace decisions. | Application |
| DocumentRenderer | Adapter boundary for render implementation; v1 accepts HTML/JSON-template input and emits PDF preview or final binary. | Infrastructure |
| IDocumentRepository | Existing ADR-012 repository for template reads and issued artifact writes. | Infrastructure |
| ActivityTimelineService | Emits source-record timeline events for issue/regenerate and material validation failures. | Application |

## Workflow Design

### Template Governance

1. Admin uploads or replaces a template through the existing document-template storage path.
2. F0027 template metadata must identify `artifactFamily` as `coi`, `acord`, or `proposal`.
3. F0027 publish requires `outbound_template:manage`, a valid template family, a renderable latest version, and required merge-field metadata.
4. Published template versions are selectable for preview/issue; prior published versions remain available for provenance.
5. Non-admin template create, replace, publish, or family metadata mutation is denied for F0027-governed outbound templates even if generic F0020 template read/link remains broader.

### Preview

1. User chooses parent record, artifact family, and published template version.
2. API authorizes parent read plus `outbound_document:preview` plus template read.
3. Merge assembler builds a typed render context from authoritative source records.
4. Missing required merge fields returns ProblemDetails with `code=missing_merge_data`; no document is written.
5. Successful preview returns a short-lived preview payload and merge diagnostics; it does not create a sidecar, increment template usage, or append timeline.

### Issue

1. User explicitly clicks Issue after preview.
2. API authorizes parent read/create, template read, and `outbound_document:issue`.
3. Service reassembles merge data server-side; it never trusts client preview output.
4. Renderer produces final PDF.
5. Service writes the generated binary through `IDocumentRepository` as an available parent-linked document.
6. Sidecar metadata stores artifact family, source snapshot hash, template document/version, issue actor/time, and prior generated artifact reference when regenerated.
7. Sidecar event `generated_issued` and one source-record `ActivityTimelineEvent` are appended atomically with metadata write where the parent supports timelines.

### Regenerate

1. User selects a previously issued generated artifact.
2. API authorizes `outbound_document:regenerate`, template read, parent create, and document read.
3. Service uses current CRM data and the selected published template unless the request explicitly pins the original template version.
4. A new document is created; the prior issued artifact remains unchanged.
5. Metadata records `regeneratedFromDocumentId` and `regenerationReason`.

## API Contract

OpenAPI tag: `OutboundDocumentGeneration`.

| Method | Route | Purpose | Auth actions | Response |
|--------|-------|---------|--------------|----------|
| POST | `/outbound-documents/preview` | Validate merge data and return an ephemeral preview. | `outbound_document:preview`, `document_template:read`, parent read | `200 GeneratedDocumentPreviewResponse`; `400/403/404/409` ProblemDetails |
| POST | `/outbound-documents/issue` | Render and issue a final generated artifact. | `outbound_document:issue`, `document:create`, `document_template:read`, parent read | `201 GeneratedDocumentIssueResponse`; `400/403/404/409` ProblemDetails |
| POST | `/outbound-documents/{documentId}/regenerate` | Create a new issued artifact from a prior generated artifact. | `outbound_document:regenerate`, `document:read`, `document:create`, `document_template:read` | `201 GeneratedDocumentIssueResponse`; `400/403/404/409` ProblemDetails |

All mutation endpoints return RFC 7807 ProblemDetails for errors and must use server-side source-data reassembly. Preview and issue requests share the same source/template selector so the client cannot smuggle rendered output into final issue.

## Schema Contract

New shared JSON schemas:

- `generated-document-request.schema.json`
- `generated-document-preview-response.schema.json`
- `generated-document-issue-response.schema.json`

Artifact-family enum: `coi | acord | proposal`.

Parent type enum reuses ADR-012: `account | submission | policy | renewal`.

Generated artifact metadata is stored in `DocumentSidecar.metadata` under a pinned metadata schema id `generated-document`. The schema must include:

- `artifactFamily`
- `sourceParent`
- `templateDocumentId`
- `templateVersion`
- `sourceSnapshotHash`
- `issuedByUserId`
- `issuedAtUtc`
- `regeneratedFromDocumentId`
- `renderEngineVersion`

## Authorization Design

F0027 adds a feature-specific authorization layer rather than weakening F0020 generic template permissions.

| Action | Roles |
|--------|-------|
| `outbound_template:manage` | Admin |
| `outbound_document:preview` | Admin, Underwriter, DistributionUser, DistributionManager, Coordinator |
| `outbound_document:issue` | Admin, Underwriter, DistributionUser, DistributionManager, Coordinator |
| `outbound_document:regenerate` | Admin, Underwriter, DistributionUser, DistributionManager, Coordinator |

Effective allow for issue/regenerate:

```text
allow ⇔ parent_abac(user, parent, read/create)
     ∧ document_template:read(template)
     ∧ outbound_document:{issue|regenerate}
     ∧ classification_policy(role, generatedClassification, create/download)
```

Failed authorization writes no sidecar event and no timeline event.

## Data And Storage Design

- No new relational document table is introduced.
- Generated artifacts are document sidecars with `type=generated-document`.
- COI, ACORD, and proposal-specific merge values live in generated-document metadata, not new fixed document columns.
- Issued binaries are immutable; regeneration creates a new document and records a prior-artifact reference.
- Template usage increments only on Issue or Regenerate, not Preview.
- Preview output is short-lived and must not be used as final issue input.

## UI And Frontend Boundaries

- New frontend code should live under `experience/src/features/documents/` unless G0 identifies an existing document-generation slice.
- Template governance extends the existing document template surfaces with outbound family metadata and Admin-only publish controls.
- Generate Document Panel belongs on account, policy, and submission document surfaces; it calls preview first and enables Issue only after a successful current preview.
- Document detail renders generated-artifact provenance and regeneration links through existing document detail patterns.

## Non-Functional Requirements

- Preview p95 <= 2 seconds for a single PDF under 5 MB.
- Issue p95 <= 3 seconds for a single generated artifact under 5 MB.
- Render failures must be typed and observable; no partial document is stored on renderer failure.
- Render engine must sanitize template inputs and escape merge values by default.
- Generated documents inherit ADR-012 path-safety and classification controls.

## Required Implementation Checks For Feature Action G0

- Confirm whether existing `IDocumentRepository` can write internally generated binaries directly as available; if not, add an explicit generated-document write method instead of bypassing the repository.
- Confirm whether F0020 template sidecar metadata can support Draft/Published state; if not, add the minimal metadata schema extension under `generated-document` and outbound-template family metadata.
- Confirm renderer library choice and supported template source format.
- Confirm account/policy/submission parent document surfaces where the Generate Document Panel mounts.
- Confirm exact source fields for COI, ACORD, and proposal merge contexts from current runtime models before implementation.
