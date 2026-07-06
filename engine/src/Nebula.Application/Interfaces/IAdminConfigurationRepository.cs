using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IAdminConfigurationRepository
{
    Task<IReadOnlyList<ConfigurationDomain>> ListDomainsAsync(CancellationToken ct);
    Task<ConfigurationDomain?> GetDomainAsync(string domainKey, CancellationToken ct);
    Task<ConfigurationDraft?> GetActiveDraftAsync(string domainKey, CancellationToken ct);
    Task<ConfigurationDraft?> GetDraftAsync(Guid draftId, CancellationToken ct);
    Task<PublishedOperationalConfigurationSet?> GetCurrentPublishedSetAsync(string domainKey, CancellationToken ct);
    Task<PublishedOperationalConfigurationSet?> GetPublishedSetAsync(string domainKey, int publishedVersion, CancellationToken ct);
    Task<IReadOnlyList<PublishedOperationalConfigurationSet>> ListPublishedSetsAsync(string domainKey, CancellationToken ct);
    Task<IReadOnlyList<ConfigurationRefreshStatus>> GetRefreshStatusesAsync(Guid publishedSetId, CancellationToken ct);
    Task<ConfigurationValidationResult?> GetLatestValidationAsync(Guid draftId, CancellationToken ct);
    Task<int> GetNextPublishedVersionAsync(string domainKey, CancellationToken ct);
    Task AddDraftAsync(ConfigurationDraft draft, CancellationToken ct);
    Task AddValidationResultAsync(ConfigurationValidationResult result, CancellationToken ct);
    Task AddPublishedSetAsync(PublishedOperationalConfigurationSet publishedSet, CancellationToken ct);
    Task AddRefreshStatusesAsync(IEnumerable<ConfigurationRefreshStatus> statuses, CancellationToken ct);
    Task AddAuditEventAsync(ConfigurationAuditEvent auditEvent, CancellationToken ct);
    Task<PaginatedResult<ConfigurationAuditEvent>> ListAuditEventsAsync(AdminConfigurationAuditQuery query, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
