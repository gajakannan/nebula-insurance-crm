using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Tests.Integration;

/// <summary>
/// Integration tests verifying nudge priority ordering (OverdueTask &gt; StaleSubmission &gt; UpcomingRenewal)
/// and the 10-item cap returned by GET /dashboard/nudges.
/// F0001-S0005 acceptance criteria coverage.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class NudgePriorityTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Resolve or create the UserProfile matching the TestAuthHandler identity.
        const string iss = "http://test.local/application/o/nebula/";
        const string sub = "test-user-001";

        var profile = db.UserProfiles.FirstOrDefault(u => u.IdpIssuer == iss && u.IdpSubject == sub);
        if (profile is null)
        {
            var now = DateTime.UtcNow;
            profile = new UserProfile
            {
                IdpIssuer = iss,
                IdpSubject = sub,
                Email = "test@nebula.local",
                DisplayName = "Test User",
                Department = "Test",
                RolesJson = "[\"Admin\"]",
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();
        }

        var testUserId = profile.Id;
        var now2 = DateTime.UtcNow;

        // Minimal account + broker required as FKs for submission and renewal.
        var account = new Account
        {
            Name = "Nudge Priority Test Co",
            StableDisplayName = "Nudge Priority Test Co",
            Industry = "Technology",
            PrimaryState = "CA",
            Region = "West",
            Status = "Active",
            CreatedAt = now2,
            UpdatedAt = now2,
            CreatedByUserId = testUserId,
            UpdatedByUserId = testUserId,
        };
        db.Accounts.Add(account);

        var broker = new Broker
        {
            LegalName = "Nudge Priority Broker",
            LicenseNumber = $"NPT-{Guid.NewGuid().ToString("N")[..8]}",
            State = "CA",
            Status = "Active",
            CreatedAt = now2,
            UpdatedAt = now2,
            CreatedByUserId = testUserId,
            UpdatedByUserId = testUserId,
        };
        db.Brokers.Add(broker);

        await db.SaveChangesAsync();

        // Priority 1 seed: one overdue task assigned to test user.
        db.Tasks.Add(new TaskItem
        {
            Title = "Nudge-Test Overdue Task",
            Status = "Open",
            Priority = "High",
            DueDate = now2.Date.AddDays(-2),
            AssignedToUserId = testUserId,
            CreatedAt = now2.AddDays(-5),
            UpdatedAt = now2,
            CreatedByUserId = testUserId,
            UpdatedByUserId = testUserId,
        });

        // Priority 2 seed: stale submission (10 days since creation, no WorkflowTransitions → 10 days in status > threshold).
        db.Submissions.Add(new Submission
        {
            AccountId = account.Id,
            BrokerId = broker.Id,
            CurrentStatus = "Received",
            EffectiveDate = now2.Date.AddMonths(3),
            PremiumEstimate = 50_000m,
            AssignedToUserId = testUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now2.AddDays(-10),
            UpdatedAt = now2,
            CreatedByUserId = testUserId,
            UpdatedByUserId = testUserId,
        });

        var policy = new Policy
        {
            PolicyNumber = $"NPT-POL-{Guid.NewGuid().ToString("N")[..8]}",
            AccountId = account.Id,
            BrokerId = broker.Id,
            CarrierId = Guid.NewGuid(),
            Carrier = NewCarrierRef("Test Carrier"),
            LineOfBusiness = "Property",
            EffectiveDate = now2.Date.AddYears(-1),
            ExpirationDate = now2.Date.AddDays(7),
            TotalPremium = 50_000m,
            PremiumCurrency = "USD",
            CurrentStatus = "Issued",
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now2,
            UpdatedAt = now2,
            CreatedByUserId = testUserId,
            UpdatedByUserId = testUserId,
        };
        db.Policies.Add(policy);

        // Priority 3 seed: upcoming renewal due in 7 days.
        db.Renewals.Add(new Renewal
        {
            AccountId = account.Id,
            BrokerId = broker.Id,
            CurrentStatus = "Identified",
            PolicyId = policy.Id,
            PolicyExpirationDate = now2.Date.AddDays(7),
            TargetOutreachDate = now2.Date.AddDays(-83),
            AssignedToUserId = testUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now2,
            UpdatedAt = now2,
            CreatedByUserId = testUserId,
            UpdatedByUserId = testUserId,
        });

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CarrierRef NewCarrierRef(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{name} {Guid.NewGuid():N}",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetNudges_ReturnsAllThreeTypes_InPriorityOrder()
    {
        var response = await _client.GetAsync("/dashboard/nudges");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<NudgesResponseDto>();
        result.ShouldNotBeNull();

        var nudges = result!.Nudges;

        // All three seeded nudge types must be present.
        nudges.ShouldContain(n => n.NudgeType == "OverdueTask",
            "an overdue task was seeded for the test user");
        nudges.ShouldContain(n => n.NudgeType == "StaleSubmission",
            "a 10-day-old submission (>5 day threshold) was seeded for the test user");
        nudges.ShouldContain(n => n.NudgeType == "UpcomingRenewal",
            "a renewal due in 7 days was seeded for the test user");

        // Verify strict priority ordering: OverdueTask index < StaleSubmission index < UpcomingRenewal index.
        var overdueIdx = nudges
            .Select((n, i) => (n, i))
            .First(x => x.n.NudgeType == "OverdueTask").i;
        var staleIdx = nudges
            .Select((n, i) => (n, i))
            .First(x => x.n.NudgeType == "StaleSubmission").i;
        var upcomingIdx = nudges
            .Select((n, i) => (n, i))
            .First(x => x.n.NudgeType == "UpcomingRenewal").i;

        overdueIdx.ShouldBeLessThan(staleIdx,
            "OverdueTask (priority 1) must appear before StaleSubmission (priority 2)");
        staleIdx.ShouldBeLessThan(upcomingIdx,
            "StaleSubmission (priority 2) must appear before UpcomingRenewal (priority 3)");
    }

    [Fact]
    public async Task GetNudges_NeverExceedsTenItems()
    {
        var response = await _client.GetAsync("/dashboard/nudges");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<NudgesResponseDto>();
        result!.Nudges.Count.ShouldBeLessThanOrEqualTo(10,
            "server must cap nudges at 10 to allow client dismiss-and-replace from the pool");
    }

    [Fact]
    public async Task GetNudges_StaleSubmission_UsesWorkflowTransitionStaleness_NotUpdatedAt()
    {
        // The stale submission has CreatedAt = 10 days ago and no WorkflowTransitions.
        // Repository falls back to CreatedAt → 10 days in status, which exceeds the 5-day threshold.
        // This test confirms the submission appears as StaleSubmission (not filtered out).
        var response = await _client.GetAsync("/dashboard/nudges");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<NudgesResponseDto>();
        var stale = result!.Nudges.FirstOrDefault(n => n.NudgeType == "StaleSubmission");

        stale.ShouldNotBeNull("submission with CreatedAt 10 days ago must produce a StaleSubmission nudge (>5 day threshold)");
        stale!.UrgencyValue.ShouldBeGreaterThanOrEqualTo(10,
            "UrgencyValue (DaysInStatus) should reflect 10 days since the submission entered its current status");
    }
}
