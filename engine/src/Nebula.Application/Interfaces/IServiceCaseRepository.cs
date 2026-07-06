using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IServiceCaseRepository
{
    Task AddAsync(ServiceCase serviceCase, CancellationToken ct = default);
    Task<ServiceCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<ServiceCase>> ListAsync(ServiceCaseListQuery query, CancellationToken ct = default);
    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken ct = default);
    Task<bool> PolicyBelongsToAccountAsync(Guid policyId, Guid accountId, CancellationToken ct = default);
    Task<bool> CommunicationExistsAsync(Guid communicationEventId, CancellationToken ct = default);
    Task<string> NextCaseNumberAsync(CancellationToken ct = default);
}
