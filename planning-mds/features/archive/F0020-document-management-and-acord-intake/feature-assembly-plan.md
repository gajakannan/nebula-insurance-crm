# Feature Assembly Plan - F0020: Document Management & ACORD Intake

**Created:** 2026-05-04
**Author:** Architect Agent
**Status:** Draft - ready for G0 validation

## Overview

F0020 adds the shared Nebula document subsystem: filesystem-backed document storage, sidecar JSON metadata, type-specific JSON metadata schema evolution, immutable versioning, a mock quarantine-then-promote ingest pipeline, classification-aware access control, document templates, and parent-record document UI. The MVP remains filesystem-first per ADR-012: no relational `Document` table is introduced, and every document operation flows through `IDocumentRepository` plus the combined `parent_abac AND classification_policy` gate.

## Source Artifacts

| Artifact | Role in this plan |
|----------|-------------------|
| `planning-mds/features/archive/F0020-document-management-and-acord-intake/PRD.md` | Product scope, UX screens, NFRs, workflow descriptions |
| `planning-mds/features/archive/F0020-document-management-and-acord-intake/F0020-S0001-*.md` through `F0020-S0012-*.md` | Story-level acceptance criteria and edge cases |
| `planning-mds/architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md` | Storage, sidecar, type-specific metadata schemas, versioning, classification, retention, and template invariants |
| `planning-mds/architecture/decisions/ADR-019-mock-quarantine-then-promote-ingest-pipeline.md` | Quarantine worker and scanner replacement contract |
| `planning-mds/api/nebula-api.yaml` | OpenAPI paths and schemas for `Documents` and `DocumentTemplates` |
| `planning-mds/security/authorization-matrix.md` and `planning-mds/security/policies/policy.csv` | Parent ABAC and document-template policy rows |
| `planning-mds/schemas/document-*.json`, `paginated-document-list.schema.json` | Shared JSON Schema contracts, including sidecar metadata schema registry |

## Assembly Slice Order

Use this order for implementation. Items inside the same bracket may be done in parallel after their prerequisites are satisfied; do not cross parallelize across numbered entries.

| Entry | Slice | Stories | Owners | Rationale |
|-------|-------|---------|--------|-----------|
| 1 | `[Backend Foundation: DTOs, repository contract, config loaders, metadata schema registry, parent access resolver, classification gate]` | S0001, S0004, S0008, S0009, S0011 | Backend, Architect, Security | All later document operations require the sidecar model, docroot config, safe path handling, taxonomy, metadata schema registry, retention, and combined gate. |
| 2 | `[Backend Ingest: upload, bulk upload, quarantine worker, mock scanner]` | S0001, S0002, S0003 | Backend, DevOps, QE | Establishes write path and promotion semantics before any read or binary streaming path is trusted. |
| 3 | `[Backend Operations: list, detail, download, replace, metadata, completeness, templates, retention]` | S0004-S0012 | Backend, Security, QE | Adds remaining API behavior over the same repository and lock primitives. |
| 4 | `[Frontend Documents Surface: contracts, hooks, schema-driven metadata fields, upload dialog, parent document panels, detail, templates]` | S0001-S0012 | Frontend, QE | UI consumes the stable OpenAPI shapes and renders type-specific metadata from the server-provided JSON Schemas; parent pages can integrate once backend contracts exist. |
| 5 | `[Quality and Deployability Evidence: integration, E2E, runtime config, security review fixtures]` | S0001-S0012 | QE, DevOps, Code Reviewer, Security | Produces evidence for G2-G4.5 and verifies the full vertical slice in runtime containers. |

## Existing Code (Must Be Modified)

