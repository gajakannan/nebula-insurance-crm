namespace Nebula.Application.DTOs;

/// <summary>Validated global-search query (camelCase over the wire).</summary>
public sealed record GlobalSearchQuery(
    string Q,
    IReadOnlyList<string> ObjectTypes,
    string? Status,
    Guid? OwnerUserId,
    string? Region,
    string? LineOfBusiness,
    string Sort,
    int Page,
    int PageSize);

public sealed record GlobalSearchResultDto(
    string ObjectType,
    Guid ObjectId,
    string Title,
    string? Subtitle,
    string? Status,
    Guid? OwnerUserId,
    string? OwnerDisplayName,
    string? LineOfBusiness,
    string? Region,
    IReadOnlyList<string> MatchedFields,
    string? Snippet,
    string TargetUrl,
    decimal Score,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset IndexedAt);

public sealed record FacetBucketDto(string Key, string? Label, int Count);

public sealed record GlobalSearchFacetsDto(
    IReadOnlyList<FacetBucketDto> ObjectTypes,
    IReadOnlyList<FacetBucketDto> Owners,
    IReadOnlyList<FacetBucketDto> Statuses,
    IReadOnlyList<FacetBucketDto> Regions,
    IReadOnlyList<FacetBucketDto> LinesOfBusiness);

public sealed record GlobalSearchResponseDto(
    IReadOnlyList<GlobalSearchResultDto> Data,
    GlobalSearchFacetsDto Facets,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    string? QueryEcho,
    DateTimeOffset GeneratedAt);

/// <summary>
/// Computed source-visibility spec for a user, applied at the query layer BEFORE
/// rows/counts/facets are materialized (F0023 Critical risk mitigation). When
/// <see cref="SeeAll"/> is false, only owner-matched or in-region projection rows
/// are visible. Mirrors the projection's denormalized scope columns
/// (OwnerUserId, Region).
/// </summary>
public sealed record ProjectionVisibility(bool SeeAll, IReadOnlyList<string> Regions, Guid UserId);
