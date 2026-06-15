using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class ProducerOwnershipRepository(AppDbContext db) : IProducerOwnershipRepository
{
    public async Task<ProducerOwnership?> GetOpenPeriodAsync(string scopeType, Guid scopeId, CancellationToken ct = default) =>
        await db.ProducerOwnership
            .FirstOrDefaultAsync(o => o.ScopeType == scopeType && o.ScopeId == scopeId && o.EffectiveTo == null, ct);

    public async Task<ProducerOwnership?> GetAsOfAsync(string scopeType, Guid scopeId, DateOnly asOf, CancellationToken ct = default) =>
        await db.ProducerOwnership
            .Include(o => o.ProducerNode)
            .Where(o => o.ScopeType == scopeType && o.ScopeId == scopeId
                        && o.EffectiveFrom <= asOf
                        && (o.EffectiveTo == null || o.EffectiveTo > asOf))
            .OrderByDescending(o => o.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(ProducerOwnership ownership, CancellationToken ct = default)
    {
        db.ProducerOwnership.Add(ownership);
        return Task.CompletedTask;
    }
}
