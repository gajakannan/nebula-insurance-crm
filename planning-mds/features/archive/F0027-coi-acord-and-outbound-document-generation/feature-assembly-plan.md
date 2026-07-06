# Feature Assembly Plan — F0027: COI, ACORD & Outbound Document Generation

**Created:** 2026-07-02
**Author:** Architect Agent
**Status:** Draft for G0 validation

## Overview

F0027 adds an outbound document generation vertical slice on top of the existing F0020 document subsystem. It introduces server-side preview, explicit issue, regeneration, generated-document provenance, Admin-governed outbound template metadata, and a document-panel UI for account, policy, and submission records. Generated artifacts remain ADR-012 documents; F0027 does not add a relational document table and does not mutate F0019 submission quote/proposal workflow state.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend contracts, renderer boundary, generated document DTOs, repository write method | S0002, S0003, S0004, S0005 | Establishes safe server-side generation and storage through `IDocumentRepository`. |
| 2 | Template governance and metadata schema/config updates | S0001 | Admin-only outbound template lifecycle must exist before users preview/issue. |
| 3 | Outbound generation service + endpoints | S0002, S0003, S0004, S0005 | Implements preview, issue, regenerate, policy gates, source-data assembly, and audit/timeline writes. |
| 4 | Frontend document slice integration | S0001-S0005 | Adds template family controls, Generate Document Panel, preview/issue/regenerate UX, and document-detail provenance. |
| 5 | Tests, runtime preflight, review evidence | S0001-S0005 | Required for G1-G6 harness gates. |

## Existing Code (Must Be Modified)

