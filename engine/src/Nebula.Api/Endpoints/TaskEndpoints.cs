using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class TaskEndpoints
{
    private const int MaxLimit = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // /my/tasks has a different path prefix, so register separately
        app.MapGet("/my/tasks", GetMyTasks)
            .WithTags("Tasks").RequireAuthorization().RequireRateLimiting("authenticated");

        var group = app.MapGroup("/tasks")
            .WithTags("Tasks")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", GetTaskList);
        group.MapGet("/{taskId:guid}", GetTaskById);
        group.MapPost("/", CreateTask);
        group.MapPut("/{taskId:guid}", UpdateTask);
        group.MapDelete("/{taskId:guid}", DeleteTask);

        return app;
    }

    private static async Task<IResult> GetMyTasks(
        int? limit, TaskService svc, ICurrentUserService user,
        IAuthorizationService authz, CancellationToken ct)
    {
        var effectiveLimit = Math.Min(limit ?? 10, MaxLimit);

        // BrokerUser: scope-isolated tasks by linked broker entity (F0009 §12).
        if (user.Roles.Contains("BrokerUser"))
        {
            // Casbin check: BrokerUser, task, read (policy.csv §2.10 — condition is 'true' for broker scope).
            var brokerUserAuthorized = false;
            foreach (var role in user.Roles)
            {
                // Use placeholder attrs — BrokerUser scope is enforced at query layer, not policy condition.
                if (await authz.AuthorizeAsync(role, "task", "read", new Dictionary<string, object>()))
                { brokerUserAuthorized = true; break; }
            }
            if (!brokerUserAuthorized) return ProblemDetailsHelper.Forbidden();

            return Results.Ok(await svc.GetBrokerScopedTasksAsync(effectiveLimit, user, ct));
        }

        // Ownership condition: the list is already scoped to the caller; assignee == subjectId by definition.
        var attrs = new Dictionary<string, object>
        {
            ["assignee"]  = user.UserId,
            ["creator"]   = user.UserId,
            ["subjectId"] = user.UserId,
        };
        var authorized = false;
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "task", "read", attrs))
            { authorized = true; break; }
        }
        if (!authorized) return ProblemDetailsHelper.Forbidden();

        return Results.Ok(await svc.GetMyTasksAsync(user.UserId, user.DisplayName, effectiveLimit, user, ct));
    }

    // F0004: GET /tasks — paginated, filtered task list
    private static async Task<IResult> GetTaskList(
        string? view,
        string? status,
        string? priority,
        DateTime? dueDateFrom,
        DateTime? dueDateTo,
        bool? overdue,
        Guid? assigneeId,
        string? linkedEntityType,
        Guid? createdById,
        string? sort,
        string? sortDir,
        int? page,
        int? pageSize,
        TaskService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        // Pre-check: caller must have task read access at all
        var readAttrs = new Dictionary<string, object>
        {
            ["assignee"]  = user.UserId,
            ["creator"]   = user.UserId,
            ["subjectId"] = user.UserId,
        };
        var authorized = false;
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "task", "read", readAttrs))
            { authorized = true; break; }
        }
        if (!authorized) return ProblemDetailsHelper.Forbidden();

        var effectiveView = view ?? "myWork";
        if (effectiveView != "myWork" && effectiveView != "assignedByMe")
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]>
                {
                    ["view"] = ["View must be 'myWork' or 'assignedByMe'."],
                });

        // Parse multi-value query params (comma-separated)
        var statusFilter = ParseMultiValue(status);
        var priorityFilter = ParseMultiValue(priority);
        var linkedEntityTypeFilter = ParseMultiValue(linkedEntityType);

        var query = new TaskListQuery(
            View: effectiveView,
            CallerUserId: user.UserId,
            StatusFilter: statusFilter,
            PriorityFilter: priorityFilter,
            DueDateFrom: dueDateFrom,
            DueDateTo: dueDateTo,
            Overdue: overdue,
            AssigneeId: assigneeId,
            LinkedEntityTypeFilter: linkedEntityTypeFilter,
            CreatedById: createdById,
            Sort: sort ?? "dueDate",
            SortDir: sortDir ?? "asc",
            Page: page ?? 1,
            PageSize: Math.Min(pageSize ?? 20, MaxLimit));

        var (result, error) = await svc.GetTaskListAsync(query, user, ct);
        return error switch
        {
            "view_not_authorized" => ProblemDetailsHelper.ViewNotAuthorized(),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> GetTaskById(
        Guid taskId, TaskService svc, ICurrentUserService user, CancellationToken ct)
    {
        // Single fetch + Casbin auth inside service. Returns 404 for both not-found and not-authorized (IDOR fix).
        var (task, error) = await svc.GetByIdAuthorizedAsync(taskId, user, ct);
        if (error is not null)
            return ProblemDetailsHelper.NotFound("Task", taskId);

        return Results.Ok(task);
    }

    // F0003-S0001 / F0004: Create Task
    private static async Task<IResult> CreateTask(
        TaskCreateRequestDto dto,
        IValidator<TaskCreateRequestDto> validator,
        TaskService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        // Casbin: task, create.
        // For managers, pass creator = user.UserId so the "true" condition fires.
        // For all roles, also pass assignee so self-assign condition still works.
        var attrs = new Dictionary<string, object>
        {
            ["assignee"]  = dto.AssignedToUserId,
            ["creator"]   = user.UserId,  // Creator is always the caller on create
            ["subjectId"] = user.UserId,
        };
        var authorized = false;
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "task", "create", attrs))
            { authorized = true; break; }
        }
        if (!authorized) return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "forbidden"        => ProblemDetailsHelper.Forbidden(),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            "validation_error" => ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]>
                {
                    ["linkedEntityType"] = ["LinkedEntityType and LinkedEntityId must both be provided or both omitted."],
                }),
            _ => Results.Created($"/tasks/{result!.Id}", result),
        };
    }

    // F0003-S0002 / F0004: Update Task
    private static async Task<IResult> UpdateTask(
        Guid taskId,
        HttpContext httpContext,
        TaskService svc,
        ICurrentUserService user,
        IValidator<TaskUpdateRequestDto> validator,
        CancellationToken ct)
    {
        // Parse If-Match header for optimistic concurrency (C-1 fix)
        var ifMatch = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        if (string.IsNullOrEmpty(ifMatch) || !uint.TryParse(ifMatch.Trim('"'), out var rowVersion))
            return Results.Problem(title: "If-Match header required", statusCode: 428);

        // Parse raw JSON to detect field presence (for DueDate/Description null vs absent)
        httpContext.Request.EnableBuffering();
        using var doc = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: ct);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { [""] = ["Request body must be a JSON object."] });

        var presentFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in root.EnumerateObject())
            presentFields.Add(prop.Name);

        // Empty payload check (minProperties: 1)
        if (presentFields.Count == 0)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { [""] = ["At least one field must be provided."] });

        var dto = root.Deserialize<TaskUpdateRequestDto>(JsonOptions);
        if (dto is null)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { [""] = ["Invalid request body."] });

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        try
        {
            // Single fetch + Casbin auth + mutation inside service (TOCTOU fix).
            var (result, error, fromStatus, toStatus) = await svc.UpdateAsync(taskId, dto, presentFields, rowVersion, user, ct);
            if (error == "invalid_status_transition")
                return ProblemDetailsHelper.InvalidStatusTransition(fromStatus!, toStatus!);
            return error switch
            {
                "not_found"              => ProblemDetailsHelper.NotFound("Task", taskId),
                "forbidden"              => ProblemDetailsHelper.Forbidden(),
                "status_change_restricted" => ProblemDetailsHelper.StatusChangeRestricted(),
                "invalid_assignee"       => ProblemDetailsHelper.InvalidAssignee(),
                "inactive_assignee"      => ProblemDetailsHelper.InactiveAssignee(),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    // F0003-S0003 / F0004: Delete Task
    private static async Task<IResult> DeleteTask(
        Guid taskId,
        TaskService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        // Single fetch + Casbin auth + mutation inside service (TOCTOU fix).
        // F0004: Service now also allows creator-based delete via Casbin policy.
        var error = await svc.DeleteAsync(taskId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Task", taskId),
            _ => Results.NoContent(),
        };
    }

    /// <summary>Parse a comma-separated query string value into a string array, or null if empty.</summary>
    private static string[]? ParseMultiValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : parts;
    }
}
