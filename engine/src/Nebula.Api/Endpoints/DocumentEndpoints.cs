using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var documents = app.MapGroup("/documents")
            .WithTags("Documents")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        documents.MapGet("/", ListDocuments);
        documents.MapPost("/", UploadDocuments).DisableAntiforgery();
        documents.MapGet("/completeness", GetCompleteness);
        documents.MapGet("/metadata-schemas", ListMetadataSchemas);
        documents.MapGet("/{documentId}", GetDocumentDetail);
        documents.MapPut("/{documentId}/replace", ReplaceDocument).DisableAntiforgery();
        documents.MapPatch("/{documentId}/metadata", UpdateDocumentMetadata);
        documents.MapGet("/{documentId}/versions/{versionRef}/binary", DownloadDocumentVersion);

        var templates = app.MapGroup("/document-templates")
            .WithTags("DocumentTemplates")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        templates.MapGet("/", ListDocumentTemplates);
        templates.MapPost("/", UploadDocumentTemplate).DisableAntiforgery();
        templates.MapPost("/{templateId}/link", LinkDocumentTemplateToParent);

        return app;
    }

    private static async Task<IResult> ListDocuments(
        [FromQuery(Name = "parent.type")] string parentType,
        [FromQuery(Name = "parent.id")] Guid parentId,
        string? classification,
        string? type,
        int? page,
        int? pageSize,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var result = await service.ListAsync(new DocumentParentRefDto(parentType, parentId), classification, type, page ?? 1, pageSize ?? 20, user, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UploadDocuments(
        IFormFileCollection files,
        [FromForm] string parentType,
        [FromForm] Guid parentId,
        [FromForm] string? defaultClassification,
        [FromForm] string? type,
        [FromForm] List<string>? metadata,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var parsedMetadata = ParseUploadMetadata(files.Count, type, metadata);
        var result = await service.UploadAsync(new DocumentParentRefDto(parentType, parentId), ToInputs(files), parsedMetadata, defaultClassification ?? "public", user, ct);
        return UploadResult(result);
    }

    private static async Task<IResult> ListMetadataSchemas(
        DocumentService service,
        CancellationToken ct)
    {
        return Results.Ok(await service.ListMetadataSchemasAsync(ct));
    }

    private static async Task<IResult> GetDocumentDetail(
        string documentId,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (detail, error) = await service.GetDetailAsync(documentId, user, ct);
        return detail is null ? Problem(error ?? "document_not_found") : Results.Ok(detail);
    }

    private static async Task<IResult> ReplaceDocument(
        string documentId,
        [FromForm] IFormFile file,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (result, error) = await service.ReplaceAsync(documentId, ToInput(file), user, ct);
        return result is null ? Problem(error ?? "replace_failed") : Results.Accepted($"/documents/{documentId}", result);
    }

    private static async Task<IResult> UpdateDocumentMetadata(
        string documentId,
        DocumentMetadataUpdateRequestDto request,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (detail, error) = await service.UpdateMetadataAsync(documentId, request, user, ct);
        return detail is null ? Problem(error ?? "metadata_access_denied") : Results.Ok(detail);
    }

    private static async Task<IResult> DownloadDocumentVersion(
        string documentId,
        string versionRef,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (binary, error) = await service.OpenDownloadAsync(documentId, versionRef, user, ct);
        return binary is null
            ? Problem(error ?? "version_not_available")
            : Results.Stream(binary.Stream, binary.ContentType, binary.DownloadFileName);
    }

    private static async Task<IResult> GetCompleteness(
        [FromQuery(Name = "parent.type")] string parentType,
        [FromQuery(Name = "parent.id")] Guid parentId,
        DocumentService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (signal, error) = await service.GetCompletenessAsync(new DocumentParentRefDto(parentType, parentId), user, ct);
        return signal is null ? Problem(error ?? "parent_access_denied") : Results.Ok(signal);
    }

    private static async Task<IResult> ListDocumentTemplates(
        string? type,
        string? classification,
        int? page,
        int? pageSize,
        DocumentTemplateService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (result, error) = await service.ListAsync(type, classification, page ?? 1, pageSize ?? 20, user, ct);
        return result is null ? Problem(error ?? "template_access_denied") : Results.Ok(result);
    }

    private static async Task<IResult> UploadDocumentTemplate(
        [FromForm] IFormFile file,
        [FromForm] string? classification,
        [FromForm] string? tags,
        DocumentTemplateService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var tagList = string.IsNullOrWhiteSpace(tags)
            ? Array.Empty<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var (result, error) = await service.UploadTemplateAsync(ToInput(file), classification ?? "public", tagList, user, ct);
        return result is null ? Problem(error ?? "template_access_denied") : Results.Accepted($"/document-templates/{result.TemplateId}", result);
    }

    private static async Task<IResult> LinkDocumentTemplateToParent(
        string templateId,
        [FromQuery(Name = "parent.type")] string parentType,
        [FromQuery(Name = "parent.id")] Guid parentId,
        DocumentTemplateService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (result, error) = await service.LinkToParentAsync(templateId, new DocumentParentRefDto(parentType, parentId), user, ct);
        return result is null ? Problem(error ?? "template_access_denied") : Results.Accepted($"/documents/{result.DocumentId}", result);
    }

    private static IReadOnlyList<DocumentUploadFileInput> ToInputs(IEnumerable<IFormFile> files) =>
        files.Select(ToInput).ToList();

    private static DocumentUploadFileInput ToInput(IFormFile file) =>
        new(file.FileName, file.ContentType, file.Length, _ => Task.FromResult(file.OpenReadStream()));

    private static IResult UploadResult(DocumentUploadResponseDto result)
    {
        if (result.Documents.Count > 0 && result.Rejected.Count == 0)
            return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
        if (result.Documents.Count > 0)
            return Results.Json(result, statusCode: StatusCodes.Status207MultiStatus);

        var code = result.Rejected.FirstOrDefault()?.Code ?? "validation_error";
        return Problem(code, result);
    }

    private static IResult Problem(string code, object? extensions = null)
    {
        var status = code switch
        {
            "file_too_large" or "batch_too_large" => StatusCodes.Status413PayloadTooLarge,
            "unsupported_type" => StatusCodes.Status415UnsupportedMediaType,
            "document_not_found" => StatusCodes.Status404NotFound,
            "version_not_available" => StatusCodes.Status409Conflict,
            "parent_access_denied" or "classification_access_denied" or "document_access_denied" or "metadata_access_denied" or "template_access_denied" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };
        var values = new Dictionary<string, object?> { ["code"] = code };
        if (extensions is not null)
            values["details"] = extensions;
        return Results.Problem(title: Title(code), statusCode: status, extensions: values);
    }

    private static string Title(string code) => code.Replace('_', ' ');

    private static IReadOnlyList<DocumentUploadFileMetadataDto>? ParseUploadMetadata(int fileCount, string? type, IReadOnlyList<string>? metadata)
    {
        if ((metadata is null || metadata.Count == 0) && string.IsNullOrWhiteSpace(type))
            return null;

        var parsed = new List<DocumentUploadFileMetadataDto>();
        for (var i = 0; i < fileCount; i++)
        {
            var dto = metadata is not null && i < metadata.Count
                ? ParseMetadataItem(metadata[i])
                : null;
            parsed.Add(new DocumentUploadFileMetadataDto(
                dto?.Classification,
                dto?.Type ?? type,
                dto?.Tags,
                dto?.Metadata));
        }

        return parsed;
    }

    private static DocumentUploadFileMetadataDto? ParseMetadataItem(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            return JsonSerializer.Deserialize<DocumentUploadFileMetadataDto>(root.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch
        {
            return null;
        }
    }
}
