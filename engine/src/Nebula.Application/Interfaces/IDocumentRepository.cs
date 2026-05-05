using Nebula.Application.DTOs;
using System.Text.Json;

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

    Task<IReadOnlyList<DocumentSidecarDto>> ListTemplateSidecarsAsync(CancellationToken ct = default);

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

    Task<DocumentSidecarDto?> AppendEventAsync(
        string documentId,
        DocumentEventDto evt,
        CancellationToken ct = default);

    Task<DocumentSidecarDto?> IncrementTemplateUseAsync(
        string templateId,
        string newDocumentId,
        Guid byUserId,
        CancellationToken ct = default);

    Task<IReadOnlyList<QuarantineEntryDto>> ListPromotableQuarantineEntriesAsync(
        DateTime nowUtc,
        TimeSpan hold,
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
    DocumentMetadataSchemaRefDto MetadataSchema,
    JsonElement Metadata,
    Guid UploadedByUserId,
    string ContentType,
    long SizeBytes,
    string OriginalFileName,
    string? ProvenanceSource,
    bool IsTemplate);

public sealed record DocumentReplaceCommand(
    Guid UploadedByUserId,
    string ContentType,
    long SizeBytes,
    string OriginalFileName);

public sealed record DocumentMetadataPatch(
    Guid ActorUserId,
    string? Classification,
    string? Type,
    IReadOnlyList<string>? Tags,
    DocumentMetadataSchemaRefDto? MetadataSchema = null,
    JsonElement? Metadata = null);

public sealed record DocumentWriteResult(string? DocumentId, int? Version, string? ErrorCode, string? Detail);

public sealed record DocumentBinaryRead(Stream Stream, string ContentType, string DownloadFileName, int Version, long SizeBytes);

public sealed record QuarantineEntryDto(string DocumentId, int Version, string QuarantinePath, DateTime UploadedAtUtc);

public sealed record PromoteResult(bool Promoted, string? ErrorCode);

public sealed record RetentionCandidateDto(
    string DocumentId,
    DocumentParentRefDto? Parent,
    string Type,
    string Classification,
    DateTime LastUploadedAtUtc);
