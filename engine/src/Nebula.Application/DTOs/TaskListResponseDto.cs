namespace Nebula.Application.DTOs;

public record TaskListResponseDto(
    IReadOnlyList<TaskListItemDto> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
