using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class TerritoryRepository(AppDbContext db) : ITerritoryRepository
{
    public async Task<Territory?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Territories.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<bool> ExistsActiveByNameAsync(string name, CancellationToken ct = default)
    {
        var lowered = name.ToLower();
        return await db.Territories.AnyAsync(t => t.IsActive && t.Name.ToLower() == lowered, ct);
    }

    public Task AddAsync(Territory territory, CancellationToken ct = default)
    {
        db.Territories.Add(territory);
        return Task.CompletedTask;
    }
}
