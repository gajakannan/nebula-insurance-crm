namespace Nebula.Application.DTOs;

public record BrokerCreateDto(
    string LegalName,
    string LicenseNumber,
    string State,
    string? Email,
    string? Phone);
