using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class ServiceCaseRepository(AppDbContext db) : IServiceCaseRepository
{
    public async Task AddAsync(ServiceCase serviceCase, CancellationToken ct = default)
    {
        await db.ServiceCases.AddAsync(serviceCase, ct);
    }

    public Task<ServiceCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return db.ServiceCases
            .Include(c => c.Account)
            .Include(c => c.Policy)
            .Include(c => c.ClaimReference)
            .Include(c => c.CommunicationLinks)
            .Include(c => c.TaskLinks)
            .Include(c => c.Transitions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<PaginatedResult<ServiceCase>> ListAsync(ServiceCaseListQuery query, CancellationToken ct = default)
    {
        var q = db.ServiceCases
            .Include(c => c.Account)
            .Include(c => c.Policy)
            .Include(c => c.ClaimReference)
            .Include(c => c.CommunicationLinks)
            .Include(c => c.TaskLinks)
            .Include(c => c.Transitions)
            .AsQueryable();

        if (query.AccountId.HasValue)
            q = q.Where(c => c.AccountId == query.AccountId.Value);
        if (query.PolicyId.HasValue)
            q = q.Where(c => c.PolicyId == query.PolicyId.Value);
        if (query.OwnerUserId.HasValue)
            q = q.Where(c => c.OwnerUserId == query.OwnerUserId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(c => c.Status == query.Status);
        else if (!query.IncludeClosed)
            q = q.Where(c => c.Status != "Closed");
        if (!string.IsNullOrWhiteSpace(query.Priority))
            q = q.Where(c => c.Priority == query.Priority);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            q = q.Where(c =>
                EF.Functions.ILike(c.CaseNumber, search) ||
                EF.Functions.ILike(c.Summary, search) ||
                (c.Description != null && EF.Functions.ILike(c.Description, search)) ||
                EF.Functions.ILike(c.Account.Name, search) ||
                (c.Policy != null && EF.Functions.ILike(c.Policy.PolicyNumber, search)) ||
                (c.ClaimReference != null && c.ClaimReference.CarrierClaimNumber != null && EF.Functions.ILike(c.ClaimReference.CarrierClaimNumber, search)));
        }
        if (query.DueAfter.HasValue)
            q = q.Where(c => c.DueDate.HasValue && c.DueDate.Value >= query.DueAfter.Value);
        if (query.DueBefore.HasValue)
            q = q.Where(c => c.DueDate.HasValue && c.DueDate.Value <= query.DueBefore.Value);

        var totalCount = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100));
        var data = await q
            .OrderBy(c => c.DueDate.HasValue ? 0 : 1)
            .ThenBy(c => c.DueDate)
            .ThenBy(c => c.Priority == "Urgent" ? 0 : c.Priority == "High" ? 1 : c.Priority == "Medium" ? 2 : 3)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<ServiceCase>(data, page, pageSize, totalCount);
    }

    public Task<bool> AccountExistsAsync(Guid accountId, CancellationToken ct = default) =>
        db.Accounts.AnyAsync(a => a.Id == accountId, ct);

    public Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken ct = default) =>
        db.Policies.AnyAsync(p => p.Id == policyId, ct);

    public Task<bool> PolicyBelongsToAccountAsync(Guid policyId, Guid accountId, CancellationToken ct = default) =>
        db.Policies.AnyAsync(p => p.Id == policyId && p.AccountId == accountId, ct);

    public Task<bool> CommunicationExistsAsync(Guid communicationEventId, CancellationToken ct = default) =>
        db.CommunicationEvents.AnyAsync(c => c.Id == communicationEventId, ct);

    public async Task<string> NextCaseNumberAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SC-{year}-";
        var count = await db.ServiceCases.CountAsync(c => c.CaseNumber.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:000000}";
    }
}
