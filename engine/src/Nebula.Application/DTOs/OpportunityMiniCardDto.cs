namespace Nebula.Application.DTOs;

public record OpportunityMiniCardDto(
    Guid EntityId,
    string EntityName,
    double? Amount,
    int DaysInStatus,
    string? AssignedUserInitials,
    string? AssignedUserDisplayName);
