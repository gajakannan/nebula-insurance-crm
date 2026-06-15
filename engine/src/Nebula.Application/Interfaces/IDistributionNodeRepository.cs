using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IDistributionNodeRepository
{
    Task<DistributionNode?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Batch-load nodes by id (used to hydrate the ordered ancestor list).</summary>
    Task<IReadOnlyList<DistributionNode>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// All descendants of a node (tracked, for transactional ancestry recompute). <paramref name="selfPrefix"/>
    /// is the node's materialized self-prefix: <c>AncestryPath + "/" + Id</c>.
    /// </summary>
    Task<IReadOnlyList<DistributionNode>> ListSubtreeAsync(string selfPrefix, CancellationToken ct = default);

    /// <summary>
    /// Depth-bounded, paginated descendant page for lazy expansion. Returns nodes with
    /// <c>Depth &lt;= rootDepth + depth</c> under the node's self-prefix, ordered by Depth then DisplayName.
    /// </summary>
    Task<(IReadOnlyList<DistributionNode> Data, int TotalCount)> ListDescendantPageAsync(
        string selfPrefix, int rootDepth, int depth, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(DistributionNode node, CancellationToken ct = default);
    Task UpdateAsync(DistributionNode node, CancellationToken ct = default);
}
