using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IQueueAssignmentRepository
{
    Task<(IReadOnlyList<WorkQueue> Queues, int TotalCount)> ListQueuesAsync(string? workType, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<WorkQueue?> GetQueueAsync(Guid queueId, CancellationToken ct = default);
    Task<WorkQueue?> GetQueueForUpdateAsync(Guid queueId, CancellationToken ct = default);
    Task<WorkQueue?> GetFallbackQueueAsync(string sourceType, CancellationToken ct = default);
    Task<bool> QueueNameExistsAsync(string name, string workType, Guid? excludingQueueId, CancellationToken ct = default);
    Task<bool> HasOpenItemsAsync(Guid queueId, CancellationToken ct = default);
    Task AddQueueAsync(WorkQueue queue, CancellationToken ct = default);
    Task<IReadOnlyList<AssignmentRule>> ListRulesAsync(Guid? queueId, string? status, CancellationToken ct = default);
    Task<IReadOnlyList<AssignmentRule>> ListActiveRulesAsync(string sourceType, CancellationToken ct = default);
    Task<AssignmentRule?> GetRuleForUpdateAsync(Guid ruleId, CancellationToken ct = default);
    Task AddRuleAsync(AssignmentRule rule, CancellationToken ct = default);
    Task<IReadOnlyList<CoverageWindow>> ListCoverageWindowsAsync(Guid? queueId, Guid? coveredUserId, string? status, CancellationToken ct = default);
    Task<IReadOnlyList<CoverageWindow>> ListActiveCoverageAsync(Guid coveredUserId, string sourceType, DateTime now, CancellationToken ct = default);
    Task<CoverageWindow?> GetCoverageForUpdateAsync(Guid coverageWindowId, CancellationToken ct = default);
    Task<bool> CoverageOverlapsAsync(Guid coveredUserId, Guid backupUserId, Guid? queueId, DateTime startsAt, DateTime endsAt, Guid? excludingId, CancellationToken ct = default);
    Task AddCoverageWindowAsync(CoverageWindow window, CancellationToken ct = default);
    Task<QueueWorkItem?> GetActiveQueueItemAsync(string sourceType, Guid sourceId, CancellationToken ct = default);
    Task<QueueWorkItem?> GetQueueItemForUpdateAsync(Guid queueItemId, CancellationToken ct = default);
    Task AddQueueItemAsync(QueueWorkItem item, CancellationToken ct = default);
    Task<(IReadOnlyList<QueueWorkItem> Items, int TotalCount)> ListQueueItemsAsync(Guid queueId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task AddRoutingDecisionAsync(RoutingDecisionLog decision, CancellationToken ct = default);
    Task<(IReadOnlyList<RoutingDecisionLog> Events, int TotalCount)> ListRoutingEventsAsync(string? sourceType, Guid? sourceId, Guid? queueItemId, int page, int pageSize, CancellationToken ct = default);
}
