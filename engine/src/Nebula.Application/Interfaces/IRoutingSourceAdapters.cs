namespace Nebula.Application.Interfaces;

public interface IRoutingSource
{
    Task<RoutingSourceSummary?> GetSummaryAsync(Guid sourceId, CancellationToken ct = default);
    Task<bool> AssignAsync(Guid sourceId, Guid assignedToUserId, Guid actorUserId, DateTime now, CancellationToken ct = default);
}

public interface ITaskRoutingSource : IRoutingSource;
public interface ISubmissionRoutingSource : IRoutingSource;
public interface IRenewalRoutingSource : IRoutingSource;

public sealed record RoutingSourceSummary(
    string SourceType,
    Guid SourceId,
    Guid? CurrentAssigneeId,
    Guid? BrokerId,
    Guid? AccountId,
    Guid? ProgramId,
    string? Region,
    string? LineOfBusiness,
    DateTime? DueOrEffectiveDate);

public interface IDistributionOwnershipResolver
{
    Task<Guid?> ResolveOwnerAsync(RoutingSourceSummary source, CancellationToken ct = default);
}
