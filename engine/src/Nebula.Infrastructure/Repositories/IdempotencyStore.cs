using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class IdempotencyStore(AppDbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> GetAsync(string key, string operation, CancellationToken ct = default) =>
        db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record =>
                record.IdempotencyKey == key
                && record.Operation == operation, ct);

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default)
    {
        db.IdempotencyRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }
}
