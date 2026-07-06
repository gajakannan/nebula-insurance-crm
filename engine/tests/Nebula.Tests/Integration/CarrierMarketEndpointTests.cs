using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.DTOs;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class CarrierMarketEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateAndGetCarrierMarket_ReturnsDetailWithChildren()
    {
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];

        var created = await CreateMarketAsync("Nebula Mutual", "NEB-MUT-001");

        var contactResponse = await _client.PostAsJsonAsync($"/carrier-markets/{created.Id}/contacts", new CarrierMarketContactUpsertDto(
            "Dana Carrier",
            "Senior Underwriter",
            "dana.carrier@example.com",
            "+14155550123",
            ["Underwriter"],
            true,
            "Primary casualty desk"));
        contactResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var appetiteResponse = await _client.PostAsJsonAsync($"/carrier-markets/{created.Id}/appetite-notes", new CarrierAppetiteNoteUpsertDto(
            "GL",
            "West",
            "Preferred",
            "Strong appetite for hospitality risks",
            "Prefers accounts with current loss runs.",
            null,
            null,
            "Underwriter call"));
        appetiteResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var appointmentResponse = await _client.PostAsJsonAsync($"/carrier-markets/{created.Id}/appointments", new CarrierAppointmentUpsertDto(
            "Appointed",
            ["CA", "OR"],
            "GL",
            "APT-001",
            null,
            null,
            "Direct appointment"));
        appointmentResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var detailResponse = await _client.GetAsync($"/carrier-markets/{created.Id}");
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CarrierMarketDetailDto>();
        detail.ShouldNotBeNull();
        detail!.Name.ShouldBe("Nebula Mutual");
        detail.Contacts.Count.ShouldBe(1);
        detail.AppetiteNotes.Count.ShouldBe(1);
        detail.Appointments.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateCarrierMarket_DuplicateCode_Returns409()
    {
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];
        await CreateMarketAsync("First Carrier", "DUP-CARRIER");

        var response = await _client.PostAsJsonAsync("/carrier-markets", NewMarket("Second Carrier", "dup-carrier"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCarrierMarket_WithIfMatch_PersistsAndEmitsTimeline()
    {
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];
        var created = await CreateMarketAsync("Editable Carrier", "EDIT-CARRIER");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/carrier-markets/{created.Id}")
        {
            Content = JsonContent.Create(new CarrierMarketUpdateDto(
                "Edited Carrier",
                "12345",
                "A",
                "Active",
                "Admitted",
                null,
                null,
                "edited@example.com",
                "+14155550999",
                "Updated market profile")),
        };
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{created.RowVersion}\""));

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CarrierMarketDto>();
        updated!.Name.ShouldBe("Edited Carrier");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timelineExists = await db.ActivityTimelineEvents.AnyAsync(e =>
            e.EntityType == "CarrierMarket"
            && e.EntityId == created.Id
            && e.EventType == "CarrierMarketUpdated");
        timelineExists.ShouldBeTrue();
    }

    [Fact]
    public async Task AddActivityLink_WithExistingSubmission_Returns201()
    {
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];
        var created = await CreateMarketAsync("Linked Carrier", "LINK-CARRIER");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var submissionId = await db.Submissions.AsNoTracking().OrderBy(s => s.CreatedAt).Select(s => s.Id).FirstAsync();

        var response = await _client.PostAsJsonAsync($"/carrier-markets/{created.Id}/activity-links", new CarrierMarketActivityLinkCreateDto(
            "Submission",
            submissionId,
            "Marketed",
            "Marketed during placement"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task BrokerUser_CannotReadCarrierMarkets()
    {
        TestAuthHandler.TestRole = "BrokerUser";
        TestAuthHandler.TestNebulaRoles = ["BrokerUser"];

        var response = await _client.GetAsync("/carrier-markets");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        TestAuthHandler.ResetF0009Overrides();
    }

    private async Task<CarrierMarketDto> CreateMarketAsync(string name, string code)
    {
        var response = await _client.PostAsJsonAsync("/carrier-markets", NewMarket(name, code));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, body);
        var created = await response.Content.ReadFromJsonAsync<CarrierMarketDto>();
        created.ShouldNotBeNull();
        return created!;
    }

    private static CarrierMarketCreateDto NewMarket(string name, string code) => new(
        code,
        name,
        null,
        null,
        "Prospect",
        "Admitted",
        null,
        null,
        "market@example.com",
        "+14155550111",
        "Commercial market relationship");
}
