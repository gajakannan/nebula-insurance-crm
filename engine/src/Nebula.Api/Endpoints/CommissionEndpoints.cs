using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class CommissionEndpoints
{
    public static RouteGroupBuilder MapCommissionEndpoints(this IEndpointRouteBuilder app)
    {
        var commissions = app.MapGroup("/")
            .WithTags("Commissions")
            .RequireAuthorization();

        commissions.MapGet("/expected-commissions", SearchExpectedCommissions);
        commissions.MapGet("/expected-commissions/{expectedCommissionId:guid}", GetExpectedCommission);
        commissions.MapPost("/expected-commissions/{expectedCommissionId:guid}/calculate", CalculateExpectedCommission);
        commissions.MapGet("/expected-commissions/{expectedCommissionId:guid}/adjustments", ListAdjustments);
        commissions.MapPost("/expected-commissions/{expectedCommissionId:guid}/adjustments", RequestAdjustment);
        commissions.MapPost("/commission-adjustments/{adjustmentId:guid}/decision", DecideAdjustment);
        commissions.MapGet("/commission-schedules", ListSchedules);
        commissions.MapPost("/commission-schedules", CreateSchedule);
        commissions.MapPut("/commission-schedules/{scheduleId:guid}", UpdateSchedule);
        commissions.MapPost("/producer-splits", UpsertProducerSplit);
        commissions.MapGet("/policies/{policyId:guid}/producer-splits", ListPolicySplits);
        commissions.MapGet("/revenue-attribution/rollups", GetRollups);

        return commissions;
    }

    private static async Task<IResult> SearchExpectedCommissions(
        string? search,
        string? status,
        string? exceptionState,
        Guid? policyId,
        Guid? producerId,
        Guid? brokerId,
        Guid? carrierMarketId,
        Guid? territoryId,
        DateOnly? periodStart,
        DateOnly? periodEnd,
        int? page,
        int? pageSize,
        CommissionRevenueService svc,
        IValidator<CommissionSearchQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "read"))
            return ProblemDetailsHelper.Forbidden();

        var query = new CommissionSearchQuery(search, status, exceptionState, policyId, producerId, brokerId, carrierMarketId, territoryId, periodStart, periodEnd, page ?? 1, pageSize ?? 20);
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length < 2)
            return Results.Ok(new { data = Array.Empty<ExpectedCommissionDto>(), page = 1, pageSize = query.PageSize, totalCount = 0, totalPages = 0 });

        var result = await svc.SearchAsync(query, user, ct);
        return Results.Ok(new { data = result.Data, page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount, totalPages = result.TotalPages });
    }

    private static async Task<IResult> GetExpectedCommission(
        Guid expectedCommissionId,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "read"))
            return ProblemDetailsHelper.Forbidden();

        var result = await svc.GetDetailAsync(expectedCommissionId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Expected commission", expectedCommissionId) : Results.Ok(result);
    }

    private static async Task<IResult> ListSchedules(
        Guid? carrierMarketId,
        ICommissionRepository repo,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "read"))
            return ProblemDetailsHelper.Forbidden();

        var schedules = await repo.ListSchedulesAsync(carrierMarketId, ct);
        return Results.Ok(schedules.Select(CommissionRevenueService.MapSchedule));
    }

    private static async Task<IResult> CreateSchedule(
        CommissionScheduleUpsertDto dto,
        IValidator<CommissionScheduleUpsertDto> validator,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "schedule_manage"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.CreateScheduleAsync(dto, user, ct);
        return error switch
        {
            "schedule_overlap" => Results.Problem(title: "Commission schedule overlaps", detail: "An active schedule already overlaps this scope and effective period.", statusCode: 409, extensions: Ext("commission_schedule_overlap")),
            _ => Results.Created($"/commission-schedules/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateSchedule(
        Guid scheduleId,
        CommissionScheduleUpsertDto dto,
        IValidator<CommissionScheduleUpsertDto> validator,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "schedule_manage"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return Results.Problem(title: "If-Match header required", statusCode: 428);

        try
        {
            var (result, error) = await svc.UpdateScheduleAsync(scheduleId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Commission schedule", scheduleId),
                "schedule_overlap" => Results.Problem(title: "Commission schedule overlaps", detail: "An active schedule already overlaps this scope and effective period.", statusCode: 409, extensions: Ext("commission_schedule_overlap")),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    private static async Task<IResult> UpsertProducerSplit(
        ProducerSplitAssignmentUpsertDto dto,
        IValidator<ProducerSplitAssignmentUpsertDto> validator,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "split_assign"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.UpsertSplitAsync(dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Policy", dto.PolicyId),
            "split_overlap" => Results.Problem(title: "Producer split overlaps", detail: "An active split assignment already overlaps this policy and effective period.", statusCode: 409, extensions: Ext("producer_split_overlap")),
            _ => Results.Created($"/policies/{result!.PolicyId}/producer-splits", result),
        };
    }

    private static async Task<IResult> ListPolicySplits(
        Guid policyId,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "read"))
            return ProblemDetailsHelper.Forbidden();
        return Results.Ok(await svc.ListPolicySplitsAsync(policyId, user, ct));
    }

    private static async Task<IResult> CalculateExpectedCommission(
        Guid expectedCommissionId,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "calculate"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.CalculateAsync(expectedCommissionId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Expected commission", expectedCommissionId),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> ListAdjustments(
        Guid expectedCommissionId,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "read"))
            return ProblemDetailsHelper.Forbidden();
        return Results.Ok(await svc.ListAdjustmentsAsync(expectedCommissionId, user, ct));
    }

    private static async Task<IResult> RequestAdjustment(
        Guid expectedCommissionId,
        CommissionAdjustmentRequestDto dto,
        IValidator<CommissionAdjustmentRequestDto> validator,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "adjustment_request"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.RequestAdjustmentAsync(expectedCommissionId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Expected commission", expectedCommissionId),
            _ => Results.Created($"/expected-commissions/{expectedCommissionId}/adjustments/{result!.Id}", result),
        };
    }

    private static async Task<IResult> DecideAdjustment(
        Guid adjustmentId,
        CommissionAdjustmentDecisionRequestDto dto,
        IValidator<CommissionAdjustmentDecisionRequestDto> validator,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "adjustment_approve"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.DecideAdjustmentAsync(adjustmentId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Commission adjustment", adjustmentId),
            "same_user_approval_denied" => ProblemDetailsHelper.Forbidden(),
            "adjustment_not_pending" => Results.Problem(title: "Commission adjustment is not pending", statusCode: 409, extensions: Ext("commission_adjustment_not_pending")),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> GetRollups(
        DateOnly startDate,
        DateOnly endDate,
        string? groupBy,
        Guid? producerId,
        Guid? brokerId,
        Guid? territoryId,
        Guid? carrierMarketId,
        string? status,
        string? exceptionState,
        IValidator<RevenueAttributionRollupQuery> validator,
        CommissionRevenueService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "commission", "rollup_read"))
            return ProblemDetailsHelper.Forbidden();

        var query = new RevenueAttributionRollupQuery(startDate, endDate, groupBy ?? "producer", producerId, brokerId, territoryId, carrierMarketId, status, exceptionState);
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        return Results.Ok(await svc.GetRollupsAsync(query, user, ct));
    }

    private static Task<bool> HasAccessAsync(IAuthorizationService authz, ICurrentUserService user, string resource, string action) =>
        AuthzHelper.HasPermissionAsync(authz, user, resource, action, new Dictionary<string, object> { ["subjectId"] = user.UserId });

    private static IResult ValidationProblem(FluentValidation.Results.ValidationResult validation) =>
        Results.ValidationProblem(validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

    private static Dictionary<string, object?> Ext(string code) => new() { ["code"] = code };

    private static bool TryGetRowVersion(HttpContext context, out uint rowVersion)
    {
        rowVersion = 0;
        if (!context.Request.Headers.TryGetValue("If-Match", out var values)) return false;
        var raw = values.FirstOrDefault()?.Trim('"');
        return uint.TryParse(raw, out rowVersion);
    }
}
