using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ISubmissionApprovalDecisionRepository
{
    Task<IReadOnlyList<SubmissionApprovalDecision>> ListBySubmissionIdAsync(Guid submissionId, CancellationToken ct = default);
    Task<SubmissionApprovalDecision?> GetLatestGrantedAsync(Guid submissionId, CancellationToken ct = default);
    Task AddAsync(SubmissionApprovalDecision decision, CancellationToken ct = default);
}
