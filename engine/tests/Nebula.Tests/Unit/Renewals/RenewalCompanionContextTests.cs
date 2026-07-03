using Shouldly;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;

namespace Nebula.Tests.Unit.Renewals;

public class RenewalCompanionContextTests
{
    [Fact]
    public async Task GetCompanionContextAsync_ReturnsContextWithTimeline_ForVisibleRenewal()
    {
        await using var db = CreateContext();
        var user = Guid.NewGuid();
        var graph = SeedGraph(db);
        var renewal = NewRenewal(graph, user, "Identified");
        db.UserProfiles.Add(NewUserProfile(user));
        db.Renewals.Add(renewal);
        db.ActivityTimelineEvents.AddRange(
            NewEvent(renewal.Id, user, "RenewalCreated", DateTime.UtcNow.AddDays(-2)),
            NewEvent(renewal.Id, user, "RenewalAssigned", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();

        var svc = NewService(db);
        var (dto, error) = await svc.GetCompanionContextAsync(renewal.Id, Admin(user), DenyAuthz());

        error.ShouldBeNull();
        dto.ShouldNotBeNull();
        dto!.RenewalId.ShouldBe(renewal.Id.ToString());
        dto.AccountName.ShouldBe("Acme Manufacturing");
        dto.BrokerName.ShouldBe("Atlas Brokerage");
        dto.WorkflowState.ShouldBe("Identified");
        dto.CanDraftOutreach.ShouldBeFalse();
        dto.Timeline.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCompanionContextAsync_NotFound_WhenNotIdentifiedOrOutreach()
    {
        await using var db = CreateContext();
        var user = Guid.NewGuid();
        var graph = SeedGraph(db);
        db.UserProfiles.Add(NewUserProfile(user));
        db.Renewals.Add(NewRenewal(graph, user, "InReview"));
        await db.SaveChangesAsync();

        var renewalId = await db.Renewals.Select(r => r.Id).FirstAsync();
        var (dto, error) = await NewService(db).GetCompanionContextAsync(renewalId, Admin(user), DenyAuthz());

        dto.ShouldBeNull();
        error.ShouldBe("not_found");
    }

    [Fact]
    public async Task GetCompanionContextAsync_NotFound_WhenCallerCannotReadRenewal()
    {
        await using var db = CreateContext();
        var owner = Guid.NewGuid();
        var graph = SeedGraph(db);
        var renewal = NewRenewal(graph, owner, "Identified");
        db.UserProfiles.Add(NewUserProfile(owner));
        db.Renewals.Add(renewal);
        await db.SaveChangesAsync();

        // A DistributionUser who is NOT the assignee cannot read it.
        var stranger = new TestUser(Guid.NewGuid(), ["DistributionUser"], []);
        var (dto, error) = await NewService(db).GetCompanionContextAsync(renewal.Id, stranger, DenyAuthz());

        dto.ShouldBeNull();
        error.ShouldBe("not_found");
    }

    private static RenewalService NewService(AppDbContext db) =>
        new(new RenewalRepository(db), null!, null!, new TimelineRepository(db), null!, null!, null!, null!, null!);

    private static ICurrentUserService Admin(Guid userId) => new TestUser(userId, ["Admin"], ["West"]);

    private static IAuthorizationService DenyAuthz() => new StubAuthz(false);

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"companion-context-{Guid.NewGuid()}").Options;
        return new AppDbContext(options);
    }

    private sealed record Graph(Guid AccountId, Guid BrokerId, Guid PolicyId);

    private static Graph SeedGraph(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var actor = Guid.NewGuid();
        var account = new Account { Id = Guid.NewGuid(), Name = "Acme Manufacturing", StableDisplayName = "Acme Manufacturing", Industry = "Manufacturing", PrimaryState = "CA", Region = "West", Status = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor };
        var broker = new Broker { Id = Guid.NewGuid(), LegalName = "Atlas Brokerage", LicenseNumber = "LIC-000001", State = "CA", Status = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor };
        var carrier = new CarrierRef { Id = Guid.NewGuid(), Name = "Sample Carrier", IsActive = true, CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor };
        var policy = new Policy { Id = Guid.NewGuid(), PolicyNumber = "POL-000001", AccountId = account.Id, BrokerId = broker.Id, CarrierId = carrier.Id, LineOfBusiness = "Cyber", EffectiveDate = now.Date.AddYears(-1), ExpirationDate = now.Date.AddDays(30), TotalPremium = 125000m, PremiumCurrency = "USD", CurrentStatus = "Active", ImportSource = "manual", AccountDisplayNameAtLink = "Acme Manufacturing", AccountStatusAtRead = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor };
        db.Accounts.Add(account);
        db.Brokers.Add(broker);
        db.CarrierRefs.Add(carrier);
        db.Policies.Add(policy);
        return new Graph(account.Id, broker.Id, policy.Id);
    }

    private static Renewal NewRenewal(Graph graph, Guid assignedTo, string status) => new()
    {
        Id = Guid.NewGuid(), AccountId = graph.AccountId, BrokerId = graph.BrokerId, PolicyId = graph.PolicyId,
        LineOfBusiness = "Cyber", CurrentStatus = status, AccountDisplayNameAtLink = "Acme Manufacturing",
        AccountStatusAtRead = "Active", PolicyExpirationDate = DateTime.UtcNow.Date.AddDays(20),
        TargetOutreachDate = DateTime.UtcNow.Date.AddDays(-40), AssignedToUserId = assignedTo,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedByUserId = assignedTo, UpdatedByUserId = assignedTo,
    };

    private static ActivityTimelineEvent NewEvent(Guid renewalId, Guid actor, string type, DateTime at) => new()
    {
        Id = Guid.NewGuid(), EntityType = "Renewal", EntityId = renewalId, EventType = type,
        EventDescription = type, ActorUserId = actor, ActorDisplayName = "Test User", OccurredAt = at,
    };

    private static UserProfile NewUserProfile(Guid id) => new()
    {
        Id = id, IdpIssuer = "http://test.local/", IdpSubject = id.ToString(), Email = $"{id}@nebula.test",
        DisplayName = "Assignee", Department = "Distribution", RegionsJson = "[\"West\"]", RolesJson = "[\"Admin\"]",
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
    };

    private sealed class TestUser(Guid userId, IReadOnlyList<string> roles, IReadOnlyList<string> regions) : ICurrentUserService
    {
        public Guid UserId => userId;
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => roles;
        public IReadOnlyList<string> Regions => regions;
        public string? BrokerTenantId => null;
    }

    private sealed class StubAuthz(bool allow) : IAuthorizationService
    {
        public Task<bool> AuthorizeAsync(string userRole, string resourceType, string action, IDictionary<string, object>? resourceAttributes = null) =>
            Task.FromResult(allow);
    }
}