| File | Current State | F0020 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Api/Program.cs` | Registers endpoint groups through `app.Map*Endpoints()` and global middleware. | Add `app.MapDocumentEndpoints();` and ensure multipart body size limits are compatible with 50 MB batch cap. |
| `engine/src/Nebula.Api/Helpers/ProblemDetailsHelper.cs` | Contains shared ProblemDetails helpers for known workflow and policy errors. | Add document-specific helpers for `unsupported_type`, `file_too_large`, `batch_too_large`, `empty_file`, `invalid_filename`, `parent_access_denied`, `classification_access_denied`, `document_access_denied`, `metadata_access_denied`, `version_not_available`, `promotion_internal_only`, and `document_not_found`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repositories, Casbin authorization, hosted services, memory cache. | Register document repository, configuration providers, classification gate, parent access resolver, document/template services, mock scanner, quarantine worker, retention service, and retention hosted service. |
| `engine/src/Nebula.Api/appsettings.json` and `appsettings.Development.json` | Runtime config includes DB, auth, CORS, rate limits, logging. | Add `Documents:RootPath`, quarantine hold/tick overrides, retention schedule, file size caps, and allowed extensions. |
| `experience/src/services/api.ts` | JSON-only `get`, `post`, `put`, `delete`; no multipart, patch, or blob helper. | Add `patch`, `postMultipart`, `putMultipart`, and `downloadBlob` helpers without forcing `Content-Type: application/json` on multipart requests. |
| `experience/src/App.tsx` | Routes existing parent pages and feature pages. | Add route(s) for `/documents/:documentId`, `/document-templates`, and link parent records to the document panel. |
| `experience/src/pages/AccountDetailPage.tsx` | Existing Account detail surface. | Add a Documents tab/rail using `ParentDocumentsPanel` with parent type `account`. |
| `experience/src/pages/SubmissionDetailPage.tsx` | Existing Submission detail surface. | Add a Documents tab/rail using parent type `submission`; completeness signal remains soft. |
| `experience/src/pages/PolicyDetailPage.tsx` | Existing Policy detail surface. | Add a Documents tab/rail using parent type `policy`. |
| `experience/src/pages/RenewalDetailPage.tsx` | Existing Renewal detail surface. | Add a Documents tab/rail using parent type `renewal`. |
| `experience/src/mocks/handlers.ts` and `experience/src/mocks/data.ts` | MSW fixtures for existing CRM entities. | Add document/list/detail/template handlers and demo sidecar fixtures. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Documents/DocumentConstants.cs` | Domain | Allowed extensions, classifications, statuses, operations, system principal names. |
| `engine/src/Nebula.Domain/Documents/DocumentIds.cs` | Domain | Strong string helpers for `doc_<ULID>` and upload ids. |
| `engine/src/Nebula.Application/DTOs/DocumentDtos.cs` | Application | DTOs matching OpenAPI component schemas. |
| `engine/src/Nebula.Application/Interfaces/IDocumentRepository.cs` | Application | Only abstraction allowed to read/write under `<docroot>`. |
| `engine/src/Nebula.Application/Interfaces/IDocumentConfigurationProvider.cs` | Application | Taxonomy, retention, and classification policy access. |
| `engine/src/Nebula.Application/Interfaces/IDocumentClassificationGate.cs` | Application | Combined classification operation evaluator. |
| `engine/src/Nebula.Application/Interfaces/IDocumentParentAccessResolver.cs` | Application | Parent ABAC bridge for account/submission/policy/renewal. |
| `engine/src/Nebula.Application/Interfaces/IQuarantineScanner.cs` | Application | Scanner replacement interface from ADR-019. |
| `engine/src/Nebula.Application/Services/DocumentService.cs` | Application | Upload, list, detail, download, replace, metadata, completeness orchestration. |
| `engine/src/Nebula.Application/Services/DocumentTemplateService.cs` | Application | Template upload/list/link operations. |
| `engine/src/Nebula.Application/Services/DocumentRetentionService.cs` | Application | Scheduled and dry-run retention sweep logic. |
| `engine/src/Nebula.Application/Validators/DocumentValidators.cs` | Application | FluentValidation validators for metadata update and parent refs; upload binary checks live in service. |
| `engine/src/Nebula.Infrastructure/Documents/LocalFileSystemDocumentRepository.cs` | Infrastructure | Atomic sidecar writes, per-document locks, safe path resolution, streaming. |
| `engine/src/Nebula.Infrastructure/Documents/YamlDocumentConfigurationProvider.cs` | Infrastructure | Loads and hot-reloads `taxonomy.yaml`, `document-retention-policies.yaml`, and `casbin-document-roles.yaml`. |
| `engine/src/Nebula.Infrastructure/Documents/DocumentParentAccessResolver.cs` | Infrastructure | Resolves parent existence and parent ABAC attributes through existing repositories/services. |
| `engine/src/Nebula.Infrastructure/Documents/DocumentClassificationGate.cs` | Infrastructure | Evaluates role/classification operation table and logs verdicts. |
| `engine/src/Nebula.Infrastructure/Documents/MockTimerScanner.cs` | Infrastructure | Returns clean after the configured hold; satisfies `IQuarantineScanner`. |
| `engine/src/Nebula.Infrastructure/Documents/QuarantinePromotionWorker.cs` | Infrastructure | Hosted worker that promotes eligible quarantine entries. |
| `engine/src/Nebula.Infrastructure/Documents/DocumentRetentionHostedService.cs` | Infrastructure | Hosted sweeper loop for S0011. |
| `engine/src/Nebula.Api/Endpoints/DocumentEndpoints.cs` | API | Minimal API group for `/documents` and `/document-templates`. |
| `experience/src/features/documents/types.ts` | Frontend | TypeScript contracts mirrored from OpenAPI. |
| `experience/src/features/documents/hooks.ts` | Frontend | TanStack Query hooks and mutations. |
| `experience/src/features/documents/components/ParentDocumentsPanel.tsx` | Frontend | Parent-scoped list, filters, pagination, upload affordance. |
| `experience/src/features/documents/components/DocumentMetadataFields.tsx` | Frontend | JSON Schema-driven type-specific metadata renderer used by upload and detail metadata forms. |
| `experience/src/features/documents/components/DocumentUploadDialog.tsx` | Frontend | Drag/drop single and bulk upload dialog. |
| `experience/src/features/documents/components/DocumentDetailView.tsx` | Frontend | Preview, metadata, version history, provenance, replace/edit actions. |
| `experience/src/features/documents/components/DocumentTemplatesLibrary.tsx` | Frontend | Template list/upload/link UI. |
| `experience/src/features/documents/tests/*.test.tsx` | Frontend tests | Component and integration coverage. |
| `experience/tests/e2e/document-management.spec.ts` | E2E | Full upload/promote/list/detail/download/template path. |

