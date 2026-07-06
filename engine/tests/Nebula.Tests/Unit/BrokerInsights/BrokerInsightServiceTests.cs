using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Shouldly;

namespace Nebula.Tests.Unit.BrokerInsights;

public class BrokerInsightServiceTests
{
    private static readonly Guid BrokerA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid BrokerB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid BrokerC = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 3, 31);

    [Fact]
    public async Task GetScorecardsAsync_GroupsBrokerMetricsAndAppliesVisibility()
    {
        var repo = new BrokerInsightRepo
        {
            Rows =
            [
                Projection(BrokerA, "Acme Brokerage", "quoteCount", 10, region: "West"),
                Projection(BrokerA, "Acme Brokerage", "bindCount", 4, region: "West"),
                Projection(BrokerB, "Hidden Brokerage", "quoteCount", 7, region: "East"),
            ],
        };
        var svc = new BrokerInsightService(repo, new BrokerInsightScope());

        var result = await svc.GetScorecardsAsync(
            new BrokerInsightScorecardQuery(null, Start, End, null, null, null, null, null, 1, 25),
            new BrokerInsightUser(Guid.NewGuid(), ["RelationshipManager"], ["West"]),
            default);

        result.Data.Count.ShouldBe(1);
        result.Data[0].BrokerId.ShouldBe(BrokerA);
        result.Data[0].Metrics.Single(m => m.MetricKey == "quoteCount").Value.ShouldBe(10);
        result.Data[0].Metrics.Single(m => m.MetricKey == "bindCount").Value.ShouldBe(4);
        repo.LastVisibility!.SeeAll.ShouldBeFalse();
        repo.LastVisibility.Regions.ShouldContain("West");
    }

    [Fact]
    public async Task GetTrendAsync_MergesBucketsAndFlagsPartialData()
    {
        var repo = new BrokerInsightRepo
        {
            Rows =
            [
                Projection(BrokerA, "Acme Brokerage", "quoteCount", 4, status: "Available", bucketStart: new DateOnly(2026, 1, 1), bucketEnd: new DateOnly(2026, 1, 31)),
                Projection(BrokerA, "Acme Brokerage", "quoteCount", 2, status: "Partial", bucketStart: new DateOnly(2026, 2, 1), bucketEnd: new DateOnly(2026, 2, 28)),
            ],
        };
        var svc = new BrokerInsightService(repo, new BrokerInsightScope());

        var trend = await svc.GetTrendAsync(
            new BrokerInsightTrendQuery(BrokerA, "quoteCount", Start, End, "month", 1, 50),
            new BrokerInsightUser(Guid.NewGuid(), ["Admin"]),
            default);

        trend.ShouldNotBeNull();
        trend.Points.Count.ShouldBe(2);
        trend.Points[1].Status.ShouldBe("Partial");
        trend.PartialData.ShouldBeTrue();
    }

    [Fact]
    public async Task GetBenchmarkAsync_SuppressesPeerStatsWhenPeerCountIsTooSmall()
    {
        var repo = new BrokerInsightRepo
        {
            Rows =
            [
                Projection(BrokerA, "Acme Brokerage", "quoteCount", 8),
                Projection(BrokerB, "Bravo Brokerage", "quoteCount", 10),
                Projection(BrokerC, "Cedar Brokerage", "quoteCount", 12),
            ],
        };
        var svc = new BrokerInsightService(repo, new BrokerInsightScope());

        var benchmark = await svc.GetBenchmarkAsync(
            new BrokerInsightBenchmarkQuery(BrokerA, Start, End, "visibleBrokerGroup"),
            new BrokerInsightUser(Guid.NewGuid(), ["Admin"]),
            default);

        benchmark.ShouldNotBeNull();
        benchmark.PeerSet.Status.ShouldBe("InsufficientPeers");
        benchmark.Metrics.Single(m => m.MetricKey == "quoteCount").Status.ShouldBe("Suppressed");
        benchmark.Metrics.Single(m => m.MetricKey == "quoteCount").PeerMedian.ShouldBeNull();
    }

    private static BrokerInsightProjection Projection(
        Guid brokerId,
        string brokerName,
        string metricKey,
        decimal value,
        string region = "West",
        string status = "Available",
        DateOnly? bucketStart = null,
        DateOnly? bucketEnd = null) => new()
        {
            BrokerId = brokerId,
            BrokerName = brokerName,
            MetricKey = metricKey,
            MetricLabel = metricKey,
            MetricFamily = metricKey == "activityCount" ? "Activity" : "Pipeline",
            PeriodStart = bucketStart ?? Start,
            PeriodEnd = bucketEnd ?? End,
            Bucket = "month",
            Value = value,
            Denominator = 10,
            Unit = "count",
            ComparisonValue = value - 1,
            SourceObjectTypesJson = "[\"Submission\"]",
            SourceRecordCount = 2,
            Region = region,
            LastSourceUpdatedAt = DateTimeOffset.UtcNow,
            ProjectedAt = DateTimeOffset.UtcNow,
            ProjectionStatus = status,
        };
}

file class BrokerInsightUser : ICurrentUserService
{
    public BrokerInsightUser(Guid id, string[]? roles = null, string[]? regions = null)
    {
        UserId = id;
        Roles = roles ?? [];
        Regions = regions ?? [];
    }

    public Guid UserId { get; }
    public string? DisplayName => "Tester";
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<string> Regions { get; }
    public string? BrokerTenantId => null;
}

file class BrokerInsightRepo : IBrokerInsightProjectionRepository
{
    public IReadOnlyList<BrokerInsightProjection> Rows { get; set; } = [];
    public ProjectionVisibility? LastVisibility { get; private set; }

    public Task<IReadOnlyList<BrokerInsightProjection>> QueryAsync(
        BrokerInsightProjectionQuery query,
        ProjectionVisibility visibility,
        CancellationToken ct)
    {
        LastVisibility = visibility;
        var rows = Rows.AsEnumerable();
        if (!visibility.SeeAll)
        {
            rows = rows.Where(r => r.Region is not null && visibility.Regions.Contains(r.Region));
        }
        if (query.BrokerId.HasValue)
        {
            rows = rows.Where(r => r.BrokerId == query.BrokerId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.MetricKey))
        {
            rows = rows.Where(r => r.MetricKey == query.MetricKey);
        }

        return Task.FromResult<IReadOnlyList<BrokerInsightProjection>>(rows.ToList());
    }

    public Task UpsertManyAsync(IReadOnlyList<BrokerInsightProjection> rows, CancellationToken ct) => Task.CompletedTask;
    public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(Rows.Count);
}

file class BrokerInsightScope : IDistributionScopeService
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
