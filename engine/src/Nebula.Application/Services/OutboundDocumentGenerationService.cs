using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Documents;

namespace Nebula.Application.Services;

public sealed class OutboundDocumentGenerationService(
    IDocumentRepository documents,
    IDocumentConfigurationProvider config,
    IDocumentClassificationGate gate,
    IAuthorizationService authorization,
    IOutboundMergeDataAssembler mergeData,
    IDocumentRenderer renderer,
    OutboundTemplateGovernanceService templates,
    DocumentService documentService)
{
    public async Task<OutboundDocumentServiceResult<GeneratedDocumentPreviewResponseDto>> PreviewAsync(
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var validation = await ValidateRequestAsync(request, user, "preview", ct);
        if (validation.ErrorCode is not null)
            return new(null, validation.ErrorCode, validation.Detail);

        var context = await mergeData.AssembleAsync(request, ct);
        var templateVersion = validation.Value!.Versions
            .Where(v => v.Status.Equals("available", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.N)
            .First().N;

        return new(new GeneratedDocumentPreviewResponseDto(
            NormalizeFamily(request.ArtifactFamily),
            request.TemplateDocumentId,
            request.TemplateVersion ?? templateVersion,
            context.SourceSnapshotHash,
            context.Diagnostics.Any(d => d.Status.Equals("missing", StringComparison.OrdinalIgnoreCase)) ? "blocked" : "ready",
            null,
            DateTime.UtcNow.AddMinutes(15),
            context.Diagnostics), null);
    }

    public async Task<OutboundDocumentServiceResult<GeneratedDocumentIssueResponseDto>> IssueAsync(
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var validation = await ValidateRequestAsync(request, user, "issue", ct);
        if (validation.ErrorCode is not null)
            return new(null, validation.ErrorCode, validation.Detail);

        var template = validation.Value!;
        var context = await mergeData.AssembleAsync(request, ct);
        if (context.Diagnostics.Any(d => d.Status.Equals("missing", StringComparison.OrdinalIgnoreCase)))
            return new(null, "missing_merge_data");

        var rendered = await renderer.RenderAsync(template, context, ct);
        await using var stream = rendered.Stream;
        var templateVersion = request.TemplateVersion ?? LatestAvailableVersion(template);
        var schema = await GeneratedSchemaAsync(ct);
        var metadata = GeneratedDocumentMetadata.Create(
            NormalizeFamily(request.ArtifactFamily),
            template.DocumentId,
            templateVersion,
            context.SourceSnapshotHash,
            request.RegeneratedFromDocumentId,
            request.RegenerationReason);

        var result = await documents.CreateGeneratedAvailableAsync(new DocumentGeneratedWriteCommand(
            request.Parent,
            $"{NormalizeFamily(request.ArtifactFamily)}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            NormalizeClassification(request.Classification),
            "generated-document",
            [NormalizeFamily(request.ArtifactFamily), "outbound"],
            schema,
            metadata,
            user.UserId,
            rendered.ContentType,
            rendered.SizeBytes,
            rendered.FileName,
            template.DocumentId,
            templateVersion), stream, ct);

        if (result.DocumentId is null)
            return new(null, result.ErrorCode ?? "issue_failed", result.Detail);

        await documents.IncrementTemplateUseAsync(template.DocumentId, result.DocumentId, user.UserId, ct);
        await documentService.AddTimelineAsync(
            TitleParent(request.Parent.Type),
            result.DocumentId,
            request.RegeneratedFromDocumentId is null ? "OutboundDocumentIssued" : "OutboundDocumentRegenerated",
            $"Generated {NormalizeFamily(request.ArtifactFamily)} document issued",
            new
            {
                documentId = result.DocumentId,
                request.Parent,
                artifactFamily = NormalizeFamily(request.ArtifactFamily),
                templateId = template.DocumentId,
                templateVersion,
                request.RegeneratedFromDocumentId,
                sourceSnapshotHash = context.SourceSnapshotHash,
            },
            user.UserId,
            user.DisplayName,
            ct);

        return new(new GeneratedDocumentIssueResponseDto(
            result.DocumentId,
            NormalizeFamily(request.ArtifactFamily),
            template.DocumentId,
            templateVersion,
            DateTime.UtcNow,
            user.UserId,
            context.SourceSnapshotHash,
            request.RegeneratedFromDocumentId,
            $"/documents/{result.DocumentId}/versions/latest/binary"), null);
    }

    public async Task<OutboundDocumentServiceResult<GeneratedDocumentIssueResponseDto>> RegenerateAsync(
        string documentId,
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var existing = await documents.FindSidecarAsync(documentId, ct);
        if (existing is null)
            return new(null, "document_not_found");

        if (!existing.Type.Equals("generated-document", StringComparison.OrdinalIgnoreCase))
            return new(null, "not_generated_document");

        var regenerateRequest = request with
        {
            Parent = existing.Parent,
            RegeneratedFromDocumentId = documentId,
        };

        return await IssueAsync(regenerateRequest, user, ct);
    }

    private async Task<OutboundDocumentServiceResult<DocumentSidecarDto>> ValidateRequestAsync(
        GeneratedDocumentRequestDto request,
        ICurrentUserService user,
        string operation,
        CancellationToken ct)
    {
        if (!DocumentConstants.ParentTypes.Contains(request.Parent.Type))
            return new(null, "invalid_parent");

        if (!DocumentConstants.Classifications.Contains(NormalizeClassification(request.Classification)))
            return new(null, "invalid_classification");

        if (!await CanOutboundAsync(user, operation))
            return new(null, "policy_denied");

        var parentAccess = await gate.AuthorizeDocumentAsync(user, request.Parent, NormalizeClassification(request.Classification), "create", ct);
        if (!parentAccess.Allowed)
            return new(null, parentAccess.Code ?? "parent_access_denied");

        if (NormalizeClassification(request.Classification).Equals("restricted", StringComparison.OrdinalIgnoreCase))
        {
            var restricted = await gate.AuthorizeDocumentAsync(user, request.Parent, "restricted", "create:restricted", ct);
            if (!restricted.Allowed)
                return new(null, restricted.Code ?? "classification_access_denied");
        }

        return await templates.ValidatePublishedTemplateAsync(request.TemplateDocumentId, request.ArtifactFamily, user, ct);
    }

    private async Task<DocumentMetadataSchemaRefDto> GeneratedSchemaAsync(CancellationToken ct)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        var schema = snapshot.MetadataSchemas.FindCurrent("generated-document");
        return schema is null
            ? new DocumentMetadataSchemaRefDto("generated-document", 1, "")
            : new DocumentMetadataSchemaRefDto(schema.Id, schema.Version, schema.SchemaHash);
    }

    private async Task<bool> CanOutboundAsync(ICurrentUserService user, string operation)
    {
        foreach (var role in user.Roles)
        {
            if (await authorization.AuthorizeAsync(role, "outbound_document", operation))
                return true;
        }

        return false;
    }

    private static int LatestAvailableVersion(DocumentSidecarDto template) =>
        template.Versions.Where(v => v.Status.Equals("available", StringComparison.OrdinalIgnoreCase)).Max(v => v.N);

    private static string NormalizeFamily(string family) => family.Trim().ToLowerInvariant();

    private static string NormalizeClassification(string classification) => classification.Trim().ToLowerInvariant();

    private static string TitleParent(string type) =>
        string.Concat(type[..1].ToUpperInvariant(), type[1..]);
}
