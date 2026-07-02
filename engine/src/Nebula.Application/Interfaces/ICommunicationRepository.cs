using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ICommunicationRepository
{
    Task AddAsync(CommunicationEvent communication, CancellationToken ct = default);
    Task AddCorrectionAsync(CommunicationCorrection correction, CancellationToken ct = default);
    Task<CommunicationEvent?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<CommunicationEvent>> ListByEntityAsync(string entityType, Guid entityId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> LinkedEntityExistsAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<string?> ResolveLinkedEntityNameAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
