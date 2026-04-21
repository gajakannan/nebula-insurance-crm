namespace Nebula.Application.DTOs;

public record AccountPolicyListItemDto(
    Guid Id,
    string PolicyNumber,
    string? Carrier,
    string? LineOfBusiness,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal? Premium,
    string CurrentStatus);
