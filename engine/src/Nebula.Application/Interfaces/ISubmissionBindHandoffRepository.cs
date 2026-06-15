using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ISubmissionBindHandoffRepository
{
    Task<SubmissionBindHandoff?> GetLatestBySubmissionIdAsync(Guid submissionId, CancellationToken ct = default);
    Task<SubmissionBindHandoff?> GetByIdempotencyKeyAsync(Guid submissionId, string idempotencyKey, CancellationToken ct = default);
    Task AddAsync(SubmissionBindHandoff handoff, CancellationToken ct = default);
    Task UpdateAsync(SubmissionBindHandoff handoff, CancellationToken ct = default);
}
