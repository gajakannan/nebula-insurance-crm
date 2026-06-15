using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Shouldly;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class ProducerOwnershipEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly Guid Seeder = Guid.Parse("dddd0000-0000-0000-0000-000000000002");

    private async Task<Guid> SeedProducerAsync(string displayName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var node = new DistributionNode
        {
            NodeType = "Producer",
            DisplayName = displayName,
            AncestryPath = "",
            Depth = 0,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = Seeder,
            UpdatedByUserId = Seeder,
        };
        db.DistributionNodes.Add(node);
        await db.SaveChangesAsync();
        return node.Id;
    }

    private async Task<HttpResponseMessage> AssignAsync(string scopeType, Guid scopeId, Guid producerId, string effectiveFrom, string? ifMatch = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/producer-ownership")
        {
            Content = JsonContent.Create(new
            {
                scopeType,
                scopeId,
                producerNodeId = producerId,
                effectiveFrom,
                assignmentReason = (string?)null,
            }),
        };
        if (ifMatch is not null)
            req.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{ifMatch}\""));
        return await _client.SendAsync(req);
    }

    private async Task<LookupJson> GetAsync(string scopeType, Guid scopeId, string? asOf = null)
    {
        var url = $"/producer-ownership?scopeType={scopeType}&scopeId={scopeId}" + (asOf is null ? "" : $"&asOf={asOf}");
        var resp = await _client.GetAsync(url);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await resp.Content.ReadFromJsonAsync<LookupJson>())!;
    }

    [Fact]
    public async Task Assign_FirstPeriod_Returns201_AndAsOfReadReturnsOwner()
    {
        var producer = await SeedProducerAsync("Owner P1");
        var scopeId = Guid.NewGuid();

        var resp = await AssignAsync("Account", scopeId, producer, "2026-04-01");

        resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var current = await GetAsync("Account", scopeId);
        current.Ownership.ShouldNotBeNull();
        current.Ownership!.ProducerNodeId.ShouldBe(producer);
    }

    [Fact]
    public async Task Reassign_ClosesPriorOpensNew_AsOfReadsAreCorrect()
    {
        var p1 = await SeedProducerAsync("Reassign P1");
        var p2 = await SeedProducerAsync("Reassign P2");
        var scopeId = Guid.NewGuid();

        (await AssignAsync("Account", scopeId, p1, "2026-01-01")).StatusCode.ShouldBe(HttpStatusCode.Created);
        var open = await GetAsync("Account", scopeId);
        (await AssignAsync("Account", scopeId, p2, "2026-04-01", open.Ownership!.RowVersion))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        (await GetAsync("Account", scopeId, "2026-02-01")).Ownership!.ProducerNodeId.ShouldBe(p1);
        (await GetAsync("Account", scopeId, "2026-05-01")).Ownership!.ProducerNodeId.ShouldBe(p2);
    }

    [Fact]
    public async Task Assign_BackdateBeforeOpenPeriod_Returns422()
    {
        var p1 = await SeedProducerAsync("Backdate P1");
        var scopeId = Guid.NewGuid();
        (await AssignAsync("Account", scopeId, p1, "2026-04-01")).StatusCode.ShouldBe(HttpStatusCode.Created);

        var resp = await AssignAsync("Account", scopeId, p1, "2026-03-01");

        resp.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Assign_ProducerNotFound_Returns404()
    {
        var resp = await AssignAsync("Account", Guid.NewGuid(), Guid.NewGuid(), "2026-04-01");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assign_RoleWithoutAssign_Returns403()
    {
        var producer = await SeedProducerAsync("Forbidden assign");
        TestAuthHandler.TestNebulaRoles = ["DistributionUser"]; // read yes, assign no
        try
        {
            var resp = await AssignAsync("Account", Guid.NewGuid(), producer, "2026-04-01");
            resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
        finally
        {
            TestAuthHandler.TestNebulaRoles = null;
        }
    }

    [Fact]
    public async Task Get_NoOwnership_ReturnsNullOwnership()
    {
        var result = await GetAsync("Account", Guid.NewGuid());
        result.Ownership.ShouldBeNull();
    }

    private record OwnershipJson(Guid Id, string ScopeType, Guid ScopeId, Guid ProducerNodeId,
        string? ProducerDisplayName, string EffectiveFrom, string? EffectiveTo, string? AssignmentReason, string RowVersion);

    private record LookupJson(string ScopeType, Guid ScopeId, string AsOf, OwnershipJson? Ownership);
}
