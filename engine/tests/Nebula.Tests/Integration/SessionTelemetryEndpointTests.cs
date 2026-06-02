using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class SessionTelemetryEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose()
    {
        TestAuthHandler.TestSubject = "test-user-001";
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestDisplayName = "Test User";
        TestAuthHandler.ResetF0009Overrides();
    }

    [Fact]
    public async Task PostSessionContinuityTelemetry_ValidBatch_Returns202Accepted()
    {
        var userId = await ArrangeCurrentUserAsync();

        var response = await _client.PostAsJsonAsync(
            "/internal/telemetry/session-continuity",
            RequestBody(userId));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostSessionContinuityTelemetry_PiiPayloadKey_Returns400ValidationProblem()
    {
        var userId = await ArrangeCurrentUserAsync();
        var body = RequestBody(
            userId,
            payload: new Dictionary<string, object?>
            {
                ["cause"] = "refresh_expired",
                ["email"] = "person@example.test",
            },
            eventName: "silent-renewal-fail");

        var response = await _client.PostAsJsonAsync(
            "/internal/telemetry/session-continuity",
            body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().ShouldBe("validation_error");
        problem.GetProperty("errors").GetRawText().ShouldContain("email");
    }

    [Fact]
    public async Task PostSessionContinuityTelemetry_UserMismatch_Returns403WithoutAuthenticateHeader()
    {
        await ArrangeCurrentUserAsync();
        var otherUser = "session-telemetry-other-001";

        var response = await _client.PostAsJsonAsync(
            "/internal/telemetry/session-continuity",
            RequestBody(otherUser));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Headers.WwwAuthenticate.ShouldBeEmpty();
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("https://nebula.local/problems/authz/forbidden");
        problem.GetProperty("code").GetString().ShouldBe("forbidden");
    }

    [Fact]
    public async Task PostSessionContinuityTelemetry_UserMismatchWithSchemaErrors_Returns400ValidationProblem()
    {
        var currentUser = await ArrangeCurrentUserAsync();
        var otherUser = "session-telemetry-other-001";
        var body = new Dictionary<string, object?>
        {
            ["events"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["event_name"] = "forced-redirect",
                    ["event_version"] = 1,
                    ["timestamp"] = DateTimeOffset.UtcNow,
                    ["user_id"] = otherUser,
                    ["session_id"] = "session-test-001",
                    ["payload"] = new Dictionary<string, object?>
                    {
                        ["cause"] = "idle_timeout",
                        ["route_at_redirect"] = "/submissions",
                    },
                },
                new Dictionary<string, object?>
                {
                    ["event_name"] = "forced-redirect",
                    ["event_version"] = 1,
                    ["timestamp"] = DateTimeOffset.UtcNow,
                    ["user_id"] = currentUser,
                    ["session_id"] = "session-test-001",
                    ["payload"] = new Dictionary<string, object?>
                    {
                        ["cause"] = "idle_timeout",
                        ["query"] = "?contains=pii",
                    },
                },
            },
        };

        var response = await _client.PostAsJsonAsync(
            "/internal/telemetry/session-continuity",
            body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().ShouldBe("validation_error");
        problem.GetProperty("errors").GetRawText().ShouldContain("query");
        problem.GetProperty("errors").GetRawText().ShouldContain("user_id");
    }

    [Fact]
    public async Task PostSessionContinuityTelemetry_InvalidToken_Returns401AuthProblem()
    {
        TestAuthHandler.Mode = TestAuthHandler.AuthMode.Invalid;

        var response = await _client.PostAsJsonAsync(
            "/internal/telemetry/session-continuity",
            RequestBody("session-telemetry-any-001"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldContain(header => header.Scheme == "Bearer");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("https://nebula.local/problems/auth/invalid-token");
        problem.GetProperty("code").GetString().ShouldBe("invalid_token");
    }

    private async Task<string> ArrangeCurrentUserAsync()
    {
        var subject = $"session-telemetry-{Guid.NewGuid():N}";
        var userId = Guid.NewGuid();
        TestAuthHandler.TestSubject = subject;
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        db.UserProfiles.Add(new UserProfile
        {
            Id = userId,
            IdpIssuer = "http://test.local/application/o/nebula/",
            IdpSubject = subject,
            Email = $"{subject}@example.test",
            DisplayName = "Session Telemetry Test User",
            Department = "",
            RolesJson = "[\"Admin\"]",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();
        // Telemetry identity is the OIDC subject (what the SPA sends as user_id),
        // not the internal UserProfile.Id.
        return subject;
    }

    private static Dictionary<string, object?> RequestBody(
        string userId,
        Dictionary<string, object?>? payload = null,
        string eventName = "forced-redirect") =>
        new()
        {
            ["events"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["event_name"] = eventName,
                    ["event_version"] = 1,
                    ["timestamp"] = DateTimeOffset.UtcNow,
                    ["user_id"] = userId,
                    ["session_id"] = "session-test-001",
                    ["payload"] = payload ?? new Dictionary<string, object?>
                    {
                        ["cause"] = "idle_timeout",
                        ["route_at_redirect"] = "/submissions",
                    },
                },
            },
        };
}
