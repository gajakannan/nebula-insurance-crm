using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Api.Endpoints;

public static class BrokerInsightEndpoints
{
    public static IEndpointRouteBuilder MapBrokerInsightEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/broker-insights")
            .WithTags("BrokerInsights")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/scorecards", Scorecards);
        group.MapGet("/{brokerId:guid}/trends", Trends);
        group.MapGet("/{brokerId:guid}/benchmarks", Benchmarks);
        group.MapGet("/{brokerId:guid}/snapshot", Snapshot);

        return app;
    }

    private static async Task<IResult> Scorecards(
        Guid? brokerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        Guid? producerId,
        Guid? territoryId,
        Guid? programId,
        string? lineOfBusiness,
        string? region,
        int? page,
        int? pageSize,
        IBrokerInsightService svc,
        IValidator<BrokerInsightScorecardQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "broker_insight", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var query = new BrokerInsightScorecardQuery(
            brokerId,
            periodStart,
            periodEnd,
            producerId,
            territoryId,
            programId,
            lineOfBusiness,
            region,
            Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? 25, 1, 200));

        var error = await ValidateAsync(query, validator, ct);
        if (error is not null) return error;

        var result = await svc.GetScorecardsAsync(query, user, ct);
        return Results.Ok(new
        {
            items = result.Data,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
        });
    }

    private static async Task<IResult> Trends(
        Guid brokerId,
        string metricKey,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? bucket,
        int? page,
        int? pageSize,
        IBrokerInsightService svc,
        IValidator<BrokerInsightTrendQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "broker_insight", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var query = new BrokerInsightTrendQuery(
            brokerId,
            metricKey,
            periodStart,
            periodEnd,
            string.IsNullOrWhiteSpace(bucket) ? "month" : bucket,
            Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? 50, 1, 200));

        var error = await ValidateAsync(query, validator, ct);
        if (error is not null) return error;

        return await svc.GetTrendAsync(query, user, ct) is { } trend
            ? Results.Ok(trend)
            : ProblemDetailsHelper.NotFound("Broker insight", brokerId);
    }

    private static async Task<IResult> Benchmarks(
        Guid brokerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? peerSet,
        IBrokerInsightService svc,
        IValidator<BrokerInsightBenchmarkQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "broker_insight", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var query = new BrokerInsightBenchmarkQuery(
            brokerId,
            periodStart,
            periodEnd,
            string.IsNullOrWhiteSpace(peerSet) ? "visibleBrokerGroup" : peerSet);

        var error = await ValidateAsync(query, validator, ct);
        if (error is not null) return error;

        return await svc.GetBenchmarkAsync(query, user, ct) is { } benchmark
            ? Results.Ok(benchmark)
            : ProblemDetailsHelper.NotFound("Broker insight", brokerId);
    }

    private static async Task<IResult> Snapshot(
        Guid brokerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IBrokerInsightService svc,
        IValidator<BrokerInsightSnapshotQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, "broker_insight", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var query = new BrokerInsightSnapshotQuery(brokerId, periodStart, periodEnd);
        var error = await ValidateAsync(query, validator, ct);
        if (error is not null) return error;

        return await svc.GetSnapshotAsync(query, user, ct) is { } snapshot
            ? Results.Ok(snapshot)
            : ProblemDetailsHelper.NotFound("Broker insight", brokerId);
    }

    private static async Task<IResult?> ValidateAsync<T>(T query, IValidator<T> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(query, ct);
        if (validation.IsValid) return null;

        return ProblemDetailsHelper.ValidationError(
            validation.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
    }
}