## Backend Code Contracts

### DTOs

```csharp
// engine/src/Nebula.Application/DTOs/DocumentDtos.cs
namespace Nebula.Application.DTOs;

public sealed record DocumentParentRefDto(string Type, Guid Id);
public sealed record DocumentLatestUploadDto(DateTime AtUtc, Guid ByUserId);
public sealed record DocumentListItemDto(
    string DocumentId,
    string LogicalName,
    string Type,
    string Classification,
    int LatestVersion,
    string Status,
    DocumentLatestUploadDto LatestUpload,
    DocumentParentRefDto Parent);

public sealed record PaginatedDocumentListDto(
    IReadOnlyList<DocumentListItemDto> Documents,
    PaginationDto Pagination);

public sealed record PaginationDto(int Page, int PageSize, int Total);

public sealed record DocumentVersionDto(
    int N,
    string FileName,
    long SizeBytes,
    string Sha256,
    string Status,
    DateTime UploadedAt,
    Guid UploadedByUserId,
    int? Supersedes);

public sealed record DocumentEventDto(
    string Kind,
    DateTime At,
    string ByUserId,
    int? Version,
    int? FromVersion,
    int? ToVersion,
    string? From,
    string? To,
    IReadOnlyDictionary<string, object?>? Changes,
    string? Error,
    string? Reason);

public sealed record DocumentSidecarDto(
    string DocumentId,
    string LogicalName,
    DocumentParentRefDto Parent,
    string Classification,
    string Type,
    IReadOnlyList<string> Tags,
    DocumentMetadataSchemaRefDto MetadataSchema,
    JsonElement Metadata,
    Guid UploaderId,
    DocumentAuditTimestampsDto AuditTimestamps,
    DocumentProvenanceDto? Provenance,
    IReadOnlyList<DocumentVersionDto> Versions,
    int? UseCount,
    DateTime? LastUsedAt,
    IReadOnlyList<DocumentEventDto> Events);

public sealed record DocumentAuditTimestampsDto(DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record DocumentProvenanceDto(string Source, DateTime? MaterializedAt, Guid? ByUserId);
public sealed record DocumentMetadataSchemaRefDto(string Id, int Version, string SchemaHash);
public sealed record DocumentMetadataSchemaDto(string Id, int Version, string Status, string SchemaHash, JsonElement Schema);
public sealed record DocumentMetadataSchemaRegistryDto(IReadOnlyList<DocumentMetadataSchemaDto> Schemas);
public sealed record DocumentDetailDto(DocumentSidecarDto Sidecar, IReadOnlyList<string?> PreviewUrls);
public sealed record DocumentUploadFileMetadataDto(string? Classification, string? Type, IReadOnlyList<string>? Tags, JsonElement? Metadata);
public sealed record DocumentUploadAcceptedItemDto(string DocumentId, string LogicalName, string Status);
public sealed record DocumentUploadRejectedItemDto(int Index, string? LogicalName, string Code, string? Detail);
public sealed record DocumentUploadResponseDto(
    IReadOnlyList<DocumentUploadAcceptedItemDto> Documents,
    IReadOnlyList<DocumentUploadRejectedItemDto> Rejected);
public sealed record DocumentReplaceResponseDto(string DocumentId, int Version, string Status);
public sealed record DocumentMetadataUpdateRequestDto(string? Classification, string? Type, IReadOnlyList<string>? Tags, JsonElement? Metadata);
public sealed record DocumentCompletenessSignalDto(
    DocumentParentRefDto Parent,
    DocumentCompletenessTotalsDto Totals,
    IReadOnlyList<DocumentTypeCountDto> ByType,
    IReadOnlyList<DocumentClassificationCountDto> ByClassification);
public sealed record DocumentCompletenessTotalsDto(int Available, int Quarantined, int FailedPromote);
public sealed record DocumentTypeCountDto(string Type, int Count);
public sealed record DocumentClassificationCountDto(string Classification, int Count);
public sealed record DocumentTemplateDto(
    string TemplateId,
    string LogicalName,
    string Type,
    string Classification,
    IReadOnlyList<string> Tags,
    int UseCount,
    DateTime? LastUsedAt,
    DateTime UploadedAtUtc,
    Guid UploadedByUserId);
public sealed record PaginatedDocumentTemplateListDto(
    IReadOnlyList<DocumentTemplateDto> Templates,
    PaginationDto Pagination);
```

