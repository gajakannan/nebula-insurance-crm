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

    private static OperationalReportQuery Query() => new(null, null, null, null, null, 50);

    private static OperationalReportProjection Proj(Guid? owner, bool dueToday = false, bool overdue = false, string ageBand = "OnTrack", int days = 1, string status = "InReview", string workflow = "Submission") => new()
    {
        SourceObjectType = "Submission", SourceObjectId = Guid.NewGuid(), TargetUrl = "/submissions/1",
        WorkflowType = workflow, CurrentStatus = status, OwnerUserId = owner, OwnerDisplayName = owner is null ? null : "A",
        IsDueToday = dueToday, IsOverdue = overdue, AgeBand = ageBand, DaysInStatus = days,
        LastSourceUpdatedAt = DateTimeOffset.UtcNow, ProjectedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetWorkloadAsync_ComputesCounts()
    {
        var repo = new RptRepo
        {
            Rows = [Proj(OwnerA, dueToday: true), Proj(null, overdue: true), Proj(OwnerA)],
        };
        var svc = new OperationalReportService(repo);

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
        var svc = new OperationalReportService(repo);

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
        var svc = new OperationalReportService(repo);

        await svc.GetWorkloadAsync(Query(), new RUser(OwnerA, ["RelationshipManager"], ["West"]), default);

        repo.LastVisibility!.SeeAll.ShouldBeFalse();
        repo.LastVisibility.Regions.ShouldContain("West");
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
        return Task.FromResult(Rows);
    }
    public Task UpsertManyAsync(IReadOnlyList<OperationalReportProjection> rows, CancellationToken ct) => Task.CompletedTask;
    public Task<int> CountAsync(CancellationToken ct) => Task.FromResult(0);
}
