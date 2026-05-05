using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Documents;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public sealed class DocumentService(
    IDocumentRepository documents,
    IDocumentConfigurationProvider config,
    IDocumentClassificationGate gate,
    ITimelineRepository timelineRepo,
    ILogger<DocumentService> logger)
{
    public async Task<DocumentUploadResponseDto> UploadAsync(
        DocumentParentRefDto parent,
        IReadOnlyList<DocumentUploadFileInput> files,
        IReadOnlyList<DocumentUploadFileMetadataDto>? metadata,
        string defaultClassification,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var accepted = new List<DocumentUploadAcceptedItemDto>();
        var rejected = new List<DocumentUploadRejectedItemDto>();
        var snapshot = await config.GetSnapshotAsync(ct);

        if (!DocumentConstants.ParentTypes.Contains(parent.Type))
            return RejectAll(files, "invalid_parent", $"Unsupported parent type '{parent.Type}'.");

        if (files.Count == 0)
            return new DocumentUploadResponseDto([], [new DocumentUploadRejectedItemDto(0, null, "empty_batch", "At least one file is required.")]);

        if (files.Count > DocumentConstants.MaxBatchFiles)
            return RejectAll(files, "batch_too_large", $"Limit is {DocumentConstants.MaxBatchFiles} files.");

        if (files.Sum(f => f.Length) > DocumentConstants.MaxBatchSizeBytes)
            return RejectAll(files, "batch_too_large", $"Batch byte limit is {DocumentConstants.MaxBatchSizeBytes}.");

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var meta = metadata is { Count: > 0 } && i < metadata.Count ? metadata[i] : null;
            var classification = Normalize(meta?.Classification ?? defaultClassification);
            var type = Normalize(meta?.Type ?? DetectType(file.FileName));
            var tags = NormalizeTags(meta?.Tags);
            var schema = snapshot.MetadataSchemas.FindCurrent(type);
            var documentMetadata = NormalizeMetadata(meta?.Metadata);
            var logicalName = Path.GetFileNameWithoutExtension(file.FileName);

            var validationError = ValidateUploadFile(file, classification, type, tags, documentMetadata, schema, snapshot);
            if (validationError is not null)
            {
                logger.LogInformation("Document upload rejected filename={FileName} code={Code}", file.FileName, validationError.Code);
                rejected.Add(new DocumentUploadRejectedItemDto(i, logicalName, validationError.Code, validationError.Detail));
                continue;
            }

            var access = await gate.AuthorizeDocumentAsync(user, parent, classification, "create", ct);
            if (access.Allowed && classification.Equals("restricted", StringComparison.OrdinalIgnoreCase))
                access = await gate.AuthorizeDocumentAsync(user, parent, classification, "create:restricted", ct);

            if (!access.Allowed)
            {
                rejected.Add(new DocumentUploadRejectedItemDto(i, logicalName, access.Code ?? "parent_access_denied", access.Dimension));
                continue;
            }

            await using var stream = await file.OpenReadStreamAsync(ct);
            var result = await documents.CreateQuarantinedAsync(new DocumentUploadCommand(
                parent,
                logicalName,
                classification,
                type,
                tags,
                ToSchemaRef(schema!),
                documentMetadata,
                user.UserId,
                file.ContentType,
                file.Length,
                file.FileName,
                "upload",
                IsTemplate: false), stream, ct);

            if (result.DocumentId is null || result.ErrorCode is not null)
            {
                rejected.Add(new DocumentUploadRejectedItemDto(i, logicalName, result.ErrorCode ?? "upload_failed", result.Detail));
                continue;
            }

            accepted.Add(new DocumentUploadAcceptedItemDto(result.DocumentId, logicalName, "quarantined"));
            await AddTimelineAsync("Document", result.DocumentId, "DocumentUploaded",
                $"Document {logicalName} uploaded",
                new { result.DocumentId, parent, classification, type, version = result.Version ?? 1 },
                user.UserId,
                user.DisplayName,
                ct);
        }

        return new DocumentUploadResponseDto(accepted, rejected);
    }

    public async Task<FileValidationResult?> ValidateTemplateUploadAsync(
        DocumentUploadFileInput file,
        string classification,
        IReadOnlyList<string> tags,
        CancellationToken ct = default)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        var schema = snapshot.MetadataSchemas.FindCurrent("template");
        var error = ValidateUploadFile(file, Normalize(classification), "template", NormalizeTags(tags), EmptyMetadata(), schema, snapshot);
        return error is null ? null : new FileValidationResult(error.Code, error.Detail);
    }

    public async Task<DocumentMetadataSchemaRegistryDto> ListMetadataSchemasAsync(CancellationToken ct = default)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        return new DocumentMetadataSchemaRegistryDto(snapshot.MetadataSchemas.Schemas
            .Where(schema => schema.IsCurrent && schema.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(schema => schema.Id)
            .Select(schema => new DocumentMetadataSchemaDto(schema.Id, schema.Version, schema.Status, schema.SchemaHash, schema.Schema))
            .ToList());
    }

    public async Task<PaginatedDocumentListDto> ListAsync(
        DocumentParentRefDto parent,
        string? classification,
        string? type,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, DocumentConstants.MaxPageSize);
        var requestedClassifications = ParseCsv(classification);
        var requestedTypes = ParseCsv(type);

        var sidecars = await documents.ListParentSidecarsAsync(parent, ct);
        var visible = new List<DocumentListItemDto>();
        foreach (var sidecar in sidecars)
        {
            if (requestedClassifications.Count > 0 && !requestedClassifications.Contains(sidecar.Classification))
                continue;
            if (requestedTypes.Count > 0 && !requestedTypes.Contains(sidecar.Type))
                continue;

            var access = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, sidecar.Classification, "read", ct);
            if (!access.Allowed)
                continue;

            var latest = sidecar.Versions.OrderByDescending(v => v.N).FirstOrDefault();
            if (latest is null)
                continue;

            visible.Add(new DocumentListItemDto(
                sidecar.DocumentId,
                sidecar.LogicalName,
                sidecar.Type,
                sidecar.Classification,
                latest.N,
                latest.Status,
                new DocumentLatestUploadDto(latest.UploadedAt, latest.UploadedByUserId),
                sidecar.Parent));
        }

        var total = visible.Count;
        var pageItems = visible
            .OrderByDescending(d => d.LatestUpload.AtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedDocumentListDto(pageItems, new PaginationDto(page, pageSize, total));
    }

    public async Task<(DocumentDetailDto? Detail, string? ErrorCode)> GetDetailAsync(
        string documentId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var sidecar = await documents.FindSidecarAsync(documentId, ct);
        if (sidecar is null)
            return (null, "document_not_found");

        var access = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, sidecar.Classification, "read", ct);
        if (!access.Allowed)
            return (null, access.Code ?? "document_access_denied");

        return (ToDetail(sidecar), null);
    }

    public async Task<(DocumentBinaryRead? Binary, string? ErrorCode)> OpenDownloadAsync(
        string documentId,
        string versionRef,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var sidecar = await documents.FindSidecarAsync(documentId, ct);
        if (sidecar is null)
            return (null, "document_not_found");

        var access = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, sidecar.Classification, "download", ct);
        if (!access.Allowed)
            return (null, access.Code ?? "document_access_denied");

        var binary = await documents.OpenVersionForReadAsync(documentId, versionRef, ct);
        if (binary is null)
            return (null, "version_not_available");

        await documents.AppendEventAsync(documentId, new DocumentEventDto(
            "downloaded",
            DateTime.UtcNow,
            user.UserId.ToString(),
            Version: binary.Version), ct);

        await AddTimelineAsync("Document", documentId, "DocumentDownloaded",
            $"Document {sidecar.LogicalName} downloaded",
            new { documentId, sidecar.Parent, version = binary.Version },
            user.UserId,
            user.DisplayName,
            ct);

        return (binary, null);
    }

    public async Task<(DocumentReplaceResponseDto? Result, string? ErrorCode)> ReplaceAsync(
        string documentId,
        DocumentUploadFileInput file,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        var sidecar = await documents.FindSidecarAsync(documentId, ct);
        if (sidecar is null)
            return (null, "document_not_found");

        var validationError = ValidateBinaryOnly(file);
        if (validationError is not null)
            return (null, validationError.Code);

        var latest = sidecar.Versions.OrderByDescending(v => v.N).FirstOrDefault();
        if (latest is null || !latest.Status.Equals("available", StringComparison.OrdinalIgnoreCase))
            return (null, "version_not_available");

        var access = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, sidecar.Classification, "replace", ct);
        if (!access.Allowed)
            return (null, access.Code ?? "document_access_denied");

        if (!snapshot.TaxonomyTypes.Contains(sidecar.Type))
            return (null, "invalid_type");

        await using var stream = await file.OpenReadStreamAsync(ct);
        var result = await documents.AppendReplacementAsync(documentId, new DocumentReplaceCommand(
            user.UserId,
            file.ContentType,
            file.Length,
            file.FileName), stream, ct);

        if (result.DocumentId is null || result.Version is null)
            return (null, result.ErrorCode ?? "replace_failed");

        await AddTimelineAsync("Document", documentId, "DocumentReplaced",
            $"Document {sidecar.LogicalName} replaced",
            new { documentId, sidecar.Parent, fromVersion = latest.N, toVersion = result.Version },
            user.UserId,
            user.DisplayName,
            ct);

        return (new DocumentReplaceResponseDto(documentId, result.Version.Value, "quarantined"), null);
    }

    public async Task<(DocumentDetailDto? Detail, string? ErrorCode)> UpdateMetadataAsync(
        string documentId,
        DocumentMetadataUpdateRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        var sidecar = await documents.FindSidecarAsync(documentId, ct);
        if (sidecar is null)
            return (null, "document_not_found");

        var targetClassification = Normalize(request.Classification ?? sidecar.Classification);
        var targetType = Normalize(request.Type ?? sidecar.Type);
        if (!DocumentConstants.Classifications.Contains(targetClassification))
            return (null, "invalid_classification");

        if (!snapshot.TaxonomyTypes.Contains(targetType))
            return (null, "invalid_type");

        var targetSchema = targetType.Equals(sidecar.Type, StringComparison.OrdinalIgnoreCase)
            ? snapshot.MetadataSchemas.Find(sidecar.MetadataSchema.Id, sidecar.MetadataSchema.Version)
            : snapshot.MetadataSchemas.FindCurrent(targetType);
        if (targetSchema is null)
            return (null, "metadata_schema_not_found");

        var targetMetadata = request.Metadata is not null
            ? NormalizeMetadata(request.Metadata)
            : targetType.Equals(sidecar.Type, StringComparison.OrdinalIgnoreCase)
                ? NormalizeMetadata(sidecar.Metadata)
                : EmptyMetadata();
        var metadataError = ValidateMetadataAttributes(targetMetadata, targetSchema);
        if (metadataError is not null)
            return (null, metadataError.Code);

        var tags = request.Tags is null ? null : NormalizeTags(request.Tags);
        if (tags is not null && tags.Any(t => t.Contains(',')))
            return (null, "invalid_tags");

        var access = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, sidecar.Classification, "update_metadata", ct);
        if (!access.Allowed)
            return (null, access.Code ?? "metadata_access_denied");

        if (targetClassification.Equals("restricted", StringComparison.OrdinalIgnoreCase)
            && !sidecar.Classification.Equals("restricted", StringComparison.OrdinalIgnoreCase))
        {
            var restrictedAccess = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, targetClassification, "create:restricted", ct);
            if (!restrictedAccess.Allowed)
                return (null, restrictedAccess.Code ?? "classification_access_denied");
        }

        if (sidecar.Classification.Equals("restricted", StringComparison.OrdinalIgnoreCase)
            && !targetClassification.Equals("restricted", StringComparison.OrdinalIgnoreCase))
        {
            var declassifyAccess = await gate.AuthorizeDocumentAsync(user, sidecar.Parent, sidecar.Classification, "declassify", ct);
            if (!declassifyAccess.Allowed)
                return (null, declassifyAccess.Code ?? "classification_access_denied");
        }

        var updated = await documents.UpdateMetadataAsync(documentId, new DocumentMetadataPatch(
            user.UserId,
            request.Classification is null ? null : targetClassification,
            request.Type is null ? null : targetType,
            tags,
            ToSchemaRef(targetSchema),
            targetMetadata), ct);

        if (updated is null)
            return (ToDetail(sidecar), null);

        await AddTimelineAsync("Document", documentId, "DocumentMetadataEdited",
            $"Document {updated.LogicalName} metadata updated",
            new { documentId, changedFields = request },
            user.UserId,
            user.DisplayName,
            ct);

        return (ToDetail(updated), null);
    }

    public async Task<(DocumentCompletenessSignalDto? Signal, string? ErrorCode)> GetCompletenessAsync(
        DocumentParentRefDto parent,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var list = await ListAsync(parent, null, null, 1, DocumentConstants.MaxPageSize, user, ct);
        var docs = list.Documents;
        var totals = new DocumentCompletenessTotalsDto(
            docs.Count(d => d.Status == "available"),
            docs.Count(d => d.Status == "quarantined"),
            docs.Count(d => d.Status == "failed_promote"));
        var byType = docs
            .GroupBy(d => d.Type)
            .Select(g => new DocumentTypeCountDto(g.Key, g.Count()))
            .OrderByDescending(d => d.Count)
            .ThenBy(d => d.Type)
            .ToList();
        var byClassification = DocumentConstants.Classifications
            .Select(c => new DocumentClassificationCountDto(c, docs.Count(d => d.Classification == c)))
            .ToList();

        return (new DocumentCompletenessSignalDto(parent, totals, byType, byClassification), null);
    }

    internal async Task AddTimelineAsync(
        string entityType,
        string documentId,
        string eventType,
        string description,
        object payload,
        Guid actorUserId,
        string? actorDisplayName,
        CancellationToken ct)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = entityType,
            EntityId = DocumentIds.StableGuid(documentId),
            EventType = eventType,
            EventPayloadJson = JsonSerializer.Serialize(payload),
            EventDescription = description,
            BrokerDescription = null,
            ActorUserId = actorUserId,
            ActorDisplayName = actorDisplayName,
            OccurredAt = DateTime.UtcNow,
        }, ct);
    }

    private static DocumentDetailDto ToDetail(DocumentSidecarDto sidecar)
    {
        var urls = sidecar.Versions
            .Select(v => v.Status == "available" && DocumentConstants.IsRenderablePreviewExtension(v.FileName)
                ? $"/documents/{sidecar.DocumentId}/versions/{v.N}/binary"
                : null)
            .ToList();
        return new DocumentDetailDto(sidecar, urls);
    }

    private static DocumentUploadResponseDto RejectAll(IReadOnlyList<DocumentUploadFileInput> files, string code, string detail) =>
        new([], files.Select((f, i) => new DocumentUploadRejectedItemDto(i, Path.GetFileNameWithoutExtension(f.FileName), code, detail)).ToList());

    private static FileValidationError? ValidateUploadFile(
        DocumentUploadFileInput file,
        string classification,
        string type,
        IReadOnlyList<string> tags,
        JsonElement metadata,
        DocumentMetadataSchemaDefinition? schema,
        DocumentConfigurationSnapshot snapshot)
    {
        return ValidateBinaryOnly(file)
            ?? ValidateMetadata(classification, type, tags, snapshot)
            ?? ValidateMetadataAttributes(metadata, schema);
    }

    private static FileValidationError? ValidateBinaryOnly(DocumentUploadFileInput file)
    {
        if (file.Length == 0)
            return new FileValidationError("empty_file", "File is empty.");
        if (file.Length > DocumentConstants.MaxFileSizeBytes)
            return new FileValidationError("file_too_large", $"File exceeds {DocumentConstants.MaxFileSizeBytes} bytes.");
        if (file.FileName.Contains('/') || file.FileName.Contains('\\') || file.FileName.Contains("..", StringComparison.Ordinal))
            return new FileValidationError("invalid_filename", "Filename cannot contain path segments.");

        var extension = Path.GetExtension(file.FileName);
        if (!DocumentConstants.AllowedExtensions.Contains(extension))
            return new FileValidationError("unsupported_type", "Unsupported file extension.");

        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !file.ContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && DocumentConstants.AllowedContentTypes.TryGetValue(extension, out var allowed)
            && !allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return new FileValidationError("unsupported_type", "File content type does not match extension.");

        return null;
    }

    private static FileValidationError? ValidateMetadata(
        string classification,
        string type,
        IReadOnlyList<string> tags,
        DocumentConfigurationSnapshot snapshot)
    {
        if (!DocumentConstants.Classifications.Contains(classification))
            return new FileValidationError("invalid_classification", "Unknown classification.");
        if (!snapshot.TaxonomyTypes.Contains(type))
            return new FileValidationError("invalid_type", "Unknown document type.");
        if (tags.Count > 10 || tags.Any(t => t.Length > 32 || t.Contains(',')))
            return new FileValidationError("invalid_tags", "Tags must be <= 10 entries, <= 32 chars each, with no commas.");
        return null;
    }

    private static FileValidationError? ValidateMetadataAttributes(JsonElement metadata, DocumentMetadataSchemaDefinition? schema)
    {
        if (schema is null)
            return new FileValidationError("metadata_schema_not_found", "Document type does not have an active metadata schema.");
        if (metadata.ValueKind is not JsonValueKind.Object)
            return new FileValidationError("invalid_metadata", "Metadata must be a JSON object.");
        if (schema.Schema.ValueKind is not JsonValueKind.Object)
            return new FileValidationError("metadata_schema_invalid", "Metadata schema is not a JSON object.");

        var properties = schema.Schema.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object
            ? props
            : default;
        var knownProperties = properties.ValueKind == JsonValueKind.Object
            ? properties.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        if (schema.Schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var requiredProperty in required.EnumerateArray().Select(item => item.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!metadata.TryGetProperty(requiredProperty!, out _))
                    return new FileValidationError("invalid_metadata", $"Metadata is missing required field '{requiredProperty}'.");
            }
        }

        var additionalProperties = !schema.Schema.TryGetProperty("additionalProperties", out var additional)
            || additional.ValueKind != JsonValueKind.False;
        foreach (var property in metadata.EnumerateObject())
        {
            if (!knownProperties.Contains(property.Name))
            {
                if (!additionalProperties)
                    return new FileValidationError("invalid_metadata", $"Metadata field '{property.Name}' is not allowed.");
                continue;
            }

            if (!properties.TryGetProperty(property.Name, out var propertySchema))
                continue;

            var propertyError = ValidateMetadataProperty(property.Name, property.Value, propertySchema);
            if (propertyError is not null)
                return propertyError;
        }

        return null;
    }

    private static FileValidationError? ValidateMetadataProperty(string name, JsonElement value, JsonElement schema)
    {
        if (value.ValueKind == JsonValueKind.Null)
            return TypeAllows(schema, "null") ? null : new FileValidationError("invalid_metadata", $"Metadata field '{name}' cannot be null.");

        if (schema.TryGetProperty("enum", out var enumValues) && enumValues.ValueKind == JsonValueKind.Array)
        {
            var allowed = enumValues.EnumerateArray().Select(item => item.ToString()).ToHashSet(StringComparer.Ordinal);
            if (!allowed.Contains(value.ToString()))
                return new FileValidationError("invalid_metadata", $"Metadata field '{name}' is outside the allowed values.");
        }

        if (TypeAllows(schema, "string"))
        {
            if (value.ValueKind != JsonValueKind.String)
                return new FileValidationError("invalid_metadata", $"Metadata field '{name}' must be a string.");
            var text = value.GetString() ?? "";
            if (schema.TryGetProperty("maxLength", out var maxLength) && maxLength.TryGetInt32(out var max) && text.Length > max)
                return new FileValidationError("invalid_metadata", $"Metadata field '{name}' is too long.");
            if (schema.TryGetProperty("format", out var format) && format.GetString() == "date" && !DateTime.TryParse(text, out _))
                return new FileValidationError("invalid_metadata", $"Metadata field '{name}' must be a date.");
            return null;
        }

        if (TypeAllows(schema, "integer"))
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var integer))
                return new FileValidationError("invalid_metadata", $"Metadata field '{name}' must be an integer.");
            return ValidateNumericBounds(name, integer, schema);
        }

        if (TypeAllows(schema, "number"))
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number))
                return new FileValidationError("invalid_metadata", $"Metadata field '{name}' must be a number.");
            return ValidateNumericBounds(name, number, schema);
        }

        if (TypeAllows(schema, "boolean") && value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
            return new FileValidationError("invalid_metadata", $"Metadata field '{name}' must be true or false.");

        return null;
    }

    private static FileValidationError? ValidateNumericBounds(string name, double value, JsonElement schema)
    {
        if (schema.TryGetProperty("minimum", out var minimum) && minimum.TryGetDouble(out var min) && value < min)
            return new FileValidationError("invalid_metadata", $"Metadata field '{name}' is below the minimum.");
        if (schema.TryGetProperty("maximum", out var maximum) && maximum.TryGetDouble(out var max) && value > max)
            return new FileValidationError("invalid_metadata", $"Metadata field '{name}' is above the maximum.");
        return null;
    }

    private static bool TypeAllows(JsonElement schema, string expected)
    {
        if (!schema.TryGetProperty("type", out var type))
            return true;
        if (type.ValueKind == JsonValueKind.String)
            return type.GetString() == expected;
        if (type.ValueKind == JsonValueKind.Array)
            return type.EnumerateArray().Any(item => item.GetString() == expected);
        return true;
    }

    private static string DetectType(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        if (name.Contains("acord", StringComparison.Ordinal)) return "acord";
        if (name.Contains("loss", StringComparison.Ordinal)) return "loss-run";
        if (name.Contains("financial", StringComparison.Ordinal) || name.Contains("finance", StringComparison.Ordinal)) return "financials";
        return "supplemental";
    }

    private static IReadOnlySet<string> ParseCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags) =>
        tags?.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList() ?? [];

    private static JsonElement NormalizeMetadata(JsonElement? metadata) =>
        metadata is null || metadata.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? EmptyMetadata()
            : metadata.Value.Clone();

    private static JsonElement EmptyMetadata()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static DocumentMetadataSchemaRefDto ToSchemaRef(DocumentMetadataSchemaDefinition schema) =>
        new(schema.Id, schema.Version, schema.SchemaHash);

    private sealed record FileValidationError(string Code, string Detail);
}

public sealed record FileValidationResult(string Code, string Detail);