### Interfaces

```csharp
// engine/src/Nebula.Application/Interfaces/IDocumentRepository.cs
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IDocumentRepository
{
    Task<DocumentWriteResult> CreateQuarantinedAsync(
        DocumentUploadCommand command,
        Stream binary,
        CancellationToken ct = default);

    Task<IReadOnlyList<DocumentSidecarDto>> ListParentSidecarsAsync(
        DocumentParentRefDto parent,
        CancellationToken ct = default);

    Task<DocumentSidecarDto?> FindSidecarAsync(string documentId, CancellationToken ct = default);

    Task<DocumentBinaryRead?> OpenVersionForReadAsync(
        string documentId,
        string versionRef,
        CancellationToken ct = default);

    Task<DocumentWriteResult> AppendReplacementAsync(
        string documentId,
        DocumentReplaceCommand command,
        Stream binary,
        CancellationToken ct = default);

    Task<DocumentSidecarDto?> UpdateMetadataAsync(
        string documentId,
        DocumentMetadataPatch patch,
        CancellationToken ct = default);

    Task<IReadOnlyList<QuarantineEntryDto>> ListPromotableQuarantineEntriesAsync(
        DateTime nowUtc,
        CancellationToken ct = default);

    Task<PromoteResult> PromoteAsync(QuarantineEntryDto entry, CancellationToken ct = default);

    Task<IReadOnlyList<RetentionCandidateDto>> ListRetentionCandidatesAsync(CancellationToken ct = default);

    Task<RetentionSweepResultDto> SweepAsync(
        IReadOnlyList<RetentionCandidateDto> candidates,
        bool dryRun,
        CancellationToken ct = default);
}

public sealed record DocumentUploadCommand(
    DocumentParentRefDto Parent,
    string LogicalName,
    string Classification,
    string Type,
    IReadOnlyList<string> Tags,
    Guid UploadedByUserId,
    string ContentType,
    long SizeBytes,
    string OriginalFileName,
    string? ProvenanceSource);

public sealed record DocumentReplaceCommand(Guid UploadedByUserId, string ContentType, long SizeBytes, string OriginalFileName);
public sealed record DocumentMetadataPatch(Guid ActorUserId, string? Classification, string? Type, IReadOnlyList<string>? Tags);
public sealed record DocumentWriteResult(string? DocumentId, int? Version, string? ErrorCode, string? Detail);
public sealed record DocumentBinaryRead(Stream Stream, string ContentType, string DownloadFileName, int Version, long SizeBytes);
public sealed record QuarantineEntryDto(string DocumentId, int Version, string QuarantinePath, DateTime UploadedAtUtc);
public sealed record PromoteResult(bool Promoted, string? ErrorCode);
public sealed record RetentionCandidateDto(string DocumentId, DocumentParentRefDto? Parent, string Type, string Classification, DateTime LastUploadedAtUtc);
public sealed record RetentionSweepResultDto(int Scanned, int Swept, IReadOnlyDictionary<string, int> SweptByType, bool DryRun);
```

```csharp
// engine/src/Nebula.Application/Interfaces/IDocumentClassificationGate.cs
namespace Nebula.Application.Interfaces;

public interface IDocumentClassificationGate
{
    Task<DocumentAccessDecision> AuthorizeAsync(
        ICurrentUserService user,
        DocumentParentRefDto parent,
        string classification,
        string operation,
        CancellationToken ct = default);
}

public sealed record DocumentAccessDecision(
    bool Allowed,
    string? Code,
    string? Dimension,
    IReadOnlyList<string> ContributingRoles);
```

```csharp
// engine/src/Nebula.Application/Interfaces/IQuarantineScanner.cs
namespace Nebula.Application.Interfaces;

public interface IQuarantineScanner
{
    Task<ScanResult> ScanAsync(QuarantineEntryDto entry, CancellationToken ct = default);
}

public abstract record ScanResult
{
    public sealed record Clean : ScanResult;
    public sealed record Infected(string Reason) : ScanResult;
    public sealed record Inconclusive(string Reason) : ScanResult;
}
```

### Service Signatures and Flows

