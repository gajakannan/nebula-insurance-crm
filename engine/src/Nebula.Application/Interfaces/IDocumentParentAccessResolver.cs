using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IDocumentParentAccessResolver
{
    Task<DocumentParentAccessDecision> AuthorizeAsync(
        ICurrentUserService user,
        DocumentParentRefDto parent,
        string action,
        CancellationToken ct = default);
}

public sealed record DocumentParentAccessDecision(bool Allowed, string? Code);
