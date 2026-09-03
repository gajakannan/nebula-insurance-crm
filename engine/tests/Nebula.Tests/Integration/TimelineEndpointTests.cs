using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Tests.Integration;

/// <summary>
/// Integration tests for timeline pagination contract (F0002-S0007, G4).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class TimelineEndpointTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TimelineEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        SetUser("test-user-001", "Admin", "Test User");
    }

    public void Dispose()
    {
        SetUser("test-user-001", "Admin", "Test User");
        TestAuthHandler.ResetF0009Overrides();
    }

    private async Task<BrokerDto> CreateBrokerAsync(string licensePrefix)
    {
        var license = $"{licensePrefix}-{Guid.NewGuid().ToString("N")[..8]}";
        var response = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Timeline Test Broker", license, "CA", null, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BrokerDto>())!;
    }

    [Fact]
    public async Task GetTimeline_ReturnsPaginatedEnvelope()
    {
        var broker = await CreateBrokerAsync("TL-PAG-001");

        var response = await _client.GetAsync(
            $"/timeline/events?entityType=Broker&entityId={broker.Id}&page=1&pageSize=50");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonPaginatedTimelineList>();
        json.ShouldNotBeNull();
        json!.Data.ShouldNotBeNull();
        json.Page.ShouldBe(1);
        json.PageSize.ShouldBe(50);
        json.TotalCount.ShouldBeGreaterThanOrEqualTo(1); // BrokerCreated event exists
        json.TotalPages.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTimeline_DefaultPageSize_Is50()
    {
        var broker = await CreateBrokerAsync("TL-PAG-002");

        var response = await _client.GetAsync(
            $"/timeline/events?entityType=Broker&entityId={broker.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonPaginatedTimelineList>();
        json!.PageSize.ShouldBe(50);
    }

    [Fact]
    public async Task GetTimeline_Page2_ReturnsEmptyDataWhenNotEnoughEvents()
    {
        var broker = await CreateBrokerAsync("TL-PAG-003");

        var response = await _client.GetAsync(
            $"/timeline/events?entityType=Broker&entityId={broker.Id}&page=2&pageSize=50");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonPaginatedTimelineList>();
        json!.Data.ShouldBeEmpty();
        json.Page.ShouldBe(2);
    }

    [Fact]
    public async Task GetTimeline_InternalOnlyForNonBroker_ReturnsValidationError()
    {
        var response = await _client.GetAsync(
            "/timeline/events?entityType=Account&internalOnly=true");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("internalOnly=true is valid only when entityType=Broker");
    }

    [Theory]
    [InlineData("BrokerUser")]
    [InlineData("ExternalUser")]
    public async Task GetTimeline_InternalOnlyForExternalPrincipal_ReturnsForbidden(string role)
    {
        SetUser($"external-{role}-{Guid.NewGuid():N}", role, "External Timeline User");

        var response = await _client.GetAsync(
            "/timeline/events?entityType=Broker&internalOnly=true");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("entityName", Case.Insensitive);
        body.ShouldNotContain("totalCount", Case.Insensitive);
    }

    [Theory]
    [InlineData("DistributionUser")]
    [InlineData("DistributionManager")]
    [InlineData("RelationshipManager")]
    [InlineData("ProgramManager")]
    [InlineData("Underwriter")]
    [InlineData("Admin")]
    public async Task GetTimeline_InternalBrokerFeed_AllEligibleRolesCanReadAuthorizedBroker(string role)
    {
        var subject = $"timeline-{role}-{Guid.NewGuid():N}";
        var userId = Guid.NewGuid();
        var brokerId = await SeedScopedBrokerAsync(subject, userId, role);
        SetUser(subject, role, $"{role} Timeline User");

        var response = await _client.GetAsync(
            $"/timeline/events?entityType=Broker&entityId={brokerId}&page=1&pageSize=20&internalOnly=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonPaginatedTimelineList>();
        result.ShouldNotBeNull();
        result!.TotalCount.ShouldBe(1);
        result.Data.Single().EntityId.ShouldBe(brokerId);
        result.Data.Single().EntityName.ShouldBe($"Authorized {role} Broker");
    }

    [Fact]
    public async Task GetTimeline_InternalBrokerFeed_ScopesBeforeCountAndReturnsNewest20()
    {
        var subject = $"timeline-scope-{Guid.NewGuid():N}";
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var (visibleBrokerId, hiddenBrokerId) = await SeedScopeMatrixAsync(subject, userId, now);
        SetUser(subject, "RelationshipManager", "Scoped Timeline User");

        var response = await _client.GetAsync(
            "/timeline/events?entityType=Broker&page=2&pageSize=100&internalOnly=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonPaginatedTimelineList>();
        result.ShouldNotBeNull();
        result!.Page.ShouldBe(1, "the F0040 feed is fixed to its first page");
        result.PageSize.ShouldBe(20, "the F0040 feed is capped at twenty rows");
        result.TotalCount.ShouldBe(21, "hidden rows must not influence the scoped count");
        result.TotalPages.ShouldBe(2);
        result.Data.Count.ShouldBe(20);
        result.Data.ShouldAllBe(item => item.EntityId == visibleBrokerId);
        result.Data.ShouldAllBe(item => item.EntityId != hiddenBrokerId);
        result.Data.ShouldAllBe(item => item.EntityName == "Visible Broker");
        result.Data.ShouldAllBe(item => item.ActorDisplayName == "Unknown User");
        result.Data.Select(item => item.OccurredAt)
            .ShouldBe(result.Data.Select(item => item.OccurredAt).OrderByDescending(value => value));
        result.Data.Select(item => item.EventDescription)
            .ShouldBe(Enumerable.Range(0, 20).Select(index => $"visible-event-{index:D2}"));
    }

    [Fact]
    public async Task GetTimeline_BrokerUserWithoutInternalOnly_RetainsLegacySafeListShape()
    {
        var tenantId = $"timeline-tenant-{Guid.NewGuid():N}";
        var broker = await CreateBrokerAsync("TL-BROKERUSER");
        await _factory.SetBrokerTenantIdAsync(broker.Id, tenantId);
        SetUser($"broker-user-{Guid.NewGuid():N}", "BrokerUser", "Broker Timeline User");
        TestAuthHandler.TestBrokerTenantId = tenantId;

        var response = await _client.GetAsync(
            "/timeline/events?entityType=Broker&limit=20");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.ValueKind.ShouldBe(JsonValueKind.Array);
        payload.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
        payload[0].TryGetProperty("brokerDescription", out _).ShouldBeTrue();
        payload[0].TryGetProperty("entityName", out _).ShouldBeFalse();
    }

    private async Task<Guid> SeedScopedBrokerAsync(string subject, Guid userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var brokerId = Guid.NewGuid();

        db.UserProfiles.Add(NewProfile(subject, userId, role, now));
        db.Brokers.Add(NewBroker(brokerId, $"Authorized {role} Broker", userId, now));
        db.ActivityTimelineEvents.Add(NewEvent(
            brokerId, "AuthorizedEvent", $"authorized-{role}", now, actorDisplayName: "Actor"));
        await db.SaveChangesAsync();
        return brokerId;
    }

    private async Task<(Guid VisibleBrokerId, Guid HiddenBrokerId)> SeedScopeMatrixAsync(
        string subject,
        Guid userId,
        DateTime now)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var visibleBrokerId = Guid.NewGuid();
        var hiddenBrokerId = Guid.NewGuid();

        db.UserProfiles.Add(NewProfile(subject, userId, "RelationshipManager", now));
        db.Brokers.Add(NewBroker(visibleBrokerId, "Visible Broker", userId, now));
        db.Brokers.Add(NewBroker(hiddenBrokerId, "Hidden Broker", Guid.NewGuid(), now));

        for (var index = 0; index < 21; index++)
        {
            db.ActivityTimelineEvents.Add(NewEvent(
                visibleBrokerId,
                "VisibleEvent",
                $"visible-event-{index:D2}",
                now.AddMinutes(-index),
                actorDisplayName: null));
        }

        // Hidden events are deliberately newer than every visible row. Applying scope after Take/Count
        // would leak their presence or displace authorized rows from the newest-twenty result.
        for (var index = 0; index < 3; index++)
        {
            db.ActivityTimelineEvents.Add(NewEvent(
                hiddenBrokerId,
                "HiddenEvent",
                $"hidden-event-{index:D2}",
                now.AddMinutes(index + 1),
                actorDisplayName: "Hidden Actor"));
        }

        await db.SaveChangesAsync();
        return (visibleBrokerId, hiddenBrokerId);
    }

    private static UserProfile NewProfile(string subject, Guid userId, string role, DateTime now) => new()
    {
        Id = userId,
        IdpIssuer = "http://test.local/application/o/nebula/",
        IdpSubject = subject,
        Email = $"{subject}@test.local",
        DisplayName = $"{role} Timeline User",
        Department = "Distribution",
        RolesJson = JsonSerializer.Serialize(new[] { role }),
        RegionsJson = JsonSerializer.Serialize(new[] { "West" }),
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static Broker NewBroker(Guid id, string name, Guid managedByUserId, DateTime now) => new()
    {
        Id = id,
        LegalName = name,
        LicenseNumber = $"TL-{id:N}"[..20],
        State = "CA",
        Status = "Active",
        ManagedByUserId = managedByUserId,
        CreatedAt = now,
        UpdatedAt = now,
        CreatedByUserId = managedByUserId,
        UpdatedByUserId = managedByUserId,
    };

    private static ActivityTimelineEvent NewEvent(
        Guid brokerId,
        string eventType,
        string description,
        DateTime occurredAt,
        string? actorDisplayName) => new()
    {
        EntityType = "Broker",
        EntityId = brokerId,
        EventType = eventType,
        EventDescription = description,
        BrokerDescription = $"Safe {description}",
        ActorUserId = Guid.NewGuid(),
        ActorDisplayName = actorDisplayName,
        OccurredAt = occurredAt,
    };

    private static void SetUser(string subject, string role, string displayName)
    {
        TestAuthHandler.TestSubject = subject;
        TestAuthHandler.TestRole = role;
        TestAuthHandler.TestNebulaRoles = [role];
        TestAuthHandler.TestDisplayName = displayName;
        TestAuthHandler.TestBrokerTenantId = null;
    }

    private record JsonPaginatedTimelineList(
        IReadOnlyList<TimelineEventDto> Data, int Page, int PageSize, int TotalCount, int TotalPages);
}
