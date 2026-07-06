using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ITerritoryAssignmentRepository
{
    /// <summary>The current open (EffectiveTo == null) assignment for a member, tracked for update.</summary>
    Task<TerritoryAssignment?> GetOpenForMemberAsync(string memberType, Guid memberId, CancellationToken ct = default);

    /// <summary>Assignments for a territory covering <paramref name="asOf"/>, paginated.</summary>
    Task<(IReadOnlyList<TerritoryAssignment> Data, int TotalCount)> ListMembersAsOfAsync(
        Guid territoryId, DateOnly asOf, int page, int pageSize, CancellationToken ct = default);

    /// <summary>The assignment covering <paramref name="asOf"/> for a member, with Territory included.</summary>
    Task<TerritoryAssignment?> GetForMemberAsOfAsync(string memberType, Guid memberId, DateOnly asOf, CancellationToken ct = default);

    Task AddAsync(TerritoryAssignment assignment, CancellationToken ct = default);
}
