namespace Nebula.Application.DTOs;

/// <summary>
/// Point-in-time producer ownership lookup result for GET /producer-ownership
/// (per producer-ownership-lookup-response.schema.json). `Ownership` is null when no period covers `AsOf`.
/// </summary>
public record ProducerOwnershipLookupResponseDto(
    string ScopeType,
    Guid ScopeId,
    DateOnly AsOf,
    ProducerOwnershipDto? Ownership);
