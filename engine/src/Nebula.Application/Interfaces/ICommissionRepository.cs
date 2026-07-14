using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ICommissionRepository
{
    Task<PaginatedResult<ExpectedCommission>> SearchExpectedCommissionsAsync(CommissionSearchQuery query, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<ExpectedCommission?> GetExpectedCommissionAsync(Guid expectedCommissionId, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<ExpectedCommission?> GetExpectedCommissionForMutationAsync(Guid expectedCommissionId, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, CommissionDisplayContextDto>> GetDisplayContextsAsync(IReadOnlyCollection<Guid> policyIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetProducerDisplayNamesAsync(IReadOnlyCollection<Guid> producerIds, CancellationToken ct = default);
    Task<IReadOnlyList<CommissionSchedule>> ListSchedulesAsync(Guid? carrierMarketId, CancellationToken ct = default);
    Task<CommissionSchedule?> GetScheduleAsync(Guid scheduleId, CancellationToken ct = default);
    Task<bool> ScheduleOverlapsAsync(CommissionSchedule schedule, Guid? excludingId, CancellationToken ct = default);
    Task AddScheduleAsync(CommissionSchedule schedule, CancellationToken ct = default);
    Task<IReadOnlyList<ProducerSplitAssignment>> ListPolicySplitsAsync(Guid policyId, CancellationToken ct = default);
    Task<bool> PolicyIsVisibleAsync(Guid policyId, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<ProducerSplitAssignment?> GetActiveSplitAsync(Guid policyId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken ct = default);
    Task<bool> SplitOverlapsAsync(Guid policyId, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid? excludingId, CancellationToken ct = default);
    Task AddSplitAsync(ProducerSplitAssignment assignment, CancellationToken ct = default);
    Task AddExpectedCommissionAsync(ExpectedCommission expectedCommission, CancellationToken ct = default);
    Task<CommissionAdjustment?> GetAdjustmentAsync(Guid adjustmentId, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<IReadOnlyList<CommissionAdjustment>> ListAdjustmentsAsync(Guid expectedCommissionId, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task AddAdjustmentAsync(CommissionAdjustment adjustment, CancellationToken ct = default);
    Task UpsertProjectionAsync(RevenueAttributionProjection projection, CancellationToken ct = default);
}
