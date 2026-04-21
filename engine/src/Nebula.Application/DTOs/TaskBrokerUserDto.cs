namespace Nebula.Application.DTOs;

/// <summary>
/// Task DTO for BrokerUser audience (F0009 §12 / BROKER-VISIBILITY-MATRIX.md).
/// Excludes InternalOnly fields: assignedToUserId, audit timestamps (except createdAt), rowVersion.
/// </summary>
public record TaskBrokerUserDto(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateTime? DueDate,
    string? LinkedEntityType,
    Guid? LinkedEntityId,
    DateTime CreatedAt);
