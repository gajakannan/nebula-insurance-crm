namespace Nebula.Application.DTOs;

/// <summary>Read model for a territory definition (per territory.schema.json). `RowVersion` is xmin as a string.</summary>
public record TerritoryDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyDictionary<string, string> Criteria,
    bool IsActive,
    string RowVersion,
    Guid? ChangedBy,
    DateTime? ChangedAt);
