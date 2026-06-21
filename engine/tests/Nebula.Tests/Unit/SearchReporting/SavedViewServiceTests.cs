using System.Text.Json;
using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit.SearchReporting;

public class SavedViewServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("11110000-0000-0000-0000-000000000001");
    private static readonly Guid OtherId = Guid.Parse("11110000-0000-0000-0000-000000000002");

    private readonly SvSavedViewRepo _repo = new();
    private readonly SvUow _uow = new();
    private SavedViewService CreateService() => new(_repo, _uow, new SvAuthz());

    private static JsonElement Obj(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static SavedViewUpsertRequestDto Personal(string name, bool isDefault = false) =>
        new(name, null, "Search", "Personal", null, null, Obj("""{"q":"acme"}"""), null, isDefault);

    [Fact]
    public async Task CreateAsync_PersonalValid_PersistsAndAudits()
    {
        var svc = CreateService();
        var user = new SvUser(OwnerId, ["Underwriter"]);

        var (result, error) = await svc.CreateAsync(Personal("My Pipeline"), user, default);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("My Pipeline");
        result.Visibility.ShouldBe("Personal");
        _repo.Store.Count.ShouldBe(1);
        _repo.Audits.ShouldContain(a => a.EventType == "Created");
        _uow.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsDuplicateError()
    {
        var svc = CreateService();
        _repo.NameExists = true;

        var (result, error) = await svc.CreateAsync(Personal("Dup"), new SvUser(OwnerId, ["Underwriter"]), default);

        error.ShouldBe("saved_view_duplicate_name");
        result.ShouldBeNull();
        _uow.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateAsync_TeamWithoutScope_ReturnsScopeRequired()
    {
        var svc = CreateService();
        var req = new SavedViewUpsertRequestDto("Team View", null, "Search", "Team", null, null, Obj("""{"q":"x"}"""), null, false);

        var (_, error) = await svc.CreateAsync(req, new SvUser(OwnerId, ["DistributionManager"], ["West"]), default);

        error.ShouldBe("saved_view_scope_required");
    }

    [Fact]
    public async Task CreateAsync_TeamByNonManager_ReturnsScopeDenied()
    {
        var svc = CreateService();
        var req = new SavedViewUpsertRequestDto("Team View", null, "Search", "Team", "Region", "West", Obj("""{"q":"x"}"""), null, false);

        var (_, error) = await svc.CreateAsync(req, new SvUser(OwnerId, ["Underwriter"], ["West"]), default);

        error.ShouldBe("saved_view_scope_denied");
    }

    [Fact]
    public async Task CreateAsync_TeamByManagerInAdministeredRegion_Succeeds()
    {
        var svc = CreateService();
        var req = new SavedViewUpsertRequestDto("Team View", null, "Search", "Team", "Region", "West", Obj("""{"q":"x"}"""), null, false);

        var (result, error) = await svc.CreateAsync(req, new SvUser(OwnerId, ["DistributionManager"], ["West"]), default);

        error.ShouldBeNull();
        result!.Visibility.ShouldBe("Team");
        result.TeamScopeKey.ShouldBe("West");
    }

    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ReturnsPreconditionFailed()
    {
        var svc = CreateService();
        var view = Seed(OwnerId, rowVersion: 5);

        var (_, error) = await svc.UpdateAsync(view.Id, Personal("Renamed"), expectedRowVersion: 99, new SvUser(OwnerId, ["Underwriter"]), default);

        error.ShouldBe("precondition_failed");
        _uow.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAsync_ByNonOwnerNonManager_ReturnsPolicyDenied()
    {
        var svc = CreateService();
        var view = Seed(OwnerId, rowVersion: 0);

        var (_, error) = await svc.UpdateAsync(view.Id, Personal("Renamed"), 0, new SvUser(OtherId, ["Underwriter"]), default);

        error.ShouldBe("policy_denied");
    }

    [Fact]
    public async Task UpdateAsync_ByOwner_UpdatesAndAudits()
    {
        var svc = CreateService();
        var view = Seed(OwnerId, rowVersion: 0);

        var (result, error) = await svc.UpdateAsync(view.Id, Personal("Renamed"), 0, new SvUser(OwnerId, ["Underwriter"]), default);

        error.ShouldBeNull();
        result!.Name.ShouldBe("Renamed");
        _repo.Audits.ShouldContain(a => a.EventType == "Updated");
        _uow.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task ArchiveAsync_ByOwner_SetsArchivedAndAudits()
    {
        var svc = CreateService();
        var view = Seed(OwnerId, rowVersion: 0);

        var error = await svc.ArchiveAsync(view.Id, 0, new SvUser(OwnerId, ["Underwriter"]), default);

        error.ShouldBeNull();
        view.ArchivedAt.ShouldNotBeNull();
        view.IsDefault.ShouldBeFalse();
        _repo.Audits.ShouldContain(a => a.EventType == "Archived");
    }

    [Fact]
    public async Task SetDefaultAsync_ClearsPriorDefault_AndAudits()
    {
        var svc = CreateService();
        var prior = Seed(OwnerId, rowVersion: 0, isDefault: true);
        var target = Seed(OwnerId, rowVersion: 0);
        _repo.ActiveDefault = prior;

        var (result, error) = await svc.SetDefaultAsync(target.Id, 0, new SvUser(OwnerId, ["Underwriter"]), default);

        error.ShouldBeNull();
        result!.IsDefault.ShouldBeTrue();
        prior.IsDefault.ShouldBeFalse();
        _repo.Audits.ShouldContain(a => a.EventType == "DefaultChanged");
    }

    [Fact]
    public async Task GetAsync_PersonalByNonOwner_ReturnsNull()
    {
        var svc = CreateService();
        var view = Seed(OwnerId, rowVersion: 0);

        var result = await svc.GetAsync(view.Id, new SvUser(OtherId, ["Underwriter"]), default);

        result.ShouldBeNull();
    }

    private SavedView Seed(Guid owner, uint rowVersion, bool isDefault = false)
    {
        var now = DateTime.UtcNow;
        var v = new SavedView
        {
            Id = Guid.NewGuid(),
            Name = "Seed",
            NormalizedName = "seed",
            ViewType = "Search",
            Visibility = "Personal",
            OwnerUserId = owner,
            CriteriaJson = """{"q":"acme"}""",
            SortJson = "{}",
            IsDefault = isDefault,
            RowVersion = rowVersion,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = owner,
            UpdatedByUserId = owner,
        };
        _repo.Store.Add(v);
        return v;
    }
}

// ── file-scoped test stubs ──────────────────────────────────────────────────

internal sealed class SvUser : ICurrentUserService
{
    public SvUser(Guid id, string[]? roles = null, string[]? regions = null, string? name = "Tester")
    {
        UserId = id;
        Roles = roles ?? [];
        Regions = regions ?? [];
        DisplayName = name;
    }

    public Guid UserId { get; }
    public string? DisplayName { get; }
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<string> Regions { get; }
    public string? BrokerTenantId => null;
}

internal sealed class SvAuthz : IAuthorizationService
{
    public Task<bool> AuthorizeAsync(string userRole, string resourceType, string action, IDictionary<string, object>? resourceAttributes = null)
    {
        if (action == "read") return Task.FromResult(true);
        object? subject = null, creator = null;
        if (resourceAttributes is not null)
        {
            resourceAttributes.TryGetValue("subjectId", out subject);
            resourceAttributes.TryGetValue("creator", out creator);
        }
        if (Equals(subject, creator)) return Task.FromResult(true);
        if (userRole is "DistributionManager" or "ProgramManager" or "Admin") return Task.FromResult(true);
        return Task.FromResult(false);
    }
}

internal sealed class SvUow : IUnitOfWork
{
    public int CommitCount { get; private set; }
    public Task CommitAsync(CancellationToken ct = default) { CommitCount++; return Task.CompletedTask; }
}

internal sealed class SvSavedViewRepo : ISavedViewRepository
{
    public List<SavedView> Store { get; } = [];
    public List<SavedViewAuditEvent> Audits { get; } = [];
    public bool NameExists { get; set; }
    public SavedView? ActiveDefault { get; set; }

    public Task<PaginatedResult<SavedView>> ListVisibleAsync(SavedViewListQuery query, ICurrentUserService user, CancellationToken ct) =>
        Task.FromResult(new PaginatedResult<SavedView>(Store, query.Page, query.PageSize, Store.Count));

    public Task<SavedView?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Store.FirstOrDefault(v => v.Id == id && !v.IsDeleted));

    public Task<bool> ActiveNameExistsAsync(string normalizedName, string viewType, string visibility, Guid ownerUserId, string? teamScopeType, string? teamScopeKey, Guid? excludeId, CancellationToken ct) =>
        Task.FromResult(NameExists);

    public Task<SavedView?> GetActiveDefaultAsync(string viewType, string visibility, Guid? ownerUserId, string? teamScopeType, string? teamScopeKey, Guid? excludeId, CancellationToken ct) =>
        Task.FromResult(ActiveDefault);

    public void Add(SavedView view) => Store.Add(view);
    public void AddAudit(SavedViewAuditEvent audit) => Audits.Add(audit);
}
