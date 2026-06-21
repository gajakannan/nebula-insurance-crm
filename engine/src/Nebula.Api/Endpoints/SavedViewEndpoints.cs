using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Api.Endpoints;

public static class SavedViewEndpoints
{
    public static IEndpointRouteBuilder MapSavedViewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/saved-views")
            .WithTags("SavedViews")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", List);
        group.MapPost("/", Create);
        group.MapGet("/{savedViewId:guid}", Get);
        group.MapPatch("/{savedViewId:guid}", Update);
        group.MapDelete("/{savedViewId:guid}", Archive);
        group.MapPut("/{savedViewId:guid}/default", SetDefault);

        return app;
    }

    private static async Task<IResult> List(
        string? viewType, string? visibility, bool? includeArchived, int? page, int? pageSize,
        ISavedViewService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "saved_view", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var query = new SavedViewListQuery(viewType, visibility, includeArchived ?? false,
            page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100));
        return Results.Ok(await svc.ListAsync(query, user, ct));
    }

    private static async Task<IResult> Get(
        Guid savedViewId, ISavedViewService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "saved_view", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var view = await svc.GetAsync(savedViewId, user, ct);
        return view is null ? ProblemDetailsHelper.NotFound("Saved view", savedViewId) : Results.Ok(view);
    }

    private static async Task<IResult> Create(
        SavedViewUpsertRequestDto dto, IValidator<SavedViewUpsertRequestDto> validator,
        ISavedViewService svc, ICurrentUserService user, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        try
        {
            var (result, error) = await svc.CreateAsync(dto, user, ct);
            return error is not null ? MapError(error, Guid.Empty) : Results.Created($"/saved-views/{result!.Id}", result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("saved view");
        }
    }

    private static async Task<IResult> Update(
        Guid savedViewId, SavedViewUpsertRequestDto dto, IValidator<SavedViewUpsertRequestDto> validator,
        HttpContext httpContext, ISavedViewService svc, ICurrentUserService user, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (rowVersion, ifMatchError) = ParseIfMatch(httpContext);
        if (ifMatchError is not null)
            return ifMatchError;

        try
        {
            var (result, error) = await svc.UpdateAsync(savedViewId, dto, rowVersion!.Value, user, ct);
            return error is not null ? MapError(error, savedViewId) : Results.Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("saved view");
        }
    }

    private static async Task<IResult> Archive(
        Guid savedViewId, HttpContext httpContext, ISavedViewService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (rowVersion, ifMatchError) = ParseIfMatch(httpContext);
        if (ifMatchError is not null)
            return ifMatchError;

        try
        {
            var error = await svc.ArchiveAsync(savedViewId, rowVersion!.Value, user, ct);
            return error is not null ? MapError(error, savedViewId) : Results.NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("saved view");
        }
    }

    private static async Task<IResult> SetDefault(
        Guid savedViewId, HttpContext httpContext, ISavedViewService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (rowVersion, ifMatchError) = ParseIfMatch(httpContext);
        if (ifMatchError is not null)
            return ifMatchError;

        try
        {
            var (result, error) = await svc.SetDefaultAsync(savedViewId, rowVersion!.Value, user, ct);
            return error is not null ? MapError(error, savedViewId) : Results.Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("saved view");
        }
    }

    private static (uint? RowVersion, IResult? Error) ParseIfMatch(HttpContext ctx)
    {
        var ifMatch = ctx.Request.Headers.IfMatch.FirstOrDefault();
        if (string.IsNullOrEmpty(ifMatch) || !uint.TryParse(ifMatch.Trim('"'), out var rv))
            return (null, Results.Problem(title: "If-Match header required", statusCode: 428));
        return (rv, null);
    }

    private static IResult ValidationProblem(FluentValidation.Results.ValidationResult validation) =>
        ProblemDetailsHelper.ValidationError(
            validation.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

    private static IResult MapError(string error, Guid id) => error switch
    {
        "not_found" => ProblemDetailsHelper.NotFound("Saved view", id),
        "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
        "saved_view_scope_required" => ProblemDetailsHelper.SavedViewScopeRequired(),
        "saved_view_scope_denied" => ProblemDetailsHelper.SavedViewScopeDenied(),
        "saved_view_duplicate_name" => ProblemDetailsHelper.SavedViewDuplicateName(),
        "saved_view_criteria_invalid" => ProblemDetailsHelper.SavedViewCriteriaInvalid(),
        "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("saved view"),
        _ => Results.Problem(title: "Unexpected error", statusCode: 500),
    };
}
