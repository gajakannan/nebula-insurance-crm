using System.Text.Json;

namespace Nebula.Application.DTOs;

public sealed record DocumentParentRefDto(string Type, Guid Id);

public sealed record DocumentLatestUploadDto(DateTime AtUtc, Guid ByUserId);

public sealed record PaginationDto(int Page, int PageSize, int Total);

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
    int? Version = null,
    int? FromVersion = null,
    int? ToVersion = null,
    string? From = null,
    string? To = null,
    IReadOnlyDictionary<string, object?>? Changes = null,
    string? Error = null,
    string? Reason = null);

public sealed record DocumentAuditTimestampsDto(DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record DocumentProvenanceDto(string Source, DateTime? MaterializedAt = null, Guid? ByUserId = null);

public sealed record DocumentMetadataSchemaRefDto(string Id, int Version, string SchemaHash);

public sealed record DocumentMetadataSchemaDto(
    string Id,
    int Version,
    string Status,
    string SchemaHash,
    JsonElement Schema);

public sealed record DocumentMetadataSchemaRegistryDto(IReadOnlyList<DocumentMetadataSchemaDto> Schemas);

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

public sealed record DocumentDetailDto(DocumentSidecarDto Sidecar, IReadOnlyList<string?> PreviewUrls);

public sealed record DocumentUploadFileMetadataDto(string? Classification, string? Type, IReadOnlyList<string>? Tags, JsonElement? Metadata = null);

public sealed record DocumentUploadFileInput(
    string FileName,
    string ContentType,
    long Length,
    Func<CancellationToken, Task<Stream>> OpenReadStreamAsync);

public sealed record DocumentUploadAcceptedItemDto(string DocumentId, string LogicalName, string Status);

public sealed record DocumentUploadRejectedItemDto(int Index, string? LogicalName, string Code, string? Detail);

public sealed record DocumentUploadResponseDto(
    IReadOnlyList<DocumentUploadAcceptedItemDto> Documents,
    IReadOnlyList<DocumentUploadRejectedItemDto> Rejected);

public sealed record DocumentReplaceResponseDto(string DocumentId, int Version, string Status);

public sealed record DocumentMetadataUpdateRequestDto(string? Classification, string? Type, IReadOnlyList<string>? Tags, JsonElement? Metadata = null);

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

public sealed record RetentionSweepResultDto(
    int Scanned,
    int Swept,
    IReadOnlyDictionary<string, int> SweptByType,
    bool DryRun);
