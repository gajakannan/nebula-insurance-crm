using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class LobSchemaEndpoints
{
    public static IEndpointRouteBuilder MapLobSchemaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/lob-schemas")
            .WithTags("LOB Schemas")
            .RequireAuthorization();

        group.MapGet("/active", ListActiveBundles);
        group.MapGet("/active/{productKey}/{productVersion}/{schemaVersion}", ResolveActiveBundle);
        group.MapGet("/{productVersionId:guid}/{stage}", ResolveBundleByProductVersion);
        group.MapPost("/{productVersionId:guid}/activate", ActivateProductVersion);
        group.MapGet("/{bundleId:guid}", GetBundle);

        return app;
    }

    private static async Task<IResult> ListActiveBundles(
        string? productKey,
        string? lineOfBusiness,
        LobSchemaService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "lob_schema", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        return Results.Ok(await svc.ListActiveAsync(productKey, lineOfBusiness, ct));
    }

    private static async Task<IResult> ResolveActiveBundle(
        string productKey,
        string productVersion,
        string schemaVersion,
        string? lineOfBusiness,
        LobSchemaService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "lob_schema", "resolve"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.ResolveActiveAsync(productKey, productVersion, schemaVersion, lineOfBusiness, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetBundle(
        Guid bundleId,
        LobSchemaService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "lob_schema", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.GetBundleAsync(bundleId, ct);
        return result is null ? ProblemDetailsHelper.NotFound("LOB schema bundle", bundleId) : Results.Ok(result);
    }

    private static async Task<IResult> ResolveBundleByProductVersion(
        Guid productVersionId,
        string stage,
        LobSchemaService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "lob_schema", "resolve"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.GetBundleByProductVersionAsync(productVersionId, stage, ct);
        return result is null ? ProblemDetailsHelper.NotFound("LOB product version", productVersionId) : Results.Ok(result);
    }

    private static async Task<IResult> ActivateProductVersion(
        Guid productVersionId,
        LobBundleActivationRequestDto request,
        LobSchemaService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "lob_schema", "activate"))
            return ProblemDetailsHelper.PolicyDenied();

        var (result, error) = await svc.ActivateProductVersionAsync(productVersionId, request, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("LOB product version", productVersionId),
            _ => Results.Ok(result),
        };
    }

    private static async Task<bool> HasAccessAsync(
        ICurrentUserService user,
        IAuthorizationService authz,
        string resource,
        string action)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, resource, action))
                return true;
        }

        return false;
    }
}
