namespace Nebula.Application.DTOs;

public record BrokerUpdateDto(
    string LegalName,
    string State,
    string Status,
    string? Email,
    string? Phone);
