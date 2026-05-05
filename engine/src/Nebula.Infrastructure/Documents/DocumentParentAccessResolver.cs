using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Documents;

public sealed class DocumentParentAccessResolver(IAuthorizationService authorization) : IDocumentParentAccessResolver
{
    public async Task<DocumentParentAccessDecision> AuthorizeAsync(
        ICurrentUserService user,
        DocumentParentRefDto parent,
        string action,
        CancellationToken ct = default)
    {
        var resource = action.StartsWith("template", StringComparison.OrdinalIgnoreCase)
            ? "document_template"
            : "document";
        var attrs = new Dictionary<string, object>
        {
            ["parentType"] = parent.Type,
            ["parentId"] = parent.Id,
            ["userId"] = user.UserId,
        };

        foreach (var role in user.Roles)
        {
            if (await authorization.AuthorizeAsync(role, resource, action, attrs))
                return new DocumentParentAccessDecision(true, null);
        }

        return new DocumentParentAccessDecision(false, "parent_access_denied");
    }
}
