namespace Nebula.Application.DTOs;

public record SubmissionUpdateDto(
    Guid? ProgramId,
    string? LineOfBusiness,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    decimal? PremiumEstimate,
    string? Description);
