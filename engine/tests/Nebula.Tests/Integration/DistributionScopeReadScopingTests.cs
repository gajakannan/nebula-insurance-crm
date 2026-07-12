using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;

namespace Nebula.Tests.Integration;

/// <summary>
/// F0037 CR-H2 regression guard: exercises the REAL EF Core no-leak predicates
/// (<see cref="DistributionScopeRepository"/>, <see cref="DistributionScopeService"/>,
/// <see cref="OperationalReportProjectionRepository"/>) against a real PostgreSQL database rather than an
/// in-memory mock. Unit tests reimplement the predicate in fakes and cannot catch a divergence between the
/// mock and the shipped `.Where` chain; these tests seed a small hierarchy and assert the predicate directly.
///
/// Requires Docker (Testcontainers PostgreSQL via <see cref="CustomWebApplicationFactory"/>); runs in CI.
/// Seeds with unique GUIDs per test and asserts on presence/absence of specific broker ids, so it is safe to
/// run against the shared integration database.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DistributionScopeReadScopingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateOnly AsOf = DateOnly.Parse("2026-07-06");

    [Fact]
    public async Task DefaultManagerView_UnionsManagedBrokerRowsAcrossRegions_AndExcludesSibling()
    {
        using var db = NewDb(out var provider);
        using (provider)
        {
            var manager = Guid.NewGuid();
            var otherUser = Guid.NewGuid();
            var managedBroker = Guid.NewGuid();
            var siblingBroker = Guid.NewGuid();

            // The manager manages `managedBroker`; `siblingBroker` belongs to someone else.
            db.Brokers.Add(NewBroker(managedBroker, managedByUserId: manager));
            db.Brokers.Add(NewBroker(siblingBroker, managedByUserId: otherUser));

            // Managed-broker row is OUT of the manager's region ("West") and owned by someone else — under the
            // pre-fix AND semantics it was hidden; under authority-union it must be visible.
            db.OperationalReportProjections.Add(NewProjection(managedBroker, region: "East", owner: otherUser));
            // Sibling-broker row (also out of region, not owned) must remain hidden.
            db.OperationalReportProjections.Add(NewProjection(siblingBroker, region: "East", owner: otherUser));
            await db.SaveChangesAsync();

            var scope = new DistributionScopeService(new DistributionScopeRepository(db));
            var user = new ScopeUser(manager, ["DistributionManager"], ["West"]);

            var visibility = await scope.ResolveAsync(new DistributionScopeRequest(null, null, null, AsOf), user, default);
            visibility.HasScope.ShouldBeTrue();
            visibility.ExplicitScopeRequested.ShouldBeFalse("default view has no explicit filter");

            var rows = await new OperationalReportProjectionRepository(db)
                .QueryAsync(EmptyQuery(), visibility, default);
            var brokerIds = rows.Where(r => r.BrokerId is not null).Select(r => r.BrokerId!.Value).ToHashSet();

            brokerIds.ShouldContain(managedBroker, "manager must see managed-broker rows regardless of region");
            brokerIds.ShouldNotContain(siblingBroker, "sibling-broker rows must be excluded (no leak)");
        }
    }

    [Fact]
    public async Task ExplicitRootNodeOutsideAuthority_FailsClosed()
    {
        using var db = NewDb(out var provider);
        using (provider)
        {
            var manager = Guid.NewGuid();
            var otherUser = Guid.NewGuid();
            var managedBroker = Guid.NewGuid();
            var siblingBroker = Guid.NewGuid();

            db.Brokers.Add(NewBroker(managedBroker, managedByUserId: manager));
            db.Brokers.Add(NewBroker(siblingBroker, managedByUserId: otherUser));
            // A broker is a distribution node sharing its GUID; the sibling node lets the hierarchy resolve.
            db.DistributionNodes.Add(NewBrokerNode(siblingBroker));
            await db.SaveChangesAsync();

            var scope = new DistributionScopeService(new DistributionScopeRepository(db));
            var user = new ScopeUser(manager, ["DistributionManager"], ["West"]);

            // Requesting a broker the manager does not manage must deny (empty scope), not narrow-and-leak.
            var visibility = await scope.ResolveAsync(
                new DistributionScopeRequest(siblingBroker, null, null, AsOf), user, default);

            visibility.HasScope.ShouldBeFalse();
            visibility.ExplanationCodes.ShouldContain("requested_scope_outside_authority");

            var rows = await new OperationalReportProjectionRepository(db)
                .QueryAsync(EmptyQuery(), visibility, default);
            rows.ShouldBeEmpty("a denied scope materializes no rows");
        }
    }

    [Fact]
    public async Task ListBrokerIdsForTerritory_ExcludesExpiredAssignments_ForAsOf()
    {
        using var db = NewDb(out var provider);
        using (provider)
        {
            var territory = Guid.NewGuid();
            var activeBroker = Guid.NewGuid();
            var expiredBroker = Guid.NewGuid();

            db.TerritoryAssignments.Add(NewAssignment(territory, activeBroker,
                effectiveFrom: new DateOnly(2026, 1, 1), effectiveTo: null));
            db.TerritoryAssignments.Add(NewAssignment(territory, expiredBroker,
                effectiveFrom: new DateOnly(2020, 1, 1), effectiveTo: new DateOnly(2021, 1, 1)));
            await db.SaveChangesAsync();

            var brokerIds = await new DistributionScopeRepository(db)
                .ListBrokerIdsForTerritoryAsync(territory, AsOf, default);

            brokerIds.ShouldContain(activeBroker);
            brokerIds.ShouldNotContain(expiredBroker, "assignments not effective on AsOf must be excluded");
        }
    }

    // ── seeding helpers ──────────────────────────────────────────────────────────────────────────────

    private AppDbContext NewDb(out IServiceScope scope)
    {
        scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private static OperationalReportQuery EmptyQuery() =>
        new(null, null, null, null, null, null, null, AsOf, 50);

    private static Broker NewBroker(Guid id, Guid managedByUserId) => new()
    {
        Id = id,
        LegalName = $"Broker {id:N}",
        LicenseNumber = $"LIC-{id:N}"[..12],
        State = "CA",
        Status = "Active",
        ManagedByUserId = managedByUserId,
    };

    private static DistributionNode NewBrokerNode(Guid id) => new()
    {
        Id = id,
        NodeType = "Broker",
        DisplayName = $"Node {id:N}",
        AncestryPath = "",
        IsActive = true,
    };

    private static OperationalReportProjection NewProjection(Guid brokerId, string region, Guid owner) => new()
    {
        SourceObjectType = "Submission",
        SourceObjectId = Guid.NewGuid(),
        TargetUrl = "/submissions/1",
        CurrentStatus = "InReview",
        OwnerUserId = owner,
        BrokerId = brokerId,
        Region = region,
        LastSourceUpdatedAt = DateTimeOffset.UtcNow,
        ProjectedAt = DateTimeOffset.UtcNow,
    };

    private static TerritoryAssignment NewAssignment(Guid territoryId, Guid memberId, DateOnly effectiveFrom, DateOnly? effectiveTo) => new()
    {
        TerritoryId = territoryId,
        MemberType = "Broker",
        MemberId = memberId,
        EffectiveFrom = effectiveFrom,
        EffectiveTo = effectiveTo,
    };

    private sealed class ScopeUser(Guid userId, IReadOnlyList<string> roles, IReadOnlyList<string> regions) : ICurrentUserService
    {
        public Guid UserId { get; } = userId;
        public string? DisplayName => "Scope Integration Tester";
        public IReadOnlyList<string> Roles { get; } = roles;
        public IReadOnlyList<string> Regions { get; } = regions;
        public string? BrokerTenantId => null;
    }
}
