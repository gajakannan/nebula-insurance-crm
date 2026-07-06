using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Services;

public abstract class AdminConfigurationDomainAdapter(AppDbContext db, string domainKey, string consumerKey) : IAdminConfigurationDomainAdapter
{
    public string DomainKey { get; } = domainKey;
    public string ConsumerKey { get; } = consumerKey;

    public virtual Task<IReadOnlyList<AdminConfigurationValidationIssueDto>> ValidatePayloadAsync(string payloadJson, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return Task.FromResult<IReadOnlyList<AdminConfigurationValidationIssueDto>>([]);
        }
        catch (JsonException ex)
        {
            return Task.FromResult<IReadOnlyList<AdminConfigurationValidationIssueDto>>(
            [
                new("invalid_json", ex.Message, "$")
            ]);
        }
    }

    public virtual Task<IReadOnlyList<AdminConfigurationChangeSummaryDto>> CompareAsync(string beforeJson, string afterJson, CancellationToken ct)
    {
        var changeType = Normalize(beforeJson) == Normalize(afterJson) ? "Unchanged" : "Modified";
        return Task.FromResult<IReadOnlyList<AdminConfigurationChangeSummaryDto>>(
        [
            new("$", changeType, changeType == "Unchanged" ? null : "published", changeType == "Unchanged" ? null : "draft")
        ]);
    }

    protected AppDbContext Db { get; } = db;

    protected static string Serialize(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static string Normalize(string json)
    {
        try { return JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json).RootElement.GetRawText(); }
        catch (JsonException) { return json; }
    }

    public abstract Task<string> BuildCurrentPayloadJsonAsync(CancellationToken ct);
}

public sealed class QueueRoutingConfigurationAdapter(AppDbContext db) : AdminConfigurationDomainAdapter(db, "queue-routing", "routing")
{
    public override async Task<string> BuildCurrentPayloadJsonAsync(CancellationToken ct)
    {
        var queues = await Db.WorkQueues.OrderBy(queue => queue.Name).Select(queue => new { queue.Id, queue.Name, queue.WorkType, queue.Status }).ToListAsync(ct);
        var rules = await Db.AssignmentRules.OrderBy(rule => rule.Precedence).Select(rule => new { rule.Id, rule.WorkQueueId, rule.Precedence, rule.Status, rule.RuleType }).ToListAsync(ct);
        var coverage = await Db.CoverageWindows.OrderBy(window => window.StartsAt).Select(window => new { window.Id, window.WorkQueueId, window.CoveredUserId, window.StartsAt, window.EndsAt, window.Status }).ToListAsync(ct);
        return Serialize(new { queues, rules, coverage });
    }
}

public sealed class WorkflowSlaConfigurationAdapter(AppDbContext db) : AdminConfigurationDomainAdapter(db, "workflow-sla-thresholds", "workflow-sla")
{
    public override async Task<string> BuildCurrentPayloadJsonAsync(CancellationToken ct)
    {
        var thresholds = await Db.WorkflowSlaThresholds
            .OrderBy(threshold => threshold.EntityType)
            .ThenBy(threshold => threshold.Status)
            .ThenBy(threshold => threshold.LineOfBusiness)
            .Select(threshold => new { threshold.Id, threshold.EntityType, threshold.Status, threshold.LineOfBusiness, threshold.WarningDays, threshold.TargetDays })
            .ToListAsync(ct);
        return Serialize(new { thresholds });
    }
}

public sealed class SearchReportDefaultsConfigurationAdapter(AppDbContext db) : AdminConfigurationDomainAdapter(db, "search-report-defaults", "search-reporting")
{
    public override async Task<string> BuildCurrentPayloadJsonAsync(CancellationToken ct)
    {
        var savedViews = await Db.SavedViews.OrderBy(view => view.Name).Select(view => new { view.Id, view.Name, view.ViewType, view.Visibility, view.TeamScopeType, view.TeamScopeKey, view.IsDefault }).ToListAsync(ct);
        return Serialize(new { savedViews, operationalReports = Array.Empty<object>() });
    }
}

public sealed class TemplateMetadataConfigurationAdapter(AppDbContext db) : AdminConfigurationDomainAdapter(db, "template-metadata", "documents")
{
    public override Task<string> BuildCurrentPayloadJsonAsync(CancellationToken ct) =>
        Task.FromResult(Serialize(new { templates = Array.Empty<object>(), note = "Template upload and rendering remain owned by F0027." }));
}

public sealed class InProcessAdminConfigurationRefreshNotifier(IEnumerable<IAdminConfigurationDomainAdapter> adapters) : IAdminConfigurationRefreshNotifier
{
    public Task<IReadOnlyList<string>> NotifyAsync(string domainKey, int publishedVersion, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(adapters.Where(adapter => adapter.DomainKey == domainKey).Select(adapter => adapter.ConsumerKey).DefaultIfEmpty(domainKey).ToList());
}
