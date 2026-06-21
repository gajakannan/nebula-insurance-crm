using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ISavedViewRepository
{
    /// <summary>Personal views owned by the caller plus team views in the caller's administered/eligible scope.</summary>
    Task<PaginatedResult<SavedView>> ListVisibleAsync(SavedViewListQuery query, ICurrentUserService user, CancellationToken ct);

    /// <summary>Tracked load (non-deleted) for mutation. Returns null for missing/soft-deleted.</summary>
    Task<SavedView?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<bool> ActiveNameExistsAsync(
        string normalizedName, string viewType, string visibility, Guid ownerUserId,
        string? teamScopeType, string? teamScopeKey, Guid? excludeId, CancellationToken ct);

    /// <summary>Tracked current active default for the same (viewType, scope), if any — used to clear on SetDefault.</summary>
    Task<SavedView?> GetActiveDefaultAsync(
        string viewType, string visibility, Guid? ownerUserId,
        string? teamScopeType, string? teamScopeKey, Guid? excludeId, CancellationToken ct);

    void Add(SavedView view);
    void AddAudit(SavedViewAuditEvent audit);
}
