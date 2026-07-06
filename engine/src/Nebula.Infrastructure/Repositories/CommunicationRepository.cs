using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class CommunicationRepository(AppDbContext db) : ICommunicationRepository
{
    public Task AddAsync(CommunicationEvent communication, CancellationToken ct = default)
    {
        db.CommunicationEvents.Add(communication);
        return Task.CompletedTask;
    }

    public Task AddCorrectionAsync(CommunicationCorrection correction, CancellationToken ct = default)
    {
        db.CommunicationCorrections.Add(correction);
        return Task.CompletedTask;
    }

    public Task<CommunicationEvent?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return db.CommunicationEvents
            .Include(e => e.Links)
            .Include(e => e.Participants)
            .Include(e => e.Corrections)
            .Include(e => e.FollowUpTaskLinks)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<PaginatedResult<CommunicationEvent>> ListByEntityAsync(
        string entityType, Guid entityId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.CommunicationEvents
            .Include(e => e.Links)
            .Include(e => e.Participants)
            .Include(e => e.FollowUpTaskLinks)
            .Where(e => e.Links.Any(l => l.EntityType == entityType && l.EntityId == entityId));

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<CommunicationEvent>(data, page, pageSize, totalCount);
    }

    public Task<bool> LinkedEntityExistsAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        return entityType switch
        {
            "Broker" => db.Brokers.AnyAsync(e => e.Id == entityId, ct),
            "Account" => db.Accounts.AnyAsync(e => e.Id == entityId, ct),
            "Submission" => db.Submissions.AnyAsync(e => e.Id == entityId, ct),
            "Policy" => db.Policies.AnyAsync(e => e.Id == entityId, ct),
            "Renewal" => db.Renewals.AnyAsync(e => e.Id == entityId, ct),
            "Task" => db.Tasks.AnyAsync(e => e.Id == entityId, ct),
            _ => Task.FromResult(false),
        };
    }

    public async Task<string?> ResolveLinkedEntityNameAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        return entityType switch
        {
            "Broker" => await db.Brokers.Where(e => e.Id == entityId).Select(e => e.LegalName).FirstOrDefaultAsync(ct),
            "Account" => await db.Accounts.Where(e => e.Id == entityId).Select(e => e.Name).FirstOrDefaultAsync(ct),
            "Submission" => await db.Submissions.Where(e => e.Id == entityId).Select(e => e.Account.Name).FirstOrDefaultAsync(ct),
            "Policy" => await db.Policies.Where(e => e.Id == entityId).Select(e => e.PolicyNumber).FirstOrDefaultAsync(ct),
            "Renewal" => await db.Renewals.Where(e => e.Id == entityId).Select(e => e.Policy.PolicyNumber).FirstOrDefaultAsync(ct),
            "Task" => await db.Tasks.Where(e => e.Id == entityId).Select(e => e.Title).FirstOrDefaultAsync(ct),
            _ => null,
        };
    }
}
