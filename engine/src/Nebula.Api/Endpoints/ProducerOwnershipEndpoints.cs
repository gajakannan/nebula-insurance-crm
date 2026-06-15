using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class ProducerOwnershipEndpoints
{
    public static RouteGroupBuilder MapProducerOwnershipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/producer-ownership")
            .WithTags("ProducerOwnership")
            .RequireAuthorization();

        group.MapGet("/", GetProducerOwnership);
        group.MapPost("/", AssignProducerOwnership);

        return group;
    }

    private static async Task<IResult> GetProducerOwnership(
        string? scopeType, Guid? scopeId, DateOnly? asOf,
        ProducerOwnershipService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "producer_ownership", "read"))
            return ProblemDetailsHelper.Forbidden();

        if (string.IsNullOrWhiteSpace(scopeType) || scopeId is null || scopeId == Guid.Empty)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { ["scope"] = ["scopeType and scopeId are required."] });

        var result = await svc.GetAsOfAsync(scopeType, scopeId.Value, asOf, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignProducerOwnership(
        ProducerOwnershipAssignmentRequestDto dto,
        IValidator<ProducerOwnershipAssignmentRequestDto> validator,
        ProducerOwnershipService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "producer_ownership", "assign"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        // If-Match is optional: there is no rowVersion to match on a first assignment. When the client
        // is reassigning, it should send the current open period's rowVersion and the service enforces it.
        uint? ifMatch = null;
        var header = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(header))
        {
            if (!uint.TryParse(header.Trim('"'), out var rv))
                return Results.Problem(title: "If-Match header invalid", statusCode: 428);
            ifMatch = rv;
        }

        try
        {
            var (result, error) = await svc.AssignAsync(dto, ifMatch, user, ct);
            return error switch
            {
                "producer_not_found" => ProblemDetailsHelper.NotFound("DistributionNode (producer)", dto.ProducerNodeId),
                "invalid_period" => ProblemDetailsHelper.OwnershipPeriodInvalid(),
                _ => Results.Created($"/producer-ownership?scopeType={dto.ScopeType}&scopeId={dto.ScopeId}", result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("producer ownership");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return ProblemDetailsHelper.OwnershipPeriodOverlap();
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("23505") || msg.Contains("duplicate key value");
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
