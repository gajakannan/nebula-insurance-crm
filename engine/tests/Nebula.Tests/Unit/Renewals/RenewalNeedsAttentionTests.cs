using Shouldly;
using Microsoft.EntityFrameworkCore;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;

namespace Nebula.Tests.Unit.Renewals;

public class RenewalNeedsAttentionTests
{
    [Fact]
    public async Task ListNeedsAttentionAsync_ReturnsIdentifiedAndOutreachInWindow_OrderedByExpiry_ExcludesOthers()
    {
        await using var db = CreateContext();
        var today = DateTime.UtcNow.Date;
        var user = Guid.NewGuid();
        var graph = SeedGraph(db);

        var overdue = NewRenewal(graph, user, "Identified", today.AddDays(-3));
        var soon = NewRenewal(graph, user, "Identified", today.AddDays(12));
        var outreach = NewRenewal(graph, user, "Outreach", today.AddDays(27));
        var farOut = NewRenewal(graph, user, "Identified", today.AddDays(120));      // excluded: beyond 90d
        var inReview = NewRenewal(graph, user, "InReview", today.AddDays(10));        // excluded: state
        var deleted = NewRenewal(graph, user, "Identified", today.AddDays(5), isDeleted: true); // excluded: soft-deleted
        db.Renewals.AddRange(overdue, soon, outreach, farOut, inReview, deleted);
        await db.SaveChangesAsync();

        var repository = new RenewalRepository(db);
        var rows = await repository.ListNeedsAttentionAsync(user, ["Admin"], [], 90);

        rows.Select(row => row.RenewalId).ShouldBe([overdue.Id, soon.Id, outreach.Id]);
    }

    [Fact]
    public async Task ListNeedsAttentionAsync_PopulatesLastBrokerContact_FromLatestOutreachEvent()
    {
        await using var db = CreateContext();
        var today = DateTime.UtcNow.Date;
        var user = Guid.NewGuid();
        var graph = SeedGraph(db);

        var withContact = NewRenewal(graph, user, "Outreach", today.AddDays(20));
        var withoutContact = NewRenewal(graph, user, "Identified", today.AddDays(25));
        db.Renewals.AddRange(withContact, withoutContact);

        var older = DateTime.UtcNow.AddDays(-40);
        var newer = DateTime.UtcNow.AddDays(-5);
        db.ActivityTimelineEvents.AddRange(
            NewContactEvent(withContact.Id, user, "RenewalOutreachDrafted", older),
            NewContactEvent(withContact.Id, user, "RenewalOutreachMockSent", newer));
        await db.SaveChangesAsync();

        var repository = new RenewalRepository(db);
        var rows = await repository.ListNeedsAttentionAsync(user, ["Admin"], [], 90);

        var contactRow = rows.Single(row => row.RenewalId == withContact.Id);
        contactRow.LastBrokerContactAt.ShouldNotBeNull();
        contactRow.LastBrokerContactAt!.Value.ShouldBe(newer, TimeSpan.FromSeconds(1));

        rows.Single(row => row.RenewalId == withoutContact.Id).LastBrokerContactAt.ShouldBeNull();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"needs-attention-tests-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    // The Renewal Include graph (Account, Broker, Policy, Carrier) is required + soft-delete
    // filtered, so a non-deleted graph must exist or EF inner-joins drop the renewal rows.
    private sealed record Graph(Guid AccountId, Guid BrokerId, Guid PolicyId);

    private static Graph SeedGraph(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var actor = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(), Name = "Acme Manufacturing", StableDisplayName = "Acme Manufacturing",
            Industry = "Manufacturing", PrimaryState = "CA", Region = "West", Status = "Active",
            CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor,
        };
        var broker = new Broker
        {
            Id = Guid.NewGuid(), LegalName = "Atlas Brokerage", LicenseNumber = "LIC-000001", State = "CA",
            Status = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor,
        };
        var carrier = new CarrierRef
        {
            Id = Guid.NewGuid(), Name = "Sample Carrier", IsActive = true,
            CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor,
        };
        var policy = new Policy
        {
            Id = Guid.NewGuid(), PolicyNumber = "POL-000001", AccountId = account.Id, BrokerId = broker.Id,
            CarrierId = carrier.Id, LineOfBusiness = "Cyber", EffectiveDate = now.Date.AddYears(-1),
            ExpirationDate = now.Date.AddDays(30), TotalPremium = 125000m, PremiumCurrency = "USD",
            CurrentStatus = "Active", ImportSource = "manual", AccountDisplayNameAtLink = "Acme Manufacturing",
            AccountStatusAtRead = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = actor, UpdatedByUserId = actor,
        };
        db.Accounts.Add(account);
        db.Brokers.Add(broker);
        db.CarrierRefs.Add(carrier);
        db.Policies.Add(policy);
        return new Graph(account.Id, broker.Id, policy.Id);
    }

    private static Renewal NewRenewal(Graph graph, Guid assignedToUserId, string status, DateTime expiry, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = graph.AccountId,
        BrokerId = graph.BrokerId,
        PolicyId = graph.PolicyId,
        LineOfBusiness = "Cyber",
        CurrentStatus = status,
        AccountDisplayNameAtLink = "Acme Manufacturing",
        AccountStatusAtRead = "Active",
        PolicyExpirationDate = expiry,
        TargetOutreachDate = expiry.AddDays(-60),
        AssignedToUserId = assignedToUserId,
        IsDeleted = isDeleted,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = assignedToUserId,
        UpdatedByUserId = assignedToUserId,
    };

    private static ActivityTimelineEvent NewContactEvent(Guid renewalId, Guid actor, string eventType, DateTime occurredAt) => new()
    {
        Id = Guid.NewGuid(), EntityType = "Renewal", EntityId = renewalId, EventType = eventType,
        EventDescription = eventType, ActorUserId = actor, OccurredAt = occurredAt,
    };
}
