namespace Nebula.Application.DTOs;

/// <summary>Body for POST /territories (per territory-create-request.schema.json).</summary>
public record TerritoryCreateRequestDto(
    string Name,
    string? Description,
    IReadOnlyDictionary<string, string> Criteria);
