using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IAccountContactRepository
{
    Task<AccountContact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<AccountContact>> ListAsync(Guid accountId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(AccountContact contact, CancellationToken ct = default);
    Task UpdateAsync(AccountContact contact, CancellationToken ct = default);
    Task<bool> HasAnotherPrimaryAsync(Guid accountId, Guid? excludeContactId, CancellationToken ct = default);
}
