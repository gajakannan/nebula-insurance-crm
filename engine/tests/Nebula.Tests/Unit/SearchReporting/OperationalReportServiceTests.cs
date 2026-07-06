using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit.SearchReporting;

public class OperationalReportServiceTests
{
    private static readonly Guid OwnerA = Guid.Parse("22220000-0000-0000-0000-00000000000a");
    private static readonly Guid OwnerB = Guid.Parse("22220000-0000-0000-0000-00000000000b");
    private static readonly Guid BrokerA = Guid.Parse("33330000-0000-0000-0000-00000000000a");
    private static readonly Guid BrokerB = Guid.Parse("33330000-0000-0000-0000-00000000000b");
    private static readonly Guid TerritoryA = Guid.Parse("44440000-0000-0000-0000-00000000000a");
    private static readonly Guid TerritoryB = Guid.Parse("44440000-0000-0000-0000-00000000000b");

    private static OperationalReportQuery Query() => new(null, null, null, null, null, null, null, null, 50);

    private static OperationalReportProjection Proj(
        Guid? owner,
        bool dueToday = false,
        bool overdue = false,
        string ageBand = "OnTrack",
        int days = 1,
        string status = "InReview",
        string workflow = "Submission",
        string sourceType = "Submission",
        Guid? brokerId = null,
        Guid? territoryId = null) => new()
    {
        SourceObjectType = sourceType, SourceObjectId = Guid.NewGuid(), TargetUrl = "/submissions/1",
        WorkflowType = workflow, CurrentStatus = status, OwnerUserId = owner, OwnerDisplayName = owner is null ? null : "A",
        IsDueToday = dueToday, IsOverdue = overdue, AgeBand = ageBand, DaysInStatus = days,
        BrokerId = brokerId, TerritoryId = territoryId,
        LastSourceUpdatedAt = DateTimeOffset.UtcNow, ProjectedAt = DateTimeOffset.UtcNow,
    };

