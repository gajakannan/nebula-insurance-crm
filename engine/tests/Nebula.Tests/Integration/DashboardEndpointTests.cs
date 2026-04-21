using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Shouldly;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nebula.Application.DTOs;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class DashboardEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "FixedAdmin";
                        options.DefaultChallengeScheme = "FixedAdmin";
                    })
                    .AddScheme<AuthenticationSchemeOptions, FixedAdminAuthHandler>("FixedAdmin", _ => { });
            });
        });

        _client = appFactory.CreateClient();
    }

    [Fact]
    public async Task GetKpis_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/kpis");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var kpis = await response.Content.ReadFromJsonAsync<DashboardKpisDto>();
        kpis.ShouldNotBeNull();
        kpis!.ActiveBrokers.ShouldBeGreaterThanOrEqualTo(0);
        kpis.OpenSubmissions.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetKpis_WithPeriodDays_Returns200()
    {
        var response = await _client.GetAsync("/dashboard/kpis?periodDays=30");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var kpis = await response.Content.ReadFromJsonAsync<DashboardKpisDto>();
        kpis.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetOpportunities_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var opportunities = await response.Content.ReadFromJsonAsync<DashboardOpportunitiesDto>();
        opportunities.ShouldNotBeNull();
        opportunities!.Submissions.ShouldNotBeNull();
        opportunities.Renewals.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetOpportunities_WithPeriodDays_Returns200()
    {
        var response = await _client.GetAsync("/dashboard/opportunities?periodDays=90");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var opportunities = await response.Content.ReadFromJsonAsync<DashboardOpportunitiesDto>();
        opportunities.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetOpportunityFlow_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/flow?entityType=submission&periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var flow = await response.Content.ReadFromJsonAsync<OpportunityFlowDto>();
        flow.ShouldNotBeNull();
        flow!.EntityType.ShouldBe("submission");
        flow.Nodes.ShouldNotBeNull();
        flow.Links.ShouldNotBeNull();

        foreach (var node in flow.Nodes)
        {
            if (node.AvgDwellDays is not null)
                node.AvgDwellDays.Value.ShouldBeGreaterThanOrEqualTo(0);

            if (node.IsTerminal)
            {
                node.Emphasis.ShouldBeNull();
            }
            else
            {
                node.Emphasis.ShouldBeOneOf("normal", "active", "blocked", "bottleneck");
            }
        }
    }

    [Fact]
    public async Task GetOpportunityFlow_InvalidEntityType_Returns400()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/flow?entityType=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOpportunityItems_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/submission/Received/items");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<OpportunityItemsDto>();
        items.ShouldNotBeNull();
        items!.Items.ShouldNotBeNull();
        items.TotalCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOpportunityAging_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/aging?entityType=submission&periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var aging = await response.Content.ReadFromJsonAsync<OpportunityAgingDto>();
        aging.ShouldNotBeNull();
        aging!.EntityType.ShouldBe("submission");
        aging.PeriodDays.ShouldBe(180);
        aging.Statuses.ShouldNotBeNull();
        foreach (var status in aging.Statuses)
        {
            status.Buckets.Count().ShouldBe(5);
            status.Buckets.Select(b => b.Key).ToList()
                .SequenceEqual(new[] { "0-2", "3-5", "6-10", "11-20", "21+" }).ShouldBeTrue();
            status.Total.ShouldBe(status.Buckets.Sum(b => b.Count));
            if (status.Sla is not null)
            {
                status.Sla.WarningDays.ShouldBeLessThan(status.Sla.TargetDays);
                (status.Sla.OnTimeCount + status.Sla.ApproachingCount + status.Sla.OverdueCount)
                    .ShouldBe(status.Total);
            }
        }
    }

    [Theory]
    [InlineData("assignedUser")]
    [InlineData("broker")]
    [InlineData("program")]
    [InlineData("lineOfBusiness")]
    [InlineData("brokerState")]
    public async Task GetOpportunityBreakdown_Returns200ForSupportedGroupByValues(string groupBy)
    {
        var response = await _client.GetAsync($"/dashboard/opportunities/submission/Received/breakdown?groupBy={groupBy}&periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var breakdown = await response.Content.ReadFromJsonAsync<OpportunityBreakdownDto>();
        breakdown.ShouldNotBeNull();
        breakdown!.EntityType.ShouldBe("submission");
        breakdown.Status.ShouldBe("Received");
        breakdown.PeriodDays.ShouldBe(180);
        breakdown.Total.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOpportunityBreakdown_InvalidGroupBy_Returns400()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/submission/Received/breakdown?groupBy=invalid");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOpportunityBreakdown_Unauthenticated_Returns401()
    {
        var unauthenticatedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "NoAuth";
                        options.DefaultChallengeScheme = "NoAuth";
                    })
                    .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("NoAuth", _ => { });
            });
        });

        using var unauthenticatedClient = unauthenticatedFactory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/dashboard/opportunities/submission/Received/breakdown?groupBy=broker");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("submission")]
    [InlineData("renewal")]
    public async Task GetOpportunityAging_SupportsEntityTypes(string entityType)
    {
        var response = await _client.GetAsync($"/dashboard/opportunities/aging?entityType={entityType}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var aging = await response.Content.ReadFromJsonAsync<OpportunityAgingDto>();
        aging!.EntityType.ShouldBe(entityType);
    }

    [Fact]
    public async Task GetOpportunityAging_InvalidEntityType_Returns400()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/aging?entityType=invalid");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOpportunityHierarchy_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/hierarchy?periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hierarchy = await response.Content.ReadFromJsonAsync<OpportunityHierarchyDto>();
        hierarchy.ShouldNotBeNull();
        hierarchy!.PeriodDays.ShouldBe(180);
        hierarchy.Root.ShouldNotBeNull();
        hierarchy.Root.Id.ShouldBe("root");
        hierarchy.Root.Children.ShouldNotBeNull();
        hierarchy.Root.Children!.Count.ShouldBe(2);
        hierarchy.Root.Children![0].Id.ShouldBe("submission");
        hierarchy.Root.Children[1].Id.ShouldBe("renewal");
    }

    [Fact]
    public async Task GetOpportunityHierarchy_ChildCountsRollUp()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/hierarchy");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hierarchy = await response.Content.ReadFromJsonAsync<OpportunityHierarchyDto>();
        var root = hierarchy!.Root;

        // Root count should equal sum of entity type children
        root.Count.ShouldBe(root.Children!.Sum(c => c.Count));

        // Each entity type count should equal sum of color group children
        foreach (var entityNode in root.Children!)
        {
            if (entityNode.Children is { Count: > 0 })
                entityNode.Count.ShouldBe(entityNode.Children.Sum(c => c.Count));
        }
    }

    [Fact]
    public async Task GetOpportunityOutcomes_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/outcomes?periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var outcomes = await response.Content.ReadFromJsonAsync<OpportunityOutcomesDto>();
        outcomes.ShouldNotBeNull();
        outcomes!.PeriodDays.ShouldBe(180);
        outcomes.Outcomes.ShouldNotBeNull();
        outcomes.Outcomes.ShouldContain(o => o.Key == "bound");
        outcomes.Outcomes.ShouldContain(o => o.Key == "no_quote");
        outcomes.Outcomes.ShouldContain(o => o.Key == "declined");
        outcomes.Outcomes.ShouldContain(o => o.Key == "expired");
        outcomes.Outcomes.ShouldContain(o => o.Key == "lost_competitor");
    }

    [Fact]
    public async Task GetOpportunityOutcomes_FilteredByRenewal_Returns200()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/outcomes?periodDays=180&entityTypes=renewal");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var outcomes = await response.Content.ReadFromJsonAsync<OpportunityOutcomesDto>();
        outcomes.ShouldNotBeNull();
        outcomes!.PeriodDays.ShouldBe(180);
    }

    [Fact]
    public async Task GetOpportunityOutcomes_InvalidEntityTypes_Returns400()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/outcomes?periodDays=180&entityTypes=endorsement");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOpportunityOutcomeItems_Returns200WithCorrectShape()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/outcomes/bound/items?periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<OpportunityItemsDto>();
        items.ShouldNotBeNull();
        items!.Items.ShouldNotBeNull();
        items.TotalCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetOpportunityOutcomeItems_InvalidOutcomeKey_Returns400()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/outcomes/invalid/items?periodDays=180");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOpportunityOutcomeItems_InvalidEntityTypes_Returns400()
    {
        var response = await _client.GetAsync("/dashboard/opportunities/outcomes/bound/items?periodDays=180&entityTypes=endorsement");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNudges_Returns200()
    {
        var response = await _client.GetAsync("/dashboard/nudges");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyTasks_Returns200()
    {
        var response = await _client.GetAsync("/my/tasks");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTimelineEvents_Returns200()
    {
        var response = await _client.GetAsync("/timeline/events?entityType=Broker");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed class FixedAdminAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new("iss", "http://test.local/application/o/nebula/"),
                new("sub", "fixed-admin-user"),
                new(ClaimTypes.NameIdentifier, "fixed-admin-user"),
                new("name", "Fixed Admin"),
                new(ClaimTypes.Name, "Fixed Admin"),
                new("role", "Admin"),
                new(ClaimTypes.Role, "Admin"),
                new("nebula_roles", "Admin"),
                new("regions", "West"),
            };

            var identity = new ClaimsIdentity(claims, "FixedAdmin");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "FixedAdmin");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class NoAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.Fail("Unauthenticated for test."));
    }
}