| File | Current State | F0027 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Application/DTOs/DocumentDtos.cs` | Contains document, template, sidecar, metadata, completeness DTOs. | **Expand** with generated-document request/preview/issue DTOs and merge diagnostics. |
| `engine/src/Nebula.Application/Interfaces/IDocumentRepository.cs` | Supports quarantined create, replace, metadata, event append, template usage. No direct generated available write. | **Expand** with `CreateGeneratedAvailableAsync(DocumentGeneratedWriteCommand, Stream, CancellationToken)`. |
| `engine/src/Nebula.Infrastructure/Documents/LocalFileSystemDocumentRepository.cs` | Writes uploads to quarantine and sidecars; promotes later. | **Expand** with generated-document write path that writes final PDF directly under parent directory with status `available`, metadata schema `generated-document`, provenance from template, and sidecar event `generated_issued`. |
| `engine/src/Nebula.Application/Services/DocumentTemplateService.cs` | Uploads/list/templates/link generic F0020 templates. | **Expand** with outbound-template metadata/publish helpers or call a new `OutboundTemplateGovernanceService`. |
| `engine/src/Nebula.Application/Services/DocumentService.cs` | Upload/list/detail/replace/metadata/download/completeness and timeline helper. | **Expand only as shared helper needed**; avoid putting F0027 orchestration here. |
| `engine/src/Nebula.Api/Endpoints/DocumentEndpoints.cs` | Maps `/documents` and `/document-templates`. | **Leave existing behavior stable**; add a separate `OutboundDocumentEndpoints.cs` group for `/outbound-documents`. |
| `engine/src/Nebula.Infrastructure/Authorization/PolicyAuthorizationService.cs` | Loads embedded `policy.csv` and authorizes resource/action pairs. | **No code change expected**; ensure F0027 rows are copied into embedded policy resource if the repo duplicates `planning-mds/security/policies/policy.csv`. |
| `experience/src/features/documents/types.ts` | Document/template TypeScript DTOs. | **Expand** with generated-document request/preview/issue types and artifact-family enum. |
| `experience/src/features/documents/hooks.ts` | TanStack Query hooks for documents/templates/upload/link/download. | **Expand** with `usePreviewGeneratedDocument`, `useIssueGeneratedDocument`, `useRegenerateGeneratedDocument`. |
| `experience/src/features/documents/components/ParentDocumentsPanel.tsx` | Shows parent docs and upload button. | **Expand** to mount Generate Document Panel and refresh document lists after issue/regenerate. |
| `experience/src/features/documents/components/DocumentTemplatesLibrary.tsx` | Uploads and lists generic templates. | **Expand** with outbound family/status metadata and Admin-only governance controls. |
| `experience/src/features/documents/components/DocumentDetailView.tsx` | Shows document detail/versions/events/download. | **Expand** with generated artifact provenance, template/version/source snapshot, and regenerate action. |
| `experience/src/mocks/documents.ts` and `experience/src/mocks/handlers.ts` | Mock document/template data and handlers. | **Expand** with generated preview/issue/regenerate mocks. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Application/DTOs/GeneratedDocumentDtos.cs` | Application | Request, preview response, issue response, diagnostics DTOs. |
| `engine/src/Nebula.Application/Interfaces/IDocumentRenderer.cs` | Application | Renderer adapter boundary. |
| `engine/src/Nebula.Application/Interfaces/IOutboundMergeDataAssembler.cs` | Application | Builds source merge context from parent records and recorded F0019 packet facts. |
| `engine/src/Nebula.Application/Services/OutboundDocumentGenerationService.cs` | Application | Preview, issue, regenerate orchestration. |
| `engine/src/Nebula.Application/Services/OutboundTemplateGovernanceService.cs` | Application | F0027 outbound template family/status validation and Admin-only governance. |
| `engine/src/Nebula.Infrastructure/Documents/SimplePdfDocumentRenderer.cs` | Infrastructure | Initial deterministic PDF renderer adapter for tests and MVP. |
| `engine/src/Nebula.Infrastructure/Documents/OutboundMergeDataAssembler.cs` | Infrastructure/Application adapter | Reads account/policy/submission/quote packet repositories and emits merge context. |
| `engine/src/Nebula.Api/Endpoints/OutboundDocumentEndpoints.cs` | API | `/outbound-documents/preview`, `/issue`, `/{documentId}/regenerate`. |
| `engine/src/Nebula.Api/data/documents/configuration/metadata-schemas/generated-document.v1.schema.json` | Runtime config | Generated artifact sidecar metadata schema. |
| `experience/src/features/documents/components/GenerateDocumentPanel.tsx` | Frontend | Source-record panel for family/template selection, preview, issue. |
| `experience/src/features/documents/components/GeneratedArtifactProvenance.tsx` | Frontend | Document detail generated-artifact provenance display. |
| `experience/src/features/documents/tests/GeneratedDocumentPanel.test.tsx` | Frontend tests | Preview/issue UI behavior. |
| `engine/tests/Nebula.Tests/Unit/OutboundDocumentGenerationServiceTests.cs` | Backend unit tests | Merge validation, preview, issue, regenerate, boundary rules. |
| `engine/tests/Nebula.Tests/Integration/OutboundDocumentEndpointTests.cs` | Backend integration tests | Endpoint auth, storage, ProblemDetails, timeline. |

## Step 1 — Backend Generated Document Foundation (S0002, S0003, S0004)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Application/DTOs/GeneratedDocumentDtos.cs
namespace Nebula.Application.DTOs;

public sealed record GeneratedDocumentRequestDto(
    DocumentParentRefDto Parent,
    string ArtifactFamily,
    string TemplateDocumentId,
    int? TemplateVersion,
    string Classification,
    string? RegeneratedFromDocumentId,
    string? RegenerationReason);

public sealed record GeneratedDocumentMergeDiagnosticDto(
    string Field,
    string Status,
    string? Detail);

public sealed record GeneratedDocumentPreviewResponseDto(
    string ArtifactFamily,
    string TemplateDocumentId,
    int TemplateVersion,
    string SourceSnapshotHash,
    string Status,
    string? PreviewUrl,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<GeneratedDocumentMergeDiagnosticDto> MergeDiagnostics);

