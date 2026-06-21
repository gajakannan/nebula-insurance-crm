using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search-results")
            .WithTags("Search")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", Search);
        return app;
    }

    private static async Task<IResult> Search(
        string? q,
        string? objectTypes,
        string? status,
        Guid? ownerUserId,
        string? region,
        string? lineOfBusiness,
        string? sort,
        int? page,
        int? pageSize,
        ISearchService svc,
        IValidator<GlobalSearchQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "global_search", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var trimmed = (q ?? string.Empty).Trim();
        if (trimmed.Length < 2)
            return ProblemDetailsHelper.SearchQueryTooShort();

        var query = new GlobalSearchQuery(
            Q: trimmed,
            ObjectTypes: AuthzHelper.ParseMulti(objectTypes),
            Status: status,
            OwnerUserId: ownerUserId,
            Region: region,
            LineOfBusiness: lineOfBusiness,
            Sort: string.IsNullOrWhiteSpace(sort) ? "relevance" : sort,
            Page: page ?? 1,
            PageSize: Math.Clamp(pageSize ?? 20, 1, 100));

        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        return Results.Ok(await svc.SearchAsync(query, user, ct));
    }
}
