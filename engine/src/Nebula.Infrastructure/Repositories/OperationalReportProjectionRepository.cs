using Microsoft.EntityFrameworkCore;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class OperationalReportProjectionRepository : IOperationalReportProjectionRepository
{
    private readonly AppDbContext _db;

    public OperationalReportProjectionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<OperationalReportProjection>> QueryAsync(
        OperationalReportQuery query, ProjectionVisibility visibility, CancellationToken ct)
    {
        // Source-visibility predicate FIRST — all report counts derive from this filtered set.
        var q = _db.OperationalReportProjections.AsNoTracking().AsQueryable();

        if (!visibility.SeeAll)
        {
            var regions = visibility.Regions.ToList();
            var uid = visibility.UserId;
            q = q.Where(p => p.OwnerUserId == uid || (p.Region != null && regions.Contains(p.Region)));
        }

        if (!string.IsNullOrWhiteSpace(query.Region))
            q = q.Where(p => p.Region == query.Region);
        if (!string.IsNullOrWhiteSpace(query.LineOfBusiness))
            q = q.Where(p => p.LineOfBusiness == query.LineOfBusiness);
        if (query.OwnerUserId.HasValue)
            q = q.Where(p => p.OwnerUserId == query.OwnerUserId);
        if (!string.IsNullOrWhiteSpace(query.WorkflowType))
            q = q.Where(p => p.WorkflowType == query.WorkflowType);

        return await q.ToListAsync(ct);
    }

    public async Task UpsertManyAsync(IReadOnlyList<OperationalReportProjection> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return;

        foreach (var incoming in rows)
        {
            var existing = await _db.OperationalReportProjections
                .FirstOrDefaultAsync(p => p.SourceObjectType == incoming.SourceObjectType && p.SourceObjectId == incoming.SourceObjectId, ct);

            if (existing is null)
            {
                _db.OperationalReportProjections.Add(incoming);
            }
            else
            {
                existing.TargetUrl = incoming.TargetUrl;
                existing.WorkflowType = incoming.WorkflowType;
                existing.CurrentStatus = incoming.CurrentStatus;
                existing.StatusEnteredAt = incoming.StatusEnteredAt;
                existing.DaysInStatus = incoming.DaysInStatus;
                existing.OwnerUserId = incoming.OwnerUserId;
                existing.OwnerDisplayName = incoming.OwnerDisplayName;
                existing.DueDate = incoming.DueDate;
                existing.IsDueToday = incoming.IsDueToday;
                existing.IsOverdue = incoming.IsOverdue;
                existing.AgeBand = incoming.AgeBand;
                existing.AccountId = incoming.AccountId;
                existing.BrokerId = incoming.BrokerId;
                existing.PolicyId = incoming.PolicyId;
                existing.LineOfBusiness = incoming.LineOfBusiness;
                existing.Region = incoming.Region;
                existing.ProgramId = incoming.ProgramId;
                existing.TerritoryId = incoming.TerritoryId;
                existing.LastSourceUpdatedAt = incoming.LastSourceUpdatedAt;
                existing.ProjectedAt = incoming.ProjectedAt;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct) => _db.OperationalReportProjections.CountAsync(ct);
}
