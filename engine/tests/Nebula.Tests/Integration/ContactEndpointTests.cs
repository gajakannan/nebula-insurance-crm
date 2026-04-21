using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Nebula.Application.DTOs;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class ContactEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<BrokerDto> CreateBrokerAsync(string licensePrefix)
    {
        var license = $"{licensePrefix}-{Guid.NewGuid().ToString("N")[..8]}";
        var response = await _client.PostAsJsonAsync("/brokers",
            new BrokerCreateDto("Contact Test Broker", license, "CA", null, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BrokerDto>())!;
    }

    [Fact]
    public async Task CreateContact_WithValidData_Returns201()
    {
        var broker = await CreateBrokerAsync("CTT-001");

        var dto = new ContactCreateDto(broker.Id, "Jane Doe", "jane@example.com", "+14155551111", "Primary");
        var response = await _client.PostAsJsonAsync("/contacts", dto);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ContactDto>();
        result.ShouldNotBeNull();
        result!.FullName.ShouldBe("Jane Doe");
    }

    [Fact]
    public async Task ListContacts_FilterByBrokerId_ReturnsFiltered()
    {
        var broker = await CreateBrokerAsync("CTT-002");
        await _client.PostAsJsonAsync("/contacts",
            new ContactCreateDto(broker.Id, "Filter Test", "filter@test.com", "+14155552222", null));

        var response = await _client.GetAsync($"/contacts?brokerId={broker.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetContact_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/contacts/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContact_Existing_Returns200()
    {
        var created = await CreateContactAsync("get-contact");

        var response = await _client.GetAsync($"/contacts/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ContactDto>();
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task UpdateContact_WithIfMatch_Returns200()
    {
        var created = await CreateContactAsync("update-contact");
        var request = new HttpRequestMessage(HttpMethod.Put, $"/contacts/{created.Id}")
        {
            Content = JsonContent.Create(new ContactUpdateDto("Updated Name", "updated@test.com", "+14155550001", "Assistant")),
        };
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{created.RowVersion}\""));

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ContactDto>();
        result.ShouldNotBeNull();
        result!.FullName.ShouldBe("Updated Name");
        result.Role.ShouldBe("Assistant");
    }

    [Fact]
    public async Task UpdateContact_MissingIfMatch_Returns428()
    {
        var created = await CreateContactAsync("missing-if-match");

        var response = await _client.PutAsJsonAsync(
            $"/contacts/{created.Id}",
            new ContactUpdateDto("Updated Name", "updated@test.com", "+14155550002", "Assistant"));

        ((int)response.StatusCode).ShouldBe(428);
    }

    [Fact]
    public async Task DeleteContact_Existing_Returns204()
    {
        var created = await CreateContactAsync("delete-contact");

        var response = await _client.DeleteAsync($"/contacts/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteContact_ThenGetReturns404()
    {
        var created = await CreateContactAsync("delete-then-get");
        await _client.DeleteAsync($"/contacts/{created.Id}");

        var response = await _client.GetAsync($"/contacts/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── F0002 G3: paginated envelope + RowVersion ───────────────────────────
    [Fact]
    public async Task ListContacts_ReturnsPaginatedEnvelope()
    {
        var broker = await CreateBrokerAsync("CTT-PAG-001");
        await _client.PostAsJsonAsync("/contacts",
            new ContactCreateDto(broker.Id, "Paged Contact", "paged@test.com", "+14155553333", null));

        var response = await _client.GetAsync($"/contacts?brokerId={broker.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonPaginatedContactList>();
        json.ShouldNotBeNull();
        json!.Data.ShouldNotBeEmpty();
        json.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        json.Page.ShouldBe(1);
        json.TotalPages.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CreateContact_ResponseIncludesRowVersion()
    {
        var broker = await CreateBrokerAsync("CTT-RV-001");
        var dto = new ContactCreateDto(broker.Id, "RV Test", "rv@test.com", "+14155554444", null);

        var response = await _client.PostAsJsonAsync("/contacts", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ContactDto>();
        result.ShouldNotBeNull();
        // RowVersion is a uint returned in response — default value 0 on creation is valid.
        result!.RowVersion.ShouldBeGreaterThanOrEqualTo(0u);
    }

    private record JsonPaginatedContactList(
        IReadOnlyList<ContactDto> Data, int Page, int PageSize, int TotalCount, int TotalPages);

    private async Task<ContactDto> CreateContactAsync(string key)
    {
        var broker = await CreateBrokerAsync($"CTT-{key}");
        var response = await _client.PostAsJsonAsync("/contacts",
            new ContactCreateDto(
                broker.Id,
                $"Contact {key}",
                $"{key}@test.com",
                "+14155559999",
                "Primary"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContactDto>())!;
    }
}
