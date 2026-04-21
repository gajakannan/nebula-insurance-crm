using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Submission?> GetByIdWithIncludesAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Submission submission, CancellationToken ct = default);
    Task UpdateAsync(Submission submission, CancellationToken ct = default);
    Task<PaginatedResult<Submission>> ListAsync(
        SubmissionListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, bool>> GetStaleFlagsAsync(
        IReadOnlyCollection<Guid> submissionIds,
        CancellationToken ct = default);
}
