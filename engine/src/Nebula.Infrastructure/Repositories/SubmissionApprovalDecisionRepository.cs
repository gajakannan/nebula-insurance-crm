using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class SubmissionApprovalDecisionRepository(AppDbContext db) : ISubmissionApprovalDecisionRepository
{
    public async Task<IReadOnlyList<SubmissionApprovalDecision>> ListBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken ct = default) =>
        await db.SubmissionApprovalDecisions
            .AsNoTracking()
            .Where(decision => decision.SubmissionId == submissionId)
            .OrderByDescending(decision => decision.DecidedAt)
            .ToListAsync(ct);

    public async Task<SubmissionApprovalDecision?> GetLatestGrantedAsync(
        Guid submissionId,
        CancellationToken ct = default) =>
        await db.SubmissionApprovalDecisions
            .Where(decision => decision.SubmissionId == submissionId && decision.Decision == "Granted")
            .OrderByDescending(decision => decision.DecidedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(SubmissionApprovalDecision decision, CancellationToken ct = default) =>
        await db.SubmissionApprovalDecisions.AddAsync(decision, ct);
}
