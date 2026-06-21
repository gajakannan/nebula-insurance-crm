using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class OperationalReportService : IOperationalReportService
{
    private readonly IOperationalReportProjectionRepository _repo;

    public OperationalReportService(IOperationalReportProjectionRepository repo) => _repo = repo;

    public async Task<OperationalWorkloadReportDto> GetWorkloadAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(query, ProjectionVisibilityResolver.For(user), ct);
        var asOf = query.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var byOwner = rows.Where(r => r.OwnerUserId != null)
            .GroupBy(r => new { r.OwnerUserId, r.OwnerDisplayName })
            .Select(g => new CountByKeyDto(g.Key.OwnerUserId!.Value.ToString(), g.Key.OwnerDisplayName, g.Count()))
            .OrderByDescending(c => c.Count).Take(50).ToList();

        var byStatus = GroupCounts(rows, r => r.CurrentStatus);
        var byWorkflow = GroupCounts(rows, r => r.WorkflowType);

        var dueToday = rows.Where(r => r.IsDueToday)
            .OrderBy(r => r.OwnerDisplayName).Take(query.DrilldownLimit).Select(MapDrilldown).ToList();
        var overdue = rows.Where(r => r.IsOverdue)
            .OrderByDescending(r => r.DaysInStatus).Take(query.DrilldownLimit).Select(MapDrilldown).ToList();

        return new OperationalWorkloadReportDto(
            TotalOpen: rows.Count,
            DueToday: rows.Count(r => r.IsDueToday),
            Overdue: rows.Count(r => r.IsOverdue),
            Unassigned: rows.Count(r => r.OwnerUserId == null),
            ByOwner: byOwner,
            ByStatus: byStatus,
            ByWorkflowType: byWorkflow,
            DueTodayDrilldown: dueToday,
            OverdueDrilldown: overdue,
            AsOf: asOf,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    public async Task<WorkflowAgingReportDto> GetWorkflowAgingAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(query, ProjectionVisibilityResolver.For(user), ct);
        var asOf = query.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var byAgeBand = rows.Where(r => r.AgeBand != null)
            .GroupBy(r => r.AgeBand)
            .Select(g => new AgingBandDto(g.Key!, g.Count()))
            .OrderBy(b => AgeBandOrder(b.AgeBand)).ToList();

        var backlog = rows
            .OrderByDescending(r => r.DaysInStatus ?? 0)
            .Take(query.DrilldownLimit).Select(MapDrilldown).ToList();

        return new WorkflowAgingReportDto(
            TotalOpen: rows.Count,
            ByAgeBand: byAgeBand,
            ByWorkflowType: GroupCounts(rows, r => r.WorkflowType),
            ByStatus: GroupCounts(rows, r => r.CurrentStatus),
            BacklogDrilldown: backlog,
            AsOf: asOf,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    private static List<CountByKeyDto> GroupCounts(IReadOnlyList<OperationalReportProjection> rows, Func<OperationalReportProjection, string?> selector) =>
        rows.Where(r => selector(r) != null)
            .GroupBy(selector)
            .Select(g => new CountByKeyDto(g.Key!, g.Key, g.Count()))
            .OrderByDescending(c => c.Count).ToList();

    private static int AgeBandOrder(string band) => band switch
    {
        "OnTrack" => 0,
        "ApproachingSla" => 1,
        "Overdue" => 2,
        _ => 3,
    };

    private static GlobalSearchResultDto MapDrilldown(OperationalReportProjection p) => new(
        ObjectType: p.SourceObjectType,
        ObjectId: p.SourceObjectId,
        Title: p.SourceObjectType,
        Subtitle: p.CurrentStatus is null ? null : $"{p.CurrentStatus}{(p.DaysInStatus is { } d ? $" · {d}d in status" : "")}",
        Status: p.CurrentStatus,
        OwnerUserId: p.OwnerUserId,
        OwnerDisplayName: p.OwnerDisplayName,
        LineOfBusiness: p.LineOfBusiness,
        Region: p.Region,
        MatchedFields: [],
        Snippet: p.AgeBand,
        TargetUrl: p.TargetUrl,
        Score: 1.0m,
        LastUpdatedAt: p.LastSourceUpdatedAt,
        IndexedAt: p.ProjectedAt);
}
