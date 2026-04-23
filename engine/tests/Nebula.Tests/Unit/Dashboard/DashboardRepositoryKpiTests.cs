using Shouldly;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit.Dashboard;

public class DashboardRepositoryKpiTests
{
    [Fact]
    public async Task GetKpisAsync_UsesPeriodWindowForRenewalRateAndTurnaround()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        SeedReferenceStatuses(db);

        var brokerA = new Broker { Id = Guid.NewGuid(), Status = "Active", LegalName = "A", LicenseNumber = "L1", State = "CA" };
        var brokerB = new Broker { Id = Guid.NewGuid(), Status = "Active", LegalName = "B", LicenseNumber = "L2", State = "TX" };
        db.Brokers.AddRange(
            brokerA,
            brokerB);

        var accountId = Guid.NewGuid();
        var brokerId = brokerA.Id;
        var userId = Guid.NewGuid();

        var openSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            BrokerId = brokerId,
            AssignedToUserId = userId,
            CurrentStatus = "Received",
            AccountDisplayNameAtLink = "Acme Manufacturing",
            AccountStatusAtRead = "Active",
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-2),
        };
        var recentSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            BrokerId = brokerId,
            AssignedToUserId = userId,
            CurrentStatus = "Bound",
            AccountDisplayNameAtLink = "Acme Manufacturing",
            AccountStatusAtRead = "Active",
            CreatedAt = now.AddDays(-20),
            UpdatedAt = now.AddDays(-10),
        };
        var midSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            BrokerId = brokerId,
            AssignedToUserId = userId,
            CurrentStatus = "Bound",
            AccountDisplayNameAtLink = "Acme Manufacturing",
            AccountStatusAtRead = "Active",
            CreatedAt = now.AddDays(-100),
            UpdatedAt = now.AddDays(-60),
        };
        db.Submissions.AddRange(openSubmission, recentSubmission, midSubmission);

        db.Renewals.AddRange(
            new Renewal
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                BrokerId = brokerId,
                PolicyId = Guid.NewGuid(),
                AssignedToUserId = userId,
                CurrentStatus = "Completed",
                AccountDisplayNameAtLink = "Acme Manufacturing",
                AccountStatusAtRead = "Active",
                PolicyExpirationDate = now.AddDays(30),
                TargetOutreachDate = now.AddDays(-60),
                BoundPolicyId = Guid.NewGuid(),
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-40),
            },
            new Renewal
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                BrokerId = brokerId,
                PolicyId = Guid.NewGuid(),
                AssignedToUserId = userId,
                CurrentStatus = "Lost",
                AccountDisplayNameAtLink = "Acme Manufacturing",
                AccountStatusAtRead = "Active",
                PolicyExpirationDate = now.AddDays(30),
                TargetOutreachDate = now.AddDays(-60),
                LostReasonCode = "CompetitiveLoss",
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-5),
            },
            new Renewal
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                BrokerId = brokerId,
                PolicyId = Guid.NewGuid(),
                AssignedToUserId = userId,
                CurrentStatus = "Completed",
                AccountDisplayNameAtLink = "Acme Manufacturing",
                AccountStatusAtRead = "Active",
                PolicyExpirationDate = now.AddDays(30),
                TargetOutreachDate = now.AddDays(-60),
                BoundPolicyId = Guid.NewGuid(),
                CreatedAt = now.AddDays(-200),
                UpdatedAt = now.AddDays(-150),
            });

        db.WorkflowTransitions.AddRange(
            new WorkflowTransition
            {
                Id = Guid.NewGuid(),
                WorkflowType = "Submission",
                EntityId = recentSubmission.Id,
                FromState = "InReview",
                ToState = "Bound",
                ActorUserId = userId,
                OccurredAt = now.AddDays(-10),
            },
            new WorkflowTransition
            {
                Id = Guid.NewGuid(),
                WorkflowType = "Submission",
                EntityId = midSubmission.Id,
                FromState = "InReview",
                ToState = "Bound",
                ActorUserId = userId,
                OccurredAt = now.AddDays(-60),
            });

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var adminUser = new TestCurrentUserService(userId, ["Admin"], ["West"]);
        var ninetyDayKpis = await repository.GetKpisAsync(adminUser, 90);
        var thirtyDayKpis = await repository.GetKpisAsync(adminUser, 30);

        ninetyDayKpis.ActiveBrokers.ShouldBe(2);
        thirtyDayKpis.ActiveBrokers.ShouldBe(2);
        ninetyDayKpis.OpenSubmissions.ShouldBe(1);
        thirtyDayKpis.OpenSubmissions.ShouldBe(1);

        ninetyDayKpis.RenewalRate.ShouldBe(50.0);
        thirtyDayKpis.RenewalRate.ShouldBe(0.0);

        ninetyDayKpis.AvgTurnaroundDays.ShouldBe(25.0);
        thirtyDayKpis.AvgTurnaroundDays.ShouldBe(10.0);
    }

    [Fact]
    public async Task GetKpisAsync_AppliesDefaultAndMaximumPeriodBounds()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        SeedReferenceStatuses(db);

        var broker = new Broker { Id = Guid.NewGuid(), Status = "Active", LegalName = "A", LicenseNumber = "L1", State = "CA" };
        db.Brokers.Add(broker);

        var accountId = Guid.NewGuid();
        var brokerId = broker.Id;
        var userId = Guid.NewGuid();

        db.Submissions.Add(new Submission
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            BrokerId = brokerId,
            AssignedToUserId = userId,
            CurrentStatus = "Received",
            AccountDisplayNameAtLink = "Acme Manufacturing",
            AccountStatusAtRead = "Active",
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-1),
        });

        db.Renewals.AddRange(
            new Renewal
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                BrokerId = brokerId,
                PolicyId = Guid.NewGuid(),
                AssignedToUserId = userId,
                CurrentStatus = "Completed",
                AccountDisplayNameAtLink = "Acme Manufacturing",
                AccountStatusAtRead = "Active",
                PolicyExpirationDate = now.AddDays(30),
                TargetOutreachDate = now.AddDays(-60),
                BoundPolicyId = Guid.NewGuid(),
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-20),
            },
            new Renewal
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                BrokerId = brokerId,
                PolicyId = Guid.NewGuid(),
                AssignedToUserId = userId,
                CurrentStatus = "Lost",
                AccountDisplayNameAtLink = "Acme Manufacturing",
                AccountStatusAtRead = "Active",
                PolicyExpirationDate = now.AddDays(30),
                TargetOutreachDate = now.AddDays(-60),
                LostReasonCode = "CompetitiveLoss",
                CreatedAt = now.AddDays(-500),
                UpdatedAt = now.AddDays(-400),
            });

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var adminUser = new TestCurrentUserService(userId, ["Admin"], ["West"]);
        var defaultWindowKpis = await repository.GetKpisAsync(adminUser, 0);
        var maxWindowKpis = await repository.GetKpisAsync(adminUser, 1000);

        defaultWindowKpis.RenewalRate.ShouldBe(100.0);
        maxWindowKpis.RenewalRate.ShouldBe(50.0);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"kpi-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedReferenceStatuses(AppDbContext db)
    {
        db.ReferenceSubmissionStatuses.AddRange(
            new ReferenceSubmissionStatus { Code = "Received", DisplayName = "Received", Description = "Received", IsTerminal = false, DisplayOrder = 1, ColorGroup = "intake" },
            new ReferenceSubmissionStatus { Code = "Bound", DisplayName = "Bound", Description = "Bound", IsTerminal = true, DisplayOrder = 2, ColorGroup = "decision" });

        db.ReferenceRenewalStatuses.AddRange(
            new ReferenceRenewalStatus { Code = "Identified", DisplayName = "Identified", Description = "Identified", IsTerminal = false, DisplayOrder = 1, ColorGroup = "intake" },
            new ReferenceRenewalStatus { Code = "Completed", DisplayName = "Completed", Description = "Completed", IsTerminal = true, DisplayOrder = 2, ColorGroup = "decision" },
            new ReferenceRenewalStatus { Code = "Lost", DisplayName = "Lost", Description = "Lost", IsTerminal = true, DisplayOrder = 3, ColorGroup = "decision" });
    }

    private sealed class TestCurrentUserService(
        Guid userId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> regions) : ICurrentUserService
    {
        public Guid UserId => userId;
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => roles;
        public IReadOnlyList<string> Regions => regions;
        public string? BrokerTenantId => null;
    }
}
