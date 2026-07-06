using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class TerritoryEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(HttpStatusCode Status, TerritoryJson? Body)> CreateTerritoryAsync(string name)
    {
        var resp = await _client.PostAsJsonAsync("/territories", new
        {
            name,
            description = (string?)null,
            criteria = new Dictionary<string, string> { ["region"] = "Northeast" },
        });
        var body = resp.StatusCode == HttpStatusCode.Created ? await resp.Content.ReadFromJsonAsync<TerritoryJson>() : null;
        return (resp.StatusCode, body);
    }

    private async Task<HttpResponseMessage> AssignMemberAsync(Guid territoryId, string memberType, Guid memberId, string effectiveFrom)
    {
        return await _client.PostAsJsonAsync($"/territories/{territoryId}/members", new
        {
            memberType,
            memberId,
            effectiveFrom,
            assignmentReason = (string?)null,
        });
    }

    [Fact]
    public async Task CreateTerritory_Returns201()
    {
        var (status, body) = await CreateTerritoryAsync($"Northeast {Guid.NewGuid():N}");
        status.ShouldBe(HttpStatusCode.Created);
        body.ShouldNotBeNull();
        body!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTerritory_DuplicateActiveName_Returns409()
    {
        var name = $"Dup Territory {Guid.NewGuid():N}";
        (await CreateTerritoryAsync(name)).Status.ShouldBe(HttpStatusCode.Created);
        (await CreateTerritoryAsync(name)).Status.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTerritory_RoleWithoutCreate_Returns403()
    {
        TestAuthHandler.TestNebulaRoles = ["DistributionUser"]; // territory read yes, create no
        try
        {
            var (status, _) = await CreateTerritoryAsync($"Forbidden {Guid.NewGuid():N}");
            status.ShouldBe(HttpStatusCode.Forbidden);
        }
        finally { TestAuthHandler.TestNebulaRoles = null; }
    }

    [Fact]
    public async Task AssignMember_Returns201_AndListMembersShowsIt()
    {
        var (_, territory) = await CreateTerritoryAsync($"Members {Guid.NewGuid():N}");
        var member = Guid.NewGuid();

        var resp = await AssignMemberAsync(territory!.Id, "Broker", member, "2026-01-01");
        resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<PagedAssignmentsJson>(
            $"/territories/{territory.Id}/members?asOf=2026-02-01&page=1&pageSize=20");
        list!.Data.Select(a => a.MemberId).ShouldContain(member);
    }

    [Fact]
    public async Task AssignMember_Reassign_AsOfReadsAreCorrect()
    {
        var (_, territory) = await CreateTerritoryAsync($"Reassign {Guid.NewGuid():N}");
        var member = Guid.NewGuid();

        (await AssignMemberAsync(territory!.Id, "Producer", member, "2026-01-01")).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await AssignMemberAsync(territory.Id, "Producer", member, "2026-04-01")).StatusCode.ShouldBe(HttpStatusCode.Created);

        var early = await _client.GetFromJsonAsync<LookupJson>(
            $"/territory-assignments?memberType=Producer&memberId={member}&asOf=2026-02-01");
        var late = await _client.GetFromJsonAsync<LookupJson>(
            $"/territory-assignments?memberType=Producer&memberId={member}&asOf=2026-05-01");

        early!.Assignment!.EffectiveFrom.ShouldStartWith("2026-01-01");
        late!.Assignment!.EffectiveFrom.ShouldStartWith("2026-04-01");
    }

    [Fact]
    public async Task AssignMember_ToDifferentTerritory_ClosesPriorOpenAssignment()
    {
        var (_, first) = await CreateTerritoryAsync($"ReassignFrom {Guid.NewGuid():N}");
        var (_, second) = await CreateTerritoryAsync($"ReassignTo {Guid.NewGuid():N}");
        var member = Guid.NewGuid();

        (await AssignMemberAsync(first!.Id, "Producer", member, "2026-01-01")).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await AssignMemberAsync(second!.Id, "Producer", member, "2026-04-01")).StatusCode.ShouldBe(HttpStatusCode.Created);

        var early = await _client.GetFromJsonAsync<LookupJson>(
            $"/territory-assignments?memberType=Producer&memberId={member}&asOf=2026-02-01");
        var late = await _client.GetFromJsonAsync<LookupJson>(
            $"/territory-assignments?memberType=Producer&memberId={member}&asOf=2026-05-01");
        var firstLateList = await _client.GetFromJsonAsync<PagedAssignmentsJson>(
            $"/territories/{first.Id}/members?asOf=2026-05-01&page=1&pageSize=20");
        var secondLateList = await _client.GetFromJsonAsync<PagedAssignmentsJson>(
            $"/territories/{second.Id}/members?asOf=2026-05-01&page=1&pageSize=20");

        early!.Assignment!.TerritoryId.ShouldBe(first.Id);
        late!.Assignment!.TerritoryId.ShouldBe(second.Id);
        firstLateList!.Data.Select(a => a.MemberId).ShouldNotContain(member);
        secondLateList!.Data.Select(a => a.MemberId).ShouldContain(member);
    }

    [Fact]
    public async Task AssignMember_BackdateBeforeOpen_Returns422()
    {
        var (_, territory) = await CreateTerritoryAsync($"Backdate {Guid.NewGuid():N}");
        var member = Guid.NewGuid();
        (await AssignMemberAsync(territory!.Id, "Broker", member, "2026-04-01")).StatusCode.ShouldBe(HttpStatusCode.Created);

        (await AssignMemberAsync(territory.Id, "Broker", member, "2026-03-01")).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AssignMember_TerritoryNotFound_Returns404()
    {
        var resp = await AssignMemberAsync(Guid.NewGuid(), "Broker", Guid.NewGuid(), "2026-01-01");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignMember_RoleWithoutAssign_Returns403()
    {
        var (_, territory) = await CreateTerritoryAsync($"ForbiddenAssign {Guid.NewGuid():N}");
        TestAuthHandler.TestNebulaRoles = ["DistributionUser"]; // territory read yes, assign no
        try
        {
            var resp = await AssignMemberAsync(territory!.Id, "Broker", Guid.NewGuid(), "2026-01-01");
            resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
        finally { TestAuthHandler.TestNebulaRoles = null; }
    }

    [Fact]
    public async Task GetAssignmentForMember_NoAssignment_ReturnsNull()
    {
        var result = await _client.GetFromJsonAsync<LookupJson>(
            $"/territory-assignments?memberType=Broker&memberId={Guid.NewGuid()}");
        result!.Assignment.ShouldBeNull();
    }

    private record TerritoryJson(Guid Id, string Name, string? Description,
        Dictionary<string, string> Criteria, bool IsActive, string RowVersion);

    private record AssignmentJson(Guid Id, Guid TerritoryId, string? TerritoryName, string MemberType,
        Guid MemberId, string? MemberDisplayName, string EffectiveFrom, string? EffectiveTo, string? AssignmentReason, string RowVersion);

    private record PagedAssignmentsJson(IReadOnlyList<AssignmentJson> Data, int Page, int PageSize, int TotalCount, int TotalPages);

    private record LookupJson(string MemberType, Guid MemberId, string AsOf, AssignmentJson? Assignment);
}
