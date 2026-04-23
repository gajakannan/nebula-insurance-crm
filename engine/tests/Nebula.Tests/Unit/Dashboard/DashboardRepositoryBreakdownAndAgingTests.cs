using Shouldly;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;
using OpportunityProgram = Nebula.Domain.Entities.Program;

namespace Nebula.Tests.Unit.Dashboard;

public class DashboardRepositoryBreakdownAndAgingTests
{
    [Fact]
    public async Task GetOpportunityBreakdownAsync_Submission_SupportsAllGroupByDimensions()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var userA = NewUser("Alice Agent");
        var userB = NewUser("Bob Broker");
        var brokerA = NewBroker("Atlas Brokerage", "CA");
        var brokerB = NewBroker("Beacon Brokerage", "TX");
        var account = NewAccount();
        var programA = NewProgram("Property Shield");
        var programB = NewProgram("Liability Prime");

        db.UserProfiles.AddRange(userA, userB);
        db.Brokers.AddRange(brokerA, brokerB);
        db.Accounts.Add(account);
        db.Programs.AddRange(programA, programB);

        db.Submissions.AddRange(
            NewSubmission(account.Id, brokerA.Id, programA.Id, userA.Id, "Received", "Property", now.AddDays(-5)),
            NewSubmission(account.Id, brokerB.Id, programA.Id, userA.Id, "Received", "CommercialAuto", now.AddDays(-4)),
            NewSubmission(account.Id, brokerB.Id, programB.Id, userB.Id, "Received", null, now.AddDays(-3)),
            NewSubmission(account.Id, brokerA.Id, programA.Id, userA.Id, "Quoted", "Property", now.AddDays(-2)));

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var currentUser = new TestCurrentUserService(Guid.NewGuid(), ["Admin"], ["West"]);

