using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IDocumentClassificationGate
{
    Task<DocumentAccessDecision> AuthorizeDocumentAsync(
        ICurrentUserService user,
        DocumentParentRefDto parent,
        string classification,
        string operation,
        CancellationToken ct = default);

    Task<DocumentAccessDecision> AuthorizeTemplateAsync(
        ICurrentUserService user,
        string classification,
        string operation,
        CancellationToken ct = default);
}

public sealed record DocumentAccessDecision(
    bool Allowed,
    string? Code,
    string? Dimension,
    IReadOnlyList<string> ContributingRoles);
