using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class SubmissionBindHandoffRepository(AppDbContext db) : ISubmissionBindHandoffRepository
{
    public async Task<SubmissionBindHandoff?> GetLatestBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken ct = default) =>
        await db.SubmissionBindHandoffs
            .Where(handoff => handoff.SubmissionId == submissionId)
            .OrderByDescending(handoff => handoff.RequestedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<SubmissionBindHandoff?> GetByIdempotencyKeyAsync(
        Guid submissionId,
        string idempotencyKey,
        CancellationToken ct = default) =>
        await db.SubmissionBindHandoffs
            .FirstOrDefaultAsync(
                handoff => handoff.SubmissionId == submissionId && handoff.IdempotencyKey == idempotencyKey,
                ct);

    public async Task AddAsync(SubmissionBindHandoff handoff, CancellationToken ct = default) =>
        await db.SubmissionBindHandoffs.AddAsync(handoff, ct);

    public Task UpdateAsync(SubmissionBindHandoff handoff, CancellationToken ct = default) =>
        Task.CompletedTask;
}
