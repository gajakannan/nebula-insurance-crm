using Microsoft.EntityFrameworkCore;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class BrokerInsightProjectionRepository : IBrokerInsightProjectionRepository
{
    private readonly AppDbContext _db;

    public BrokerInsightProjectionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BrokerInsightProjection>> QueryAsync(
        BrokerInsightProjectionQuery query,
        ProjectionVisibility visibility,
        CancellationToken ct)
    {
        // Source visibility is deliberately applied before all metric aggregation.
        var q = _db.BrokerInsightProjections.AsNoTracking().AsQueryable();

        if (!visibility.HasScope)
            q = q.Where(p => false);

        var brokerIds = visibility.BrokerIds.ToList();
        var territoryIds = visibility.TerritoryIds.ToList();
        var producerIds = visibility.ProducerUserIds.ToList();

        if (!visibility.SeeAll)
        {
            var regions = visibility.Regions.ToList();
            q = q.Where(p => p.Region != null && regions.Contains(p.Region));
        }

        if (brokerIds.Count > 0)
            q = q.Where(p => brokerIds.Contains(p.BrokerId));
        if (territoryIds.Count > 0)
            q = q.Where(p => p.TerritoryId != null && territoryIds.Contains(p.TerritoryId.Value));
        if (producerIds.Count > 0)
            q = q.Where(p => p.ProducerId != null && producerIds.Contains(p.ProducerId.Value));

        if (query.BrokerId.HasValue)
            q = q.Where(p => p.BrokerId == query.BrokerId.Value);
        if (!string.IsNullOrWhiteSpace(query.MetricKey))
            q = q.Where(p => p.MetricKey == query.MetricKey);
        if (!string.IsNullOrWhiteSpace(query.Bucket))
            q = q.Where(p => p.Bucket == query.Bucket);
        if (query.ProducerId.HasValue)
            q = q.Where(p => p.ProducerId == query.ProducerId.Value);
        if (query.TerritoryId.HasValue)
            q = q.Where(p => p.TerritoryId == query.TerritoryId.Value);
        if (query.ProgramId.HasValue)
            q = q.Where(p => p.ProgramId == query.ProgramId.Value);
        if (!string.IsNullOrWhiteSpace(query.LineOfBusiness))
            q = q.Where(p => p.LineOfBusiness == query.LineOfBusiness);
        if (!string.IsNullOrWhiteSpace(query.Region))
            q = q.Where(p => p.Region == query.Region);

        q = q.Where(p => p.PeriodStart <= query.PeriodEnd && p.PeriodEnd >= query.PeriodStart);

        return await q
            .OrderBy(p => p.BrokerName)
            .ThenBy(p => p.MetricKey)
            .ThenBy(p => p.PeriodStart)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);
    }

    public async Task UpsertManyAsync(IReadOnlyList<BrokerInsightProjection> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return;

        foreach (var incoming in rows)
        {
            var existing = await _db.BrokerInsightProjections.FirstOrDefaultAsync(
                p => p.BrokerId == incoming.BrokerId
                     && p.MetricKey == incoming.MetricKey
                     && p.PeriodStart == incoming.PeriodStart
                     && p.PeriodEnd == incoming.PeriodEnd
                     && p.Bucket == incoming.Bucket,
                ct);

            if (existing is null)
            {
                _db.BrokerInsightProjections.Add(incoming);
            }
            else
            {
                existing.BrokerName = incoming.BrokerName;
                existing.MetricLabel = incoming.MetricLabel;
                existing.MetricFamily = incoming.MetricFamily;
                existing.Value = incoming.Value;
                existing.Denominator = incoming.Denominator;
                existing.Unit = incoming.Unit;
                existing.ComparisonValue = incoming.ComparisonValue;
                existing.ComparisonPeriodStart = incoming.ComparisonPeriodStart;
                existing.ComparisonPeriodEnd = incoming.ComparisonPeriodEnd;
                existing.SourceObjectTypesJson = incoming.SourceObjectTypesJson;
                existing.SourceRecordCount = incoming.SourceRecordCount;
                existing.ProgramId = incoming.ProgramId;
                existing.ProducerId = incoming.ProducerId;
                existing.TerritoryId = incoming.TerritoryId;
                existing.LineOfBusiness = incoming.LineOfBusiness;
                existing.Region = incoming.Region;
                existing.LastSourceUpdatedAt = incoming.LastSourceUpdatedAt;
                existing.ProjectedAt = incoming.ProjectedAt;
                existing.ProjectionStatus = incoming.ProjectionStatus;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct) => _db.BrokerInsightProjections.CountAsync(ct);
}
