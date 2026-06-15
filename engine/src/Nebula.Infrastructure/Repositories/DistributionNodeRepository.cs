using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class DistributionNodeRepository(AppDbContext db) : IDistributionNodeRepository
{
    public async Task<DistributionNode?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.DistributionNodes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<DistributionNode>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await db.DistributionNodes.Where(n => ids.Contains(n.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DistributionNode>> ListSubtreeAsync(string selfPrefix, CancellationToken ct = default)
    {
        var withSlash = selfPrefix + "/";
        return await db.DistributionNodes
            .Where(n => n.AncestryPath == selfPrefix || n.AncestryPath.StartsWith(withSlash))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<DistributionNode> Data, int TotalCount)> ListDescendantPageAsync(
        string selfPrefix, int rootDepth, int depth, int page, int pageSize, CancellationToken ct = default)
    {
        var withSlash = selfPrefix + "/";
        var maxDepth = rootDepth + depth;
        var query = db.DistributionNodes
            .Where(n => (n.AncestryPath == selfPrefix || n.AncestryPath.StartsWith(withSlash)) && n.Depth <= maxDepth);

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(n => n.Depth).ThenBy(n => n.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (data, total);
    }

    public Task AddAsync(DistributionNode node, CancellationToken ct = default)
    {
        db.DistributionNodes.Add(node);
        return Task.CompletedTask;
    }

    // Change tracking persists mutations on CommitAsync; no explicit update needed.
    public Task UpdateAsync(DistributionNode node, CancellationToken ct = default) => Task.CompletedTask;
}
