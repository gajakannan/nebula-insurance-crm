using System.Text.Json;

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

public sealed record OutboundDocumentServiceResult<T>(T? Value, string? ErrorCode, string? Detail = null);

public static class GeneratedDocumentMetadata
{
    public static JsonElement Create(
        string artifactFamily,
        string templateDocumentId,
        int templateVersion,
        string sourceSnapshotHash,
        string? regeneratedFromDocumentId,
        string? regenerationReason)
    {
        return JsonSerializer.SerializeToElement(new
        {
            artifactFamily,
            templateDocumentId,
            templateVersion,
            sourceSnapshotHash,
            regeneratedFromDocumentId,
            regenerationReason,
        });
    }
}
