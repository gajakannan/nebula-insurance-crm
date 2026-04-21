namespace Nebula.Api.Endpoints;

/// <summary>
/// Authentication lifecycle endpoints.
/// </summary>
public static class AuthEndpoints
{
    // Cookie name shared with the frontend token-storage layer.
    internal const string RefreshTokenCookieName = "refresh_token";

    // Revocation path relative to the authentik application base.
    // Authority = http(s)://<host>/application/o/nebula/
    // Revocation = http(s)://<host>/application/o/nebula/revoke/
    internal const string RevocationPathSuffix = "revoke/";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth")
            .AllowAnonymous(); // §2.1: endpoint must accept unauthenticated requests

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Revoke the refresh token and clear the session cookie.");

        return app;
    }

    /// <summary>
    /// POST /auth/logout
    ///
    /// Reads the refresh_token httpOnly cookie, best-effort revokes it at the authentik
    /// revocation endpoint, then responds 204 with a Set-Cookie that clears the cookie
    /// regardless of revocation success.
    ///
    /// Accepts unauthenticated requests — the session may already be invalid (401 path).
    /// </summary>
    internal static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await RevokeRefreshTokenAsync(refreshToken, configuration, httpClientFactory, logger, ct);
        }
        else
        {
            logger.LogDebug("POST /auth/logout: no refresh_token cookie present — skipping revocation (graceful no-op).");
        }

        // Clear the cookie regardless of revocation outcome.
        // §2.1: Set-Cookie: refresh_token=; Max-Age=0; HttpOnly; Secure; SameSite=Strict; Path=/
        AppendClearCookie(httpContext.Response);

        return Results.NoContent();
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static async Task RevokeRefreshTokenAsync(
        string refreshToken,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken ct)
    {
        var authority = configuration["Authentication:Authority"];
        if (string.IsNullOrEmpty(authority))
        {
            // Development mode (no Authority set) — nothing to revoke.
            logger.LogDebug("POST /auth/logout: Authentication:Authority not configured — skipping revocation.");
            return;
        }

        // Authority already ends with '/': http://localhost:9000/application/o/nebula/
        // Revocation endpoint: http://localhost:9000/application/o/nebula/revoke/
        var revocationUrl = authority.TrimEnd('/') + "/" + RevocationPathSuffix;

        try
        {
            var client = httpClientFactory.CreateClient("AuthentikRevocation");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", refreshToken),
                new KeyValuePair<string, string>("token_type_hint", "refresh_token"),
            });

            var response = await client.PostAsync(revocationUrl, formContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "POST /auth/logout: authentik revocation returned non-success status {StatusCode} from {RevocationUrl}. Revocation is best-effort — continuing with cookie clear.",
                    (int)response.StatusCode,
                    revocationUrl);
            }
            else
            {
                logger.LogInformation(
                    "POST /auth/logout: refresh token successfully revoked at {RevocationUrl}.",
                    revocationUrl);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: log failure but never throw or return error to caller (§2.1).
            logger.LogWarning(ex,
                "POST /auth/logout: failed to reach authentik revocation endpoint at {RevocationUrl}. Revocation is best-effort — continuing with cookie clear.",
                revocationUrl);
        }
    }

    private static void AppendClearCookie(HttpResponse response)
    {
        // §2.1 contract: Max-Age=0; HttpOnly; Secure; SameSite=Strict; Path=/
        response.Cookies.Append(RefreshTokenCookieName, string.Empty, new CookieOptions
        {
            MaxAge = TimeSpan.Zero,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }
}
