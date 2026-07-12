using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class AdminConfigurationRepository(AppDbContext db) : IAdminConfigurationRepository
{
    public async Task<IReadOnlyList<ConfigurationDomain>> ListDomainsAsync(CancellationToken ct) =>
        await db.ConfigurationDomains.OrderBy(domain => domain.DisplayName).ToListAsync(ct);

    public Task<ConfigurationDomain?> GetDomainAsync(string domainKey, CancellationToken ct) =>
        db.ConfigurationDomains.FirstOrDefaultAsync(domain => domain.DomainKey == domainKey, ct);

    public Task<ConfigurationDraft?> GetActiveDraftAsync(string domainKey, CancellationToken ct) =>
        db.ConfigurationDrafts
            .Where(draft => draft.DomainKey == domainKey && draft.Status != "Published" && draft.Status != "Superseded")
            .OrderByDescending(draft => draft.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<ConfigurationDraft?> GetDraftAsync(Guid draftId, CancellationToken ct) =>
        db.ConfigurationDrafts.FirstOrDefaultAsync(draft => draft.Id == draftId, ct);

    public Task<PublishedOperationalConfigurationSet?> GetCurrentPublishedSetAsync(string domainKey, CancellationToken ct) =>
        db.PublishedOperationalConfigurationSets
            .Where(set => set.DomainKey == domainKey)
            .OrderByDescending(set => set.PublishedVersion)
            .FirstOrDefaultAsync(ct);

    public Task<PublishedOperationalConfigurationSet?> GetPublishedSetAsync(string domainKey, int publishedVersion, CancellationToken ct) =>
        db.PublishedOperationalConfigurationSets.FirstOrDefaultAsync(set => set.DomainKey == domainKey && set.PublishedVersion == publishedVersion, ct);

    public async Task<IReadOnlyList<PublishedOperationalConfigurationSet>> ListPublishedSetsAsync(string domainKey, CancellationToken ct) =>
        await db.PublishedOperationalConfigurationSets
            .Where(set => set.DomainKey == domainKey)
            .OrderByDescending(set => set.PublishedVersion)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConfigurationRefreshStatus>> GetRefreshStatusesAsync(Guid publishedSetId, CancellationToken ct) =>
        await db.ConfigurationRefreshStatuses.Where(status => status.PublishedSetId == publishedSetId).OrderBy(status => status.ConsumerKey).ToListAsync(ct);

    public Task<ConfigurationValidationResult?> GetLatestValidationAsync(Guid draftId, CancellationToken ct) =>
        db.ConfigurationValidationResults
            .Where(result => result.DraftId == draftId)
            .OrderByDescending(result => result.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<int> GetNextPublishedVersionAsync(string domainKey, CancellationToken ct)
    {
        var current = await db.PublishedOperationalConfigurationSets
            .Where(set => set.DomainKey == domainKey)
            .MaxAsync(set => (int?)set.PublishedVersion, ct);
        return (current ?? 0) + 1;
    }

    public async Task AddDraftAsync(ConfigurationDraft draft, CancellationToken ct) => await db.ConfigurationDrafts.AddAsync(draft, ct);

    public async Task AddValidationResultAsync(ConfigurationValidationResult result, CancellationToken ct) => await db.ConfigurationValidationResults.AddAsync(result, ct);

    public async Task AddPublishedSetAsync(PublishedOperationalConfigurationSet publishedSet, CancellationToken ct) => await db.PublishedOperationalConfigurationSets.AddAsync(publishedSet, ct);

    public async Task AddRefreshStatusesAsync(IEnumerable<ConfigurationRefreshStatus> statuses, CancellationToken ct) => await db.ConfigurationRefreshStatuses.AddRangeAsync(statuses, ct);

    public async Task AddAuditEventAsync(ConfigurationAuditEvent auditEvent, CancellationToken ct) => await db.ConfigurationAuditEvents.AddAsync(auditEvent, ct);

    public async Task<PaginatedResult<ConfigurationAuditEvent>> ListAuditEventsAsync(AdminConfigurationAuditQuery query, CancellationToken ct)
    {
        var records = db.ConfigurationAuditEvents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.DomainKey))
            records = records.Where(record => record.DomainKey == query.DomainKey);
        if (!string.IsNullOrWhiteSpace(query.Action))
            records = records.Where(record => record.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.Outcome))
            records = records.Where(record => record.Outcome == query.Outcome);
        if (query.ActorUserId is { } actor)
            records = records.Where(record => record.ActorUserId == actor);
        if (query.From is { } from)
            records = records.Where(record => record.CreatedAt >= from);
        if (query.To is { } to)
            records = records.Where(record => record.CreatedAt <= to);

        var total = await records.CountAsync(ct);
        var items = await records
            .OrderByDescending(record => record.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);
        return new PaginatedResult<ConfigurationAuditEvent>(items, total, query.Page, query.PageSize);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
