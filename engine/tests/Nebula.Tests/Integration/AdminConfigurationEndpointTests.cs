using System.Net;
using System.Net.Http.Json;
using Nebula.Application.DTOs;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class AdminConfigurationEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose()
    {
        TestAuthHandler.ResetF0009Overrides();
    }

    [Fact]
    public async Task ListDomains_AsAdmin_ReturnsSeededConfigurationCatalog()
    {
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];

        var response = await _client.GetAsync("/admin/configuration-domains");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var domains = await response.Content.ReadFromJsonAsync<IReadOnlyList<AdminConfigurationDomainDto>>();
        domains.ShouldNotBeNull();
        domains!.Select(domain => domain.DomainKey).ShouldContain("queue-routing");
        domains.Select(domain => domain.DomainKey).ShouldContain("workflow-sla-thresholds");
        domains.Select(domain => domain.DomainKey).ShouldContain("search-report-defaults");
        domains.Select(domain => domain.DomainKey).ShouldContain("template-metadata");
    }

    [Fact]
    public async Task CreateDraft_WithoutReason_ReturnsBadRequest()
    {
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];

        var response = await _client.PostAsJsonAsync(
            "/admin/configuration-domains/queue-routing/drafts",
            new AdminConfigurationDraftCreateRequestDto(""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
