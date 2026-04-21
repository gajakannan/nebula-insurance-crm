using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Nebula.Application.DTOs;

namespace Nebula.Tests.Integration;

/// <summary>
/// Integration tests for timeline pagination contract (F0002-S0007, G4).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class TimelineEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

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

    private record JsonPaginatedTimelineList(
        IReadOnlyList<TimelineEventDto> Data, int Page, int PageSize, int TotalCount, int TotalPages);
}
