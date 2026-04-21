using Nebula.Application.DTOs;
using Nebula.Application.Common;

namespace Nebula.Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardKpisDto> GetKpisAsync(ICurrentUserService user, int periodDays = 90, CancellationToken ct = default);
    Task<DashboardOpportunitiesDto> GetOpportunitiesAsync(ICurrentUserService user, int periodDays = 180, CancellationToken ct = default);
    Task<OpportunityFlowDto> GetOpportunityFlowAsync(ICurrentUserService user, string entityType, int periodDays, CancellationToken ct = default);
    Task<OpportunityItemsDto> GetOpportunityItemsAsync(ICurrentUserService user, string entityType, string status, CancellationToken ct = default);
    Task<OpportunityBreakdownDto> GetOpportunityBreakdownAsync(ICurrentUserService user, string entityType, string status, string groupBy, int periodDays, CancellationToken ct = default);
    Task<OpportunityAgingDto> GetOpportunityAgingAsync(ICurrentUserService user, string entityType, int periodDays, CancellationToken ct = default);
    Task<OpportunityHierarchyDto> GetOpportunityHierarchyAsync(ICurrentUserService user, int periodDays, CancellationToken ct = default);
    Task<OpportunityOutcomesDto> GetOpportunityOutcomesAsync(ICurrentUserService user, int periodDays, IReadOnlyCollection<string>? entityTypes = null, CancellationToken ct = default);
    Task<OpportunityItemsDto> GetOpportunityOutcomeItemsAsync(ICurrentUserService user, string outcomeKey, int periodDays, IReadOnlyCollection<string>? entityTypes = null, CancellationToken ct = default);
    Task<IReadOnlyList<NudgeCardDto>> GetNudgesAsync(Guid userId, CancellationToken ct = default);
    /// <summary>
    /// BrokerUser variant: returns only OverdueTask nudges linked to the specified broker IDs (F0009 §14).
    /// StaleSubmission and UpcomingRenewal types are excluded entirely.
    /// </summary>
    Task<IReadOnlyList<NudgeCardDto>> GetNudgesForBrokerUserAsync(IReadOnlyList<Guid> brokerIds, CancellationToken ct = default);
}