public sealed record GeneratedDocumentIssueResponseDto(
    string DocumentId,
    string ArtifactFamily,
    string TemplateDocumentId,
    int TemplateVersion,
    DateTime IssuedAtUtc,
    Guid IssuedByUserId,
    string SourceSnapshotHash,
    string? RegeneratedFromDocumentId,
    string? DownloadUrl);

public sealed record OutboundMergeContext(
    DocumentParentRefDto Parent,
    string ArtifactFamily,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<GeneratedDocumentMergeDiagnosticDto> Diagnostics,
    string SourceSnapshotHash);

public sealed record RenderedDocumentBinary(
    Stream Stream,
    string ContentType,
    string FileName,
    long SizeBytes);
```

```csharp
// engine/src/Nebula.Application/Interfaces/IDocumentRenderer.cs
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IDocumentRenderer
{
    Task<RenderedDocumentBinary> RenderAsync(
        DocumentSidecarDto template,
        OutboundMergeContext context,
        CancellationToken ct = default);
}
```

```csharp
// engine/src/Nebula.Application/Interfaces/IOutboundMergeDataAssembler.cs
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IOutboundMergeDataAssembler
{
    Task<OutboundMergeContext> AssembleAsync(
        GeneratedDocumentRequestDto request,
        CancellationToken ct = default);
}
```

```csharp
// engine/src/Nebula.Application/Interfaces/IDocumentRepository.cs
public sealed record DocumentGeneratedWriteCommand(
    DocumentParentRefDto Parent,
    string LogicalName,
    string Classification,
    string Type,
    IReadOnlyList<string> Tags,
    DocumentMetadataSchemaRefDto MetadataSchema,
    JsonElement Metadata,
    Guid IssuedByUserId,
    string ContentType,
    long SizeBytes,
    string OriginalFileName,
    string TemplateDocumentId,
    int TemplateVersion);

Task<DocumentWriteResult> CreateGeneratedAvailableAsync(
    DocumentGeneratedWriteCommand command,
    Stream binary,
    CancellationToken ct = default);
