namespace Nebula.Application.DTOs;

public record AccountRelationshipRequestDto(
    string RelationshipType,
    string NewValue,
    string? Notes);