```csharp
// engine/src/Nebula.Application/Services/DocumentService.cs
public sealed class DocumentService(
    IDocumentRepository documents,
    IDocumentConfigurationProvider config,
    IDocumentClassificationGate gate,
    ITimelineRepository timelineRepo,
    ILogger<DocumentService> logger)
{
    public Task<DocumentUploadResponseDto> UploadAsync(
        DocumentParentRefDto parent,
        IReadOnlyList<DocumentUploadFileInput> files,
        IReadOnlyList<DocumentUploadFileMetadataDto>? metadata,
        string defaultClassification,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<PaginatedDocumentListDto> ListAsync(
        DocumentParentRefDto parent,
        string? classification,
        string? type,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentDetailDto? Detail, string? ErrorCode)> GetDetailAsync(
        string documentId,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentBinaryRead? Binary, string? ErrorCode)> OpenDownloadAsync(
        string documentId,
        string versionRef,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentReplaceResponseDto? Result, string? ErrorCode)> ReplaceAsync(
        string documentId,
        DocumentUploadFileInput file,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentDetailDto? Detail, string? ErrorCode)> UpdateMetadataAsync(
        string documentId,
        DocumentMetadataUpdateRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentCompletenessSignalDto? Signal, string? ErrorCode)> GetCompletenessAsync(
        DocumentParentRefDto parent,
        ICurrentUserService user,
        CancellationToken ct = default);
}
```

`UploadAsync(parent, files, metadata, defaultClassification, user)`:

1. Validate `files.Count` in `1..25` and total bytes <= 50 MB; all-invalid returns `400`, partial returns `207`, all accepted returns `202`.
2. For each file, reject empty, > 5 MB, nested folder entry, path separator, unsupported extension, extension/content-type mismatch, taxonomy miss, missing metadata schema, or invalid type-specific JSON metadata before writing bytes.
3. Resolve effective classification and run `gate.AuthorizeAsync(user, parent, classification, "create")`; if classification is `restricted`, also require `document:create:restricted`.
4. Resolve the active metadata schema for the effective type from `<docroot>/configuration/metadata-schemas/registry.yaml`, validate `metadata`, and pin `{ id, version, schemaHash }` into sidecar `metadataSchema`.
5. Call `IDocumentRepository.CreateQuarantinedAsync`; repository writes binary to `<docroot>/quarantine/{upload-id}`, writes sidecar JSON to target parent folder with `versions[0].status = "quarantined"`, and appends `uploaded`.
6. Emit one `ActivityTimelineEvent` with `EntityType = "Document"`, `EventType = "DocumentUploaded"`, and payload `{ documentId, parent, classification, type, version }`.
7. Return accepted/rejected item arrays.

`OpenDownloadAsync(documentId, versionRef, user)`:

1. Load sidecar by `documentId`; missing returns `document_not_found`.
2. Run combined read/download gate on sidecar parent and classification.
3. Resolve `latest` to latest available version; if selected version is `quarantined` or `failed_promote`, return `version_not_available`.
4. Append `downloaded` sidecar event and `ActivityTimelineEvent` only after authorization and immediately before streaming.
5. Repository returns stream with path confirmed under parent directory.

`UpdateMetadataAsync(documentId, request, user)`:

1. Load sidecar; missing returns `document_not_found`.
2. Run `update_metadata`; when setting `restricted`, require `create:restricted`; when downgrading from `restricted`, require `declassify`.
3. Validate new type against taxonomy and tags against count/length/no comma rules.
4. Validate JSON `metadata` against the sidecar's stored `metadataSchema`; if type changes, validate against the current active schema for the new type and pin that schema version.
5. If no values change, return existing detail without appending an event.
6. Repository serializes through the per-document lock, updates top-level metadata, `metadataSchema`, and `metadata`, appends `classified` or `metadata_edited`, and returns updated sidecar.
7. Emit `ActivityTimelineEvent` with `DocumentMetadataEdited`.

```csharp
// engine/src/Nebula.Application/Services/DocumentService.cs
public sealed record DocumentUploadFileInput(
    string FileName,
    string ContentType,
    long Length,
    Func<CancellationToken, Task<Stream>> OpenReadStreamAsync);

// engine/src/Nebula.Application/Services/DocumentTemplateService.cs
public sealed class DocumentTemplateService(
    IDocumentRepository documents,
    IDocumentConfigurationProvider config,
    IDocumentClassificationGate gate,
    ITimelineRepository timelineRepo)
{
    public Task<(PaginatedDocumentTemplateListDto? Result, string? ErrorCode)> ListAsync(
        string? type,
        string? classification,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentTemplateDto? Result, string? ErrorCode)> UploadTemplateAsync(
        DocumentUploadFileInput file,
        string classification,
        IReadOnlyList<string> tags,
        ICurrentUserService user,
        CancellationToken ct = default);

    public Task<(DocumentUploadAcceptedItemDto? Result, string? ErrorCode)> LinkToParentAsync(
        string templateId,
        DocumentParentRefDto parent,
        ICurrentUserService user,
        CancellationToken ct = default);
}
```

