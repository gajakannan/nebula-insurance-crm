using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IRenewalRepository
{
    Task<Renewal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Renewal?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Renewal renewal, CancellationToken ct = default);
    Task<bool> HasActiveRenewalForPolicyAsync(Guid policyId, CancellationToken ct = default);
    Task<PaginatedResult<Renewal>> ListAsync(RenewalListQuery query, CancellationToken ct = default);
    Task UpdateAsync(Renewal renewal, CancellationToken ct = default);
}
