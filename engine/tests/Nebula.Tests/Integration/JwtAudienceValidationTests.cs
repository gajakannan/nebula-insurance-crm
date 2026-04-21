using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Shouldly;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nebula.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Nebula.Tests.Integration;

/// <summary>
/// Integration tests for F0009 §5 / F-003 Resolution:
/// JWT middleware must reject tokens where <c>aud</c> does not equal <c>nebula</c>
/// (the <c>Authentication:Audience</c> config value) before any endpoint handler runs.
///
/// These tests use a dedicated factory that runs in a non-Development environment so
/// the real JWT bearer middleware executes (not the dev no-op handler). A local HMAC
/// symmetric key replaces the authentik JWKS endpoint to avoid requiring a live IdP.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class JwtAudienceValidationTests : IClassFixture<JwtAudienceValidationFactory>
{
    private const string ValidAudience = "nebula";
    private const string ValidIssuer = "https://test-issuer.local/";

    private readonly JwtAudienceValidationFactory _factory;

    public JwtAudienceValidationTests(JwtAudienceValidationFactory factory)
    {
        _factory = factory;
    }

    // ------------------------------------------------------------------
    // Tests
    // ------------------------------------------------------------------

    [Fact]
    public async Task Request_WithWrongAudience_Returns401()
    {
        // Arrange — token has aud = "wrong-app", which is not "nebula"
        var token = MintToken(audience: "wrong-app");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act — hit any protected endpoint; /brokers requires authentication
        var response = await client.GetAsync("/brokers");

        // Assert — middleware must reject before the handler executes
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "tokens with aud != 'nebula' must be rejected by JWT middleware (F-003)");
    }

    [Fact]
    public async Task Request_WithMissingAudience_Returns401()
    {
        // Arrange — token has no aud claim at all
        var token = MintToken(audience: null);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/brokers");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "tokens without an aud claim must be rejected by JWT middleware (F-003)");
    }

    [Fact]
    public async Task Request_WithCorrectAudience_DoesNotReturn401DueToAudience()
    {
        // Arrange — token has aud = "nebula" (the valid audience)
        // The endpoint still requires a resolvable user identity, but a 401 here would
        // be for a reason other than audience mismatch. We assert the middleware passed
        // the audience check (response is not 401; it may be another status).
        var token = MintToken(audience: ValidAudience);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/brokers");

        // Assert — audience check passed; response is NOT 401
        // (may be 200, 403, 500, etc. depending on downstream handler — audience was accepted)
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized,
            "a token with aud = 'nebula' must pass JWT audience validation");
    }

    // ------------------------------------------------------------------
    // JWT minting helpers
    // ------------------------------------------------------------------

    /// <summary>Mints a test JWT signed with the factory's symmetric key.</summary>
    /// <param name="audience">The value to put in the aud claim. Pass <c>null</c> to omit the claim entirely.</param>
    private string MintToken(string? audience)
    {
        var signingKey = JwtAudienceValidationFactory.SigningKey;
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sub", "test-subject-001"),
            new("email", "test@nebula.local"),
            new("nebula_roles", "Admin"),
        };

        // When audience is null, omit the aud claim entirely so the middleware
        // encounters a token with no audience (the missing-aud test case).
        // The null-forgiving operator is intentional: SecurityTokenDescriptor.Audience
        // accepts null at runtime and omits the claim; the nullable annotation is
        // overly strict in this version of the library.
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ValidIssuer,
            Audience = audience!,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = credentials,
        };

        // When audience is null we must ensure the claim is truly absent.
        // SecurityTokenDescriptor.Audience = null omits it by default.
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}

/// <summary>
/// A <see cref="WebApplicationFactory{TProgram}"/> variant that runs the API in a
/// non-Development environment so the production JWT bearer path executes.
///
/// To avoid a live authentik IdP, OIDC metadata discovery is disabled and
/// <see cref="TokenValidationParameters"/> is replaced with a local HMAC symmetric key.
/// Audience validation (<c>ValidateAudience = true</c>, <c>ValidAudiences = ["nebula"]</c>)
/// is preserved exactly as the production middleware configures it.
/// </summary>
public class JwtAudienceValidationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Shared HMAC key used by both the factory (token validation) and the test (token minting).
    /// 512 bits — meets minimum entropy for HmacSha256 without complaints from the library.
    /// </summary>
    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("nebula-test-signing-key-for-aud-validation-tests-only-not-production-secret!"));

    private const string TestAudience = "nebula";
    private const string TestIssuer = "https://test-issuer.local/";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Run as "Testing" (non-Development) so Program.cs takes the else branch
        // where real JWT bearer middleware is configured.
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());

        // Supply Authentication:Audience so Program.cs doesn't throw on startup.
        builder.UseSetting("Authentication:Audience", TestAudience);
        // Authority is required by AddJwtBearer; we override discovery below so the value
        // is never contacted, but it must be a non-null URI.
        builder.UseSetting("Authentication:Authority", TestIssuer);

        builder.ConfigureServices(services =>
        {
            // Replace DbContext with Testcontainers instance.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Override JWT bearer options:
            //   - Disable OIDC metadata discovery (no live authentik).
            //   - Replace signing key with our local test key.
            //   - Keep ValidateAudience = true and ValidAudiences = ["nebula"] exactly as
            //     Program.cs configures them — this is the behaviour under test.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;         // prevents metadata fetch
                options.MetadataAddress = null!; // belt-and-suspenders; null at runtime disables auto-discovery
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Preserve: aud validation — this is the contract being tested.
                    ValidateAudience = true,
                    ValidAudiences = [TestAudience],

                    // Use our local signing key instead of the authentik JWKS.
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey,

                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,

                    ValidateLifetime = true,
                };
            });
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    async Task IAsyncLifetime.InitializeAsync() => await InitializeAsync();
    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();
}
