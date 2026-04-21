using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<Contact>> ListAsync(Guid? brokerId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Contact contact, CancellationToken ct = default);
    Task UpdateAsync(Contact contact, CancellationToken ct = default);
}
