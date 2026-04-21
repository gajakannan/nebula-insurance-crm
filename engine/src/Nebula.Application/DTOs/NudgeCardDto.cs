namespace Nebula.Application.DTOs;

public record NudgeCardDto(
    string NudgeType,
    string Title,
    string Description,
    string LinkedEntityType,
    Guid LinkedEntityId,
    string LinkedEntityName,
    int UrgencyValue,
    string CtaLabel);
