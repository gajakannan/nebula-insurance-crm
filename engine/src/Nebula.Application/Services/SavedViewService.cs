using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class SavedViewService : ISavedViewService
{
    private static readonly string[] ManagerRoles = ["Admin", "ProgramManager", "DistributionManager"];
    private static readonly string[] AllowedScopeTypes = ["Role", "Region", "Program", "Territory"];

    private readonly ISavedViewRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;

    public SavedViewService(ISavedViewRepository repo, IUnitOfWork uow, IAuthorizationService authz)
    {
        _repo = repo;
        _uow = uow;
        _authz = authz;
    }

    public async Task<PaginatedResult<SavedViewDto>> ListAsync(SavedViewListQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var page = await _repo.ListVisibleAsync(query, user, ct);
        var dtos = page.Data.Select(v => Map(v, user)).ToList();
        return new PaginatedResult<SavedViewDto>(dtos, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<SavedViewDto?> GetAsync(Guid savedViewId, ICurrentUserService user, CancellationToken ct)
    {
        var view = await _repo.GetByIdAsync(savedViewId, ct);
        if (view is null || view.ArchivedAt != null || !CanView(user, view))
            return null; // 404 to avoid leaking existence
        return Map(view, user);
    }

    public async Task<(SavedViewDto? Result, string? Error)> CreateAsync(SavedViewUpsertRequestDto request, ICurrentUserService user, CancellationToken ct)
    {
        if (request.Criteria.ValueKind != JsonValueKind.Object)
            return (null, "saved_view_criteria_invalid");

        var isTeam = request.Visibility == "Team";
        var scopeType = isTeam ? request.TeamScopeType : null;
        var scopeKey = isTeam ? request.TeamScopeKey : null;

        if (isTeam)
        {
            if (string.IsNullOrWhiteSpace(scopeType) || string.IsNullOrWhiteSpace(scopeKey) || !AllowedScopeTypes.Contains(scopeType))
                return (null, "saved_view_scope_required");
        }

        if (!await AuthorizeAsync(user, "manage", user.UserId))
            return (null, "policy_denied");

        if (isTeam && !IsAdministeredTeamScope(user, scopeType!, scopeKey!))
            return (null, "saved_view_scope_denied");

        var normalized = NormalizeName(request.Name);
        if (await _repo.ActiveNameExistsAsync(normalized, request.ViewType, request.Visibility, user.UserId, scopeType, scopeKey, null, ct))
            return (null, "saved_view_duplicate_name");

        var now = DateTime.UtcNow;
        var view = new SavedView
        {
            Name = request.Name.Trim(),
            NormalizedName = normalized,
            Description = request.Description,
            ViewType = request.ViewType,
            Visibility = request.Visibility,
            OwnerUserId = user.UserId,
            TeamScopeType = scopeType,
            TeamScopeKey = scopeKey,
            CriteriaJson = request.Criteria.GetRawText(),
            SortJson = request.Sort.HasValue ? request.Sort.Value.GetRawText() : "{}",
            IsDefault = false,
            LastEditedByUserId = user.UserId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        _repo.Add(view);

        if (request.IsDefault)
        {
            await ClearOtherDefaultsAsync(view, user, ct);
            view.IsDefault = true;
        }

        _repo.AddAudit(NewAudit(view.Id, "Created", user.UserId, null, view));
        await _uow.CommitAsync(ct);
        return (Map(view, user), null);
    }

    public async Task<(SavedViewDto? Result, string? Error)> UpdateAsync(Guid savedViewId, SavedViewUpsertRequestDto request, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct)
    {
        var view = await _repo.GetByIdAsync(savedViewId, ct);
        if (view is null || view.ArchivedAt != null)
            return (null, "not_found");

        var (authError, _) = await AuthorizeMutationAsync(view, user, "manage", ct);
        if (authError is not null)
            return (null, authError);

        if (request.Criteria.ValueKind != JsonValueKind.Object)
            return (null, "saved_view_criteria_invalid");

        if (view.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        var normalized = NormalizeName(request.Name);
        if (await _repo.ActiveNameExistsAsync(normalized, view.ViewType, view.Visibility, view.OwnerUserId, view.TeamScopeType, view.TeamScopeKey, view.Id, ct))
            return (null, "saved_view_duplicate_name");

        var before = SnapshotJson(view);
        view.Name = request.Name.Trim();
        view.NormalizedName = normalized;
        view.Description = request.Description;
        view.CriteriaJson = request.Criteria.GetRawText();
        if (request.Sort.HasValue)
            view.SortJson = request.Sort.Value.GetRawText();
        Touch(view, user);

        _repo.AddAudit(NewAudit(view.Id, "Updated", user.UserId, before, view));
        await _uow.CommitAsync(ct);
        return (Map(view, user), null);
    }

    public async Task<string?> ArchiveAsync(Guid savedViewId, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct)
    {
        var view = await _repo.GetByIdAsync(savedViewId, ct);
        if (view is null || view.ArchivedAt != null)
            return "not_found";

        var (authError, _) = await AuthorizeMutationAsync(view, user, "manage", ct);
        if (authError is not null)
            return authError;

        if (view.RowVersion != expectedRowVersion)
            return "precondition_failed";

        var before = SnapshotJson(view);
        view.ArchivedAt = DateTimeOffset.UtcNow;
        view.IsDefault = false;
        Touch(view, user);

        _repo.AddAudit(NewAudit(view.Id, "Archived", user.UserId, before, view));
        await _uow.CommitAsync(ct);
        return null;
    }

    public async Task<(SavedViewDto? Result, string? Error)> SetDefaultAsync(Guid savedViewId, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct)
    {
        var view = await _repo.GetByIdAsync(savedViewId, ct);
        if (view is null || view.ArchivedAt != null)
            return (null, "not_found");

        var (authError, _) = await AuthorizeMutationAsync(view, user, "default", ct);
        if (authError is not null)
            return (null, authError);

        if (view.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        await ClearOtherDefaultsAsync(view, user, ct);

        var before = SnapshotJson(view);
        view.IsDefault = true;
        Touch(view, user);

        _repo.AddAudit(NewAudit(view.Id, "DefaultChanged", user.UserId, before, view));
        await _uow.CommitAsync(ct);
        return (Map(view, user), null);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private async Task<(string? Error, bool Ok)> AuthorizeMutationAsync(SavedView view, ICurrentUserService user, string action, CancellationToken ct)
    {
        if (!await AuthorizeAsync(user, action, view.OwnerUserId))
            return ("policy_denied", false);
        if (view.Visibility == "Team" && !IsAdministeredTeamScope(user, view.TeamScopeType, view.TeamScopeKey))
            return ("saved_view_scope_denied", false);
        return (null, true);
    }

    private async Task<bool> AuthorizeAsync(ICurrentUserService user, string action, Guid ownerUserId)
    {
        var attrs = new Dictionary<string, object>
        {
            ["creator"] = ownerUserId,
            ["subjectId"] = user.UserId,
        };
        foreach (var role in user.Roles)
        {
            if (await _authz.AuthorizeAsync(role, "saved_view", action, attrs))
                return true;
        }
        return false;
    }

    private async Task ClearOtherDefaultsAsync(SavedView view, ICurrentUserService user, CancellationToken ct)
    {
        var owner = view.Visibility == "Personal" ? view.OwnerUserId : (Guid?)null;
        var prior = await _repo.GetActiveDefaultAsync(view.ViewType, view.Visibility, owner, view.TeamScopeType, view.TeamScopeKey, view.Id, ct);
        if (prior is not null && prior.IsDefault)
        {
            var priorBefore = SnapshotJson(prior);
            prior.IsDefault = false;
            Touch(prior, user);
            _repo.AddAudit(NewAudit(prior.Id, "DefaultChanged", user.UserId, priorBefore, prior));
        }
    }

    private bool IsAdministeredTeamScope(ICurrentUserService user, string? scopeType, string? scopeKey)
    {
        if (string.IsNullOrWhiteSpace(scopeType) || string.IsNullOrWhiteSpace(scopeKey))
            return false;
        if (user.Roles.Contains("Admin"))
            return true;
        var isManager = user.Roles.Any(r => r is "DistributionManager" or "ProgramManager");
        if (!isManager)
            return false;
        // User auth context carries Roles + Regions; Program/Territory team scopes require Admin (MVP).
        return (scopeType == "Region" && user.Regions.Contains(scopeKey))
            || (scopeType == "Role" && user.Roles.Contains(scopeKey));
    }

    private static bool CanView(ICurrentUserService user, SavedView view)
    {
        if (view.Visibility == "Personal")
            return view.OwnerUserId == user.UserId;
        if (user.Roles.Any(r => ManagerRoles.Contains(r)))
            return true;
        return (view.TeamScopeType == "Region" && view.TeamScopeKey != null && user.Regions.Contains(view.TeamScopeKey))
            || (view.TeamScopeType == "Role" && view.TeamScopeKey != null && user.Roles.Contains(view.TeamScopeKey));
    }

    private static void Touch(SavedView view, ICurrentUserService user)
    {
        view.LastEditedByUserId = user.UserId;
        view.UpdatedAt = DateTime.UtcNow;
        view.UpdatedByUserId = user.UserId;
    }

    private static string NormalizeName(string name) =>
        string.Join(' ', name.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static SavedViewAuditEvent NewAudit(Guid savedViewId, string eventType, Guid actor, string? beforeJson, SavedView after) => new()
    {
        SavedViewId = savedViewId,
        EventType = eventType,
        ActorUserId = actor,
        OccurredAt = DateTimeOffset.UtcNow,
        BeforeJson = beforeJson,
        AfterJson = SnapshotJson(after),
    };

    private static string SnapshotJson(SavedView v) => JsonSerializer.Serialize(new
    {
        v.Name,
        v.Description,
        v.ViewType,
        v.Visibility,
        v.TeamScopeType,
        v.TeamScopeKey,
        Criteria = v.CriteriaJson,
        Sort = v.SortJson,
        v.IsDefault,
        ArchivedAt = v.ArchivedAt,
    });

    private static SavedViewDto Map(SavedView v, ICurrentUserService user) => new(
        Id: v.Id,
        Name: v.Name,
        Description: v.Description,
        ViewType: v.ViewType,
        Visibility: v.Visibility,
        TeamScopeType: v.TeamScopeType,
        TeamScopeKey: v.TeamScopeKey,
        Criteria: ParseJson(v.CriteriaJson),
        Sort: ParseJson(v.SortJson),
        OwnerUserId: v.OwnerUserId,
        OwnerDisplayName: v.OwnerUserId == user.UserId ? user.DisplayName : null,
        LastEditedByUserId: v.LastEditedByUserId,
        LastEditedByDisplayName: v.LastEditedByUserId == user.UserId ? user.DisplayName : null,
        IsDefault: v.IsDefault,
        ArchivedAt: v.ArchivedAt,
        CreatedAt: ToOffset(v.CreatedAt),
        UpdatedAt: ToOffset(v.UpdatedAt),
        RowVersion: v.RowVersion.ToString());

    private static DateTimeOffset ToOffset(DateTime dt) =>
        new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));

    private static JsonElement ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            json = "{}";
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
