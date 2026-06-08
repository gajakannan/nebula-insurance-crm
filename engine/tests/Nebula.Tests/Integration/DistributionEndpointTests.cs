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
public class DistributionEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly Guid Seeder = Guid.Parse("dddd0000-0000-0000-0000-000000000001");

    // Seeds a node with an explicit materialized ancestry path/depth and returns its id.
    private async Task<Guid> SeedNodeAsync(string nodeType, string displayName, Guid? parentId, string ancestryPath, int depth, int childCount = 0)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var node = new DistributionNode
        {
            NodeType = nodeType,
            DisplayName = displayName,
            ParentId = parentId,
            AncestryPath = ancestryPath,
            Depth = depth,
            ChildCount = childCount,
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

    private async Task<NodeJson> GetNodeAsync(Guid id)
    {
        var resp = await _client.GetAsync($"/distribution-nodes/{id}/ancestors");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AncestorsJson>();
        return body!.Node;
    }

    private async Task<HttpResponseMessage> SetParentAsync(Guid nodeId, Guid? parentId, string rowVersion)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"/distribution-nodes/{nodeId}/parent")
        {
            Content = JsonContent.Create(new DistributionNodeParentRequestDto(parentId, null)),
        };
        req.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{rowVersion}\""));
        return await _client.SendAsync(req);
    }

    [Fact]
    public async Task SetParent_ValidMove_Returns200_AndRecomputesAncestry()
    {
        var parent = await SeedNodeAsync("MGA", "Acme MGA setparent", null, "", 0);
        var child = await SeedNodeAsync("Broker", "NE Brokers setparent", null, "", 0);

        var childNode = await GetNodeAsync(child);
        var resp = await SetParentAsync(child, parent, childNode.RowVersion);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await resp.Content.ReadFromJsonAsync<NodeJson>();
        updated!.ParentId.ShouldBe(parent);
        updated.Depth.ShouldBe(1);
        updated.AncestryPath.ShouldBe(new[] { parent });
    }

    [Fact]
    public async Task SetParent_SelfParent_Returns422()
    {
        var node = await SeedNodeAsync("Broker", "Self parent test", null, "", 0);
        var n = await GetNodeAsync(node);

        var resp = await SetParentAsync(node, node, n.RowVersion);

        resp.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SetParent_CreatesCycle_Returns409()
    {
        var root = await SeedNodeAsync("MGA", "Cycle root", null, "", 0, childCount: 1);
        var child = await SeedNodeAsync("Broker", "Cycle child", root, $"/{root}", 1);

        // Attempt to move root under its own child → cycle.
        var rootNode = await GetNodeAsync(root);
        var resp = await SetParentAsync(root, child, rootNode.RowVersion);

        resp.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SetParent_MissingIfMatch_Returns428()
    {
        var node = await SeedNodeAsync("Broker", "No if-match", null, "", 0);
        var req = new HttpRequestMessage(HttpMethod.Put, $"/distribution-nodes/{node}/parent")
        {
            Content = JsonContent.Create(new DistributionNodeParentRequestDto(null, null)),
        };

        var resp = await _client.SendAsync(req);

        ((int)resp.StatusCode).ShouldBe(428);
    }

    [Fact]
    public async Task SetParent_NonExistentNode_Returns404()
    {
        var resp = await SetParentAsync(Guid.NewGuid(), null, "1");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetParent_RoleWithoutUpdate_Returns403()
    {
        var node = await SeedNodeAsync("Broker", "Forbidden update", null, "", 0);
        var n = await GetNodeAsync(node);

        TestAuthHandler.TestNebulaRoles = ["DistributionUser"]; // read yes, update no
        try
        {
            var resp = await SetParentAsync(node, null, n.RowVersion);
            resp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
        finally
        {
            TestAuthHandler.TestNebulaRoles = null; // restore default (Admin)
        }
    }

    [Fact]
    public async Task GetAncestors_ReturnsOrderedRootToParent()
    {
        var a = await SeedNodeAsync("MGA", "Anc A", null, "", 0, childCount: 1);
        var b = await SeedNodeAsync("Broker", "Anc B", a, $"/{a}", 1, childCount: 1);
        var c = await SeedNodeAsync("Producer", "Anc C", b, $"/{a}/{b}", 2);

        var resp = await _client.GetAsync($"/distribution-nodes/{c}/ancestors");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AncestorsJson>();
        body!.Node.Id.ShouldBe(c);
        body.Ancestors.Select(x => x.Id).ShouldBe(new[] { a, b });
    }

    [Fact]
    public async Task ListDescendants_ReturnsChildren()
    {
        var root = await SeedNodeAsync("MGA", "Desc root", null, "", 0, childCount: 2);
        var b = await SeedNodeAsync("Broker", "Desc B", root, $"/{root}", 1);
        var c = await SeedNodeAsync("Broker", "Desc C", root, $"/{root}", 1);

        var resp = await _client.GetAsync($"/distribution-nodes/{root}/descendants?depth=2&page=1&pageSize=20");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PagedNodesJson>();
        body!.Data.Select(x => x.Id).ShouldContain(b);
        body.Data.Select(x => x.Id).ShouldContain(c);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    private record NodeJson(
        Guid Id, string NodeType, string DisplayName, Guid? ParentId,
        IReadOnlyList<Guid> AncestryPath, int Depth, int ChildCount, bool IsActive, string RowVersion);

    private record AncestorsJson(NodeJson Node, IReadOnlyList<NodeJson> Ancestors);

    private record PagedNodesJson(
        IReadOnlyList<NodeJson> Data, int Page, int PageSize, int TotalCount, int TotalPages);
}