`LinkToParentAsync(templateId, parent, user)`:

1. Load template sidecar and require `document_template:link` plus classification read for the template.
2. Require parent `document:create` and classification create for the target document.
3. Copy latest available template binary into a new parent-side quarantined document.
4. Parent-side sidecar has `provenance.source = "template:{templateId}"`, `materializedAt`, and `byUserId`.
5. Source template sidecar increments `useCount`, sets `lastUsedAt`, and appends `linked`; parent-side document appends `materialised`.
6. Emit `DocumentTemplateLinked` and `DocumentMaterialised` timeline events.

## API Endpoint Plan

All endpoints live in `engine/src/Nebula.Api/Endpoints/DocumentEndpoints.cs` and are mapped from `Program.cs`.

| Endpoint | Handler | Statuses | Notes |
|----------|---------|----------|-------|
| `GET /documents` | `ListDocuments` | 200, 400, 401, 403 | Query: `parent.type`, `parent.id`, `classification`, `type`, `page`, `pageSize`. |
| `POST /documents` | `UploadDocuments` | 202, 207, 400, 401, 403, 413, 415 | Multipart. Do not set JSON content type. |
| `GET /documents/metadata-schemas` | `ListMetadataSchemas` | 200, 401, 403 | Returns active JSON Schemas from the document metadata schema registry. |
| `GET /documents/{documentId}` | `GetDocumentDetail` | 200, 401, 403, 404 | `previewUrls` only for available `pdf`/`png`. |
| `PUT /documents/{documentId}/replace` | `ReplaceDocument` | 202, 400, 401, 403, 404, 409, 413, 415 | Multipart single binary. |
| `PATCH /documents/{documentId}/metadata` | `UpdateDocumentMetadata` | 200, 400, 401, 403, 404 | JSON body. |
| `GET /documents/{documentId}/versions/{versionRef}/binary` | `DownloadDocumentVersion` | 200, 401, 403, 404, 409 | Stream bytes; no full buffer. |
| `GET /documents/completeness` | `GetDocumentCompleteness` | 200, 400, 401, 403 | Soft signal only; never returns `isComplete`. |
| `GET /document-templates` | `ListDocumentTemplates` | 200, 401, 403 | Paginates templates filtered by classification. |
| `POST /document-templates` | `UploadDocumentTemplate` | 202, 400, 401, 403, 413, 415 | Multipart. |
| `POST /document-templates/{templateId}/link` | `LinkDocumentTemplateToParent` | 202, 400, 401, 403, 404 | Query parent ref. |

## Casbin Enforcement

### Parent ABAC

- Resource: `document`, actions: `read`, `create`, `replace`, `update_metadata`, `download`, `create:restricted`, `declassify`.
- Resource: `document_template`, actions: `read`, `create`, `replace`, `link`.
- Hydrated attrs: `{ parentType, parentId, accountId, brokerId, region, assignedUserId, producerUserId }` where resolvable from parent type.
- Pattern: parent resolver first confirms the parent exists and returns attributes; then each user role is evaluated by `IAuthorizationService.AuthorizeAsync(role, resource, action, attrs)`.

### Classification Gate

- Source: `<docroot>/configuration/casbin-document-roles.yaml`.
- Classification ops: `read`, `create`, `replace`, `update_metadata`, `download`, `create:restricted`, `declassify`, `template:read`, `template:create`, `template:replace`, `template:link`.
- Missing role, tier, or operation denies.
- Multiple roles: any allow wins, with contributing roles logged.
- Every evaluation logs `{ actor, op, documentId?, parentVerdict, classificationVerdict, finalVerdict }`.

## Timeline Events

| Operation | EventType | EntityType | Payload |
|-----------|-----------|------------|---------|
| Upload accepted | `DocumentUploaded` | `Document` | `{ documentId, parent, classification, type, version }` |
| Promotion | `DocumentPromoted` | `Document` | `{ documentId, parent, version, status: "available" }` |
| Download | `DocumentDownloaded` | `Document` | `{ documentId, parent, version }` |
| Replace | `DocumentReplaced` | `Document` | `{ documentId, parent, fromVersion, toVersion }` |
| Metadata edit | `DocumentMetadataEdited` | `Document` | `{ documentId, changedFields }` |
| Template upload | `DocumentTemplateUploaded` | `DocumentTemplate` | `{ templateId, classification, version }` |
| Template link | `DocumentTemplateLinked` | `DocumentTemplate` | `{ templateId, parent, newDocumentId }` |
| Template materialise | `DocumentMaterialised` | `Document` | `{ documentId, sourceTemplateId, parent }` |
| Retention sweep | `DocumentSwept` | `Document` | `{ documentId, parent, type, classification, dryRun }` |

Sidecar `events[]` remains the granular audit source. `ActivityTimelineEvent` is the cross-feature feed summary.

## Frontend Plan

