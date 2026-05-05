using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Documents;

public sealed class DocumentClassificationGate(
    IDocumentConfigurationProvider config,
    IDocumentParentAccessResolver parentAccess,
    IAuthorizationService authorization,
    ILogger<DocumentClassificationGate> logger) : IDocumentClassificationGate
{
    public async Task<DocumentAccessDecision> AuthorizeDocumentAsync(
        ICurrentUserService user,
        DocumentParentRefDto parent,
        string classification,
        string operation,
        CancellationToken ct = default)
    {
        var parentDecision = await parentAccess.AuthorizeAsync(user, parent, operation, ct);
        if (!parentDecision.Allowed)
            return Log(user, classification, operation, false, false, [], "parent_access_denied", "parent_abac");

        var classificationDecision = await AuthorizeClassificationAsync(user, classification, operation, ct);
        return Log(user, classification, operation, true, classificationDecision.Allowed, classificationDecision.ContributingRoles,
            classificationDecision.Allowed ? null : "classification_access_denied",
            classificationDecision.Allowed ? null : "classification_policy");
    }

    public async Task<DocumentAccessDecision> AuthorizeTemplateAsync(
        ICurrentUserService user,
        string classification,
        string operation,
        CancellationToken ct = default)
    {
        var parentAllowed = false;
        foreach (var role in user.Roles)
        {
            if (await authorization.AuthorizeAsync(role, "document_template", operation))
            {
                parentAllowed = true;
                break;
            }
        }

        if (!parentAllowed)
            return Log(user, classification, operation, false, false, [], "template_access_denied", "parent_abac");

        var classificationDecision = await AuthorizeClassificationAsync(user, classification, operation, ct);
        return Log(user, classification, operation, true, classificationDecision.Allowed, classificationDecision.ContributingRoles,
            classificationDecision.Allowed ? null : "classification_access_denied",
            classificationDecision.Allowed ? null : "classification_policy");
    }

    private async Task<DocumentAccessDecision> AuthorizeClassificationAsync(
        ICurrentUserService user,
        string classification,
        string operation,
        CancellationToken ct)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        var contributing = new List<string>();
        foreach (var role in user.Roles)
        {
            var row = snapshot.ClassificationPolicy.FirstOrDefault(r =>
                r.Role.Equals(role, StringComparison.OrdinalIgnoreCase)
                && r.Tier.Equals(classification, StringComparison.OrdinalIgnoreCase)
                && r.Operation.Equals(operation, StringComparison.OrdinalIgnoreCase));

            if (row?.Verdict.Equals("allow", StringComparison.OrdinalIgnoreCase) == true)
                contributing.Add(role);
        }

        return new DocumentAccessDecision(contributing.Count > 0, null, null, contributing);
    }

    private DocumentAccessDecision Log(
        ICurrentUserService user,
        string classification,
        string operation,
        bool parentVerdict,
        bool classificationVerdict,
        IReadOnlyList<string> roles,
        string? code,
        string? dimension)
    {
        var allowed = parentVerdict && classificationVerdict;
        logger.LogInformation(
            "Document access evaluated actor={Actor} op={Operation} classification={Classification} parentVerdict={ParentVerdict} classificationVerdict={ClassificationVerdict} finalVerdict={FinalVerdict}",
            user.UserId,
            operation,
            classification,
            parentVerdict,
            classificationVerdict,
            allowed);
        return new DocumentAccessDecision(allowed, code, dimension, roles);
    }
}
