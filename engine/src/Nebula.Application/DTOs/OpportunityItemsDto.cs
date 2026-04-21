namespace Nebula.Application.DTOs;

public record OpportunityItemsDto(
    IReadOnlyList<OpportunityMiniCardDto> Items,
    int TotalCount);
