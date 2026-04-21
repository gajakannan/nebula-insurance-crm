using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Shouldly;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class DashboardReadAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardReadAuthorizationTests(CustomWebApplicationFactory factory)
    {
        var appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "FixedBrokerUser";
                        options.DefaultChallengeScheme = "FixedBrokerUser";
                    })
                    .AddScheme<AuthenticationSchemeOptions, FixedBrokerUserAuthHandler>("FixedBrokerUser", _ => { });
            });
        });

        _client = appFactory.CreateClient();
    }

    [Theory]
    [InlineData("/dashboard/kpis")]
    [InlineData("/dashboard/opportunities")]
    [InlineData("/dashboard/opportunities/flow?entityType=submission")]
    [InlineData("/dashboard/opportunities/submission/Received/breakdown?groupBy=broker")]
    [InlineData("/dashboard/opportunities/outcomes")]
    [InlineData("/dashboard/opportunities/outcomes/bound/items")]
    [InlineData("/dashboard/opportunities/aging?entityType=submission")]
    [InlineData("/dashboard/opportunities/hierarchy")]
    public async Task AggregateDashboardEndpoints_AsBrokerUser_ReturnForbidden(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed class FixedBrokerUserAuthHandler(
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
                new("sub", "fixed-broker-user"),
                new(ClaimTypes.NameIdentifier, "fixed-broker-user"),
                new("name", "Fixed BrokerUser"),
                new(ClaimTypes.Name, "Fixed BrokerUser"),
                new("role", "BrokerUser"),
                new(ClaimTypes.Role, "BrokerUser"),
                new("nebula_roles", "BrokerUser"),
                new("broker_tenant_id", "broker-tenant-xyz"),
                new("regions", "West"),
            };

            var identity = new ClaimsIdentity(claims, "FixedBrokerUser");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "FixedBrokerUser");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