    private static BrokerInsightProjection Insight(
        Guid brokerId,
        string brokerName,
        string metricFamily,
        Guid territoryId,
        Guid producerId,
        int sourceRecordCount) => new()
    {
        BrokerId = brokerId,
        BrokerName = brokerName,
        MetricKey = metricFamily == "Activity" ? "activityCount" : "productionAmount",
        MetricLabel = metricFamily,
        MetricFamily = metricFamily,
        PeriodStart = DateOnly.Parse("2026-01-01"),
        PeriodEnd = DateOnly.Parse("2026-07-06"),
        ProducerId = producerId,
        TerritoryId = territoryId,
        SourceRecordCount = sourceRecordCount,
        Region = "West",
        LastSourceUpdatedAt = DateTimeOffset.UtcNow,
        ProjectedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetWorkloadAsync_ComputesCounts()
    {
        var repo = new RptRepo
        {
            Rows = [Proj(OwnerA, dueToday: true), Proj(null, overdue: true), Proj(OwnerA)],
        };
        var svc = new OperationalReportService(repo, new InsightRepo(), new RptScope());

        var r = await svc.GetWorkloadAsync(Query(), new RUser(OwnerA, ["Admin"]), default);

        r.TotalOpen.ShouldBe(3);
        r.DueToday.ShouldBe(1);
        r.Overdue.ShouldBe(1);
        r.Unassigned.ShouldBe(1);
        r.ByOwner.Sum(c => c.Count).ShouldBe(2); // two owned by A
    }

    [Fact]
    public async Task GetWorkflowAgingAsync_GroupsBandsAndOrders()
    {
        var repo = new RptRepo
        {
            Rows =
            [
                Proj(OwnerA, ageBand: "Overdue", days: 30, overdue: true),
                Proj(OwnerA, ageBand: "OnTrack", days: 2),
                Proj(OwnerA, ageBand: "OnTrack", days: 1),
            ],
        };
        var svc = new OperationalReportService(repo, new InsightRepo(), new RptScope());

        var r = await svc.GetWorkflowAgingAsync(Query(), new RUser(OwnerA, ["Admin"]), default);

        r.TotalOpen.ShouldBe(3);
        r.ByAgeBand.First().AgeBand.ShouldBe("OnTrack"); // ordered OnTrack < Overdue
        r.ByAgeBand.Single(b => b.AgeBand == "OnTrack").Count.ShouldBe(2);
        r.BacklogDrilldown.First().Subtitle.ShouldContain("30d"); // highest DaysInStatus first
    }

    [Fact]
    public async Task GetWorkloadAsync_ScopedRole_PassesNonSeeAllVisibility()
    {
        var repo = new RptRepo { Rows = [] };
        var svc = new OperationalReportService(repo, new InsightRepo(), new RptScope());

        await svc.GetWorkloadAsync(Query(), new RUser(OwnerA, ["RelationshipManager"], ["West"]), default);

        repo.LastVisibility!.SeeAll.ShouldBeFalse();
        repo.LastVisibility.Regions.ShouldContain("West");
    }

    [Fact]
    public async Task GetDistributionRollupsAsync_AggregatesOnlyRowsReturnedByVisibilityPredicate()
    {
        var repo = new RptRepo
        {
            Rows =
            [
                Proj(OwnerA, brokerId: BrokerA, territoryId: TerritoryA),
                Proj(OwnerA, overdue: true, sourceType: "Policy", brokerId: BrokerA, territoryId: TerritoryA),
                Proj(OwnerA, sourceType: "Task", brokerId: BrokerA, territoryId: TerritoryA),
                Proj(OwnerB, sourceType: "Policy", brokerId: BrokerA, territoryId: TerritoryA),
            ],
        };
        var query = new DistributionRollupQuery("Hierarchy", "Workflow", DateOnly.Parse("2026-07-06"), null, TerritoryA, null, 25);
        var svc = new OperationalReportService(repo, new InsightRepo(), new RptScope());

        var result = await svc.GetDistributionRollupsAsync(query, new RUser(OwnerA, ["DistributionManager"], ["West"]), default);

        repo.LastVisibility!.TerritoryIds.ShouldContain(TerritoryA);
        result.Scope!.TerritoryId.ShouldBe(TerritoryA);
        result.Totals.RecordCount.ShouldBe(3);
        result.Totals.ProductionCount.ShouldBe(2);
        result.Totals.WorkflowOverdue.ShouldBe(1);
        result.Totals.ActivityCount.ShouldBe(1);
        result.Rows.Single().GroupKey.ShouldBe(BrokerA.ToString());
        result.Rows.Single().DrilldownUrl.ShouldBe($"/operational-reports?report=workload&rootNodeId={BrokerA}&asOf=2026-07-06");
    }

    [Fact]
    public async Task GetDistributionRollupsAsync_ActivityMetricFamilyUsesBrokerInsightProjection()
    {
        var insightRepo = new InsightRepo
        {
            Rows =
            [
                Insight(BrokerA, "Alpha", "Activity", TerritoryA, OwnerA, 3),
                Insight(BrokerB, "Hidden", "Activity", TerritoryB, OwnerB, 9),
                Insight(BrokerA, "Alpha", "Production", TerritoryA, OwnerA, 2),
            ],
        };
        var query = new DistributionRollupQuery("Producer", "Activity", DateOnly.Parse("2026-07-06"), null, TerritoryA, OwnerA, 25);
        var svc = new OperationalReportService(new RptRepo(), insightRepo, new RptScope());

        var result = await svc.GetDistributionRollupsAsync(query, new RUser(OwnerA, ["DistributionManager"], ["West"]), default);

        insightRepo.LastVisibility!.TerritoryIds.ShouldContain(TerritoryA);
        insightRepo.LastVisibility.ProducerUserIds.ShouldContain(OwnerA);
        result.MetricFamily.ShouldBe("Activity");
        result.Totals.RecordCount.ShouldBe(3);
        result.Totals.ActivityCount.ShouldBe(3);
        result.Totals.ProductionCount.ShouldBe(0);
        result.Rows.Single().GroupKey.ShouldBe(OwnerA.ToString());
    }

    [Fact]
    public async Task GetDistributionRollupsAsync_ExternalRoleFailsClosed()
    {
        var insightRepo = new InsightRepo
        {
            Rows = [Insight(BrokerB, "Hidden", "Production", TerritoryB, OwnerB, 7)],
        };
        var svc = new OperationalReportService(new RptRepo(), insightRepo, new RptScope());

        var result = await svc.GetDistributionRollupsAsync(
            new DistributionRollupQuery("Hierarchy", "Production", null, null, null, null, 25),
            new RUser(OwnerA, ["ExternalUser"]),
            default);

        insightRepo.LastVisibility!.HasScope.ShouldBeFalse();
        insightRepo.LastVisibility.ExplanationCodes.ShouldContain("external_denied");
        result.Totals.RecordCount.ShouldBe(0);
        result.Rows.ShouldBeEmpty();
    }
}

file class RUser : ICurrentUserService
{
    public RUser(Guid id, string[]? roles = null, string[]? regions = null)
    {
        UserId = id; Roles = roles ?? []; Regions = regions ?? [];
    }
    public Guid UserId { get; }
    public string? DisplayName => "Tester";
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<string> Regions { get; }
    public string? BrokerTenantId => null;
}

file class RptRepo : IOperationalReportProjectionRepository
{
    public IReadOnlyList<OperationalReportProjection> Rows { get; set; } = [];
    public ProjectionVisibility? LastVisibility { get; private set; }

    public Task<IReadOnlyList<OperationalReportProjection>> QueryAsync(OperationalReportQuery query, ProjectionVisibility visibility, CancellationToken ct)
    {
        LastVisibility = visibility;
        if (!visibility.HasScope)
            return Task.FromResult<IReadOnlyList<OperationalReportProjection>>([]);

        IEnumerable<OperationalReportProjection> rows = Rows;
        if (!visibility.SeeAll)
        {
            rows = rows.Where(r =>
                r.OwnerUserId == visibility.UserId
                || (r.Region is not null && visibility.Regions.Contains(r.Region)));
        }

        if (visibility.BrokerIds.Count > 0)
            rows = rows.Where(r => r.BrokerId is { } brokerId && visibility.BrokerIds.Contains(brokerId));
        if (visibility.TerritoryIds.Count > 0)
            rows = rows.Where(r => r.TerritoryId is { } territoryId && visibility.TerritoryIds.Contains(territoryId));
        if (visibility.ProducerUserIds.Count > 0)
            rows = rows.Where(r => r.OwnerUserId is { } ownerId && visibility.ProducerUserIds.Contains(ownerId));

        return Task.FromResult<IReadOnlyList<OperationalReportProjection>>(rows.ToList());
    }
    public Task UpsertManyAsync(IReadOnlyList<OperationalReportProjection> rows, CancellationToken ct) => Task.CompletedTask;
    public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(0);
}

file class InsightRepo : IBrokerInsightProjectionRepository
{
    public IReadOnlyList<BrokerInsightProjection> Rows { get; set; } = [];
    public ProjectionVisibility? LastVisibility { get; private set; }

    public Task<IReadOnlyList<BrokerInsightProjection>> QueryAsync(BrokerInsightProjectionQuery query, ProjectionVisibility visibility, CancellationToken ct)
    {
        LastVisibility = visibility;
        if (!visibility.HasScope)
            return Task.FromResult<IReadOnlyList<BrokerInsightProjection>>([]);

        IEnumerable<BrokerInsightProjection> rows = Rows;
        if (!visibility.SeeAll)
        {
            rows = rows.Where(r => r.Region is not null && visibility.Regions.Contains(r.Region));
        }

        if (visibility.BrokerIds.Count > 0)
            rows = rows.Where(r => visibility.BrokerIds.Contains(r.BrokerId));
        if (visibility.TerritoryIds.Count > 0)
            rows = rows.Where(r => r.TerritoryId is { } territoryId && visibility.TerritoryIds.Contains(territoryId));
        if (visibility.ProducerUserIds.Count > 0)
            rows = rows.Where(r => r.ProducerId is { } producerId && visibility.ProducerUserIds.Contains(producerId));
        if (query.TerritoryId.HasValue)
            rows = rows.Where(r => r.TerritoryId == query.TerritoryId);
        if (query.ProducerId.HasValue)
            rows = rows.Where(r => r.ProducerId == query.ProducerId);

        return Task.FromResult<IReadOnlyList<BrokerInsightProjection>>(rows.ToList());
    }

    public Task UpsertManyAsync(IReadOnlyList<BrokerInsightProjection> rows, CancellationToken ct) => Task.CompletedTask;
    public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(Rows.Count);
}

file class RptScope : IDistributionScopeService
{
    public Task<ProjectionVisibility> ResolveAsync(DistributionScopeRequest request, ICurrentUserService user, CancellationToken ct)
    {
        var externalDenied = user.Roles.Any(r => r is "ExternalUser" or "BrokerUser");
        var territoryIds = request.TerritoryId is { } territoryId ? new HashSet<Guid> { territoryId } : [];
        var producerIds = request.ProducerUserId is { } producerId ? new HashSet<Guid> { producerId } : [];
        var brokerIds = request.RootNodeId is { } rootId ? new HashSet<Guid> { rootId } : [];
        var asOf = request.AsOf ?? DateOnly.Parse("2026-07-06");

        return Task.FromResult(new ProjectionVisibility(
            SeeAll: user.Roles.Contains("Admin"),
            UserId: user.UserId,
            Roles: user.Roles,
            Regions: user.Regions,
            DistributionNodeIds: new HashSet<Guid>(),
            BrokerIds: brokerIds,
            TerritoryIds: territoryIds,
            ProducerUserIds: producerIds,
            AsOf: asOf,
            HasScope: !externalDenied,
            ExplanationCodes: externalDenied ? ["external_denied"] : ["test_scope"]));
    }

    public Task<bool> CanReadDistributionNodeAsync(Guid nodeId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> CanReadTerritoryAsync(Guid territoryId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> CanReadBrokerAsync(Guid brokerId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> CanReadProducerAsync(Guid producerUserId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct) => Task.FromResult(true);
}
