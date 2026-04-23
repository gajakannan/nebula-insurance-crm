using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class AccountEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string TestIssuer = "http://test.local/application/o/nebula/";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateAccount_ListSummaryAndWorkspaceRails_ReturnExpectedData()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var created = await CreateAccountViaApiAsync($"Account 360 {Guid.NewGuid():N}"[..20]);
        var brokerId = await SeedBrokerAsync(currentUserId);
        var submission = await SeedSubmissionAsync(currentUserId, created.Id, brokerId, "Received", "Cyber");
        var policy = await SeedPolicyAsync(currentUserId, created.Id, brokerId, "Property", 45);
        var renewal = await SeedRenewalAsync(currentUserId, created.Id, brokerId, policy.Id, "Property", "Identified");

        var listResponse = await _client.GetAsync($"/accounts?include=summary&q={Uri.EscapeDataString(created.DisplayName)}&page=1&pageSize=10");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PageEnvelope<AccountListItemDto>>();
        list.ShouldNotBeNull();

        var listItem = list!.Data.Single(item => item.Id == created.Id);
        listItem.Status.ShouldBe(AccountStatuses.Active);
        listItem.ActivePolicyCount.ShouldBe(1);
        listItem.OpenSubmissionCount.ShouldBe(1);
        listItem.RenewalDueCount.ShouldBe(1);

        var summaryResponse = await _client.GetAsync($"/accounts/{created.Id}/summary");
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<AccountSummaryDto>();
        summary.ShouldNotBeNull();
        summary!.ActivePolicyCount.ShouldBe(1);
        summary.OpenSubmissionCount.ShouldBe(1);
        summary.RenewalDueCount.ShouldBe(1);

        var policiesResponse = await _client.GetAsync($"/accounts/{created.Id}/policies?page=1&pageSize=10");
        policiesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var policies = await policiesResponse.Content.ReadFromJsonAsync<PageEnvelope<AccountPolicyListItemDto>>();
        policies.ShouldNotBeNull();
        policies!.Data.ShouldContain(item => item.Id == policy.Id);

        var submissionsResponse = await _client.GetAsync($"/accounts/{created.Id}/submissions?page=1&pageSize=10");
        submissionsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var submissions = await submissionsResponse.Content.ReadFromJsonAsync<PaginatedResult<SubmissionListItemDto>>();
        submissions.ShouldNotBeNull();
        submissions!.Data.ShouldContain(item =>
            item.Id == submission.Id
            && item.AccountId == created.Id
            && item.AccountDisplayName == created.StableDisplayName
            && item.AccountStatus == AccountStatuses.Active);

        var renewalsResponse = await _client.GetAsync($"/accounts/{created.Id}/renewals?page=1&pageSize=10");
        renewalsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renewals = await renewalsResponse.Content.ReadFromJsonAsync<PaginatedResult<RenewalListItemDto>>();
        renewals.ShouldNotBeNull();
        renewals!.Data.ShouldContain(item =>
            item.Id == renewal.Id
            && item.AccountId == created.Id
            && item.AccountDisplayName == created.StableDisplayName
            && item.AccountStatus == AccountStatuses.Active);

        var timelineResponse = await _client.GetAsync($"/accounts/{created.Id}/timeline?page=1&pageSize=10");
        timelineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<PaginatedResult<TimelineEventDto>>();
        timeline.ShouldNotBeNull();
        timeline!.Data.ShouldContain(item => item.EventType == "AccountCreated");
    }

    [Fact]
    public async Task ChangeRelationship_RecordsHistoryAndTimeline()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var accountId = await SeedAccountAsync(currentUserId, $"Relationship {Guid.NewGuid():N}"[..20]);
        var brokerId = await SeedBrokerAsync(currentUserId);
        var detail = await GetAccountAsync(accountId);

        var response = await PostJsonAsync(
            $"/accounts/{accountId}/relationships",
            new AccountRelationshipRequestDto("BrokerOfRecord", brokerId.ToString(), "alignment update"),
            detail.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>();
        updated.ShouldNotBeNull();
        updated!.BrokerOfRecordId.ShouldBe(brokerId);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var history = await db.AccountRelationshipHistory
                .SingleAsync(entry => entry.AccountId == accountId && entry.RelationshipType == "BrokerOfRecord");
            history.NewValue.ShouldBe(brokerId.ToString());
            history.Notes.ShouldBe("alignment update");
        }

        var timelineResponse = await _client.GetAsync($"/accounts/{accountId}/timeline?page=1&pageSize=10");
        timelineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<PaginatedResult<TimelineEventDto>>();
        timeline.ShouldNotBeNull();
        timeline!.Data.ShouldContain(item => item.EventType == "AccountRelationshipChanged");
    }

    [Fact]
    public async Task ContactCrud_EnforcesPrimaryUniquenessAndSoftDelete()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var accountId = await SeedAccountAsync(currentUserId, $"Contacts {Guid.NewGuid():N}"[..20]);

        var createPrimaryResponse = await _client.PostAsJsonAsync(
            $"/accounts/{accountId}/contacts",
            new AccountContactRequestDto("Pat Primary", "Risk Manager", "pat.primary@example.test", "555-0100", true));

        createPrimaryResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var primaryContact = await createPrimaryResponse.Content.ReadFromJsonAsync<AccountContactDto>();
        primaryContact.ShouldNotBeNull();

        var duplicatePrimaryResponse = await _client.PostAsJsonAsync(
            $"/accounts/{accountId}/contacts",
            new AccountContactRequestDto("Taylor Primary", "Controller", "taylor.primary@example.test", "555-0101", true));

        duplicatePrimaryResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var listResponse = await _client.GetAsync($"/accounts/{accountId}/contacts?page=1&pageSize=10");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contacts = await listResponse.Content.ReadFromJsonAsync<PageEnvelope<AccountContactDto>>();
        contacts.ShouldNotBeNull();
        contacts!.Data.Count.ShouldBe(1);

        var updateResponse = await PutJsonAsync(
            $"/accounts/{accountId}/contacts/{primaryContact!.Id}",
            new AccountContactRequestDto("Pat Updated", "Risk Manager", "pat.updated@example.test", "555-0102", false),
            primaryContact.RowVersion);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AccountContactDto>();
        updated.ShouldNotBeNull();
        updated!.FullName.ShouldBe("Pat Updated");
        updated.IsPrimary.ShouldBeFalse();

        var deleteResponse = await DeleteAsync(
            $"/accounts/{accountId}/contacts/{updated.Id}",
            updated.RowVersion);

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterDeleteResponse = await _client.GetAsync($"/accounts/{accountId}/contacts?page=1&pageSize=10");
        afterDeleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var afterDelete = await afterDeleteResponse.Content.ReadFromJsonAsync<PageEnvelope<AccountContactDto>>();
        afterDelete.ShouldNotBeNull();
        afterDelete!.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAccount_Returns410AndOnlyAppearsWhenIncludeRemoved()
    {
        var created = await CreateAccountViaApiAsync($"Delete {Guid.NewGuid():N}"[..20]);

        var deleteResponse = await PostJsonAsync(
            $"/accounts/{created.Id}/lifecycle",
            new AccountLifecycleRequestDto(AccountStatuses.Deleted, "Duplicate", null),
            created.RowVersion);

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var deleted = await deleteResponse.Content.ReadFromJsonAsync<AccountDto>();
        deleted.ShouldNotBeNull();
        deleted!.Status.ShouldBe(AccountStatuses.Deleted);

        var detailResponse = await _client.GetAsync($"/accounts/{created.Id}");
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.Gone);
        var detailProblem = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        detailProblem.GetProperty("code").GetString().ShouldBe("account_deleted");
        detailProblem.GetProperty("stableDisplayName").GetString().ShouldBe(created.StableDisplayName);

        var summaryResponse = await _client.GetAsync($"/accounts/{created.Id}/summary");
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.Gone);

        var defaultListResponse = await _client.GetAsync($"/accounts?q={Uri.EscapeDataString(created.DisplayName)}");
        defaultListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var defaultList = await defaultListResponse.Content.ReadFromJsonAsync<PageEnvelope<AccountListItemDto>>();
        defaultList.ShouldNotBeNull();
        defaultList!.Data.ShouldNotContain(item => item.Id == created.Id);

        var removedListResponse = await _client.GetAsync($"/accounts?includeRemoved=true&status=Deleted&q={Uri.EscapeDataString(created.DisplayName)}");
        removedListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var removedList = await removedListResponse.Content.ReadFromJsonAsync<PageEnvelope<AccountListItemDto>>();
        removedList.ShouldNotBeNull();
        removedList!.Data.ShouldContain(item => item.Id == created.Id && item.Status == AccountStatuses.Deleted);
    }

    [Fact]
    public async Task MergeAccount_PropagatesSubmissionAndRenewalFallbackContract()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var sourceId = await SeedAccountAsync(currentUserId, $"Merge Source {Guid.NewGuid():N}"[..20]);
        var survivorId = await SeedAccountAsync(currentUserId, $"Merge Survivor {Guid.NewGuid():N}"[..20]);
        var brokerId = await SeedBrokerAsync(currentUserId);
        var submission = await SeedSubmissionAsync(currentUserId, sourceId, brokerId, "Received", "Cyber");
        var policy = await SeedPolicyAsync(currentUserId, sourceId, brokerId, "Property", 30);
        var renewal = await SeedRenewalAsync(currentUserId, sourceId, brokerId, policy.Id, "Property", "Identified");
        var source = await GetAccountAsync(sourceId);

        var mergeResponse = await PostJsonAsync(
            $"/accounts/{sourceId}/merge",
            new AccountMergeRequestDto(survivorId, "duplicate cleanup"),
            source.RowVersion);

        mergeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var merged = await mergeResponse.Content.ReadFromJsonAsync<AccountDto>();
        merged.ShouldNotBeNull();
        merged!.Status.ShouldBe(AccountStatuses.Merged);
        merged.SurvivorAccountId.ShouldBe(survivorId);

        var submissionDetail = await GetSubmissionAsync(submission.Id);
        submissionDetail.AccountDisplayName.ShouldBe(source.StableDisplayName);
        submissionDetail.AccountStatus.ShouldBe(AccountStatuses.Merged);
        submissionDetail.AccountSurvivorId.ShouldBe(survivorId);

        var renewalDetail = await GetRenewalAsync(renewal.Id);
        renewalDetail.AccountDisplayName.ShouldBe(source.StableDisplayName);
        renewalDetail.AccountStatus.ShouldBe(AccountStatuses.Merged);
        renewalDetail.AccountSurvivorId.ShouldBe(survivorId);

        var submissionListResponse = await _client.GetAsync($"/submissions?accountId={sourceId}");
        submissionListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var submissionList = await submissionListResponse.Content.ReadFromJsonAsync<PaginatedResult<SubmissionListItemDto>>();
        submissionList.ShouldNotBeNull();
        submissionList!.Data.ShouldContain(item =>
            item.Id == submission.Id
            && item.AccountDisplayName == source.StableDisplayName
            && item.AccountStatus == AccountStatuses.Merged
            && item.AccountSurvivorId == survivorId);

        var renewalListResponse = await _client.GetAsync($"/renewals?accountId={sourceId}");
        renewalListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renewalList = await renewalListResponse.Content.ReadFromJsonAsync<PaginatedResult<RenewalListItemDto>>();
        renewalList.ShouldNotBeNull();
        renewalList!.Data.ShouldContain(item =>
            item.Id == renewal.Id
            && item.AccountDisplayName == source.StableDisplayName
            && item.AccountStatus == AccountStatuses.Merged
            && item.AccountSurvivorId == survivorId);

        var timelineResponse = await _client.GetAsync($"/accounts/{sourceId}/timeline?page=1&pageSize=10");
        timelineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<PaginatedResult<TimelineEventDto>>();
        timeline.ShouldNotBeNull();
        timeline!.Data.ShouldContain(item => item.EventType == "AccountMerged");
    }

    [Fact]
    public async Task MergePreview_ReturnsLinkedRecordCounts()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var sourceId = await SeedAccountAsync(currentUserId, $"Preview Src {Guid.NewGuid():N}"[..20]);
        var survivorId = await SeedAccountAsync(currentUserId, $"Preview Surv {Guid.NewGuid():N}"[..20]);
        var brokerId = await SeedBrokerAsync(currentUserId);
        await SeedSubmissionAsync(currentUserId, sourceId, brokerId, "Received", "Cyber");
        var policy = await SeedPolicyAsync(currentUserId, sourceId, brokerId, "Property", 30);
        await SeedRenewalAsync(currentUserId, sourceId, brokerId, policy.Id, "Property", "Identified");

        var response = await _client.GetAsync($"/accounts/{sourceId}/merge-preview?survivorId={survivorId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<AccountMergePreviewDto>();
        preview.ShouldNotBeNull();
        preview!.SourceAccountId.ShouldBe(sourceId);
        preview.SurvivorAccountId.ShouldBe(survivorId);
        preview.SubmissionCount.ShouldBe(1);
        preview.PolicyCount.ShouldBe(1);
        preview.RenewalCount.ShouldBe(1);
        preview.TotalLinked.ShouldBeGreaterThanOrEqualTo(3);
        preview.Threshold.ShouldBe(AccountService.MergeLinkedRecordsThreshold);
    }

    [Fact]
    public async Task MergePreview_RejectsSelfMergeWithConflict()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var accountId = await SeedAccountAsync(currentUserId, $"Self Preview {Guid.NewGuid():N}"[..20]);

        var response = await _client.GetAsync($"/accounts/{accountId}/merge-preview?survivorId={accountId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task MergeAccount_ReturnsContentTooLarge_WhenLinkedRecordsExceedThreshold()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var sourceId = await SeedAccountAsync(currentUserId, $"Large Src {Guid.NewGuid():N}"[..20]);
        var survivorId = await SeedAccountAsync(currentUserId, $"Large Surv {Guid.NewGuid():N}"[..20]);
        await SeedAccountTimelineEventsAsync(sourceId, currentUserId, AccountService.MergeLinkedRecordsThreshold + 1);
        var source = await GetAccountAsync(sourceId);

        var response = await PostJsonAsync(
            $"/accounts/{sourceId}/merge",
            new AccountMergeRequestDto(survivorId, "too large"),
            source.RowVersion);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().ShouldBe("merge_too_large");
        problem.GetProperty("threshold").GetInt32().ShouldBe(AccountService.MergeLinkedRecordsThreshold);
    }

    [Fact]
    public async Task MergeAccount_WithIdempotencyKey_ReplaysFirstResponseAndDoesNotDuplicateTimeline()
    {
        var currentUserId = await EnsureCurrentUserProfileAsync();
        var sourceId = await SeedAccountAsync(currentUserId, $"Idemp Src {Guid.NewGuid():N}"[..20]);
        var survivorId = await SeedAccountAsync(currentUserId, $"Idemp Surv {Guid.NewGuid():N}"[..20]);
        var source = await GetAccountAsync(sourceId);
        var idempotencyKey = $"merge-{Guid.NewGuid():N}";

        var first = await PostJsonAsync(
            $"/accounts/{sourceId}/merge",
            new AccountMergeRequestDto(survivorId, "idempotent merge"),
            source.RowVersion,
            idempotencyKey);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<AccountDto>();
        firstBody.ShouldNotBeNull();
        firstBody!.Status.ShouldBe(AccountStatuses.Merged);

        var second = await PostJsonAsync(
            $"/accounts/{sourceId}/merge",
            new AccountMergeRequestDto(survivorId, "idempotent merge"),
            source.RowVersion,
            idempotencyKey);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<AccountDto>();
        secondBody.ShouldNotBeNull();
        secondBody!.Id.ShouldBe(firstBody.Id);
        secondBody.Status.ShouldBe(AccountStatuses.Merged);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mergedTimelineEvents = await db.ActivityTimelineEvents
            .Where(evt => evt.EntityType == "Account" && evt.EntityId == sourceId && evt.EventType == "AccountMerged")
            .CountAsync();
        mergedTimelineEvents.ShouldBe(1);
    }

    private async Task SeedAccountTimelineEventsAsync(Guid accountId, Guid actorUserId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        for (var index = 0; index < count; index++)
        {
            db.ActivityTimelineEvents.Add(new ActivityTimelineEvent
            {
                EntityType = "Account",
                EntityId = accountId,
                EventType = "AccountStubEvent",
                EventDescription = $"seed event {index}",
                ActorUserId = actorUserId,
                ActorDisplayName = "Test User",
                OccurredAt = now.AddSeconds(-index),
            });
        }
        await db.SaveChangesAsync();
    }

    private async Task<AccountDto> CreateAccountViaApiAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new AccountCreateRequestDto(
            displayName,
            $"{displayName} LLC",
            $"TIN-{Guid.NewGuid():N}"[..20],
            "Technology",
            "Cyber",
            null,
            null,
            "W-01",
            "West",
            "100 Main Street",
            null,
            "Los Angeles",
            "CA",
            "90001",
            "USA",
            null,
            null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private async Task<AccountDto> GetAccountAsync(Guid accountId)
    {
        var response = await _client.GetAsync($"/accounts/{accountId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private async Task<SubmissionDto> GetSubmissionAsync(Guid submissionId)
    {
        var response = await _client.GetAsync($"/submissions/{submissionId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SubmissionDto>())!;
    }

    private async Task<RenewalDto> GetRenewalAsync(Guid renewalId)
    {
        var response = await _client.GetAsync($"/renewals/{renewalId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<RenewalDto>())!;
    }

    private async Task<Guid> SeedAccountAsync(Guid createdByUserId, string displayName, string region = "West")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var account = new Account
        {
            Name = displayName,
            StableDisplayName = displayName,
            LegalName = $"{displayName} Holdings LLC",
            TaxId = $"TIN-{Guid.NewGuid():N}"[..20],
            Industry = "Technology",
            PrimaryLineOfBusiness = "Cyber",
            PrimaryState = "CA",
            Region = region,
            TerritoryCode = "W-01",
            Address1 = "100 Main Street",
            City = "Los Angeles",
            PostalCode = "90001",
            Country = "USA",
            Status = AccountStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId,
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private async Task<Guid> SeedBrokerAsync(Guid createdByUserId, string region = "West")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var broker = new Broker
        {
            LegalName = $"Broker {Guid.NewGuid():N}"[..20],
            LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..16],
            State = "CA",
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId,
        };

        db.Brokers.Add(broker);
        db.BrokerRegions.Add(new BrokerRegion
        {
            BrokerId = broker.Id,
            Region = region,
        });
        await db.SaveChangesAsync();
        return broker.Id;
    }

    private async Task<Submission> SeedSubmissionAsync(
        Guid createdByUserId,
        Guid accountId,
        Guid brokerId,
        string status,
        string lineOfBusiness)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Accounts.SingleAsync(entity => entity.Id == accountId);
        var now = DateTime.UtcNow;

        var submission = new Submission
        {
            AccountId = accountId,
            BrokerId = brokerId,
            CurrentStatus = status,
            EffectiveDate = now.Date.AddDays(30),
            ExpirationDate = now.Date.AddMonths(12),
            LineOfBusiness = lineOfBusiness,
            PremiumEstimate = 45000m,
            Description = "Account endpoint seeded submission",
            AssignedToUserId = createdByUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId,
        };

        db.Submissions.Add(submission);
        db.WorkflowTransitions.Add(new WorkflowTransition
        {
            WorkflowType = "Submission",
            EntityId = submission.Id,
            FromState = null,
            ToState = status,
            ActorUserId = createdByUserId,
            OccurredAt = now,
        });
        db.ActivityTimelineEvents.Add(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submission.Id,
            EventType = "SubmissionCreated",
            EventDescription = "Submission created",
            ActorUserId = createdByUserId,
            ActorDisplayName = "Test User",
            OccurredAt = now,
        });
        await db.SaveChangesAsync();
        return submission;
    }

    private async Task<Policy> SeedPolicyAsync(
        Guid createdByUserId,
        Guid accountId,
        Guid brokerId,
        string lineOfBusiness,
        int expirationDays)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Accounts.SingleAsync(entity => entity.Id == accountId);
        var now = DateTime.UtcNow;
        var carrier = NewCarrierRef("Acme Carrier");

        var policy = new Policy
        {
            PolicyNumber = $"POL-{Guid.NewGuid():N}"[..16],
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

    private async Task<Renewal> SeedRenewalAsync(
        Guid createdByUserId,
        Guid accountId,
        Guid brokerId,
        Guid policyId,
        string lineOfBusiness,
        string status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Accounts.SingleAsync(entity => entity.Id == accountId);
        var policy = await db.Policies.SingleAsync(entity => entity.Id == policyId);
        var now = DateTime.UtcNow;

        var renewal = new Renewal
        {
            AccountId = accountId,
            BrokerId = brokerId,
            PolicyId = policyId,
            CurrentStatus = status,
            LineOfBusiness = lineOfBusiness,
            PolicyExpirationDate = policy.ExpirationDate,
            TargetOutreachDate = policy.ExpirationDate.AddDays(-90),
            AssignedToUserId = createdByUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId,
        };

        db.Renewals.Add(renewal);
        db.WorkflowTransitions.Add(new WorkflowTransition
        {
            WorkflowType = "Renewal",
            EntityId = renewal.Id,
            FromState = null,
            ToState = status,
            ActorUserId = createdByUserId,
            OccurredAt = now,
        });
        db.ActivityTimelineEvents.Add(new ActivityTimelineEvent
        {
            EntityType = "Renewal",
            EntityId = renewal.Id,
            EventType = "RenewalCreated",
            EventDescription = $"Renewal created from policy {policy.PolicyNumber}",
            ActorUserId = createdByUserId,
            ActorDisplayName = "Test User",
            OccurredAt = now,
        });
        await db.SaveChangesAsync();
        return renewal;
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
        => await PostJsonAsync(url, body, rowVersion, idempotencyKey: null);

    private async Task<HttpResponseMessage> PostJsonAsync<TBody>(string url, TBody body, string rowVersion, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{rowVersion}\"");
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteAsync(string url, string rowVersion)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{rowVersion}\"");
        return await _client.SendAsync(request);
    }

    private sealed record PageEnvelope<T>(
        IReadOnlyList<T> Data,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}
