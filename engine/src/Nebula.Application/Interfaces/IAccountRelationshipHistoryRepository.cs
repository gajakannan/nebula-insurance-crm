using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IAccountRelationshipHistoryRepository
{
    Task AddAsync(AccountRelationshipHistory history, CancellationToken ct = default);
    Task<IReadOnlyList<AccountRelationshipHistory>> ListByAccountIdAsync(Guid accountId, CancellationToken ct = default);
}
