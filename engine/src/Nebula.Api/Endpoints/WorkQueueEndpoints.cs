using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class WorkQueueEndpoints
{
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapWorkQueueEndpoints(this IEndpointRouteBuilder app)
    {
        var queues = app.MapGroup("/work-queues")
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        queues.MapGet("/", ListQueues);
        queues.MapPost("/", CreateQueue);
        queues.MapGet("/{queueId:guid}", GetQueue);
        queues.MapPut("/{queueId:guid}", UpdateQueue);
        queues.MapPut("/{queueId:guid}/members", UpdateQueueMembers);
        queues.MapGet("/{queueId:guid}/items", ListQueueItems);
        queues.MapPost("/{queueId:guid}/rebalance", RebalanceQueue);

        var rules = app.MapGroup("/assignment-rules")
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        rules.MapGet("/", ListRules);
        rules.MapPost("/", CreateRule);
        rules.MapPut("/{ruleId:guid}", UpdateRule);

        var coverage = app.MapGroup("/coverage-windows")
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        coverage.MapGet("/", ListCoverageWindows);
        coverage.MapPost("/", CreateCoverageWindow);
        coverage.MapPut("/{coverageWindowId:guid}", UpdateCoverageWindow);

        app.MapPut("/queue-work-items/{queueItemId:guid}/assignment", ReassignQueueItem)
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        app.MapGet("/routing-events", ListRoutingEvents)
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        app.MapPost("/routing-events/route", RouteWork)
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        return app;
    }

    private static async Task<IResult> ListQueues(
        string? workType, string? status, int? page, int? pageSize,
        OperationsRoutingService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (items, total) = await svc.ListQueuesAsync(workType, status, page ?? 1, Math.Min(pageSize ?? 50, MaxPageSize), user, ct);
        return Results.Ok(new { items, totalCount = total });
    }

    private static async Task<IResult> GetQueue(Guid queueId, OperationsRoutingService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (result, error) = await svc.GetQueueAsync(queueId, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "queue_not_found" => ProblemDetailsHelper.NotFound("WorkQueue", queueId),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> CreateQueue(
        WorkQueueUpsertRequestDto dto,
        IValidator<WorkQueueUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.UpsertQueueAsync(null, dto, null, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "assignment_rule_conflict" => Conflict("assignment_rule_conflict", "Queue name already exists for this work type or the requested queue state conflicts with routing rules."),
            _ => Results.Created($"/work-queues/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateQueue(
        Guid queueId,
        WorkQueueUpsertRequestDto dto,
        HttpContext httpContext,
        IValidator<WorkQueueUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!TryRowVersion(httpContext, out var rowVersion, out var problem))
            return problem!;

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.UpsertQueueAsync(queueId, dto, rowVersion, user, ct);
        return QueueMutationResult(result, error, queueId);
    }

    private static Task<IResult> UpdateQueueMembers(
        Guid queueId,
        WorkQueueUpsertRequestDto dto,
        HttpContext httpContext,
        IValidator<WorkQueueUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct) =>
        UpdateQueue(queueId, dto, httpContext, validator, svc, user, ct);

    private static async Task<IResult> ListQueueItems(
        Guid queueId, string? status, int? page, int? pageSize,
        OperationsRoutingService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (items, total) = await svc.ListQueueItemsAsync(queueId, status, page ?? 1, Math.Min(pageSize ?? 50, MaxPageSize), user, ct);
        return Results.Ok(new { items, totalCount = total });
    }

    private static async Task<IResult> ListRules(Guid? queueId, string? status, OperationsRoutingService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (items, total) = await svc.ListRulesAsync(queueId, status, user, ct);
        return Results.Ok(new { items, totalCount = total });
    }

    private static async Task<IResult> CreateRule(
        AssignmentRuleUpsertRequestDto dto,
        IValidator<AssignmentRuleUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.UpsertRuleAsync(null, dto, null, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "queue_not_found" => ProblemDetailsHelper.NotFound("WorkQueue", dto.WorkQueueId),
            _ => Results.Created($"/assignment-rules/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateRule(
        Guid ruleId,
        AssignmentRuleUpsertRequestDto dto,
        HttpContext httpContext,
        IValidator<AssignmentRuleUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!TryRowVersion(httpContext, out var rowVersion, out var problem))
            return problem!;

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.UpsertRuleAsync(ruleId, dto, rowVersion, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "queue_not_found" => ProblemDetailsHelper.NotFound("WorkQueue", dto.WorkQueueId),
            "not_found" => ProblemDetailsHelper.NotFound("AssignmentRule", ruleId),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> ListCoverageWindows(
        Guid? queueId, Guid? coveredUserId, string? status,
        OperationsRoutingService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (items, total) = await svc.ListCoverageWindowsAsync(queueId, coveredUserId, status, user, ct);
        return Results.Ok(new { items, totalCount = total });
    }

    private static async Task<IResult> CreateCoverageWindow(
        CoverageWindowUpsertRequestDto dto,
        IValidator<CoverageWindowUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.UpsertCoverageWindowAsync(null, dto, null, user, ct);
        return CoverageResult(result, error, null);
    }

    private static async Task<IResult> UpdateCoverageWindow(
        Guid coverageWindowId,
        CoverageWindowUpsertRequestDto dto,
        HttpContext httpContext,
        IValidator<CoverageWindowUpsertRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!TryRowVersion(httpContext, out var rowVersion, out var problem))
            return problem!;

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.UpsertCoverageWindowAsync(coverageWindowId, dto, rowVersion, user, ct);
        return CoverageResult(result, error, coverageWindowId);
    }

    private static async Task<IResult> ReassignQueueItem(
        Guid queueItemId,
        QueueReassignmentRequestDto dto,
        HttpContext httpContext,
        IValidator<QueueReassignmentRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!TryRowVersion(httpContext, out var rowVersion, out var problem))
            return problem!;

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.ReassignAsync(queueItemId, dto, rowVersion, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "not_found" => ProblemDetailsHelper.NotFound("QueueWorkItem", queueItemId),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            "queue_item_closed" => Conflict("queue_item_closed", "Closed queue items cannot be reassigned."),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> RebalanceQueue(
        Guid queueId,
        QueueRebalanceRequestDto dto,
        IValidator<QueueRebalanceRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.RebalanceAsync(queueId, dto, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "queue_not_found" => ProblemDetailsHelper.NotFound("WorkQueue", queueId),
            _ => Results.Created($"/work-queues/{queueId}/items", result),
        };
    }

    private static async Task<IResult> ListRoutingEvents(
        string? sourceType, Guid? sourceId, Guid? queueItemId, int? page, int? pageSize,
        OperationsRoutingService svc, ICurrentUserService user, CancellationToken ct)
    {
        var (items, total) = await svc.ListRoutingEventsAsync(sourceType, sourceId, queueItemId, page ?? 1, Math.Min(pageSize ?? 50, MaxPageSize), user, ct);
        return Results.Ok(new { items, totalCount = total });
    }

    private static async Task<IResult> RouteWork(
        QueueRouteRequestDto dto,
        IValidator<QueueRouteRequestDto> validator,
        OperationsRoutingService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.RouteAsync(dto.SourceType, dto.SourceId, user, ct);
        return error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "not_found" => ProblemDetailsHelper.NotFound(dto.SourceType, dto.SourceId),
            "routing_no_match" => Conflict("routing_no_match", "No active rule or fallback queue matched this work item."),
            _ => Results.Created($"/work-queues/{result!.WorkQueueId}/items", result),
        };
    }

    private static IResult QueueMutationResult(WorkQueueDto? result, string? error, Guid queueId) =>
        error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "queue_not_found" => ProblemDetailsHelper.NotFound("WorkQueue", queueId),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            "assignment_rule_conflict" => Conflict("assignment_rule_conflict", "Queue name already exists for this work type or the requested queue state conflicts with routing rules."),
            "queue_inactive" => Conflict("queue_inactive", "Queue with open work cannot be deactivated."),
            _ => Results.Ok(result),
        };

    private static IResult CoverageResult(CoverageWindowDto? result, string? error, Guid? coverageWindowId) =>
        error switch
        {
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            "not_found" => ProblemDetailsHelper.NotFound("CoverageWindow", coverageWindowId ?? Guid.Empty),
            "coverage_window_overlap" => Conflict("coverage_window_overlap", "Coverage window overlaps an existing active or scheduled window."),
            _ when result is not null && coverageWindowId is null => Results.Created($"/coverage-windows/{result.Id}", result),
            _ => Results.Ok(result),
        };

    private static bool TryRowVersion(HttpContext httpContext, out uint rowVersion, out IResult? problem)
    {
        var ifMatch = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ifMatch) || !uint.TryParse(ifMatch.Trim('"'), out rowVersion))
        {
            rowVersion = 0;
            problem = Results.Problem(title: "If-Match header required", statusCode: 428);
            return false;
        }

        problem = null;
        return true;
    }

    private static IResult ValidationProblem(FluentValidation.Results.ValidationResult validation) =>
        ProblemDetailsHelper.ValidationError(
            validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

    private static IResult Conflict(string code, string detail) => Results.Problem(
        title: "Queue operation conflict",
        detail: detail,
        statusCode: 409,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
