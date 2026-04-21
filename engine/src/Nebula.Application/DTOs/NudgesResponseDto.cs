namespace Nebula.Application.DTOs;

public record NudgesResponseDto(
    IReadOnlyList<NudgeCardDto> Nudges);
