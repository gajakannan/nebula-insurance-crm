using Nebula.Application.DTOs;

namespace Nebula.Application.Services;

public sealed record LobAttributeStorageResult(
    string? AttributesJson,
    Guid? LobProductVersionId,
    IReadOnlyList<LobValidationIssueDto> Errors)
{
    public bool IsValid => Errors.Count == 0;
    public string RequiredAttributesJson => AttributesJson ?? throw new InvalidOperationException("LOB attributes JSON is unavailable for an invalid storage result.");
    public Guid RequiredLobProductVersionId => LobProductVersionId ?? throw new InvalidOperationException("LOB product version is unavailable for an invalid storage result.");
}