```

### Logic Flow

`LocalFileSystemDocumentRepository.CreateGeneratedAvailableAsync(command, binary, ct) → DocumentWriteResult`

1. Validate parent type using existing `DocumentConstants.ParentTypes`; reject invalid parent as `invalid_parent`.
2. Generate new `documentId`; create parent directory through existing `ParentDirectory`.
3. Sanitize logical name using existing `MakeSafeLogicalName`; disambiguate in parent directory.
4. Copy binary directly to final version path `{logicalName}-v1.pdf`; compute sha256.
5. Write sidecar with `Type = "generated-document"`, `versions[0].status = "available"`, `provenance.source = $"template:{TemplateDocumentId}"`, metadata schema `generated-document`.
6. Add event kind `generated_issued`; update sidecar audit timestamps.
7. Return `DocumentWriteResult(documentId, 1, null, null)`.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Generate Document Panel | Issue | `POST /outbound-documents/issue` | `OutboundDocumentGenerationService.IssueAsync` | `DocumentSidecarDto` generated artifact | `outbound_document:issue`, `document:create`, `document_template:read` | N/A; creates new document | `400 missing_merge_data`, `403 policy_denied`, `404 template_not_found`, `409 template_not_published` | Sidecar `generated_issued`; source timeline `OutboundDocumentIssued` | Integration test reloads `/documents?parent.*` and finds available generated artifact. |
| Document Detail | Regenerate | `POST /outbound-documents/{documentId}/regenerate` | `OutboundDocumentGenerationService.RegenerateAsync` | New `DocumentSidecarDto` with `regeneratedFromDocumentId` | `outbound_document:regenerate`, `document:read`, `document:create` | N/A; creates new document | `404 document_not_found`, `409 not_generated_document` | Sidecar `generated_issued`; source timeline `OutboundDocumentRegenerated` | Integration test prior document unchanged and new document exists. |

## Step 2 — Template Governance (S0001)

### Logic Flow

`OutboundTemplateGovernanceService.ValidatePublishedTemplateAsync(templateId, family, user, ct) → (DocumentSidecarDto?, string?)`

1. Load template sidecar by `templateId`.
2. Require `sidecar.Type == "template"` and latest available version.
3. Read metadata fields: `artifactFamily`, `outboundStatus`, `requiredMergeFields`.
4. Require family in `coi | acord | proposal`.
5. For publish/manage paths, require any user role authorizes `outbound_template:manage`.
6. Return template or error code: `template_not_found`, `template_not_published`, `invalid_artifact_family`, `template_access_denied`.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Document Templates Library | Upload/replace outbound template metadata | Existing `POST /document-templates` and future metadata patch | `DocumentTemplateService.UploadTemplateAsync` + governance validation | Template sidecar metadata | `outbound_template:manage` for F0027-governed fields | Sidecar write lock | `403 template_access_denied`, `400 invalid_artifact_family` | Sidecar `uploaded`/`metadata_edited`; timeline `DocumentTemplateUploaded` | Admin can publish; DistributionUser cannot mutate F0027 outbound governance fields. |

## Step 3 — Outbound Generation Endpoints (S0002, S0003, S0004, S0005)

### API Endpoints

```csharp
// engine/src/Nebula.Api/Endpoints/OutboundDocumentEndpoints.cs
public static class OutboundDocumentEndpoints
{
    public static IEndpointRouteBuilder MapOutboundDocumentEndpoints(this IEndpointRouteBuilder app);
}
```

Routes:

- `POST /outbound-documents/preview`
- `POST /outbound-documents/issue`
- `POST /outbound-documents/{documentId}/regenerate`

Add `app.MapOutboundDocumentEndpoints();` in `engine/src/Nebula.Api/Program.cs` near `MapDocumentEndpoints()`.

### Service Methods

```csharp
// engine/src/Nebula.Application/Services/OutboundDocumentGenerationService.cs
public sealed class OutboundDocumentGenerationService
{
    public Task<(GeneratedDocumentPreviewResponseDto? Result, string? ErrorCode)> PreviewAsync(
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(GeneratedDocumentIssueResponseDto? Result, string? ErrorCode)> IssueAsync(
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(GeneratedDocumentIssueResponseDto? Result, string? ErrorCode)> RegenerateAsync(
        string documentId,
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default);
}
```

### Casbin Enforcement

- Preview:
  - Resource: `outbound_document`, action: `preview`
  - Also require template read through `IDocumentClassificationGate.AuthorizeTemplateAsync`.
- Issue:
  - Resource: `outbound_document`, action: `issue`
  - Also require document create through `IDocumentClassificationGate.AuthorizeDocumentAsync`.
- Regenerate:
  - Resource: `outbound_document`, action: `regenerate`
  - Also require read of prior generated document and create on parent.

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `GeneratedDocumentPreviewResponseDto` | Preview success or diagnostics returned. |
| 201 Created | `GeneratedDocumentIssueResponseDto` | Issue/regenerate success. |
| 400 | ProblemDetails (`validation_error`, `missing_merge_data`, `invalid_artifact_family`) | Request or merge validation failure. |
| 403 | ProblemDetails (`policy_denied`, `template_access_denied`, `parent_access_denied`) | Authorization denial. |
| 404 | ProblemDetails (`template_not_found`, `document_not_found`) | Missing template/prior document. |
| 409 | ProblemDetails (`template_not_published`, `render_failed`, `not_generated_document`) | Business rule conflict. |

## Step 4 — Frontend Document Generation UX (S0001-S0005)

### New Types And Hooks

Add to `experience/src/features/documents/types.ts`:

```ts
export type GeneratedArtifactFamily = 'coi' | 'acord' | 'proposal'

export interface GeneratedDocumentRequestDto {
  parent: DocumentParentRefDto
  artifactFamily: GeneratedArtifactFamily
  templateDocumentId: string
  templateVersion?: number | null
  classification?: DocumentClassification
  regeneratedFromDocumentId?: string | null
  regenerationReason?: string | null
}

export interface GeneratedDocumentMergeDiagnosticDto {
  field: string
  status: 'ok' | 'missing' | 'invalid'
  detail: string | null
}

export interface GeneratedDocumentPreviewResponseDto {
  artifactFamily: GeneratedArtifactFamily
  templateDocumentId: string
  templateVersion: number
  sourceSnapshotHash: string
  status: 'ready' | 'missing_merge_data' | 'render_failed'
  previewUrl: string | null
  expiresAtUtc: string | null
  mergeDiagnostics: GeneratedDocumentMergeDiagnosticDto[]
}

export interface GeneratedDocumentIssueResponseDto {
  documentId: string
  artifactFamily: GeneratedArtifactFamily
  templateDocumentId: string
  templateVersion: number
  issuedAtUtc: string
  issuedByUserId: string
  sourceSnapshotHash: string
  regeneratedFromDocumentId: string | null
  downloadUrl: string | null
}
```

Add hooks to `experience/src/features/documents/hooks.ts`:

- `usePreviewGeneratedDocument(parent)`
- `useIssueGeneratedDocument(parent)`
- `useRegenerateGeneratedDocument(parent, documentId)`

On issue/regenerate success, invalidate:

- `['documents', 'parent', parent]`
- `['documents', 'completeness', parent]`
- `['documents', 'detail', documentId]` for regenerate.

### Components

- `GenerateDocumentPanel.tsx` mounts inside `ParentDocumentsPanel` above the list.
- Use select controls for family/template/classification and icon buttons for preview/issue.
- Issue button disabled until latest preview response has `status === "ready"`.
- `GeneratedArtifactProvenance.tsx` renders generated metadata on `DocumentDetailView`.

### Frontend Test Expectations

- Distribution user can choose published template, preview, then issue; document list invalidates.
- Missing merge fields render diagnostics and keep Issue disabled.
- Regenerate creates new artifact link and keeps prior artifact visible.
- Non-admin template governance controls are hidden/disabled.

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | DTOs, renderer boundary, merge assembler, repository generated write, service, endpoints, tests. | Backend Developer | Planned |
| Frontend (`experience/`) | hooks, types, GenerateDocumentPanel, template governance UI, provenance UI, tests. | Frontend Developer | Planned |
| AI (`neuron/`) | No AI scope. | N/A | Omitted |
| Quality | Unit/integration/component coverage and end-to-end acceptance mapping. | Quality Engineer | Planned |
| DevOps/Runtime | Runtime preflight, render dependency/deployability checks, document storage config evidence. | DevOps | Planned |

## Dependency Order

```text
Step 0 (Architect): feature assembly plan + G0 validation
Step 1 (Backend):   DTOs/interfaces/repository generated write + renderer stub
Step 2 (Backend):   merge assembler + outbound generation service
Step 3 (Backend):   API endpoints + Program registration + tests
  ---- Backend checkpoint: dotnet build + targeted outbound/document tests pass ----
Step 4 (Frontend):  types/hooks + GenerateDocumentPanel + template/provenance UI
  ---- Frontend checkpoint: targeted vitest + build pass ----
Step 5 (QE):        lifecycle scenario validation and evidence package updates
```

## Integration Checkpoints

### After Backend

- [ ] `POST /outbound-documents/preview` returns diagnostics without writing a sidecar.
- [ ] `POST /outbound-documents/issue` writes an available `generated-document` sidecar and binary through `IDocumentRepository`.
- [ ] `POST /outbound-documents/{documentId}/regenerate` creates a new document and leaves prior artifact immutable.
- [ ] F0019 submission packet is read-only source context; no submission transition or packet mutation occurs.
- [ ] Casbin denies ExternalUser and RelationshipManager for outbound generation.

### After Frontend

- [ ] Generate Document Panel appears on account, policy, and submission document surfaces.
- [ ] Preview must precede Issue in the UI.
- [ ] Missing merge diagnostics are visible and Issue stays disabled.
- [ ] Issued artifact appears in parent document list after query invalidation.

### Cross-Story Verification

- [ ] Admin-governed templates can be used by operating users without letting them edit governance metadata.
- [ ] Full lifecycle: publish template → preview → issue → retrieve detail/download → regenerate.
- [ ] All generated artifacts show template id/version, source parent, snapshot hash, issued actor/time.
- [ ] ProblemDetails format uses `code` extension matching existing endpoint style.

## Integration Checklist

- [ ] API contract compatibility validated.
- [ ] Frontend contract compatibility validated.
- [ ] Test cases mapped to all five story acceptance criteria.
- [ ] Runtime evidence artifacts identified under `planning-mds/operations/evidence/runs/2026-07-02-b9316621/artifacts/`.
- [ ] Mutation traceability tables completed for template governance, issue, and regenerate.
- [ ] No render-only test closes mutation stories.
- [ ] Run/deploy instructions updated if renderer dependency requires configuration.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Generated PDF rendering library may require a new runtime dependency. | Medium | Start with deterministic simple PDF renderer or HTML-to-PDF adapter selected during G1; record deployability evidence. | Backend/DevOps |
| Existing document metadata schema registry lacks `generated-document`. | Medium | Add config schema + registry entry and validate via document config provider tests. | Backend |
| Generic F0020 template permissions are broader than F0027 rules. | High | Enforce F0027-specific `outbound_template:manage` and `outbound_document:*` actions in generation services. | Backend/Security |
| F0019 packet boundary could drift into rating/workflow mutation. | High | Add boundary regression test: proposal rendering reads packet facts only and never calls submission transition/packet update paths. | Backend/QE |

## JSON Serialization Convention

- Backend DTOs use PascalCase records with ASP.NET web defaults producing camelCase JSON.
- JSON Schemas use Draft-07 nullable type arrays; OpenAPI uses 3.0 nullable syntax.
- Date/time fields are UTC ISO 8601 strings.

## DI Registration Changes

Add registrations in `engine/src/Nebula.Infrastructure/DependencyInjection.cs` or API service setup consistent with existing service registration:

- `IDocumentRenderer -> SimplePdfDocumentRenderer`
- `IOutboundMergeDataAssembler -> OutboundMergeDataAssembler`
- `OutboundTemplateGovernanceService`
- `OutboundDocumentGenerationService`

## Casbin Policy Sync

F0027 added planning policy rows in `planning-mds/security/policies/policy.csv`. During implementation, ensure the embedded runtime `policy.csv` resource consumed by `PolicyAuthorizationService` includes:

- `outbound_template, manage`
- `outbound_document, preview`
- `outbound_document, issue`
- `outbound_document, regenerate`

## Knowledge-Graph Binding Plan

Expected G7 as-built binding delta:

- Add backend code-index bindings for `capability:outbound-document-generation`, `entity:generated-document-artifact`, and endpoints:
  - `engine/src/Nebula.Application/DTOs/GeneratedDocumentDtos.cs`
  - `engine/src/Nebula.Application/Interfaces/IDocumentRenderer.cs`
  - `engine/src/Nebula.Application/Interfaces/IOutboundMergeDataAssembler.cs`
  - `engine/src/Nebula.Application/Services/OutboundDocumentGenerationService.cs`
  - `engine/src/Nebula.Application/Services/OutboundTemplateGovernanceService.cs`
  - `engine/src/Nebula.Infrastructure/Documents/SimplePdfDocumentRenderer.cs`
  - `engine/src/Nebula.Infrastructure/Documents/OutboundMergeDataAssembler.cs`
  - `engine/src/Nebula.Api/Endpoints/OutboundDocumentEndpoints.cs`
- Add frontend bindings:
  - `experience/src/features/documents/components/GenerateDocumentPanel.tsx`
  - `experience/src/features/documents/components/GeneratedArtifactProvenance.tsx`
  - `experience/src/features/documents/hooks.ts`
  - `experience/src/features/documents/types.ts`
- Preserve existing F0020 document/template bindings.
