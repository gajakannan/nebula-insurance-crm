using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class BrokerEndpoints
{
    public static RouteGroupBuilder MapBrokerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/brokers")
            .WithTags("Brokers")
            .RequireAuthorization();

        group.MapGet("/", ListBrokers);
        group.MapPost("/", CreateBroker);
        group.MapGet("/{brokerId:guid}", GetBroker);
        group.MapPut("/{brokerId:guid}", UpdateBroker);
        group.MapDelete("/{brokerId:guid}", DeleteBroker);
        group.MapPost("/{brokerId:guid}/reactivate", ReactivateBroker);

        return group;
    }

    private static async Task<IResult> ListBrokers(
        string? q, string? status, int? page, int? pageSize,
        BrokerService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        // BrokerUser: scope-isolated view — Casbin not consulted; scope isolation enforced by service.
        if (user.Roles.Contains("BrokerUser"))
        {
            var brokerUserResult = await svc.ListForBrokerUserAsync(user, ct);
            return Results.Ok(new { data = brokerUserResult.Data, page = brokerUserResult.Page, pageSize = brokerUserResult.PageSize, totalCount = brokerUserResult.TotalCount, totalPages = brokerUserResult.TotalPages });
        }

        if (!await HasAccessAsync(user, authz, "broker", "search"))
            return ProblemDetailsHelper.Forbidden();

        if (status is not null && status is not ("Active" or "Inactive" or "Pending"))
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { ["status"] = [$"Invalid status '{status}'. Must be Active, Inactive, or Pending."] });

        var result = await svc.ListAsync(q, status, page ?? 1, Math.Min(pageSize ?? 20, 100), user, ct);
        return Results.Ok(new { data = result.Data, page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount, totalPages = result.TotalPages });
    }

    private static async Task<IResult> CreateBroker(
        BrokerCreateDto dto,
        IValidator<BrokerCreateDto> validator,
        BrokerService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "broker", "create"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "duplicate_license" => ProblemDetailsHelper.DuplicateLicense(),
            _ => Results.Created($"/brokers/{result!.Id}", result),
        };
    }

    private static async Task<IResult> GetBroker(
        Guid brokerId, BrokerService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        // BrokerUser: scope-isolated detail — throws BrokerScopeUnresolvableException (→ 403) if cross-broker.
        if (user.Roles.Contains("BrokerUser"))
        {
            var brokerUserResult = await svc.GetByIdForBrokerUserAsync(brokerId, user, ct);
            return brokerUserResult is null ? ProblemDetailsHelper.NotFound("Broker", brokerId) : Results.Ok(brokerUserResult);
        }

        if (!await HasAccessAsync(user, authz, "broker", "read"))
            return ProblemDetailsHelper.Forbidden();

        var result = await svc.GetByIdAsync(brokerId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Broker", brokerId) : Results.Ok(result);
    }

    private static async Task<IResult> UpdateBroker(
        Guid brokerId,
        BrokerUpdateDto dto,
        IValidator<BrokerUpdateDto> validator,
        BrokerService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "broker", "update"))
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
            var (result, error) = await svc.UpdateAsync(brokerId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Broker", brokerId),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    private static async Task<IResult> DeleteBroker(
        Guid brokerId, BrokerService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "broker", "delete"))
            return ProblemDetailsHelper.Forbidden();

        var error = await svc.DeleteAsync(brokerId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Broker", brokerId),
            "active_dependencies_exist" => ProblemDetailsHelper.ActiveDependenciesExist(),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> ReactivateBroker(
        Guid brokerId,
        BrokerService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "broker", "reactivate"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.ReactivateAsync(brokerId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Broker", brokerId),
            "already_active" => ProblemDetailsHelper.AlreadyActive(),
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
