namespace Nebula.Application.DTOs;

public record UserSummaryDto(
    Guid UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsActive);
