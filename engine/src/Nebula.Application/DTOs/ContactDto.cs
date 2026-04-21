namespace Nebula.Application.DTOs;

public record ContactDto(
    Guid Id,
    Guid? BrokerId,
    Guid? AccountId,
    string FullName,
    string? Email,
    string? Phone,
    string Role,
    uint RowVersion);
