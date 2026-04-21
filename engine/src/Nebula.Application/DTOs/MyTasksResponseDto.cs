namespace Nebula.Application.DTOs;

public record MyTasksResponseDto(
    IReadOnlyList<TaskSummaryDto> Tasks,
    int TotalCount);
