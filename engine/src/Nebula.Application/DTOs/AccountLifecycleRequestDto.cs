namespace Nebula.Application.DTOs;

public record AccountLifecycleRequestDto(
    string ToState,
    string? ReasonCode,
    string? ReasonDetail);
