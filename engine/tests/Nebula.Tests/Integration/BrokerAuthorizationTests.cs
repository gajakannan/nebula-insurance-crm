using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Nebula.Application.DTOs;

namespace Nebula.Tests.Integration;

/// <summary>
/// Casbin authorization tests for F0002 broker/contact/timeline endpoints (G1).
/// Uses a role-less user (TestNebulaRoles = []) to verify all protected endpoints
/// return 403 when the caller lacks the required permission.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class BrokerAuthorizationTests(CustomWebApplicationFactory factory)
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

    private static void SetNoRolesContext()
    {
        TestAuthHandler.TestRole = "ExternalUser";
        TestAuthHandler.TestNebulaRoles = [];
    }

    // ── Broker endpoints ────────────────────────────────────────────────────
    [Fact]
    public async Task ListBrokers_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.GetAsync("/brokers");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBroker_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Auth Test", "AUTH-001", "CA", null, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBroker_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.GetAsync($"/brokers/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteBroker_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.DeleteAsync($"/brokers/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReactivateBroker_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.PostAsync($"/brokers/{Guid.NewGuid()}/reactivate", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Contact endpoints ───────────────────────────────────────────────────
    [Fact]
    public async Task ListContacts_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.GetAsync("/contacts");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateContact_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.PostAsJsonAsync("/contacts",
            new ContactCreateDto(Guid.NewGuid(), "Test", "t@t.com", "+11234567890", null));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteContact_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.DeleteAsync($"/contacts/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── Timeline endpoint ───────────────────────────────────────────────────
    [Fact]
    public async Task GetTimeline_WithNoRoles_Returns403()
    {
        SetNoRolesContext();
        var response = await _client.GetAsync("/timeline/events?entityType=Broker");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