### Contracts and Hooks

`experience/src/features/documents/types.ts` mirrors the DTOs above. `experience/src/features/documents/hooks.ts` exports:

```ts
export function useDocuments(parent: DocumentParentRef, filters: DocumentListFilters): UseQueryResult<PaginatedDocumentList>
export function useDocumentDetail(documentId: string): UseQueryResult<DocumentDetail>
export function useUploadDocuments(parent: DocumentParentRef): UseMutationResult<DocumentUploadResponse, ApiError, UploadDocumentsInput>
export function useReplaceDocument(documentId: string): UseMutationResult<DocumentReplaceResponse, ApiError, File>
export function useUpdateDocumentMetadata(documentId: string): UseMutationResult<DocumentDetail, ApiError, DocumentMetadataUpdateRequest>
export function useDownloadDocumentVersion(documentId: string): UseMutationResult<void, ApiError, string>
export function useDocumentCompleteness(parent: DocumentParentRef): UseQueryResult<DocumentCompletenessSignal>
export function useDocumentTemplates(filters: DocumentTemplateFilters): UseQueryResult<PaginatedDocumentTemplateList>
export function useUploadDocumentTemplate(): UseMutationResult<DocumentTemplate, ApiError, UploadTemplateInput>
export function useLinkDocumentTemplate(templateId: string): UseMutationResult<DocumentUploadAcceptedItem, ApiError, DocumentParentRef>
```

### Components

| Component | Responsibilities |
|-----------|------------------|
| `ParentDocumentsPanel` | Parent-scoped list, classification/type filters, pagination, scanning/failed badges, upload and template-link actions. |
| `DocumentUploadDialog` | Drag/drop up to 25 files, per-file validation preview, default and per-file classification/type, progress rows, accepted/rejected summary. |
| `DocumentDetailView` | Preview for `pdf`/`png`, placeholder for Office/CSV, schema-driven metadata editor, version history with download buttons, provenance/events. |
| `DocumentTemplatesLibrary` | Template listing, upload template dialog, filter by type/classification, link template to selected parent. |
| `DocumentStatusBadge` | Stable width badges for `quarantined`, `available`, `failed_promote`. |

### UI Guardrails

- Use existing UI primitives from `components/ui` and feature-slice placement under `features/documents`.
- Do not add raw palette classes outside theme tokens; run `lint:theme`.
- Parent document panels must remain dense and work-focused; no marketing hero or nested cards.
- Mobile list uses stable card rows; desktop uses table layout with fixed action column.

## DevOps and Runtime Configuration

| Item | Required Work |
|------|---------------|
| Document root | Configure `Documents:RootPath` to a mounted path inside the engine runtime container, default `/app/data/documents` in containers and `./data/documents` in development. |
| Seed config | Ensure first run creates `configuration/taxonomy.yaml`, `document-retention-policies.yaml`, and `casbin-document-roles.yaml` when absent. Do not overwrite existing operator files. |
| Volume | Add/verify a persistent volume in compose for the document repository. |
| Limits | Configure request body cap 50 MB and per-file service validation 5 MB. |
| Hosted services | Register quarantine promotion worker and retention hosted service with bounded intervals. |
| Evidence | Capture `docker compose ps`, health check, API smoke, worker log, and retention dry-run evidence under `planning-mds/operations/evidence/F0020/`. |

## Quality Plan

### Backend Tests

| Test File | Coverage |
|-----------|----------|
| `engine/tests/Nebula.Application.Tests/Documents/DocumentServiceTests.cs` | Upload validation, batch partial success, metadata idempotency, completeness masking. |
| `engine/tests/Nebula.Infrastructure.Tests/Documents/LocalFileSystemDocumentRepositoryTests.cs` | Atomic writes, path traversal rejection, per-document lock, sidecar event append, streaming path safety. |
| `engine/tests/Nebula.Infrastructure.Tests/Documents/YamlDocumentConfigurationProviderTests.cs` | Taxonomy, retention ceiling, classification policy closed-by-default, hot reload. |
| `engine/tests/Nebula.Api.Tests/DocumentEndpointsTests.cs` | HTTP statuses, ProblemDetails codes, multipart upload, download 409, authorization denials. |
| `engine/tests/Nebula.Infrastructure.Tests/Documents/QuarantinePromotionWorkerTests.cs` | 60 s hold, idempotent promote, failed promote, scanner replacement contract. |
| `engine/tests/Nebula.Application.Tests/Documents/DocumentRetentionServiceTests.cs` | Dry-run, full sweep, orphan handling, per-type cap. |

### Frontend Tests

