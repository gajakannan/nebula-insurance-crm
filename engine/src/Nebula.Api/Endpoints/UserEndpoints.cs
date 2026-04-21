using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users", SearchUsers)
            .WithTags("Users")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        return app;
    }

    private static async Task<IResult> SearchUsers(
        string? q,
        bool? activeOnly,
        int? limit,
        UserService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(q) || q.Length < 2)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]>
                {
                    ["q"] = ["Search query must be at least 2 characters."],
                });

        var effectiveLimit = Math.Min(limit ?? 20, 50);
        var result = await svc.SearchAsync(q, activeOnly ?? true, effectiveLimit, user, ct);

        if (result is null)
            return ProblemDetailsHelper.Forbidden();

        return Results.Ok(result);
    }
}
