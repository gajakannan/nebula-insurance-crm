using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class WorkflowEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string TestIssuer = "http://test.local/application/o/nebula/";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateSubmission_WithValidData_Returns201AndSetsCurrentUserAsOwner()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var (accountId, brokerId) = await SeedAccountAndBrokerAsync();

        var response = await _client.PostAsJsonAsync("/submissions", new SubmissionCreateDto(
            accountId,
            brokerId,
            DateTime.UtcNow.Date.AddDays(30),
            null,
            "Cyber",
            25000m,
            null,
            "Submission intake from integration test"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        result.ShouldNotBeNull();
        result!.CurrentStatus.ShouldBe("Received");
        result.AssignedToUserId.ShouldBe(currentUserId);
        result.RowVersion.ShouldNotBeNullOrWhiteSpace();
        result.AccountId.ShouldBe(accountId);
        result.BrokerId.ShouldBe(brokerId);
    }

    [Fact]
    public async Task GetSubmission_Existing_Returns200WithCompletenessPayload()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var submission = await SeedSubmissionAsync(currentUserId, "Received", "Cyber");

        var response = await _client.GetAsync($"/submissions/{submission.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(submission.Id);
        result.CurrentStatus.ShouldBe("Received");
        result.Completeness.ShouldNotBeNull();
        result.RowVersion.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ListSubmissions_FilteredByAccountId_ReturnsOnlySeededSubmission()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var submission = await SeedSubmissionAsync(currentUserId, "Received", "Cyber");

        var response = await _client.GetAsync($"/submissions?accountId={submission.AccountId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SubmissionListItemDto>>();
        result.ShouldNotBeNull();
        result!.Data.ShouldContain(item => item.Id == submission.Id);
    }

    [Fact]
    public async Task UpdateSubmission_WithIfMatch_Returns200AndPersistsChanges()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var created = await CreateSubmissionViaApiAsync(currentUserId);

        var response = await PutJsonAsync(
            $"/submissions/{created.Id}",
            new
            {
                lineOfBusiness = "Property",
                expirationDate = DateTime.UtcNow.Date.AddMonths(13),
                premiumEstimate = 41000m,
                description = "Updated description",
            },
            created.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        result.ShouldNotBeNull();
        result!.LineOfBusiness.ShouldBe("Property");
        result.Description.ShouldBe("Updated description");
        result.PremiumEstimate.ShouldBe(41000m);
    }

    [Fact]
    public async Task UpdateSubmission_WithExplicitNulls_ClearsOptionalFields()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var submission = await SeedSubmissionAsync(currentUserId, "Received", "Cyber");
        var detail = await GetSubmissionAsync(submission.Id);

        var response = await PutJsonAsync(
            $"/submissions/{submission.Id}",
            new
            {
                lineOfBusiness = (string?)null,
                expirationDate = (DateTime?)null,
                premiumEstimate = (decimal?)null,
                description = (string?)null,
            },
            detail.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        result.ShouldNotBeNull();
        result!.LineOfBusiness.ShouldBeNull();
        result.ExpirationDate.ShouldBeNull();
        result.PremiumEstimate.ShouldBeNull();
        result.Description.ShouldBeNull();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Submissions.SingleAsync(s => s.Id == submission.Id);
        persisted.LineOfBusiness.ShouldBeNull();
        persisted.ExpirationDate.ShouldBeNull();
        persisted.PremiumEstimate.ShouldBeNull();
        persisted.Description.ShouldBeNull();

        var timelineEvent = await db.ActivityTimelineEvents
            .Where(e => e.EntityType == "Submission"
                && e.EntityId == submission.Id
                && e.EventType == "SubmissionUpdated")
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync();
        timelineEvent.ShouldNotBeNull();
        timelineEvent!.EventPayloadJson.ShouldContain("lineOfBusiness");
        timelineEvent.EventPayloadJson.ShouldContain("description");
    }

    [Fact]
    public async Task PostSubmissionTransition_WithoutIfMatch_Returns412()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var created = await CreateSubmissionViaApiAsync(currentUserId);

        var response = await _client.PostAsJsonAsync(
            $"/submissions/{created.Id}/transitions",
            new WorkflowTransitionRequestDto("Triaging", "start review"));

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task PostSubmissionTransition_Valid_Returns201AndPersists()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var created = await CreateSubmissionViaApiAsync(currentUserId);

        var response = await PostJsonAsync(
            $"/submissions/{created.Id}/transitions",
            new WorkflowTransitionRequestDto("Triaging", "start review"),
            created.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WorkflowTransitionRecordDto>();
        result.ShouldNotBeNull();
        result!.FromState.ShouldBe("Received");
        result.ToState.ShouldBe("Triaging");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Submissions.SingleAsync(s => s.Id == created.Id);
        persisted.CurrentStatus.ShouldBe("Triaging");
    }

    [Fact]
    public async Task PostSubmissionTransition_ToReadyForUwReview_WhenIncomplete_Returns409()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var created = await CreateSubmissionViaApiAsync(currentUserId);
        var triaged = await PostJsonAsync(
            $"/submissions/{created.Id}/transitions",
            new WorkflowTransitionRequestDto("Triaging", "start review"),
            created.RowVersion);
        triaged.StatusCode.ShouldBe(HttpStatusCode.Created);

        var refreshed = await GetSubmissionAsync(created.Id);
        var response = await PostJsonAsync(
            $"/submissions/{created.Id}/transitions",
            new WorkflowTransitionRequestDto("ReadyForUWReview", "handoff"),
            refreshed.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().ShouldBe("missing_transition_prerequisite");
    }

    [Fact]
    public async Task PutSubmissionAssignment_ReadyForUwReview_ToNonUnderwriter_Returns400()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var submission = await SeedSubmissionAsync(currentUserId, "ReadyForUWReview", "Cyber");
        var assignee = await SeedUserProfileAsync($"dist-user-{Guid.NewGuid():N}", "DistributionUser", true);
        var detail = await GetSubmissionAsync(submission.Id);

        var response = await PutJsonAsync(
            $"/submissions/{submission.Id}/assignment",
            new SubmissionAssignmentRequestDto(assignee.Id),
            detail.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().ShouldBe("invalid_assignee");
    }

    [Fact]
    public async Task GetSubmissionTimeline_ReturnsPagedEvents()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var created = await CreateSubmissionViaApiAsync(currentUserId);

        var response = await _client.GetAsync($"/submissions/{created.Id}/timeline?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<TimelineEventDto>>();
        result.ShouldNotBeNull();
        result!.Data.ShouldContain(item => item.EventType == "SubmissionCreated");
    }

    [Fact]
    public async Task CreateRenewal_WithValidPolicy_Returns201WithPolicyContext()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var policy = await SeedPolicyAsync(currentUserId, expirationDays: 45, lineOfBusiness: "Property");

        var response = await _client.PostAsJsonAsync("/renewals", new RenewalCreateDto(
            policy.Id,
            null,
            null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RenewalDto>();
        result.ShouldNotBeNull();
        result!.PolicyId.ShouldBe(policy.Id);
        result.CurrentStatus.ShouldBe("Identified");
        result.AssignedToUserId.ShouldBe(currentUserId);
        result.PolicyNumber.ShouldBe(policy.PolicyNumber);
        result.PolicyCarrier.ShouldBe(policy.Carrier?.Name);
        result.AccountName.ShouldNotBeNullOrWhiteSpace();
        result.BrokerName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateRenewal_ForPolicyWithExistingActiveRenewal_Returns409()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var policy = await SeedPolicyAsync(currentUserId, expirationDays: 60, lineOfBusiness: "Cyber");
        await SeedRenewalAsync(currentUserId, "Identified", policy);

        var response = await _client.PostAsJsonAsync("/renewals", new RenewalCreateDto(
            policy.Id,
            null,
            null));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().ShouldBe("duplicate_renewal");
    }

    [Fact]
    public async Task ListRenewals_WithUrgencyFilter_ReturnsMatchingRenewal()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var overduePolicy = await SeedPolicyAsync(currentUserId, expirationDays: 15, lineOfBusiness: "Property");
        var onTrackPolicy = await SeedPolicyAsync(currentUserId, expirationDays: 180, lineOfBusiness: "Property");
        var overdueRenewal = await SeedRenewalAsync(currentUserId, "Identified", overduePolicy);
        await SeedRenewalAsync(currentUserId, "Identified", onTrackPolicy);

        var response = await _client.GetAsync("/renewals?urgency=overdue&page=1&pageSize=25");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<RenewalListItemDto>>();
        result.ShouldNotBeNull();
        result!.Data.ShouldContain(item => item.Id == overdueRenewal.Id);
        result.Data.ShouldAllBe(item => item.Urgency == "overdue");
    }

    [Fact]
    public async Task GetRenewal_Existing_ReturnsDetailWithPolicyAccountAndBrokerContext()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var policy = await SeedPolicyAsync(currentUserId, expirationDays: 75, lineOfBusiness: "WorkersCompensation");
        var renewal = await SeedRenewalAsync(currentUserId, "Identified", policy);

        var response = await _client.GetAsync($"/renewals/{renewal.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RenewalDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(renewal.Id);
        result.PolicyNumber.ShouldBe(policy.PolicyNumber);
        result.PolicyCarrier.ShouldBe(policy.Carrier?.Name);
        result.AccountIndustry.ShouldNotBeNullOrWhiteSpace();
        result.BrokerLicenseNumber.ShouldNotBeNullOrWhiteSpace();
        result.AvailableTransitions.ShouldContain("Outreach");
    }

    [Fact]
    public async Task PostRenewalTransition_WithoutIfMatch_Returns412()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var policy = await SeedPolicyAsync(currentUserId, expirationDays: 60);
        var renewal = await SeedRenewalAsync(currentUserId, "Identified", policy);

        var response = await _client.PostAsJsonAsync(
            $"/renewals/{renewal.Id}/transitions",
            new RenewalTransitionRequestDto("Outreach", "starting outreach"));

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task PostRenewalTransition_Valid_Returns201AndPersists()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var policy = await SeedPolicyAsync(currentUserId, expirationDays: 60);
        var renewal = await SeedRenewalAsync(currentUserId, "Identified", policy);
        var detail = await GetRenewalAsync(renewal.Id);

        var response = await PostJsonAsync(
            $"/renewals/{renewal.Id}/transitions",
            new RenewalTransitionRequestDto("Outreach", "starting outreach"),
            detail.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WorkflowTransitionRecordDto>();
        result.ShouldNotBeNull();
        result!.FromState.ShouldBe("Identified");
        result.ToState.ShouldBe("Outreach");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Renewals.SingleAsync(entity => entity.Id == renewal.Id);
        persisted.CurrentStatus.ShouldBe("Outreach");
    }

    [Fact]
    public async Task PutRenewalAssignment_AsDistributionUser_Returns403()
    {
        var previousSubject = TestAuthHandler.TestSubject;
        var previousRole = TestAuthHandler.TestRole;
        var previousRoles = TestAuthHandler.TestNebulaRoles;

        try
        {
            TestAuthHandler.TestSubject = $"dist-user-{Guid.NewGuid():N}";
            TestAuthHandler.TestRole = "DistributionUser";
            TestAuthHandler.TestNebulaRoles = ["DistributionUser"];

            var currentUserId = await EnsureCurrentUserProfileAsync();
            var policy = await SeedPolicyAsync(currentUserId, expirationDays: 45);
            var renewal = await SeedRenewalAsync(currentUserId, "Identified", policy);
            var detail = await GetRenewalAsync(renewal.Id);

            var response = await PutJsonAsync(
                $"/renewals/{renewal.Id}/assignment",
                new RenewalAssignmentRequestDto(currentUserId),
                detail.RowVersion);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
            payload.GetProperty("code").GetString().ShouldBe("policy_denied");
        }
        finally
        {
            TestAuthHandler.TestSubject = previousSubject;
            TestAuthHandler.TestRole = previousRole;
            TestAuthHandler.TestNebulaRoles = previousRoles;
        }
    }

    [Fact]
    public async Task PutRenewalAssignment_AsDistributionManager_Returns200AndPersists()
    {
        var previousSubject = TestAuthHandler.TestSubject;
        var previousRole = TestAuthHandler.TestRole;
        var previousRoles = TestAuthHandler.TestNebulaRoles;

        try
        {
            TestAuthHandler.TestSubject = $"dist-manager-{Guid.NewGuid():N}";
            TestAuthHandler.TestRole = "DistributionManager";
            TestAuthHandler.TestNebulaRoles = ["DistributionManager"];

            var currentUserId = await EnsureCurrentUserProfileAsync();
            var assignee = await SeedUserProfileAsync($"dist-assignee-{Guid.NewGuid():N}", "DistributionUser", true);
            var policy = await SeedPolicyAsync(currentUserId, expirationDays: 45);
            var renewal = await SeedRenewalAsync(currentUserId, "Outreach", policy);
            var detail = await GetRenewalAsync(renewal.Id);

            var response = await PutJsonAsync(
                $"/renewals/{renewal.Id}/assignment",
                new RenewalAssignmentRequestDto(assignee.Id),
                detail.RowVersion);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<RenewalDto>();
            result.ShouldNotBeNull();
            result!.AssignedToUserId.ShouldBe(assignee.Id);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await db.Renewals.SingleAsync(entity => entity.Id == renewal.Id);
            persisted.AssignedToUserId.ShouldBe(assignee.Id);
        }
        finally
        {
            TestAuthHandler.TestSubject = previousSubject;
            TestAuthHandler.TestRole = previousRole;
            TestAuthHandler.TestNebulaRoles = previousRoles;
        }
    }

    [Fact]
    public async Task GetRenewalTimeline_ReturnsPagedEvents()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var policy = await SeedPolicyAsync(currentUserId, expirationDays: 45);
        var renewal = await SeedRenewalAsync(currentUserId, "Identified", policy);

        var response = await _client.GetAsync($"/renewals/{renewal.Id}/timeline?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<TimelineEventDto>>();
        result.ShouldNotBeNull();
        result!.Data.ShouldContain(item => item.EventType == "RenewalCreated");
    }

    private async Task<SubmissionDto> CreateSubmissionViaApiAsync(Guid currentUserId)
    {
        var (accountId, brokerId) = await SeedAccountAndBrokerAsync();
        var response = await _client.PostAsJsonAsync("/submissions", new SubmissionCreateDto(
            accountId,
            brokerId,
            DateTime.UtcNow.Date.AddDays(30),
            null,
            "Cyber",
            25000m,
            null,
            "Submission intake from integration test"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
    }

    private async Task<RenewalDto> GetRenewalAsync(Guid renewalId)
    {
        var response = await _client.GetAsync($"/renewals/{renewalId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<RenewalDto>())!;
    }

    private async Task<SubmissionDto> GetSubmissionAsync(Guid submissionId)
    {
        var response = await _client.GetAsync($"/submissions/{submissionId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
    }

    private async Task<Policy> SeedPolicyAsync(
        Guid createdByUserId,
        string region = "West",
        string lineOfBusiness = "Property",
        int expirationDays = 45)
    {
        var (accountId, brokerId) = await SeedAccountAndBrokerAsync(region);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Accounts.SingleAsync(entity => entity.Id == accountId);
        var broker = await db.Brokers.SingleAsync(entity => entity.Id == brokerId);
        var now = DateTime.UtcNow;
        var carrier = NewCarrierRef("Acme Carrier");

        var policy = new Policy
        {
            PolicyNumber = $"POL-{Guid.NewGuid():N}"[..12],
            AccountId = accountId,
            BrokerId = brokerId,
            CarrierId = carrier.Id,
            Carrier = carrier,
            LineOfBusiness = lineOfBusiness,
            EffectiveDate = now.Date.AddDays(expirationDays - 365),
            ExpirationDate = now.Date.AddDays(expirationDays),
            TotalPremium = 125000m,
            PremiumCurrency = "USD",
            CurrentStatus = "Issued",
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            Account = account,
            Broker = broker,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId,
        };

        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy;
    }

    private static CarrierRef NewCarrierRef(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{name} {Guid.NewGuid():N}",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private async Task<Renewal> SeedRenewalAsync(Guid assignedToUserId, string status, Policy policy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var account = await db.Accounts.SingleAsync(entity => entity.Id == policy.AccountId);
        var renewal = new Renewal
        {
            AccountId = policy.AccountId,
            BrokerId = policy.BrokerId,
            PolicyId = policy.Id,
            CurrentStatus = status,
            LineOfBusiness = policy.LineOfBusiness,
            PolicyExpirationDate = policy.ExpirationDate,
            TargetOutreachDate = policy.ExpirationDate.AddDays(-GetRenewalTargetDays(policy.LineOfBusiness)),
            AssignedToUserId = assignedToUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = assignedToUserId,
            UpdatedByUserId = assignedToUserId,
        };

        db.Renewals.Add(renewal);
        db.WorkflowTransitions.Add(new WorkflowTransition
        {
            WorkflowType = "Renewal",
            EntityId = renewal.Id,
            FromState = null,
            ToState = status,
            ActorUserId = assignedToUserId,
            OccurredAt = now,
        });
        db.ActivityTimelineEvents.Add(new ActivityTimelineEvent
        {
            EntityType = "Renewal",
            EntityId = renewal.Id,
            EventType = "RenewalCreated",
            EventDescription = $"Renewal created from policy {policy.PolicyNumber}",
            ActorUserId = assignedToUserId,
            ActorDisplayName = "Test User",
            OccurredAt = now,
        });
        await db.SaveChangesAsync();
        return renewal;
    }

    private async Task<(Guid AccountId, Guid BrokerId)> SeedAccountAndBrokerAsync(string region = "West")
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var account = new Account
        {
            Name = $"Submission Account {Guid.NewGuid():N}",
            StableDisplayName = string.Empty,
            Industry = "Technology",
            PrimaryState = "CA",
            Region = region,
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = currentUserId,
            UpdatedByUserId = currentUserId,
        };
        account.StableDisplayName = account.Name;

        var broker = new Broker
        {
            LegalName = $"Submission Broker {Guid.NewGuid():N}",
            LicenseNumber = $"SUB-{Guid.NewGuid():N}"[..12],
            State = "CA",
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = currentUserId,
            UpdatedByUserId = currentUserId,
        };

        db.Accounts.Add(account);
        db.Brokers.Add(broker);
        db.BrokerRegions.Add(new BrokerRegion
        {
            BrokerId = broker.Id,
            Region = region,
        });
        await db.SaveChangesAsync();

        return (account.Id, broker.Id);
    }

    private async Task<Submission> SeedSubmissionAsync(Guid assignedToUserId, string status, string lineOfBusiness)
    {
        var (accountId, brokerId) = await SeedAccountAndBrokerAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var account = await db.Accounts.SingleAsync(entity => entity.Id == accountId);

        var submission = new Submission
        {
            AccountId = accountId,
            BrokerId = brokerId,
            CurrentStatus = status,
            EffectiveDate = now.Date.AddDays(30),
            ExpirationDate = now.Date.AddMonths(12),
            LineOfBusiness = lineOfBusiness,
            PremiumEstimate = 25000m,
            Description = "Seeded submission",
            AssignedToUserId = assignedToUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = assignedToUserId,
            UpdatedByUserId = assignedToUserId,
        };

        db.Submissions.Add(submission);
        db.WorkflowTransitions.Add(new WorkflowTransition
        {
            WorkflowType = "Submission",
            EntityId = submission.Id,
            FromState = null,
            ToState = status,
            ActorUserId = assignedToUserId,
            OccurredAt = now,
        });
        db.ActivityTimelineEvents.Add(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submission.Id,
            EventType = "SubmissionCreated",
            EventDescription = "Submission created",
            ActorUserId = assignedToUserId,
            ActorDisplayName = "Test User",
            OccurredAt = now,
        });
        await db.SaveChangesAsync();

        return submission;
    }

    private async Task<Guid> EnsureCurrentUserProfileAsync()
    {
        var roles = TestAuthHandler.TestNebulaRoles ?? [TestAuthHandler.TestRole];
        var existing = await GetUserProfileAsync(TestAuthHandler.TestSubject);
        if (existing is not null)
            return existing.Id;

        var seeded = await SeedUserProfileAsync(TestAuthHandler.TestSubject, roles[0], true);
        return seeded.Id;
    }

    private async Task<UserProfile?> GetUserProfileAsync(string subject)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.UserProfiles.FirstOrDefaultAsync(profile =>
            profile.IdpIssuer == TestIssuer && profile.IdpSubject == subject);
    }

    private async Task<UserProfile> SeedUserProfileAsync(string subject, string role, bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.UserProfiles.FirstOrDefaultAsync(profile =>
            profile.IdpIssuer == TestIssuer && profile.IdpSubject == subject);
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var profile = new UserProfile
        {
            IdpIssuer = TestIssuer,
            IdpSubject = subject,
            Email = $"{subject}@example.test",
            DisplayName = subject,
            Department = "QA",
            RegionsJson = "[\"West\"]",
            RolesJson = JsonSerializer.Serialize(new[] { role }),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    private async Task<HttpResponseMessage> PutJsonAsync<TBody>(string url, TBody body, string rowVersion)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{rowVersion}\"");
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostJsonAsync<TBody>(string url, TBody body, string rowVersion)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{rowVersion}\"");
        return await _client.SendAsync(request);
    }

    private static int GetRenewalTargetDays(string? lineOfBusiness) => lineOfBusiness switch
    {
        "WorkersCompensation" => 120,
        "Cyber" => 60,
        _ => 90,
    };
}
