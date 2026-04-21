using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class AccountRelationshipHistoryRepository(AppDbContext db) : IAccountRelationshipHistoryRepository
{
    public Task AddAsync(AccountRelationshipHistory history, CancellationToken ct = default)
    {
        db.AccountRelationshipHistory.Add(history);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<AccountRelationshipHistory>> ListByAccountIdAsync(Guid accountId, CancellationToken ct = default) =>
        await db.AccountRelationshipHistory
            .Where(history => history.AccountId == accountId)
            .OrderByDescending(history => history.EffectiveAt)
            .ToListAsync(ct);
}
