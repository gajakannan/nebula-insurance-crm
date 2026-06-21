using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.DTOs;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class SearchEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SearchResults_SeededPolicyNumber_ReturnsPolicyResult()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var policy = await db.Policies
            .AsNoTracking()
            .OrderBy(p => p.PolicyNumber)
            .Select(p => new { p.Id, p.PolicyNumber })
            .FirstAsync();

        var projected = await db.SearchDocuments
            .AsNoTracking()
            .AnyAsync(d => d.ObjectType == "Policy" && d.ObjectId == policy.Id);
        projected.ShouldBeTrue();

        var response = await _client.GetAsync($"/search-results?q={Uri.EscapeDataString(policy.PolicyNumber)}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        var result = await response.Content.ReadFromJsonAsync<GlobalSearchResponseDto>();
        result.ShouldNotBeNull();
        result!.Data.ShouldContain(d =>
            d.ObjectType == "Policy"
            && d.ObjectId == policy.Id
            && d.Title == policy.PolicyNumber);
    }
}
