using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class ContactRepository(AppDbContext db) : IContactRepository
{
    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Contacts.Include(c => c.Broker).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PaginatedResult<Contact>> ListAsync(
        Guid? brokerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Contacts.Include(c => c.Broker).AsQueryable();

        if (brokerId.HasValue)
            query = query.Where(c => c.BrokerId == brokerId.Value);

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Contact>(data, page, pageSize, totalCount);
    }

    public Task AddAsync(Contact contact, CancellationToken ct = default)
    {
        db.Contacts.Add(contact);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Contact contact, CancellationToken ct = default) => Task.CompletedTask;
}
