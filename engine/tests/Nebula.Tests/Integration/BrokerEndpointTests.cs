using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Nebula.Application.DTOs;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class BrokerEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateBroker_WithValidData_Returns201()
    {
        var dto = new BrokerCreateDto("Test Broker LLC", "TEST-LIC-001", "CA", "test@broker.com", "+14155551234");

        var response = await _client.PostAsJsonAsync("/brokers", dto);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<BrokerDto>();
        result.ShouldNotBeNull();
        result!.LegalName.ShouldBe("Test Broker LLC");
        result.Status.ShouldBe("Pending");
    }

    [Fact]
    public async Task CreateBroker_DuplicateLicense_Returns409()
    {
        var dto = new BrokerCreateDto("First Broker", "DUP-LIC-001", "NY", null, null);
        await _client.PostAsJsonAsync("/brokers", dto);

        var dto2 = new BrokerCreateDto("Second Broker", "DUP-LIC-001", "CA", null, null);
        var response = await _client.PostAsJsonAsync("/brokers", dto2);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListBrokers_ReturnsPagedResult()
    {
        await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Listed Broker", "LIST-001", "TX", null, null));

        var response = await _client.GetAsync("/brokers?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonPaginatedBrokerList>();
        json.ShouldNotBeNull();
        json!.Data.ShouldNotBeEmpty();
        json.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetBroker_ExistingId_Returns200()
    {
        var create = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Get Broker", "GET-001", "WA", null, null));
        var created = await create.Content.ReadFromJsonAsync<BrokerDto>();

        var response = await _client.GetAsync($"/brokers/{created!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBroker_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/brokers/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBroker_WithIfMatch_Returns200()
    {
        var create = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Update Broker", "UPD-001", "OR", null, null));
        var created = await create.Content.ReadFromJsonAsync<BrokerDto>();

        var updateDto = new BrokerUpdateDto("Updated Broker Name", "WA", "Active", "new@email.com", null);
        var request = new HttpRequestMessage(HttpMethod.Put, $"/brokers/{created!.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{created.RowVersion}\""));

        var response = await _client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteBroker_ExistingBroker_Returns204()
    {
        var create = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Delete Broker", "DEL-001", "NV", null, null));
        var created = await create.Content.ReadFromJsonAsync<BrokerDto>();

        var response = await _client.DeleteAsync($"/brokers/{created!.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBroker_InvalidState_Returns400()
    {
        var dto = new BrokerCreateDto("Bad Broker", "BAD-001", "INVALID", null, null);
        var response = await _client.PostAsJsonAsync("/brokers", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── F0002-S0005: deactivation sets Status=Inactive ─────────────────────
    [Fact]
    public async Task DeleteBroker_SetsStatusInactive()
    {
        var create = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Status Inactive Test", "STAT-001", "CA", null, null));
        var created = await create.Content.ReadFromJsonAsync<BrokerDto>();

        await _client.DeleteAsync($"/brokers/{created!.Id}");

        // Admin can see deactivated brokers — verify Status=Inactive
        var get = await _client.GetAsync($"/brokers/{created.Id}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        var broker = await get.Content.ReadFromJsonAsync<BrokerDto>();
        broker!.Status.ShouldBe("Inactive");
        broker.IsDeactivated.ShouldBeTrue();
    }

    // ── F0002-S0008: reactivation endpoint ─────────────────────────────────
    [Fact]
    public async Task ReactivateBroker_AfterDeactivation_Returns200WithActiveStatus()
    {
        var create = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Reactivate Test", "REACT-001", "TX", null, null));
        var created = await create.Content.ReadFromJsonAsync<BrokerDto>();

        await _client.DeleteAsync($"/brokers/{created!.Id}");

        var response = await _client.PostAsync($"/brokers/{created.Id}/reactivate", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BrokerDto>();
        result!.Status.ShouldBe("Active");
        result.IsDeactivated.ShouldBeFalse();
    }

    [Fact]
    public async Task ReactivateBroker_AlreadyActive_Returns409()
    {
        var create = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Already Active", "REACT-002", "NY", null, null));
        var created = await create.Content.ReadFromJsonAsync<BrokerDto>();

        var response = await _client.PostAsync($"/brokers/{created!.Id}/reactivate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReactivateBroker_NonExistent_Returns404()
    {
        var response = await _client.PostAsync($"/brokers/{Guid.NewGuid()}/reactivate", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private record JsonPaginatedBrokerList(
        IReadOnlyList<BrokerDto> Data, int Page, int PageSize, int TotalCount, int TotalPages);
}
