using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class TimelineEndpoints
{
    public static IEndpointRouteBuilder MapTimelineEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/timeline/events", async (
            string entityType, Guid? entityId, int? page, int? pageSize, int? limit,
            TimelineService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct) =>
        {
            // BrokerUser: scope-isolated, approved event types only with BrokerDescription (F0009 §8.1).
            // BrokerUser path is not paginated (flat list, limit-based) and bypasses Casbin.
            if (user.Roles.Contains("BrokerUser"))
                return Results.Ok(await svc.ListEventsForBrokerUserAsync(limit ?? 20, user, ct));

            if (!await HasAccessAsync(user, authz, "timeline_event", "read"))
                return ProblemDetailsHelper.Forbidden();

            // Paginated path (F0002-S0007): page + pageSize params.
            var effectivePage = page ?? 1;
            var effectivePageSize = Math.Min(pageSize ?? 50, 100);
            var result = await svc.ListEventsPagedAsync(entityType, entityId, effectivePage, effectivePageSize, user, ct);
            return Results.Ok(new
            {
                data = result.Data,
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
            });
        })
        .WithTags("Timeline").RequireAuthorization();

        return app;
    }

    private static async Task<bool> HasAccessAsync(
        ICurrentUserService user, IAuthorizationService authz, string resource, string action)
    {
        foreach (var role in user.Roles)
            if (await authz.AuthorizeAsync(role, resource, action))
                return true;
        return false;
    }
}
