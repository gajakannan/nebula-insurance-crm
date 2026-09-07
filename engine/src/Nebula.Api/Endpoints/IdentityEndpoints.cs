using System.Security.Claims;

namespace Nebula.Api.Endpoints;

/// <summary>
/// Minimal identity introspection endpoint used by Neuron before it touches its
/// own conversation store. JWT authentication and all token validation happen in
/// the engine middleware; this endpoint returns only the stable OIDC identity.
/// </summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/internal/identity", (HttpContext context) =>
        {
            var subject = context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var issuer = context.User.FindFirstValue("iss");
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(issuer))
                return Results.Unauthorized();

            return Results.Ok(new { subject, issuer });
        })
        .WithTags("Internal")
        .RequireAuthorization()
        .RequireRateLimiting("authenticated");

        return app;
    }
}
