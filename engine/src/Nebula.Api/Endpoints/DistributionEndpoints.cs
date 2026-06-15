using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class DistributionEndpoints
{
    public static RouteGroupBuilder MapDistributionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/distribution-nodes")
            .WithTags("DistributionHierarchy")
            .RequireAuthorization();

        group.MapPut("/{nodeId:guid}/parent", SetParent);
        group.MapGet("/{nodeId:guid}/ancestors", GetAncestors);
        group.MapGet("/{nodeId:guid}/descendants", ListDescendants);

        return group;
    }

    private static async Task<IResult> SetParent(
        Guid nodeId,
        DistributionNodeParentRequestDto dto,
        IValidator<DistributionNodeParentRequestDto> validator,
        DistributionNodeService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "distribution_node", "update"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var ifMatch = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        if (string.IsNullOrEmpty(ifMatch) || !uint.TryParse(ifMatch.Trim('"'), out var rowVersion))
            return Results.Problem(title: "If-Match header required", statusCode: 428);

        try
        {
            var (result, error) = await svc.SetParentAsync(nodeId, dto.ParentId, dto.Note, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("DistributionNode", nodeId),
                "self_parent" => ProblemDetailsHelper.DistributionNodeSelfParent(),
                "invalid_parent" => ProblemDetailsHelper.InvalidDistributionParent(),
                "cycle" => ProblemDetailsHelper.DistributionNodeCycle(),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("distribution node");
        }
    }

    private static async Task<IResult> GetAncestors(
        Guid nodeId, DistributionNodeService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "distribution_node", "read"))
            return ProblemDetailsHelper.Forbidden();

        var result = await svc.GetAncestorsAsync(nodeId, ct);
        return result is null ? ProblemDetailsHelper.NotFound("DistributionNode", nodeId) : Results.Ok(result);
    }

    private static async Task<IResult> ListDescendants(
        Guid nodeId, int? depth, int? page, int? pageSize,
        DistributionNodeService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "distribution_node", "read"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.ListDescendantsAsync(nodeId, depth, page ?? 1, Math.Min(pageSize ?? 20, 100), ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("DistributionNode", nodeId),
            _ => Results.Ok(result),
        };
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
