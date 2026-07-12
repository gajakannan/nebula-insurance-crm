using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Tests.Unit.SearchReporting;

public class DistributionScopeServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11110000-0000-0000-0000-000000000001");
    private static readonly Guid AuthorizedBroker = Guid.Parse("22220000-0000-0000-0000-000000000001");
    private static readonly Guid SiblingBroker = Guid.Parse("22220000-0000-0000-0000-000000000002");
    private static readonly Guid AuthorizedTerritory = Guid.Parse("33330000-0000-0000-0000-000000000001");
    private static readonly Guid ProducerUser = Guid.Parse("44440000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ResolveAsync_ManagerRequestOutsideAuthorityFailsClosed()
    {
        var repo = new ScopeRepo
        {
            Hierarchies =
            {
                [SiblingBroker] = new DistributionHierarchyScope(true, new HashSet<Guid> { SiblingBroker }, new HashSet<Guid> { SiblingBroker }),
            },
            Authority = new DistributionAuthorityScope(
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedTerritory },
                new HashSet<Guid>(),
                ["managed_broker_authority"]),
        };
        var service = new DistributionScopeService(repo);

        var visibility = await service.ResolveAsync(
            new DistributionScopeRequest(SiblingBroker, null, null, DateOnly.Parse("2026-07-06")),
            new ScopeUser(UserId, ["DistributionManager"], ["West"]),
            default);

        visibility.HasScope.ShouldBeFalse();
        visibility.ExplanationCodes.ShouldContain("requested_scope_outside_authority");
    }

    [Fact]
    public async Task ResolveAsync_RelationshipManagerCanNarrowToAuthorizedTerritoryAndProducer()
    {
        var repo = new ScopeRepo
        {
            Authority = new DistributionAuthorityScope(
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedTerritory },
                new HashSet<Guid> { ProducerUser },
                ["producer_user_authority"]),
        };
        repo.TerritoryBrokerIds[AuthorizedTerritory] = new HashSet<Guid> { AuthorizedBroker };
        var service = new DistributionScopeService(repo);

        var visibility = await service.ResolveAsync(
            new DistributionScopeRequest(null, AuthorizedTerritory, ProducerUser, DateOnly.Parse("2026-07-06")),
            new ScopeUser(UserId, ["RelationshipManager"], ["West"]),
            default);

        visibility.HasScope.ShouldBeTrue();
        visibility.TerritoryIds.ShouldBe([AuthorizedTerritory], ignoreOrder: true);
        visibility.ProducerUserIds.ShouldBe([ProducerUser], ignoreOrder: true);
        visibility.BrokerIds.ShouldBe([AuthorizedBroker], ignoreOrder: true);
    }

    [Fact]
    public async Task CanReadBrokerAsync_ReturnsFalseForHiddenSibling()
    {
        var repo = new ScopeRepo
        {
            Hierarchies =
            {
                [SiblingBroker] = new DistributionHierarchyScope(true, new HashSet<Guid> { SiblingBroker }, new HashSet<Guid> { SiblingBroker }),
            },
            Authority = new DistributionAuthorityScope(
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                ["managed_broker_authority"]),
        };
        var service = new DistributionScopeService(repo);

        var canRead = await service.CanReadBrokerAsync(
            SiblingBroker,
            new ScopeUser(UserId, ["DistributionManager"], ["West"]),
            DateOnly.Parse("2026-07-06"),
            default);

        canRead.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_DefaultManagerView_UsesAuthorityUnionAndIsNotExplicitlyScoped()
    {
        var repo = new ScopeRepo
        {
            Authority = new DistributionAuthorityScope(
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedTerritory },
                new HashSet<Guid>(),
                ["managed_broker_authority"]),
        };
        var service = new DistributionScopeService(repo);

        var visibility = await service.ResolveAsync(
            new DistributionScopeRequest(null, null, null, DateOnly.Parse("2026-07-06")),
            new ScopeUser(UserId, ["DistributionManager"], ["West"]),
            default);

        visibility.HasScope.ShouldBeTrue();
        // Default view (no explicit rootNode/territory/producer) → authority union, OR-ed in the repos so a
        // manager sees managed-broker rows regardless of region. CR-H1 regression guard.
        visibility.ExplicitScopeRequested.ShouldBeFalse();
        visibility.BrokerIds.ShouldContain(AuthorizedBroker);
    }

    [Fact]
    public async Task ResolveAsync_ExplicitTerritoryFilter_IsExplicitlyScoped()
    {
        var repo = new ScopeRepo
        {
            Authority = new DistributionAuthorityScope(
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedBroker },
                new HashSet<Guid> { AuthorizedTerritory },
                new HashSet<Guid>(),
                ["managed_broker_authority"]),
        };
        repo.TerritoryBrokerIds[AuthorizedTerritory] = new HashSet<Guid> { AuthorizedBroker };
        var service = new DistributionScopeService(repo);

        var visibility = await service.ResolveAsync(
            new DistributionScopeRequest(null, AuthorizedTerritory, null, DateOnly.Parse("2026-07-06")),
            new ScopeUser(UserId, ["DistributionManager"], ["West"]),
            default);

        visibility.HasScope.ShouldBeTrue();
        // Explicit filter → the request lands in the Requested* narrowing sets...
        visibility.ExplicitScopeRequested.ShouldBeTrue();
        visibility.RequestedTerritoryIds.ShouldNotBeNull().ShouldContain(AuthorizedTerritory);
        // ...while the authority union is preserved in BrokerIds, so the repos still OR in managed-broker
        // rows within the requested slice regardless of region/ownership (CR-L3 regression guard).
        visibility.BrokerIds.ShouldContain(AuthorizedBroker);
    }
}

file class ScopeUser : ICurrentUserService
{
    public ScopeUser(Guid userId, IReadOnlyList<string> roles, IReadOnlyList<string> regions)
    {
        UserId = userId;
        Roles = roles;
        Regions = regions;
    }

    public Guid UserId { get; }
    public string? DisplayName => "Scope Tester";
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<string> Regions { get; }
    public string? BrokerTenantId => null;
}

file class ScopeRepo : IDistributionScopeRepository
{
    public Dictionary<Guid, DistributionHierarchyScope> Hierarchies { get; } = [];
    public Dictionary<Guid, IReadOnlySet<Guid>> TerritoryBrokerIds { get; } = [];
    public DistributionAuthorityScope Authority { get; set; } = new(
        new HashSet<Guid>(),
        new HashSet<Guid>(),
        new HashSet<Guid>(),
        new HashSet<Guid>(),
        []);

    public Task<DistributionHierarchyScope> ResolveHierarchyScopeAsync(Guid rootNodeId, CancellationToken ct) =>
        Task.FromResult(Hierarchies.TryGetValue(rootNodeId, out var scope)
            ? scope
            : new DistributionHierarchyScope(false, new HashSet<Guid>(), new HashSet<Guid>()));

    public Task<IReadOnlySet<Guid>> ListBrokerIdsForTerritoryAsync(Guid territoryId, DateOnly asOf, CancellationToken ct) =>
        Task.FromResult(TerritoryBrokerIds.TryGetValue(territoryId, out var brokerIds) ? brokerIds : new HashSet<Guid>());

    public Task<DistributionAuthorityScope> ResolveAuthorityScopeAsync(
        Guid userId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> regions,
        DateOnly asOf,
        CancellationToken ct) => Task.FromResult(Authority);
}
