using System.Text.Json;

namespace Nebula.Application.DTOs;

public sealed record SavedViewUpsertRequestDto(
    string Name,
    string? Description,
    string ViewType,
    string Visibility,
    string? TeamScopeType,
    string? TeamScopeKey,
    JsonElement Criteria,
    JsonElement? Sort,
    bool IsDefault);

public sealed record SavedViewDto(
    Guid Id,
    string Name,
    string? Description,
    string ViewType,
    string Visibility,
    string? TeamScopeType,
    string? TeamScopeKey,
    JsonElement Criteria,
    JsonElement Sort,
    Guid OwnerUserId,
    string? OwnerDisplayName,
    Guid? LastEditedByUserId,
    string? LastEditedByDisplayName,
    bool IsDefault,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public sealed record SavedViewListQuery(
    string? ViewType,
    string? Visibility,
    bool IncludeArchived,
    int Page,
    int PageSize);
