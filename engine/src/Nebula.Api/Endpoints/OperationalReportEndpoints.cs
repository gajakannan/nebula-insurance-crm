using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Api.Endpoints;

public static class OperationalReportEndpoints
{
    public static IEndpointRouteBuilder MapOperationalReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/operational-reports")
            .WithTags("OperationalReports")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/workload", Workload);
        group.MapGet("/workflow-aging", WorkflowAging);

        return app;
    }

    private static async Task<IResult> Workload(
        string? region, string? lineOfBusiness, Guid? ownerUserId, string? workflowType, DateOnly? asOf, int? drilldownLimit,
        IOperationalReportService svc, IValidator<OperationalReportQuery> validator,
        ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        var (query, error) = await BuildQuery(region, lineOfBusiness, ownerUserId, workflowType, asOf, drilldownLimit, validator, user, authz, ct);
        if (error is not null) return error;
        return Results.Ok(await svc.GetWorkloadAsync(query!, user, ct));
    }

    private static async Task<IResult> WorkflowAging(
        string? region, string? lineOfBusiness, Guid? ownerUserId, string? workflowType, DateOnly? asOf, int? drilldownLimit,
        IOperationalReportService svc, IValidator<OperationalReportQuery> validator,
        ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        var (query, error) = await BuildQuery(region, lineOfBusiness, ownerUserId, workflowType, asOf, drilldownLimit, validator, user, authz, ct);
        if (error is not null) return error;
        return Results.Ok(await svc.GetWorkflowAgingAsync(query!, user, ct));
    }

    private static async Task<(OperationalReportQuery? Query, IResult? Error)> BuildQuery(
        string? region, string? lineOfBusiness, Guid? ownerUserId, string? workflowType, DateOnly? asOf, int? drilldownLimit,
        IValidator<OperationalReportQuery> validator, ICurrentUserService user, IAuthorizationService authz, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "operational_report", "read"))
            return (null, ProblemDetailsHelper.PolicyDenied());

        var query = new OperationalReportQuery(
            Region: region,
            LineOfBusiness: lineOfBusiness,
            OwnerUserId: ownerUserId,
            WorkflowType: workflowType,
            AsOf: asOf,
            DrilldownLimit: Math.Clamp(drilldownLimit ?? 50, 1, 200));

        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
            return (null, ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));

        return (query, null);
    }
}
