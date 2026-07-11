namespace Nebula.Application.DTOs;

/// <summary>Validated global-search query (camelCase over the wire).</summary>
public sealed record GlobalSearchQuery(
    string Q,
    IReadOnlyList<string> ObjectTypes,
    string? Status,
    Guid? OwnerUserId,
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    DateOnly? AsOf,
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
/// rows/counts/facets are materialized. F0037 extends the earlier owner/region
/// shape with distribution hierarchy, broker, territory, producer, and as-of scope.
/// </summary>
/// <remarks>
/// WHY: the broker/territory/producer sets are combined differently depending on
/// <see cref="ExplicitScopeRequested"/>:
/// <list type="bullet">
/// <item>Default view (no explicit filter) — the sets are the caller's <b>authority union</b>: a row is
/// visible if it is owned by the caller, in one of their regions, for one of their authorized brokers, or
/// for one of their authorized producers. This is a union so that (e.g.) a manager sees every managed
/// broker's rows regardless of region. Territory is intentionally NOT a default grant — territory ids are
/// derived from broker authority, so OR-ing them would expose sibling brokers that merely share a
/// territory. Territory only ever narrows an explicit request.</item>
/// <item>Explicit request (caller passed rootNodeId / territoryId / producerUserId) — the
/// <c>Requested*</c> sets carry the authorized intersection of that request and are AND-ed as a narrowing
/// filter ON TOP of the authority union. So a manager still sees managed-broker rows within the requested
/// slice regardless of region/ownership, while anything outside authority fails closed to an empty scope
/// upstream (see DistributionScopeService).</item>
/// </list>
/// The <c>BrokerIds</c> / <c>TerritoryIds</c> / <c>ProducerUserIds</c> fields always hold the caller's
/// <b>authority union</b>; the <c>Requested*</c> fields (null unless an explicit filter was passed) hold the
/// narrowing sets.
/// </remarks>
public sealed record ProjectionVisibility(
    bool SeeAll,
    Guid UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Regions,
    IReadOnlySet<Guid> DistributionNodeIds,
    IReadOnlySet<Guid> BrokerIds,
    IReadOnlySet<Guid> TerritoryIds,
    IReadOnlySet<Guid> ProducerUserIds,
    DateOnly AsOf,
    bool HasScope,
    IReadOnlyList<string> ExplanationCodes,
    bool ExplicitScopeRequested = false,
    IReadOnlySet<Guid>? RequestedBrokerIds = null,
    IReadOnlySet<Guid>? RequestedTerritoryIds = null,
    IReadOnlySet<Guid>? RequestedProducerUserIds = null);

public sealed record DistributionScopeRequest(
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    DateOnly? AsOf);
