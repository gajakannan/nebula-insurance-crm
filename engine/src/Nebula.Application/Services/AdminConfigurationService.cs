using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class AdminConfigurationService(
    IAdminConfigurationRepository repository,
    IEnumerable<IAdminConfigurationDomainAdapter> adapters,
    IAdminConfigurationRefreshNotifier refreshNotifier)
{
    private readonly Dictionary<string, IAdminConfigurationDomainAdapter> _adapters =
        adapters.ToDictionary(adapter => adapter.DomainKey, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<AdminConfigurationDomainDto>> ListDomainsAsync(ICurrentUserService user, CancellationToken ct)
    {
        var domains = await repository.ListDomainsAsync(ct);
        var results = new List<AdminConfigurationDomainDto>();
        foreach (var domain in domains)
        {
            var current = await repository.GetCurrentPublishedSetAsync(domain.DomainKey, ct);
            var draft = await repository.GetActiveDraftAsync(domain.DomainKey, ct);
            var validation = draft is null ? null : await repository.GetLatestValidationAsync(draft.Id, ct);
            results.Add(MapDomain(domain, current, draft, validation));
        }
        return results;
    }

    public async Task<(AdminConfigurationDomainDetailDto? Result, string? Error)> GetDomainAsync(string domainKey, ICurrentUserService user, CancellationToken ct)
    {
        var domain = await repository.GetDomainAsync(domainKey, ct);
        if (domain is null)
            return (null, "domain_not_found");

        var current = await repository.GetCurrentPublishedSetAsync(domain.DomainKey, ct);
        var draft = await repository.GetActiveDraftAsync(domain.DomainKey, ct);
        var validation = draft is null ? null : await repository.GetLatestValidationAsync(draft.Id, ct);
        var refreshStatuses = current is null ? [] : await repository.GetRefreshStatusesAsync(current.Id, ct);
        var publishedSets = await repository.ListPublishedSetsAsync(domain.DomainKey, ct);
        return (new AdminConfigurationDomainDetailDto(
            MapDomain(domain, current, draft, validation),
            draft is null ? null : MapDraft(draft, validation),
            current is null ? null : MapPublishedSet(current, refreshStatuses),
            refreshStatuses.Select(MapRefreshStatus).ToList(),
            publishedSets.Select(set => MapPublishedSet(set, set.Id == current?.Id ? refreshStatuses : [])).ToList()), null);
    }

    public async Task<(AdminConfigurationDraftDto? Result, string? Error)> CreateDraftAsync(string domainKey, AdminConfigurationDraftCreateRequestDto request, ICurrentUserService user, CancellationToken ct)
    {
        var domain = await repository.GetDomainAsync(domainKey, ct);
        if (domain is null)
            return (null, "domain_not_found");

        if (!_adapters.TryGetValue(domain.DomainKey, out var adapter))
            return (null, "domain_unsupported");

        var activeDraft = await repository.GetActiveDraftAsync(domain.DomainKey, ct);
        if (activeDraft is not null)
            return (MapDraft(activeDraft, await repository.GetLatestValidationAsync(activeDraft.Id, ct)), "active_draft_exists");

        var current = await repository.GetCurrentPublishedSetAsync(domain.DomainKey, ct);
        var payloadJson = current?.PayloadSnapshotJson ?? await adapter.BuildCurrentPayloadJsonAsync(ct);
        var now = DateTime.UtcNow;
        var draft = new ConfigurationDraft
        {
            DomainKey = domain.DomainKey,
            BasePublishedVersion = current?.PublishedVersion ?? 0,
            DraftVersion = (current?.PublishedVersion ?? 0) + 1,
            Status = "Draft",
            PayloadJson = NormalizeJson(payloadJson),
            PayloadHash = Hash(NormalizeJson(payloadJson)),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        await repository.AddDraftAsync(draft, ct);
        await AddAuditAsync(domain.DomainKey, draft.Id, null, "DraftCreated", "Succeeded", user.UserId, AuditSummary(new
        {
            reason = request.Reason,
            draftVersion = draft.DraftVersion,
            basePublishedVersion = draft.BasePublishedVersion
        }), ct);
        await repository.SaveChangesAsync(ct);
        return (MapDraft(draft, null), null);
    }

    public async Task<(AdminConfigurationDraftDto? Result, string? Error)> UpdateDraftAsync(Guid draftId, AdminConfigurationDraftUpdateRequestDto request, uint rowVersion, ICurrentUserService user, CancellationToken ct)
    {
        var draft = await repository.GetDraftAsync(draftId, ct);
        if (draft is null)
            return (null, "draft_not_found");
        if (draft.RowVersion != rowVersion)
            return (null, "concurrency_conflict");
        if (draft.Status is "Published" or "Superseded")
            return (null, "draft_not_editable");

        var payloadJson = NormalizeJson(request.Payload.GetRawText());
        draft.PayloadJson = payloadJson;
        draft.PayloadHash = Hash(payloadJson);
        draft.Status = "Draft";
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedByUserId = user.UserId;
        await AddAuditAsync(draft.DomainKey, draft.Id, null, "DraftUpdated", "Succeeded", user.UserId, AuditSummary(new
        {
            reason = request.Reason,
            draftVersion = draft.DraftVersion,
            payloadHash = draft.PayloadHash
        }), ct);
        await repository.SaveChangesAsync(ct);
        return (MapDraft(draft, await repository.GetLatestValidationAsync(draft.Id, ct)), null);
    }

    public async Task<(AdminConfigurationValidationResultDto? Result, string? Error)> ValidateDraftAsync(Guid draftId, ICurrentUserService user, CancellationToken ct)
    {
        var draft = await repository.GetDraftAsync(draftId, ct);
        if (draft is null)
            return (null, "draft_not_found");
        if (!_adapters.TryGetValue(draft.DomainKey, out var adapter))
            return (null, "domain_unsupported");

        var current = await repository.GetCurrentPublishedSetAsync(draft.DomainKey, ct);
        var errors = await adapter.ValidatePayloadAsync(draft.PayloadJson, ct);
        var compare = await adapter.CompareAsync(current?.PayloadSnapshotJson ?? "{}", draft.PayloadJson, ct);
        var result = new ConfigurationValidationResult
        {
            DraftId = draft.Id,
            Status = errors.Count == 0 ? "Passed" : "Failed",
            DraftPayloadHash = draft.PayloadHash,
            BlockingErrorsJson = JsonSerializer.Serialize(errors),
            WarningsJson = "[]",
            CompareSummaryJson = JsonSerializer.Serialize(compare),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        draft.Status = result.Status == "Passed" ? "ValidationPassed" : "ValidationFailed";
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedByUserId = user.UserId;
        await repository.AddValidationResultAsync(result, ct);
        await AddAuditAsync(draft.DomainKey, draft.Id, null, result.Status == "Passed" ? "ValidationPassed" : "ValidationFailed", result.Status, user.UserId, AuditSummary(new
        {
            draftVersion = draft.DraftVersion,
            blockingErrorCount = errors.Count,
            warningCount = 0,
            compareChangeCount = compare.Count
        }), ct);
        await repository.SaveChangesAsync(ct);
        return (MapValidationResult(result), null);
    }

    public async Task<(AdminConfigurationValidationResultDto? Result, string? Error)> CompareDraftAsync(Guid draftId, ICurrentUserService user, CancellationToken ct)
    {
        var draft = await repository.GetDraftAsync(draftId, ct);
        if (draft is null)
            return (null, "draft_not_found");
        if (!_adapters.TryGetValue(draft.DomainKey, out var adapter))
            return (null, "domain_unsupported");

        var current = await repository.GetCurrentPublishedSetAsync(draft.DomainKey, ct);
        var compare = await adapter.CompareAsync(current?.PayloadSnapshotJson ?? "{}", draft.PayloadJson, ct);
        return (new AdminConfigurationValidationResultDto(Guid.Empty, draft.Id, "Compared", draft.PayloadHash, [], [], compare), null);
    }

    public async Task<(AdminConfigurationPublishedSetDto? Result, string? Error)> PublishDraftAsync(Guid draftId, AdminConfigurationPublishRequestDto request, ICurrentUserService user, CancellationToken ct)
    {
        var draft = await repository.GetDraftAsync(draftId, ct);
        if (draft is null)
            return (null, "draft_not_found");
        var latestValidation = await repository.GetLatestValidationAsync(draft.Id, ct);
        if (latestValidation is null || latestValidation.Status != "Passed" || latestValidation.DraftPayloadHash != draft.PayloadHash)
            return (null, "validation_required");
        var current = await repository.GetCurrentPublishedSetAsync(draft.DomainKey, ct);
        if ((current?.PublishedVersion ?? 0) != draft.BasePublishedVersion)
            return (null, "stale_published_version");

        var published = await CreatePublishedSetAsync(draft.DomainKey, draft.PayloadJson, draft.PayloadHash, request.Reason, user.UserId, ct);
        draft.Status = "Published";
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedByUserId = user.UserId;
        await AddAuditAsync(draft.DomainKey, draft.Id, published.Id, "Published", "Succeeded", user.UserId, AuditSummary(new
        {
            reason = request.Reason,
            draftVersion = draft.DraftVersion,
            publishedVersion = published.PublishedVersion,
            priorPublishedVersion = current?.PublishedVersion
        }), ct);
        await repository.SaveChangesAsync(ct);
        return (MapPublishedSet(published, await repository.GetRefreshStatusesAsync(published.Id, ct)), null);
    }

    public async Task<(AdminConfigurationPublishedSetDto? Result, string? Error)> RollbackAsync(string domainKey, AdminConfigurationPublishRequestDto request, ICurrentUserService user, CancellationToken ct)
    {
        var domain = await repository.GetDomainAsync(domainKey, ct);
        if (domain is null)
            return (null, "domain_not_found");
        if (!domain.SupportsRollback)
            return (null, "rollback_not_supported");
        if (request.TargetPublishedVersion is null)
            return (null, "rollback_target_required");
        var target = await repository.GetPublishedSetAsync(domain.DomainKey, request.TargetPublishedVersion.Value, ct);
        if (target is null)
            return (null, "published_set_not_found");

        var published = await CreatePublishedSetAsync(domain.DomainKey, target.PayloadSnapshotJson, target.PayloadHash, request.Reason, user.UserId, ct);
        await AddAuditAsync(domain.DomainKey, null, published.Id, "RollbackPublished", "Succeeded", user.UserId, AuditSummary(new
        {
            reason = request.Reason,
            targetPublishedVersion = target.PublishedVersion,
            publishedVersion = published.PublishedVersion
        }), ct);
        await repository.SaveChangesAsync(ct);
        return (MapPublishedSet(published, await repository.GetRefreshStatusesAsync(published.Id, ct)), null);
    }

    public Task<PaginatedResult<ConfigurationAuditEvent>> ListAuditEventsAsync(AdminConfigurationAuditQuery query, ICurrentUserService user, CancellationToken ct) =>
        repository.ListAuditEventsAsync(query with { Page = Math.Max(query.Page, 1), PageSize = Math.Clamp(query.PageSize, 1, 100) }, ct);

    private async Task<PublishedOperationalConfigurationSet> CreatePublishedSetAsync(string domainKey, string payloadJson, string payloadHash, string reason, Guid userId, CancellationToken ct)
    {
        var published = new PublishedOperationalConfigurationSet
        {
            DomainKey = domainKey,
            PublishedVersion = await repository.GetNextPublishedVersionAsync(domainKey, ct),
            PayloadSnapshotJson = payloadJson,
            PayloadHash = payloadHash,
            PublishedByUserId = userId,
            PublishReason = reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };
        await repository.AddPublishedSetAsync(published, ct);
        var consumers = await refreshNotifier.NotifyAsync(domainKey, published.PublishedVersion, ct);
        await repository.AddRefreshStatusesAsync(consumers.Select(consumer => new ConfigurationRefreshStatus
        {
            PublishedSetId = published.Id,
            ConsumerKey = consumer,
            Status = "Refreshed",
            RefreshedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        }), ct);
        return published;
    }

    private async Task AddAuditAsync(string domainKey, Guid? draftId, Guid? publishedSetId, string action, string outcome, Guid actorUserId, string summaryJson, CancellationToken ct) =>
        await repository.AddAuditEventAsync(new ConfigurationAuditEvent
        {
            DomainKey = domainKey,
            DraftId = draftId,
            PublishedSetId = publishedSetId,
            Action = action,
            Outcome = outcome,
            ActorUserId = actorUserId,
            SummaryJson = summaryJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
        }, ct);

    private static string AuditSummary(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static AdminConfigurationDomainDto MapDomain(ConfigurationDomain domain, PublishedOperationalConfigurationSet? current, ConfigurationDraft? draft, ConfigurationValidationResult? validation) =>
        new(domain.DomainKey, domain.DisplayName, domain.OwningModule, domain.Status, domain.EditableSchemaRef, domain.SupportsRollback, current?.PublishedVersion, draft?.Status, validation?.Status, current?.PublishedByUserId.ToString(), current?.CreatedAt);

    private static AdminConfigurationDraftDto MapDraft(ConfigurationDraft draft, ConfigurationValidationResult? validation) =>
        new(draft.Id, draft.DomainKey, draft.BasePublishedVersion, draft.DraftVersion, draft.Status, ParseJson(draft.PayloadJson), draft.PayloadHash, draft.RowVersion.ToString(), validation is null ? null : MapValidationResult(validation));

    private static AdminConfigurationValidationResultDto MapValidationResult(ConfigurationValidationResult result) =>
        new(result.Id, result.DraftId, result.Status, result.DraftPayloadHash, DeserializeList<AdminConfigurationValidationIssueDto>(result.BlockingErrorsJson), DeserializeList<AdminConfigurationValidationIssueDto>(result.WarningsJson), DeserializeList<AdminConfigurationChangeSummaryDto>(result.CompareSummaryJson));

    private static AdminConfigurationPublishedSetDto MapPublishedSet(PublishedOperationalConfigurationSet set, IReadOnlyList<ConfigurationRefreshStatus> refreshStatuses) =>
        new(set.Id, set.DomainKey, set.PublishedVersion, ParseJson(set.PayloadSnapshotJson), set.PayloadHash, set.PublishedByUserId, set.CreatedAt, set.PublishReason, refreshStatuses.Select(MapRefreshStatus).ToList());

    private static AdminConfigurationRefreshStatusDto MapRefreshStatus(ConfigurationRefreshStatus status) =>
        new(status.Id, status.ConsumerKey, status.Status, status.RefreshedAt, status.ErrorSummary);

    public static AdminConfigurationAuditEventDto MapAuditEvent(ConfigurationAuditEvent auditEvent) =>
        new(auditEvent.Id, auditEvent.DomainKey, auditEvent.DraftId, auditEvent.PublishedSetId, auditEvent.Action, auditEvent.Outcome, auditEvent.ActorUserId, auditEvent.CreatedAt, ParseJson(auditEvent.SummaryJson));

    private static JsonElement ParseJson(string json) => JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json).RootElement.Clone();

    private static IReadOnlyList<T> DeserializeList<T>(string json) =>
        JsonSerializer.Deserialize<IReadOnlyList<T>>(string.IsNullOrWhiteSpace(json) ? "[]" : json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

    private static string NormalizeJson(string json) => ParseJson(json).GetRawText();

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
