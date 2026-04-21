using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class DashboardEndpoints
{
    private static readonly HashSet<string> SupportedOutcomeKeys =
    [
        "bound",
        "no_quote",
        "declined",
        "expired",
        "lost_competitor",
    ];

    private static readonly HashSet<string> SupportedBreakdownGroupBy =
    [
        "assigneduser",
        "broker",
        "program",
        "lineofbusiness",
        "brokerstate",
    ];

    private static readonly HashSet<string> SupportedEntityTypes =
    [
        "submission",
        "renewal",
    ];

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/kpis", GetKpis);
        group.MapGet("/opportunities", GetOpportunities);
        group.MapGet("/opportunities/outcomes", GetOpportunityOutcomes);
        group.MapGet("/opportunities/outcomes/{outcomeKey}/items", GetOpportunityOutcomeItems);
        group.MapGet("/opportunities/flow", GetOpportunityFlow);
        group.MapGet("/opportunities/{entityType}/{status}/items", GetOpportunityItems);
        group.MapGet("/opportunities/{entityType}/{status}/breakdown", GetOpportunityBreakdown);
        group.MapGet("/opportunities/aging", GetOpportunityAging);
        group.MapGet("/opportunities/hierarchy", GetOpportunityHierarchy);
        group.MapGet("/nudges", GetNudges);

        return app;
    }

    private static async Task<IResult> GetKpis(
        int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_kpi"))
            return ProblemDetailsHelper.Forbidden();
        return Results.Ok(await svc.GetKpisAsync(user, periodDays ?? 90, ct));
    }

    private static async Task<IResult> GetOpportunities(
        int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();
        return Results.Ok(await svc.GetOpportunitiesAsync(user, periodDays ?? 180, ct));
    }

    private static async Task<IResult> GetOpportunityFlow(
        string entityType, int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();

        if (entityType is not ("submission" or "renewal"))
        {
            return Results.BadRequest(new
            {
                code = "invalid_entity_type",
                message = "entityType must be 'submission' or 'renewal'.",
            });
        }

        return Results.Ok(await svc.GetOpportunityFlowAsync(user, entityType, periodDays ?? 180, ct));
    }

    private static async Task<IResult> GetOpportunityItems(
        string entityType, string status,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();
        return Results.Ok(await svc.GetOpportunityItemsAsync(user, entityType, status, ct));
    }

    private static async Task<IResult> GetOpportunityBreakdown(
        string entityType,
        string status,
        string? groupBy,
        int? periodDays,
        DashboardService svc,
        IAuthorizationService authz,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();

        if (entityType is not ("submission" or "renewal"))
        {
            return Results.BadRequest(new
            {
                code = "invalid_entity_type",
                message = "entityType must be 'submission' or 'renewal'.",
            });
        }

        var normalizedGroupBy = groupBy?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedGroupBy) || !SupportedBreakdownGroupBy.Contains(normalizedGroupBy))
        {
            return Results.BadRequest(new
            {
                code = "invalid_group_by",
                message = "groupBy must be one of: assignedUser, broker, program, lineOfBusiness, brokerState.",
            });
        }

        return Results.Ok(await svc.GetOpportunityBreakdownAsync(user, entityType, status, normalizedGroupBy, periodDays ?? 180, ct));
    }

    private static async Task<IResult> GetOpportunityOutcomes(
        string? entityTypes,
        int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();

        if (!TryParseEntityTypes(entityTypes, out var parsedEntityTypes, out var errorResult))
            return errorResult!;

        return Results.Ok(await svc.GetOpportunityOutcomesAsync(user, periodDays ?? 180, parsedEntityTypes, ct));
    }

    private static async Task<IResult> GetOpportunityOutcomeItems(
        string outcomeKey,
        string? entityTypes,
        int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();

        var normalizedKey = outcomeKey.Trim().ToLowerInvariant();
        if (!SupportedOutcomeKeys.Contains(normalizedKey))
        {
            return Results.BadRequest(new
            {
                code = "invalid_outcome_key",
                message = "outcomeKey must be one of: bound, no_quote, declined, expired, lost_competitor.",
            });
        }

        if (!TryParseEntityTypes(entityTypes, out var parsedEntityTypes, out var errorResult))
            return errorResult!;

        return Results.Ok(await svc.GetOpportunityOutcomeItemsAsync(user, normalizedKey, periodDays ?? 180, parsedEntityTypes, ct));
    }

    private static async Task<IResult> GetOpportunityAging(
        string entityType, int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();

        if (entityType is not ("submission" or "renewal"))
        {
            return Results.BadRequest(new
            {
                code = "invalid_entity_type",
                message = "entityType must be 'submission' or 'renewal'.",
            });
        }

        return Results.Ok(await svc.GetOpportunityAgingAsync(user, entityType, periodDays ?? 180, ct));
    }

    private static async Task<IResult> GetOpportunityHierarchy(
        int? periodDays,
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_pipeline"))
            return ProblemDetailsHelper.Forbidden();

        return Results.Ok(await svc.GetOpportunityHierarchyAsync(user, periodDays ?? 180, ct));
    }

    private static async Task<IResult> GetNudges(
        DashboardService svc, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "dashboard_nudge"))
            return ProblemDetailsHelper.Forbidden();

        // BrokerUser: scope-isolated nudges — OverdueTask only, linked to broker scope (F0009 §14).
        if (user.Roles.Contains("BrokerUser"))
            return Results.Ok(await svc.GetNudgesForBrokerUserAsync(user, ct));

        return Results.Ok(await svc.GetNudgesAsync(user.UserId, user, ct));
    }

    private static async Task<bool> HasAccessAsync(
        ICurrentUserService user, IAuthorizationService authz, string resource)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, resource, "read"))
                return true;
        }
        return false;
    }

    private static bool TryParseEntityTypes(
        string? entityTypes,
        out IReadOnlyList<string>? parsedEntityTypes,
        out IResult? errorResult)
    {
        errorResult = null;

        if (string.IsNullOrWhiteSpace(entityTypes))
        {
            parsedEntityTypes = null;
            return true;
        }

        var normalized = entityTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalized.Count == 0)
        {
            parsedEntityTypes = null;
            return true;
        }

        if (normalized.Any(value => !SupportedEntityTypes.Contains(value)))
        {
            parsedEntityTypes = null;
            errorResult = Results.BadRequest(new
            {
                code = "invalid_entity_types",
                message = "entityTypes must be a comma-separated list containing only 'submission' or 'renewal'.",
            });
            return false;
        }

        parsedEntityTypes = normalized;
        return true;
    }
}
