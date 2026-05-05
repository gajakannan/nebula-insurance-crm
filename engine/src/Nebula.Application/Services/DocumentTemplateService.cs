using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Documents;

namespace Nebula.Application.Services;

public sealed class DocumentTemplateService(
    IDocumentRepository documents,
    IDocumentClassificationGate gate,
    IDocumentConfigurationProvider config,
    DocumentService documentService)
{
    public async Task<(PaginatedDocumentTemplateListDto? Result, string? ErrorCode)> ListAsync(
        string? type,
        string? classification,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, DocumentConstants.MaxPageSize);
        var templates = await documents.ListTemplateSidecarsAsync(ct);
        var visible = new List<DocumentTemplateDto>();

        foreach (var sidecar in templates)
        {
            if (!string.IsNullOrWhiteSpace(classification)
                && !sidecar.Classification.Equals(classification, StringComparison.OrdinalIgnoreCase))
                continue;

            var access = await gate.AuthorizeTemplateAsync(user, sidecar.Classification, "read", ct);
            if (!access.Allowed)
                continue;

            visible.Add(ToTemplate(sidecar));
        }

        var total = visible.Count;
        var items = visible
            .Where(t => string.IsNullOrWhiteSpace(type) || t.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.LogicalName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (new PaginatedDocumentTemplateListDto(items, new PaginationDto(page, pageSize, total)), null);
    }

    public async Task<(DocumentTemplateDto? Result, string? ErrorCode)> UploadTemplateAsync(
        DocumentUploadFileInput file,
        string classification,
        IReadOnlyList<string> tags,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        classification = classification.Trim().ToLowerInvariant();
        tags = tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var validation = await documentService.ValidateTemplateUploadAsync(file, classification, tags, ct);
        if (validation is not null)
            return (null, validation.Code);

        var access = await gate.AuthorizeTemplateAsync(user, classification, "create", ct);
        if (!access.Allowed)
            return (null, access.Code ?? "template_access_denied");

        var parent = new DocumentParentRefDto("template", Guid.Empty);
        var snapshot = await config.GetSnapshotAsync(ct);
        var schema = snapshot.MetadataSchemas.FindCurrent("template");
        if (schema is null)
            return (null, "metadata_schema_not_found");

        await using var stream = await file.OpenReadStreamAsync(ct);
        var result = await documents.CreateQuarantinedAsync(new DocumentUploadCommand(
            parent,
            Path.GetFileNameWithoutExtension(file.FileName),
            classification,
            "template",
            tags,
            ToSchemaRef(schema),
            EmptyMetadata(),
            user.UserId,
            file.ContentType,
            file.Length,
            file.FileName,
            "upload",
            IsTemplate: true), stream, ct);

        if (result.DocumentId is null)
            return (null, result.ErrorCode ?? "upload_failed");

        var sidecar = await documents.FindSidecarAsync(result.DocumentId, ct);
        if (sidecar is null)
            return (null, "document_not_found");

        await documentService.AddTimelineAsync("DocumentTemplate", result.DocumentId, "DocumentTemplateUploaded",
            $"Document template {sidecar.LogicalName} uploaded",
            new { templateId = result.DocumentId, classification, version = result.Version ?? 1 },
            user.UserId,
            user.DisplayName,
            ct);

        return (ToTemplate(sidecar), null);
    }

    public async Task<(DocumentUploadAcceptedItemDto? Result, string? ErrorCode)> LinkToParentAsync(
        string templateId,
        DocumentParentRefDto parent,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var template = await documents.FindSidecarAsync(templateId, ct);
        if (template is null)
            return (null, "document_not_found");

        var templateAccess = await gate.AuthorizeTemplateAsync(user, template.Classification, "link", ct);
        if (!templateAccess.Allowed)
            return (null, templateAccess.Code ?? "template_access_denied");

        var parentAccess = await gate.AuthorizeDocumentAsync(user, parent, template.Classification, "create", ct);
        if (!parentAccess.Allowed)
            return (null, parentAccess.Code ?? "parent_access_denied");

        var binary = await documents.OpenVersionForReadAsync(templateId, "latest", ct);
        if (binary is null)
            return (null, "version_not_available");

        var targetType = template.Type == "template" ? "supplemental" : template.Type;
        var snapshot = await config.GetSnapshotAsync(ct);
        var schema = snapshot.MetadataSchemas.FindCurrent(targetType);
        if (schema is null)
            return (null, "metadata_schema_not_found");

        await using var stream = binary.Stream;
        var result = await documents.CreateQuarantinedAsync(new DocumentUploadCommand(
            parent,
            template.LogicalName,
            template.Classification,
            targetType,
            template.Tags,
            ToSchemaRef(schema),
            EmptyMetadata(),
            user.UserId,
            binary.ContentType,
            binary.SizeBytes,
            binary.DownloadFileName,
            $"template:{templateId}",
            IsTemplate: false), stream, ct);

        if (result.DocumentId is null)
            return (null, result.ErrorCode ?? "upload_failed");

        await documents.IncrementTemplateUseAsync(templateId, result.DocumentId, user.UserId, ct);
        await documentService.AddTimelineAsync("DocumentTemplate", templateId, "DocumentTemplateLinked",
            $"Document template {template.LogicalName} linked",
            new { templateId, parent, newDocumentId = result.DocumentId },
            user.UserId,
            user.DisplayName,
            ct);
        await documentService.AddTimelineAsync("Document", result.DocumentId, "DocumentMaterialised",
            $"Document {template.LogicalName} materialised from template",
            new { documentId = result.DocumentId, sourceTemplateId = templateId, parent },
            user.UserId,
            user.DisplayName,
            ct);

        return (new DocumentUploadAcceptedItemDto(result.DocumentId, template.LogicalName, "quarantined"), null);
    }

    private static DocumentTemplateDto ToTemplate(DocumentSidecarDto sidecar)
    {
        var firstVersion = sidecar.Versions.OrderBy(v => v.N).First();
        return new DocumentTemplateDto(
            sidecar.DocumentId,
            sidecar.LogicalName,
            "template",
            sidecar.Classification,
            sidecar.Tags,
            sidecar.UseCount ?? 0,
            sidecar.LastUsedAt,
            firstVersion.UploadedAt,
            firstVersion.UploadedByUserId);
    }

    private static JsonElement EmptyMetadata()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static DocumentMetadataSchemaRefDto ToSchemaRef(DocumentMetadataSchemaDefinition schema) =>
        new(schema.Id, schema.Version, schema.SchemaHash);
}
