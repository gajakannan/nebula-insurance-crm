using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class SavedViewRepository : ISavedViewRepository
{
    private static readonly string[] ManagerRoles = ["DistributionManager", "ProgramManager", "Admin"];

    private readonly AppDbContext _db;

    public SavedViewRepository(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<SavedView>> ListVisibleAsync(SavedViewListQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var uid = user.UserId;
        var isManager = user.Roles.Any(r => ManagerRoles.Contains(r));
        var regionList = user.Regions.ToList();
        var roleList = user.Roles.ToList();

        var q = _db.SavedViews.AsNoTracking().AsQueryable();

        if (!query.IncludeArchived)
            q = q.Where(v => v.ArchivedAt == null);
        if (!string.IsNullOrWhiteSpace(query.ViewType))
            q = q.Where(v => v.ViewType == query.ViewType);
        if (!string.IsNullOrWhiteSpace(query.Visibility))
            q = q.Where(v => v.Visibility == query.Visibility);

        // Personal: owner-only. Team: caller's administered/eligible scope (managers see all team views).
        q = q.Where(v =>
            (v.Visibility == "Personal" && v.OwnerUserId == uid) ||
            (v.Visibility == "Team" && (
                isManager ||
                (v.TeamScopeType == "Region" && v.TeamScopeKey != null && regionList.Contains(v.TeamScopeKey)) ||
                (v.TeamScopeType == "Role" && v.TeamScopeKey != null && roleList.Contains(v.TeamScopeKey)))));

        var total = await q.CountAsync(ct);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await q
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<SavedView>(items, page, pageSize, total);
    }

    public Task<SavedView?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.SavedViews.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<bool> ActiveNameExistsAsync(
        string normalizedName, string viewType, string visibility, Guid ownerUserId,
        string? teamScopeType, string? teamScopeKey, Guid? excludeId, CancellationToken ct)
    {
        var q = _db.SavedViews.AsNoTracking().Where(v =>
            v.ArchivedAt == null &&
            v.NormalizedName == normalizedName &&
            v.ViewType == viewType &&
            v.Visibility == visibility);

        q = visibility == "Team"
            ? q.Where(v => v.TeamScopeType == teamScopeType && v.TeamScopeKey == teamScopeKey)
            : q.Where(v => v.OwnerUserId == ownerUserId);

        if (excludeId.HasValue)
            q = q.Where(v => v.Id != excludeId.Value);

        return q.AnyAsync(ct);
    }

    public Task<SavedView?> GetActiveDefaultAsync(
        string viewType, string visibility, Guid? ownerUserId,
        string? teamScopeType, string? teamScopeKey, Guid? excludeId, CancellationToken ct)
    {
        var q = _db.SavedViews.Where(v =>
            v.ArchivedAt == null &&
            v.IsDefault &&
            v.ViewType == viewType &&
            v.Visibility == visibility);

        q = visibility == "Team"
            ? q.Where(v => v.TeamScopeType == teamScopeType && v.TeamScopeKey == teamScopeKey)
            : q.Where(v => v.OwnerUserId == ownerUserId);

        if (excludeId.HasValue)
            q = q.Where(v => v.Id != excludeId.Value);

        return q.FirstOrDefaultAsync(ct);
    }

    public void Add(SavedView view) => _db.SavedViews.Add(view);

    public void AddAudit(SavedViewAuditEvent audit) => _db.SavedViewAuditEvents.Add(audit);
}
