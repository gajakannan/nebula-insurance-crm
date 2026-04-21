using Nebula.Application.DTOs;
using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetAccessibleByIdAsync(
        Guid id,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default);
    Task<PaginatedResult<Account>> ListAsync(
        AccountListQuery query,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, AccountSummaryProjection>> GetSummaryProjectionAsync(
        IReadOnlyCollection<Guid> accountIds,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default);
    Task<PaginatedResult<Policy>> ListPoliciesAsync(Guid accountId, int page, int pageSize, CancellationToken ct = default);
    Task<AccountMergeImpactProjection> GetMergeImpactAsync(Guid accountId, CancellationToken ct = default);
    Task PropagateFallbackStateAsync(
        Guid accountId,
        string displayName,
        string status,
        Guid? survivorAccountId,
        CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task<bool> ExistsActiveTaxIdAsync(string taxId, Guid? excludeAccountId, CancellationToken ct = default);
}
