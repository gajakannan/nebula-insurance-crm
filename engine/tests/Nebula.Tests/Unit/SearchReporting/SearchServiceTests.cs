using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit.SearchReporting;

public class SearchServiceTests
{
    private static GlobalSearchQuery Query(int page = 1, int pageSize = 20) =>
        new("acme", [], null, null, null, null, null, null, null, null, "relevance", page, pageSize);

    private static GlobalSearchFacetsDto EmptyFacets() => new([], [], [], [], []);

    [Fact]
    public async Task SearchAsync_MapsResultsAndComputesPaging()
    {
        var repo = new SrchRepo
        {
            Result = new SearchQueryResult(
                [new SearchDocument
                {
                    ObjectType = "Account", ObjectId = Guid.NewGuid(), TargetUrl = "/accounts/1",
                    Title = "Acme Corp", Subtitle = "Acme Holdings", Status = "Active",
                    SearchText = "acme corp", MatchedFieldHintsJson = "[\"title\"]",
                    SourceUpdatedAt = DateTimeOffset.UtcNow, IndexedAt = DateTimeOffset.UtcNow,
                }],
                TotalCount: 1, EmptyFacets()),
        };
        var svc = new SearchService(repo, new SScope());

        var resp = await svc.SearchAsync(Query(), new SUser(Guid.NewGuid(), ["Admin"]), default);

        resp.Data.Count.ShouldBe(1);
        resp.Data[0].Title.ShouldBe("Acme Corp");
        resp.Data[0].MatchedFields.ShouldContain("title");
        resp.TotalCount.ShouldBe(1);
        resp.TotalPages.ShouldBe(1);
        resp.QueryEcho.ShouldBe("acme");
    }

    [Fact]
    public async Task SearchAsync_Admin_GetsSeeAllVisibility()
    {
        var repo = new SrchRepo { Result = new SearchQueryResult([], 0, EmptyFacets()) };
        var svc = new SearchService(repo, new SScope());

        await svc.SearchAsync(Query(), new SUser(Guid.NewGuid(), ["Admin"]), default);

        repo.LastVisibility!.SeeAll.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_ScopedRole_NotSeeAll_PassesRegions()
    {
        var repo = new SrchRepo { Result = new SearchQueryResult([], 0, EmptyFacets()) };
        var svc = new SearchService(repo, new SScope());

        await svc.SearchAsync(Query(), new SUser(Guid.NewGuid(), ["Underwriter"], ["West", "East"]), default);

        repo.LastVisibility!.SeeAll.ShouldBeFalse();
        repo.LastVisibility.Regions.ShouldContain("West");
    }
}

file class SUser : ICurrentUserService
{
    public SUser(Guid id, string[]? roles = null, string[]? regions = null)
    {
        UserId = id; Roles = roles ?? []; Regions = regions ?? [];
    }
    public Guid UserId { get; }
    public string? DisplayName => "Tester";
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<string> Regions { get; }
    public string? BrokerTenantId => null;
}

file class SrchRepo : ISearchDocumentRepository
{
    public SearchQueryResult Result { get; set; } = new([], 0, new([], [], [], [], []));
    public ProjectionVisibility? LastVisibility { get; private set; }

    public Task<SearchQueryResult> SearchAsync(GlobalSearchQuery query, ProjectionVisibility visibility, CancellationToken ct)
    {
        LastVisibility = visibility;
        return Task.FromResult(Result);
    }
    public Task UpsertManyAsync(IReadOnlyList<SearchDocument> documents, CancellationToken ct) => Task.CompletedTask;
    public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(0);
}

file class SScope : IDistributionScopeService
{
    public Task<ProjectionVisibility> ResolveAsync(DistributionScopeRequest request, ICurrentUserService user, CancellationToken ct)
    {
        var externalDenied = user.Roles.Any(r => r is "ExternalUser" or "BrokerUser");
        return Task.FromResult(new ProjectionVisibility(
            SeeAll: user.Roles.Contains("Admin"),
            UserId: user.UserId,
            Roles: user.Roles,
            Regions: user.Regions,
            DistributionNodeIds: new HashSet<Guid>(),
            BrokerIds: request.RootNodeId is { } rootId ? new HashSet<Guid> { rootId } : [],
            TerritoryIds: request.TerritoryId is { } territoryId ? new HashSet<Guid> { territoryId } : [],
            ProducerUserIds: request.ProducerUserId is { } producerId ? new HashSet<Guid> { producerId } : [],
            AsOf: request.AsOf ?? DateOnly.Parse("2026-07-06"),
            HasScope: !externalDenied,
            ExplanationCodes: externalDenied ? ["external_denied"] : ["test_scope"]));
    }

    public Task<bool> CanReadDistributionNodeAsync(Guid nodeId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> CanReadTerritoryAsync(Guid territoryId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> CanReadBrokerAsync(Guid brokerId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> CanReadProducerAsync(Guid producerUserId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
}
