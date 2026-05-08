using System.Text.Json;

namespace Nebula.Application.DTOs;

public record LobAttributeEnvelopeDto(
    string ProductKey,
    string ProductVersion,
    string SchemaVersion,
    string? LineOfBusiness,
    JsonElement Attributes);

public record LobValidationIssueDto(
    string Code,
    string Path,
    string Message,
    string Severity);

public record LobValidationResultDto(
    bool IsValid,
    IReadOnlyList<LobValidationIssueDto> Errors);

public record LobSchemaBundleDto(
    Guid Id,
    string ProductKey,
    string ProductVersion,
    string? LineOfBusiness,
    string SchemaVersion,
    string Status,
    object? DataSchema,
    object? UiSchema,
    object? Rules,
    object? ProjectionMap,
    string ContentHash,
    DateTime? ActivatedAt,
    Guid? ActivatedByUserId,
    string RowVersion);

public record LobBundleActivationRequestDto(
    string? ChangeNote);

public record LobBundleActivationResultDto(
    LobSchemaBundleDto Bundle,
    Guid EventId);
