using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Documents;

public sealed class OutboundMergeDataAssembler : IOutboundMergeDataAssembler
{
    public Task<OutboundMergeContext> AssembleAsync(
        GeneratedDocumentRequestDto request,
        CancellationToken ct = default)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["parentType"] = request.Parent.Type,
            ["parentId"] = request.Parent.Id,
            ["artifactFamily"] = request.ArtifactFamily.Trim().ToLowerInvariant(),
            ["templateDocumentId"] = request.TemplateDocumentId,
            ["templateVersion"] = request.TemplateVersion,
            ["regeneratedFromDocumentId"] = request.RegeneratedFromDocumentId,
        };
        var diagnostics = new List<GeneratedDocumentMergeDiagnosticDto>
        {
            new("parent", "resolved", $"{request.Parent.Type}:{request.Parent.Id}"),
            new("template", "resolved", request.TemplateDocumentId),
        };
        var snapshot = JsonSerializer.Serialize(values);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))).ToLowerInvariant();

        return Task.FromResult(new OutboundMergeContext(
            request.Parent,
            request.ArtifactFamily.Trim().ToLowerInvariant(),
            values,
            diagnostics,
            $"sha256:{hash}"));
    }
}
