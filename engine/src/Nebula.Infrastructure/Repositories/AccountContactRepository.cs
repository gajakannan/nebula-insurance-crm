using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class AccountContactRepository(AppDbContext db) : IAccountContactRepository
{
    public async Task<AccountContact?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.AccountContacts.FirstOrDefaultAsync(contact => contact.Id == id, ct);

    public async Task<PaginatedResult<AccountContact>> ListAsync(Guid accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AccountContacts.Where(contact => contact.AccountId == accountId);
        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(contact => contact.IsPrimary)
            .ThenBy(contact => contact.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<AccountContact>(data, page, pageSize, totalCount);
    }

    public Task AddAsync(AccountContact contact, CancellationToken ct = default)
    {
        db.AccountContacts.Add(contact);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AccountContact contact, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<bool> HasAnotherPrimaryAsync(Guid accountId, Guid? excludeContactId, CancellationToken ct = default) =>
        await db.AccountContacts.AnyAsync(contact =>
            contact.AccountId == accountId
            && contact.IsPrimary
            && (!excludeContactId.HasValue || contact.Id != excludeContactId.Value), ct);
}
