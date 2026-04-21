namespace Nebula.Application.DTOs;

public record RenewalCreateDto(
    Guid PolicyId,
    Guid? AssignedToUserId,
    string? LineOfBusiness);
