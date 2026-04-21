namespace Nebula.Application.DTOs;

public record SubmissionCreateDto(
    Guid AccountId,
    Guid BrokerId,
    DateTime EffectiveDate,
    Guid? ProgramId = null,
    string? LineOfBusiness = null,
    decimal? PremiumEstimate = null,
    DateTime? ExpirationDate = null,
    string? Description = null);
