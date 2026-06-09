using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class TerritoryEndpoints
{
    public static IEndpointRouteBuilder MapTerritoryEndpoints(this IEndpointRouteBuilder app)
    {
        var territories = app.MapGroup("/territories")
            .WithTags("Territories")
            .RequireAuthorization();
        territories.MapPost("/", CreateTerritory);
        territories.MapGet("/{territoryId:guid}/members", ListMembers);
        territories.MapPost("/{territoryId:guid}/members", AssignMember);

        var assignments = app.MapGroup("/territory-assignments")
            .WithTags("Territories")
            .RequireAuthorization();
        assignments.MapGet("/", GetAssignmentForMember);

        return app;
    }

    private static async Task<IResult> CreateTerritory(
        TerritoryCreateRequestDto dto,
        IValidator<TerritoryCreateRequestDto> validator,
        TerritoryService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "territory", "create"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var (result, error) = await svc.CreateTerritoryAsync(dto, user, ct);
        return error switch
        {
            "duplicate_name" => ProblemDetailsHelper.TerritoryDuplicateName(),
            _ => Results.Created($"/territories/{result!.Id}", result),
        };
    }

    private static async Task<IResult> ListMembers(
        Guid territoryId, DateOnly? asOf, int? page, int? pageSize,
        TerritoryService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "territory", "read"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.ListMembersAsync(territoryId, asOf, page ?? 1, Math.Min(pageSize ?? 20, 100), ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Territory", territoryId),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> AssignMember(
        Guid territoryId,
        TerritoryMemberAssignmentRequestDto dto,
        IValidator<TerritoryMemberAssignmentRequestDto> validator,
        TerritoryService svc, ICurrentUserService user, IAuthorizationService authz,
        HttpContext httpContext, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "territory", "assign"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

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
            var (result, error) = await svc.AssignMemberAsync(territoryId, dto, ifMatch, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Territory", territoryId),
                "invalid_period" => ProblemDetailsHelper.TerritoryAssignmentPeriodInvalid(),
                _ => Results.Created($"/territories/{territoryId}/members", result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.PreconditionFailed("territory");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return ProblemDetailsHelper.TerritoryAssignmentOverlap();
        }
    }

    private static async Task<IResult> GetAssignmentForMember(
        string? memberType, Guid? memberId, DateOnly? asOf,
        TerritoryService svc, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "territory", "read"))
            return ProblemDetailsHelper.Forbidden();

        if (string.IsNullOrWhiteSpace(memberType) || memberId is null || memberId == Guid.Empty)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { ["member"] = ["memberType and memberId are required."] });

        var result = await svc.GetForMemberAsync(memberType, memberId.Value, asOf, ct);
        return Results.Ok(result);
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