        var assignedUser = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "assigneduser", 180);
        var broker = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "broker", 180);
        var program = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "program", 180);
        var lineOfBusiness = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "lineofbusiness", 180);
        var brokerState = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "brokerstate", 180);

        assignedUser.Total.ShouldBe(3);
        assignedUser.Groups.ShouldContain(g => g.Key == "Alice Agent" && g.Label == "Alice Agent" && g.Count == 2);
        assignedUser.Groups.ShouldContain(g => g.Key == "Bob Broker" && g.Label == "Bob Broker" && g.Count == 1);

        broker.Groups.ShouldContain(g => g.Key == "Atlas Brokerage" && g.Label == "Atlas Brokerage" && g.Count == 1);
        broker.Groups.ShouldContain(g => g.Key == "Beacon Brokerage" && g.Label == "Beacon Brokerage" && g.Count == 2);

        program.Groups.ShouldContain(g => g.Key == "Property Shield" && g.Label == "Property Shield" && g.Count == 2);
        program.Groups.ShouldContain(g => g.Key == "Liability Prime" && g.Label == "Liability Prime" && g.Count == 1);

        lineOfBusiness.Groups.ShouldContain(g => g.Key == "Property" && g.Label == "Property" && g.Count == 1);
        lineOfBusiness.Groups.ShouldContain(g => g.Key == "CommercialAuto" && g.Label == "Commercial Auto" && g.Count == 1);
        lineOfBusiness.Groups.ShouldContain(g => g.Key == null && g.Label == "Unknown" && g.Count == 1);

        brokerState.Groups.ShouldContain(g => g.Key == "CA" && g.Label == "CA" && g.Count == 1);
        brokerState.Groups.ShouldContain(g => g.Key == "TX" && g.Label == "TX" && g.Count == 2);
    }

    [Fact]
    public async Task GetOpportunityBreakdownAsync_Renewal_ResolvesProgramAndUnknownGroups()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var userA = NewUser("Alice Agent");
        var userB = NewUser("Bob Broker");
        var brokerA = NewBroker("Atlas Brokerage", "CA");
        var brokerB = NewBroker("Beacon Brokerage", "TX");
        var account = NewAccount();
        var programA = NewProgram("Property Shield");
        var programB = NewProgram("Liability Prime");
        var submissionA = NewSubmission(account.Id, brokerA.Id, programA.Id, userA.Id, "Bound", "Property", now.AddDays(-15));
        var submissionB = NewSubmission(account.Id, brokerB.Id, programB.Id, userB.Id, "Bound", "GeneralLiability", now.AddDays(-14));

        db.UserProfiles.AddRange(userA, userB);
        db.Brokers.AddRange(brokerA, brokerB);
        db.Accounts.Add(account);
        db.Programs.AddRange(programA, programB);
        db.Submissions.AddRange(submissionA, submissionB);

        db.Renewals.AddRange(
            NewRenewal(account.Id, brokerA.Id, submissionA.Id, userA.Id, "Identified", "Cyber", now.AddDays(-5)),
            NewRenewal(account.Id, brokerB.Id, submissionB.Id, userB.Id, "Identified", null, now.AddDays(-4)),
            NewRenewal(account.Id, brokerB.Id, null, userB.Id, "Identified", "ProfessionalLiability", now.AddDays(-3)));

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var currentUser = new TestCurrentUserService(Guid.NewGuid(), ["Admin"], ["West"]);
        var programBreakdown = await repository.GetOpportunityBreakdownAsync(currentUser, "renewal", "Identified", "program", 180);

        programBreakdown.Total.ShouldBe(3);
        programBreakdown.Groups.ShouldContain(g => g.Key == "Property Shield" && g.Label == "Property Shield" && g.Count == 1);
        programBreakdown.Groups.ShouldContain(g => g.Key == "Liability Prime" && g.Label == "Liability Prime" && g.Count == 1);
        programBreakdown.Groups.ShouldContain(g => g.Key == null && g.Label == "Unknown" && g.Count == 1);
    }

    [Fact]
    public async Task GetOpportunityAgingAsync_ComputesSlaBands_WithBoundaryCoverage()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var account = NewAccount();
        var broker = NewBroker("Atlas Brokerage", "CA");
        var assignee = Guid.NewGuid();

        db.Accounts.Add(account);
        db.Brokers.Add(broker);
        db.ReferenceSubmissionStatuses.AddRange(
            new ReferenceSubmissionStatus
            {
                Code = "Triaging",
                DisplayName = "Triaging",
                Description = "Triaging",
                IsTerminal = false,
                DisplayOrder = 1,
                ColorGroup = "triage",
            },
            new ReferenceSubmissionStatus
            {
                Code = "Bound",
                DisplayName = "Bound",
                Description = "Bound",
                IsTerminal = true,
                DisplayOrder = 2,
                ColorGroup = "won",
            });

        db.WorkflowSlaThresholds.Add(new WorkflowSlaThreshold
        {
            Id = Guid.NewGuid(),
            EntityType = "submission",
            Status = "Triaging",
            WarningDays = 2,
            TargetDays = 5,
            CreatedAt = now,
            UpdatedAt = now,
        });

        db.Submissions.AddRange(
            NewSubmission(account.Id, broker.Id, null, assignee, "Triaging", "Property", now.AddDays(-2).AddMinutes(-5)),
            NewSubmission(account.Id, broker.Id, null, assignee, "Triaging", "Property", now.AddDays(-3).AddMinutes(-5)),
            NewSubmission(account.Id, broker.Id, null, assignee, "Triaging", "Property", now.AddDays(-5).AddMinutes(-5)),
            NewSubmission(account.Id, broker.Id, null, assignee, "Triaging", "Property", now.AddDays(-6).AddMinutes(-5)));

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var currentUser = new TestCurrentUserService(Guid.NewGuid(), ["Admin"], ["West"]);
        var result = await repository.GetOpportunityAgingAsync(currentUser, "submission", 180);
        var triaging = result.Statuses.Single(status => status.Status == "Triaging");

        triaging.Total.ShouldBe(4);
        triaging.Sla.ShouldNotBeNull();
        triaging.Sla!.WarningDays.ShouldBe(2);
        triaging.Sla.TargetDays.ShouldBe(5);
        triaging.Sla.OnTimeCount.ShouldBe(1);
        triaging.Sla.ApproachingCount.ShouldBe(2);
        triaging.Sla.OverdueCount.ShouldBe(1);
        triaging.Sla.OnTimeCount.ShouldBe(triaging.Total - triaging.Sla.ApproachingCount - triaging.Sla.OverdueCount);
    }

    [Fact]
    public async Task OpportunityWindowing_KeepsFlowBreakdownAndAgingAligned()
    {
        await using var db = CreateContext();
        var now = DateTime.UtcNow;
        var account = NewAccount();
        var recentBroker = NewBroker("Atlas Brokerage", "CA");
        var olderBroker = NewBroker("Beacon Brokerage", "TX");
        var assignee = Guid.NewGuid();

        db.Accounts.Add(account);
        db.Brokers.AddRange(recentBroker, olderBroker);
        db.ReferenceSubmissionStatuses.AddRange(
            new ReferenceSubmissionStatus
            {
                Code = "Received",
                DisplayName = "Received",
                Description = "Received",
                IsTerminal = false,
                DisplayOrder = 1,
                ColorGroup = "intake",
            },
            new ReferenceSubmissionStatus
            {
                Code = "Bound",
                DisplayName = "Bound",
                Description = "Bound",
                IsTerminal = true,
                DisplayOrder = 2,
                ColorGroup = "won",
            });

        db.Submissions.AddRange(
            NewSubmission(account.Id, recentBroker.Id, null, assignee, "Received", "Property", now.AddDays(-10)),
            NewSubmission(account.Id, olderBroker.Id, null, assignee, "Received", "Property", now.AddDays(-45)));

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var currentUser = new TestCurrentUserService(Guid.NewGuid(), ["Admin"], ["West"]);

        var flow30 = await repository.GetOpportunityFlowAsync(currentUser, "submission", 30);
        var breakdown30 = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "broker", 30);
        var aging30 = await repository.GetOpportunityAgingAsync(currentUser, "submission", 30);

        flow30.Nodes.Single(node => node.Status == "Received").CurrentCount.ShouldBe(1);
        breakdown30.Total.ShouldBe(1);
        var singleGroup = breakdown30.Groups.Single(group => group.Label == recentBroker.LegalName && group.Count == 1);
        singleGroup.ShouldNotBeNull();
        breakdown30.Groups.Count().ShouldBe(1);
        aging30.Statuses.Single(status => status.Status == "Received").Total.ShouldBe(1);

        var flow90 = await repository.GetOpportunityFlowAsync(currentUser, "submission", 90);
        var breakdown90 = await repository.GetOpportunityBreakdownAsync(currentUser, "submission", "Received", "broker", 90);
        var aging90 = await repository.GetOpportunityAgingAsync(currentUser, "submission", 90);

        flow90.Nodes.Single(node => node.Status == "Received").CurrentCount.ShouldBe(2);
        breakdown90.Total.ShouldBe(2);
        breakdown90.Groups.ShouldContain(g => g.Key == recentBroker.LegalName && g.Label == recentBroker.LegalName && g.Count == 1);
        breakdown90.Groups.ShouldContain(g => g.Key == olderBroker.LegalName && g.Label == olderBroker.LegalName && g.Count == 1);
        aging90.Statuses.Single(status => status.Status == "Received").Total.ShouldBe(2);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"breakdown-aging-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static Account NewAccount() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Acme Manufacturing",
        StableDisplayName = "Acme Manufacturing",
        Industry = "Manufacturing",
        PrimaryState = "CA",
        Region = "West",
        Status = "Active",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid(),
        UpdatedByUserId = Guid.NewGuid(),
    };

    private static Broker NewBroker(string legalName, string state) => new()
    {
        Id = Guid.NewGuid(),
        LegalName = legalName,
        LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
        State = state,
        Status = "Active",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid(),
        UpdatedByUserId = Guid.NewGuid(),
    };

    private static OpportunityProgram NewProgram(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        ProgramCode = $"PRG-{Guid.NewGuid():N}"[..12],
        MgaId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid(),
        UpdatedByUserId = Guid.NewGuid(),
    };

    private static UserProfile NewUser(string displayName)
    {
        var normalized = displayName.Replace(" ", ".").ToLowerInvariant();
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            IdpIssuer = "http://test.local/application/o/nebula/",
            IdpSubject = normalized,
            Email = $"{normalized}@nebula.test",
            DisplayName = displayName,
            Department = "Distribution",
            RegionsJson = "[\"West\"]",
            RolesJson = "[\"Admin\"]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static Submission NewSubmission(
        Guid accountId,
        Guid brokerId,
        Guid? programId,
        Guid assignedToUserId,
        string status,
        string? lineOfBusiness,
        DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        BrokerId = brokerId,
        ProgramId = programId,
        LineOfBusiness = lineOfBusiness,
        CurrentStatus = status,
        AccountDisplayNameAtLink = "Acme Manufacturing",
        AccountStatusAtRead = "Active",
        EffectiveDate = DateTime.UtcNow.Date,
        PremiumEstimate = 125000m,
        AssignedToUserId = assignedToUserId,
        CreatedAt = createdAt,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = assignedToUserId,
        UpdatedByUserId = assignedToUserId,
    };

    private static Renewal NewRenewal(
        Guid accountId,
        Guid brokerId,
        Guid? renewalSubmissionId,
        Guid assignedToUserId,
        string status,
        string? lineOfBusiness,
        DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        BrokerId = brokerId,
        PolicyId = Guid.NewGuid(),
        RenewalSubmissionId = renewalSubmissionId,
        LineOfBusiness = lineOfBusiness,
        CurrentStatus = status,
        AccountDisplayNameAtLink = "Acme Manufacturing",
        AccountStatusAtRead = "Active",
        PolicyExpirationDate = DateTime.UtcNow.Date.AddDays(30),
        TargetOutreachDate = DateTime.UtcNow.Date.AddDays(-60),
        AssignedToUserId = assignedToUserId,
        CreatedAt = createdAt,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = assignedToUserId,
        UpdatedByUserId = assignedToUserId,
    };

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
