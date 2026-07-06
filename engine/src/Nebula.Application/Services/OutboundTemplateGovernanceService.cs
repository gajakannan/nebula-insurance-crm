using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public sealed class OutboundTemplateGovernanceService(
    IDocumentRepository documents,
    IDocumentClassificationGate gate,
    IAuthorizationService authorization)
{
    private static readonly HashSet<string> Families = new(StringComparer.OrdinalIgnoreCase)
    {
        "coi",
        "acord",
        "proposal",
    };

    public async Task<OutboundDocumentServiceResult<DocumentSidecarDto>> ValidatePublishedTemplateAsync(
        string templateDocumentId,
        string artifactFamily,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!Families.Contains(artifactFamily))
            return new(null, "invalid_artifact_family");

        var template = await documents.FindSidecarAsync(templateDocumentId, ct);
        if (template is null || !template.Parent.Type.Equals("template", StringComparison.OrdinalIgnoreCase))
            return new(null, "template_not_found");

        var access = await gate.AuthorizeTemplateAsync(user, template.Classification, "read", ct);
        if (!access.Allowed)
            return new(null, access.Code ?? "template_access_denied");

        if (!template.Type.Equals("template", StringComparison.OrdinalIgnoreCase))
            return new(null, "template_not_found");

        var latest = template.Versions
            .Where(v => v.Status.Equals("available", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.N)
            .FirstOrDefault();
        if (latest is null)
            return new(null, "template_not_published");

        if (!MetadataEquals(template, "artifactFamily", artifactFamily))
            return new(null, "invalid_artifact_family");

        if (!MetadataEquals(template, "outboundStatus", "published"))
            return new(null, "template_not_published");

        return new(template, null);
    }

    public async Task<bool> CanManageAsync(ICurrentUserService user)
    {
        foreach (var role in user.Roles)
        {
            if (await authorization.AuthorizeAsync(role, "outbound_template", "manage"))
                return true;
        }

        return false;
    }

    private static bool MetadataEquals(DocumentSidecarDto sidecar, string propertyName, string expected)
    {
        if (!sidecar.Metadata.TryGetProperty(propertyName, out var value))
            return false;

        return value.ValueKind == System.Text.Json.JsonValueKind.String
            && value.GetString()?.Equals(expected, StringComparison.OrdinalIgnoreCase) == true;
    }
}
