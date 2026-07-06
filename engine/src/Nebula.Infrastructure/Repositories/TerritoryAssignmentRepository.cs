using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class TerritoryAssignmentRepository(AppDbContext db) : ITerritoryAssignmentRepository
{
    public async Task<TerritoryAssignment?> GetOpenForMemberAsync(string memberType, Guid memberId, CancellationToken ct = default) =>
        await db.TerritoryAssignments.FirstOrDefaultAsync(
            a => a.MemberType == memberType && a.MemberId == memberId && a.EffectiveTo == null, ct);

    public async Task<(IReadOnlyList<TerritoryAssignment> Data, int TotalCount)> ListMembersAsOfAsync(
        Guid territoryId, DateOnly asOf, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.TerritoryAssignments
            .Where(a => a.TerritoryId == territoryId
                        && a.EffectiveFrom <= asOf
                        && (a.EffectiveTo == null || a.EffectiveTo > asOf));

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(a => a.MemberType).ThenBy(a => a.EffectiveFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (data, total);
    }

    public async Task<TerritoryAssignment?> GetForMemberAsOfAsync(string memberType, Guid memberId, DateOnly asOf, CancellationToken ct = default) =>
        await db.TerritoryAssignments
            .Include(a => a.Territory)
            .Where(a => a.MemberType == memberType && a.MemberId == memberId
                        && a.EffectiveFrom <= asOf
                        && (a.EffectiveTo == null || a.EffectiveTo > asOf))
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(TerritoryAssignment assignment, CancellationToken ct = default)
    {
        db.TerritoryAssignments.Add(assignment);
        return Task.CompletedTask;
    }
}
