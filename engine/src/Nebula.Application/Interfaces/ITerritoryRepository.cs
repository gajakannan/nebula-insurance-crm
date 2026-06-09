using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ITerritoryRepository
{
    Task<Territory?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>True if an active (non-deleted) territory already exists with the same name (case-insensitive).</summary>
    Task<bool> ExistsActiveByNameAsync(string name, CancellationToken ct = default);

    Task AddAsync(Territory territory, CancellationToken ct = default);
}
