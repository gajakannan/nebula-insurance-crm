using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nebula.Tests.Integration;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public static string TestSubject { get; set; } = "test-user-001";
    public static string TestRole { get; set; } = "Admin";
    public static string TestDisplayName { get; set; } = "Test User";
    /// <summary>
    /// Optional extra nebula_roles claims (F0009). Null = emit only TestRole as nebula_roles.
    /// </summary>
    public static string[]? TestNebulaRoles { get; set; }
    /// <summary>
    /// Optional broker_tenant_id claim (F0009 BrokerUser scope). Null = not emitted.
    /// </summary>
    public static string? TestBrokerTenantId { get; set; }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new("iss", "http://test.local/application/o/nebula/"),
            new("sub", TestSubject),
            new(ClaimTypes.NameIdentifier, TestSubject),
            new("name", TestDisplayName),
            new(ClaimTypes.Name, TestDisplayName),
            new("role", TestRole),
            new(ClaimTypes.Role, TestRole),
            new("regions", "West"),
        };

        // nebula_roles: used by HttpCurrentUserService.Roles and Casbin policy checks.
        var nebulaRoles = TestNebulaRoles ?? [TestRole];
        foreach (var r in nebulaRoles)
            claims.Add(new Claim("nebula_roles", r));

        if (TestBrokerTenantId is not null)
            claims.Add(new Claim("broker_tenant_id", TestBrokerTenantId));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>Resets all optional F0009 properties to default (call in test teardown).</summary>
    public static void ResetF0009Overrides()
    {
        TestNebulaRoles = null;
        TestBrokerTenantId = null;
    }
}
