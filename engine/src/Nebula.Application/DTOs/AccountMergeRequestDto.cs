namespace Nebula.Application.DTOs;

public record AccountMergeRequestDto(
    Guid SurvivorAccountId,
    string? Notes);
