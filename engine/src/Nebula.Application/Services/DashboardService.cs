using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public class DashboardService(IDashboardRepository dashboardRepo, BrokerScopeResolver scopeResolver, ILogger<DashboardService> logger)
{
    private readonly ILogger<DashboardService> _logger = logger;

    public Task<DashboardKpisDto> GetKpisAsync(ICurrentUserService user, int periodDays = 90, CancellationToken ct = default) =>
        dashboardRepo.GetKpisAsync(user, periodDays, ct);

    public Task<DashboardOpportunitiesDto> GetOpportunitiesAsync(ICurrentUserService user, int periodDays = 180, CancellationToken ct = default) =>
        dashboardRepo.GetOpportunitiesAsync(user, periodDays, ct);

    public Task<OpportunityFlowDto> GetOpportunityFlowAsync(ICurrentUserService user, string entityType, int periodDays = 180, CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityFlowAsync(user, entityType, periodDays, ct);

    public Task<OpportunityItemsDto> GetOpportunityItemsAsync(ICurrentUserService user, string entityType, string status, CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityItemsAsync(user, entityType, status, ct);

    public Task<OpportunityBreakdownDto> GetOpportunityBreakdownAsync(
        ICurrentUserService user,
        string entityType,
        string status,
        string groupBy,
        int periodDays = 180,
        CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityBreakdownAsync(user, entityType, status, groupBy, periodDays, ct);

    public Task<OpportunityAgingDto> GetOpportunityAgingAsync(ICurrentUserService user, string entityType, int periodDays = 180, CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityAgingAsync(user, entityType, periodDays, ct);

    public Task<OpportunityHierarchyDto> GetOpportunityHierarchyAsync(ICurrentUserService user, int periodDays = 180, CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityHierarchyAsync(user, periodDays, ct);

    public Task<OpportunityOutcomesDto> GetOpportunityOutcomesAsync(
        ICurrentUserService user,
        int periodDays = 180,
        IReadOnlyCollection<string>? entityTypes = null,
        CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityOutcomesAsync(user, periodDays, entityTypes, ct);

    public Task<OpportunityItemsDto> GetOpportunityOutcomeItemsAsync(
        ICurrentUserService user,
        string outcomeKey,
        int periodDays = 180,
        IReadOnlyCollection<string>? entityTypes = null,
        CancellationToken ct = default) =>
        dashboardRepo.GetOpportunityOutcomeItemsAsync(user, outcomeKey, periodDays, entityTypes, ct);

    public async Task<NudgesResponseDto> GetNudgesAsync(Guid userId, ICurrentUserService user, CancellationToken ct = default)
    {
        var nudges = await dashboardRepo.GetNudgesAsync(userId, ct);
        AuditBrokerUserRead(user, "dashboard.nudges", null);
        return new NudgesResponseDto(nudges);
    }

    /// <summary>
    /// BrokerUser variant: returns only OverdueTask nudges for tasks linked to their broker scope (F0009 §14).
    /// Empty result returned if no overdue tasks; 403 thrown only if scope cannot be resolved.
    /// </summary>
    public async Task<NudgesResponseDto> GetNudgesForBrokerUserAsync(ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        var nudges = await dashboardRepo.GetNudgesForBrokerUserAsync([resolvedBrokerId], ct);
        AuditBrokerUserRead(user, "dashboard.nudges", null, resolvedBrokerId);
        return new NudgesResponseDto(nudges);
    }

    private void AuditBrokerUserRead(ICurrentUserService user, string resource, Guid? entityId, Guid? resolvedBrokerId = null)
    {
        if (!user.Roles.Contains("BrokerUser")) return;
        _logger.LogInformation(
            "BrokerUser access: {Resource} by BrokerTenantId={BrokerTenantId} ResolvedBrokerId={ResolvedBrokerId} EntityId={EntityId} OccurredAt={OccurredAt}",
            resource,
            user.BrokerTenantId,
            resolvedBrokerId,
            entityId,
            DateTime.UtcNow);
    }
}
