using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

/// <summary>F0038-S0008 — companion telemetry ingest (POST /internal/telemetry/neuron-companion).</summary>
[Collection(IntegrationTestCollection.Name)]
public class NeuronCompanionTelemetryEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private const string Path = "/internal/telemetry/neuron-companion";
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose()
    {
        TestAuthHandler.TestSubject = "test-user-001";
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestDisplayName = "Test User";
        TestAuthHandler.ResetF0009Overrides();
    }

    [Fact]
    public async Task Post_ValidBatch_Returns202Accepted()
    {
        var userId = await ArrangeCurrentUserAsync();

        var response = await _client.PostAsJsonAsync(
            Path, Body(Surfaced(userId), DraftReady(userId), DailyActive(userId)));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Post_UserMismatch_Returns403()
    {
        await ArrangeCurrentUserAsync();

        var response = await _client.PostAsJsonAsync(Path, Body(Surfaced("neuron-telemetry-other-001")));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("https://nebula.local/problems/authz/forbidden");
    }

    [Fact]
    public async Task Post_PrimaryEventMissingRenewalId_Returns400()
    {
        var userId = await ArrangeCurrentUserAsync();
        var surfaced = Surfaced(userId);
        surfaced.Remove("renewal_id");

        var response = await _client.PostAsJsonAsync(Path, Body(surfaced));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().ShouldBe("validation_error");
        problem.GetProperty("errors").GetRawText().ShouldContain("renewal_id");
    }

    [Fact]
    public async Task Post_DailyActiveMissingPersona_Returns400()
    {
        var userId = await ArrangeCurrentUserAsync();
        var dailyActive = DailyActive(userId);
        dailyActive.Remove("persona");

        var response = await _client.PostAsJsonAsync(Path, Body(dailyActive));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").GetRawText().ShouldContain("persona");
    }

    [Fact]
    public async Task Post_UnknownEventName_Returns400()
    {
        var userId = await ArrangeCurrentUserAsync();
        var bad = Surfaced(userId);
        bad["event_name"] = "not-a-real-event";

        var response = await _client.PostAsJsonAsync(Path, Body(bad));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").GetRawText().ShouldContain("event_name");
    }

    private static Dictionary<string, object?> Body(params Dictionary<string, object?>[] events) =>
        new() { ["events"] = events };

    private static Dictionary<string, object?> Surfaced(string userId) => new()
    {
        ["event_name"] = "needs-attention-surfaced",
        ["event_version"] = 1,
        ["timestamp"] = DateTimeOffset.UtcNow,
        ["user_id"] = userId,
        ["thread_id"] = Guid.NewGuid().ToString(),
        ["renewal_id"] = Guid.NewGuid().ToString(),
    };

    private static Dictionary<string, object?> DraftReady(string userId) => new()
    {
        ["event_name"] = "draft-ready",
        ["event_version"] = 1,
        ["timestamp"] = DateTimeOffset.UtcNow,
        ["user_id"] = userId,
        ["thread_id"] = Guid.NewGuid().ToString(),
        ["renewal_id"] = Guid.NewGuid().ToString(),
    };

    private static Dictionary<string, object?> DailyActive(string userId) => new()
    {
        ["event_name"] = "companion-daily-active",
        ["event_version"] = 1,
        ["timestamp"] = DateTimeOffset.UtcNow,
        ["user_id"] = userId,
        ["persona"] = "underwriter",
    };

    private async Task<string> ArrangeCurrentUserAsync()
    {
        var subject = $"neuron-telemetry-{Guid.NewGuid():N}";
        var userId = Guid.NewGuid();
        TestAuthHandler.TestSubject = subject;
        TestAuthHandler.TestRole = "Underwriter";
        TestAuthHandler.TestNebulaRoles = ["Underwriter"];

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.UserProfiles.Add(new UserProfile
        {
            Id = userId,
            IdpIssuer = "http://test.local/application/o/nebula/",
            IdpSubject = subject,
            Email = $"{subject}@example.test",
            DisplayName = "Neuron Telemetry Test User",
            Department = "",
            RolesJson = "[\"Underwriter\"]",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();
        // Telemetry identity is the OIDC subject (what Neuron forwards as user_id).
        return subject;
    }
}
