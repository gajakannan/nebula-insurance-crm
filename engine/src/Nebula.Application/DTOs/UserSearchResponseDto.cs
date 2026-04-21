namespace Nebula.Application.DTOs;

public record UserSearchResponseDto(IReadOnlyList<UserSummaryDto> Users);