| Test File | Coverage |
|-----------|----------|
| `experience/src/features/documents/tests/ParentDocumentsPanel.test.tsx` | Filters, statuses, empty state, pagination, upload launch. |
| `experience/src/features/documents/tests/DocumentUploadDialog.test.tsx` | Drag/drop, per-file validation, rejected rows, submit payload. |
| `experience/src/features/documents/tests/DocumentDetailView.test.tsx` | Preview vs placeholder, version downloads, metadata edit, provenance. |
| `experience/src/features/documents/tests/DocumentTemplatesLibrary.test.tsx` | Template filters, upload, link flow. |
| `experience/tests/e2e/document-management.spec.ts` | Upload -> promote -> list -> detail -> download -> replace -> metadata -> template link. |

## Integration Checkpoints

### After Entry 1 - Backend Foundation

- [ ] `IDocumentRepository` is the only code path under `<docroot>`.
- [ ] YAML loaders validate taxonomy, retention, and classification policy; bad reload keeps prior policy.
- [ ] Parent ABAC and classification gate both deny by default.
- [ ] No relational document migration exists.

### After Entry 2 - Backend Ingest

- [ ] `POST /documents` returns 202, 207, 400, 413, 415 correctly.
- [ ] Quarantined binaries stay under `<docroot>/quarantine/{upload-id}`.
- [ ] Sidecar JSON is written to the parent folder with `versions[0].status = "quarantined"`.
- [ ] Worker promotes after configured hold and appends `promoted` once.

### After Entry 3 - Backend Operations

- [ ] List, detail, download, replace, metadata, completeness, templates, and retention endpoints/services pass integration tests.
- [ ] Download streams without full buffering and appends audit only on success.
- [ ] Replace and metadata edit serialize through the same per-document lock.
- [ ] Completeness signal masks restricted counts for disallowed roles and never returns `isComplete`.

### After Entry 4 - Frontend

- [ ] Parent pages expose document panels for account, submission, policy, and renewal.
- [ ] Upload dialog supports single and bulk flows with per-file rejected reasons.
- [ ] Detail view renders preview for `pdf`/`png`, placeholders for other allowed types, and provenance/events.
- [ ] Templates library can upload and link templates.
- [ ] `pnpm --dir experience lint`, `lint:theme`, `build`, and tests pass.

### After Entry 5 - Evidence and Review

- [ ] Runtime preflight recorded before validation commands.
- [ ] Backend, frontend, E2E, deployability, code review, and security evidence paths exist under `planning-mds/operations/evidence/F0020/`.
- [ ] Required signoff roles in `STATUS.md` have PASS/APPROVED ledger entries with reviewer, date, and evidence path.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Filesystem sidecar writes can race between replace, metadata edit, promotion, and retention. | High | Implement named per-document lock in repository and use it for all sidecar mutations. | Backend |
| Multipart helper currently does not exist in frontend API wrapper. | Medium | Extend `api.ts` with multipart/blob helpers before UI components. | Frontend |
| 60 s quarantine makes CI slow. | Medium | Use config override within ADR bounds; tests may set hold to 30 s minimum and use worker methods directly for unit tests. | QE |
| External role semantics conflict with the general InternalOnly rule. | Medium | Follow F0020 exception in authorization matrix and classification YAML; Security signoff required. | Security |
| Retention cap is intentionally too short for production insurance records. | Medium | Keep 10-day hard ceiling explicit as MVP-only; do not generalize to production retention. | Architect |

## JSON Serialization Convention

- API JSON remains camelCase.
- OpenAPI 3.0.3 uses `nullable: true`; JSON Schema Draft-07 files use type arrays for nullable fields.
- Date/time fields are UTC ISO-8601.
- `documentId` and `templateId` are strings matching `^doc_[0-9A-HJKMNP-TV-Z]{26}$`.
- `byUserId` in `events[]` may be a UUID string or system principal token (`system:quarantine-worker`, `system:retention-sweeper`).

## DI Registration Changes

Add to `engine/src/Nebula.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddSingleton<IDocumentConfigurationProvider, YamlDocumentConfigurationProvider>();
services.AddScoped<IDocumentParentAccessResolver, DocumentParentAccessResolver>();
services.AddScoped<IDocumentClassificationGate, DocumentClassificationGate>();
services.AddScoped<IDocumentRepository, LocalFileSystemDocumentRepository>();
services.AddScoped<DocumentService>();
services.AddScoped<DocumentTemplateService>();
services.AddScoped<DocumentRetentionService>();
services.AddSingleton<IQuarantineScanner, MockTimerScanner>();
services.AddHostedService<QuarantinePromotionWorker>();
services.AddHostedService<DocumentRetentionHostedService>();
```

## Casbin Policy Sync

Architecture already updated `planning-mds/security/policies/policy.csv` for document and document-template parent ABAC. Implementation must ensure the runtime Casbin policy source consumed by `CasbinAuthorizationService` includes the same rows. If the runtime policy is embedded/copied elsewhere during build, copy the F0020 rows as part of the backend slice and record evidence.
