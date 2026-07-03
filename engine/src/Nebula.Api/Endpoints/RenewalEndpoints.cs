using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class RenewalEndpoints
{
    public static IEndpointRouteBuilder MapRenewalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/renewals")
            .WithTags("Renewals")
            .RequireAuthorization();

        group.MapGet("/", ListRenewals);
        group.MapGet("/needs-attention", ListNeedsAttention);
        group.MapGet("/{renewalId:guid}/companion-context", GetCompanionContext);
        group.MapPost("/{renewalId:guid}/outreach-draft", PostOutreachDraft);
        group.MapPost("/{renewalId:guid}/outreach-mock-send", PostOutreachMockSend);
        group.MapPost("/", CreateRenewal);
        group.MapGet("/{renewalId:guid}", GetRenewal);
        group.MapPut("/{renewalId:guid}/lob-attributes", PutLobAttributes);
        group.MapPut("/{renewalId:guid}/assignment", PutAssignment);
        group.MapGet("/{renewalId:guid}/timeline", GetTimeline);
        group.MapPost("/{renewalId:guid}/transitions", PostTransition);

        return app;
    }

    private static async Task<IResult> ListRenewals(
        string? dueWindow,
        string? status,
        Guid? assignedToUserId,
        string? lineOfBusiness,
        Guid? accountId,
        Guid? brokerId,
        bool? includeTerminal,
        string? urgency,
        string? sort,
        string? sortDir,
        int? page,
        int? pageSize,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var errors = ValidateListQuery(dueWindow, urgency, sort, sortDir, page, pageSize);
        if (errors.Count > 0)
            return ProblemDetailsHelper.ValidationError(errors);

        var query = new RenewalListQuery(
            user.UserId,
            user.Roles,
            user.Regions,
            dueWindow,
            status,
            assignedToUserId,
            lineOfBusiness,
            accountId,
            brokerId,
            includeTerminal ?? false,
            urgency,
            sort ?? "policyExpirationDate",
            sortDir ?? "asc",
            page ?? 1,
            pageSize ?? 25);

        return Results.Ok(await svc.ListAsync(query, ct));
    }

    private static async Task<IResult> CreateRenewal(
        RenewalCreateDto dto,
        IValidator<RenewalCreateDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "create"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        var (result, error, detail, lobErrors) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Policy", dto.PolicyId),
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "duplicate_renewal" => ProblemDetailsHelper.DuplicateRenewal(),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            "lob_validation_failed" => ProblemDetailsHelper.LobValidationFailed(lobErrors ?? []),
            "invalid_assignee_role" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
            {
                ["assignedToUserId"] = [detail ?? "Target user does not have the required role for this renewal stage."],
            }),
            _ => Results.Created($"/renewals/{result!.Id}", result),
        };
    }

    private static async Task<IResult> GetRenewal(
        Guid renewalId,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.GetByIdAsync(renewalId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Renewal", renewalId) : Results.Ok(result);
    }

    private static async Task<IResult> PutLobAttributes(
        Guid renewalId,
        RenewalLobAttributesUpdateDto dto,
        IValidator<RenewalLobAttributesUpdateDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "update"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("renewal");

        var (result, error, lobErrors) = await svc.UpdateLobAttributesAsync(renewalId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("renewal"),
            "attributes_readonly" => Results.Problem(title: "Renewal attributes are read-only", detail: "Completed and lost renewals cannot update product attributes.", statusCode: 409),
            "lob_validation_failed" => ProblemDetailsHelper.LobValidationFailed(lobErrors ?? []),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> PutAssignment(
        Guid renewalId,
        RenewalAssignmentRequestDto dto,
        IValidator<RenewalAssignmentRequestDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "assign"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("renewal");

        var (result, error, detail) = await svc.AssignAsync(renewalId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("renewal"),
            "assignment_not_allowed_in_terminal_state" => ProblemDetailsHelper.AssignmentNotAllowedInTerminalState(),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            "invalid_assignee_role" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
            {
                ["assignedToUserId"] = [detail ?? "Target user does not have the required role for this renewal stage."],
            }),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> GetTimeline(
        Guid renewalId,
        int? page,
        int? pageSize,
        RenewalService renewalSvc,
        TimelineService timelineSvc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var normalizedPage = page ?? 1;
        var normalizedPageSize = pageSize ?? 25;
        var errors = ValidatePageQuery(normalizedPage, normalizedPageSize);
        if (errors.Count > 0)
            return ProblemDetailsHelper.ValidationError(errors);

        if (!await renewalSvc.ExistsAsync(renewalId, user, ct))
            return ProblemDetailsHelper.NotFound("Renewal", renewalId);

        return Results.Ok(await timelineSvc.ListEventsPagedAsync("Renewal", renewalId, normalizedPage, normalizedPageSize, user, ct));
    }

    private static async Task<IResult> PostTransition(
        Guid renewalId,
        RenewalTransitionRequestDto dto,
        IValidator<RenewalTransitionRequestDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "transition"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("renewal");

        var (result, error, missingItems) = await svc.TransitionAsync(renewalId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", dto.ToState),
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("renewal"),
            "missing_transition_prerequisite" => ProblemDetailsHelper.MissingTransitionPrerequisite(missingItems ?? []),
            _ => Results.Created($"/renewals/{renewalId}/transitions", result),
        };
    }

    private static async Task<IResult> ListNeedsAttention(
        int? withinDays,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var window = withinDays ?? 90;
        if (window < 1 || window > 365)
        {
            return ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
            {
                ["withinDays"] = ["withinDays must be between 1 and 365."],
            });
        }

        var items = await svc.GetNeedsAttentionAsync(window, user, authz, ct);
        return Results.Ok(new RenewalNeedsAttentionResponse(items));
    }

    private static async Task<IResult> GetCompanionContext(
        Guid renewalId,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var (dto, error) = await svc.GetCompanionContextAsync(renewalId, user, authz, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            _ => Results.Ok(dto),
        };
    }

    private static async Task<IResult> PostOutreachDraft(
        Guid renewalId,
        RenewalOutreachDraftRequestDto dto,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "draft_outreach"))
            return ProblemDetailsHelper.PolicyDenied();

        if (dto?.Provenance is null || string.IsNullOrWhiteSpace(dto.DraftBody) || dto.DraftBody.Length > 8000)
        {
            return ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
            {
                ["draft_body"] = ["draft_body (1..8000 chars) and provenance are required."],
            });
        }

        var (result, error, detail) = await svc.PersistOutreachDraftAsync(renewalId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "invalid_state" => Results.Problem(
                title: "Invalid renewal state",
                detail: "An outreach draft requires an Identified or Outreach renewal.",
                statusCode: 409),
            "content_constraint" => Results.Problem(
                title: "Draft content not allowed",
                detail: detail ?? "Draft must not include premium/quote/coverage-terms/binding language.",
                statusCode: 422),
            _ => Results.Created($"/renewals/{renewalId}/timeline", result),
        };
    }

    private static async Task<IResult> PostOutreachMockSend(
        Guid renewalId,
        RenewalOutreachMockSendRequestDto dto,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "renewal", "draft_outreach"))
            return ProblemDetailsHelper.PolicyDenied();

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("renewal");

        if (dto?.Provenance is null || string.IsNullOrWhiteSpace(dto.FinalDraftBody) || dto.FinalDraftBody.Length > 8000)
        {
            return ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
            {
                ["final_draft_body"] = ["final_draft_body (1..8000 chars) and provenance are required."],
            });
        }

        var (result, error, detail) = await svc.OutreachMockSendAsync(renewalId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("renewal"),
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", "Outreach"),
            "content_constraint" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
            {
                ["final_draft_body"] = [detail ?? "Content not allowed."],
            }),
            _ => Results.Created($"/renewals/{renewalId}/transitions", result),
        };
    }

    private static async Task<bool> HasAccessAsync(
        ICurrentUserService user,
        IAuthorizationService authz,
        string resource,
        string action)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, resource, action))
                return true;
        }

        return false;
    }

    private static bool TryParseExpectedRowVersion(HttpContext httpContext, out uint rowVersion)
    {
        var ifMatch = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        return uint.TryParse(ifMatch?.Trim('"'), out rowVersion);
    }

    private static Dictionary<string, string[]> ValidateListQuery(
        string? dueWindow,
        string? urgency,
        string? sort,
        string? sortDir,
        int? page,
        int? pageSize)
    {
        var errors = ValidatePageQuery(page ?? 1, pageSize ?? 25);
        var allowedDueWindows = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "all", "90", "60", "45", "overdue" };
        var allowedUrgencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "overdue", "approaching" };
        var allowedSorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "policyExpirationDate",
            "accountName",
            "currentStatus",
            "assignedToUserId",
        };

        if (!string.IsNullOrWhiteSpace(dueWindow) && !allowedDueWindows.Contains(dueWindow))
            errors["dueWindow"] = [$"dueWindow must be one of: {string.Join(", ", allowedDueWindows.OrderBy(value => value))}."];

        if (!string.IsNullOrWhiteSpace(urgency) && !allowedUrgencies.Contains(urgency))
            errors["urgency"] = [$"urgency must be one of: {string.Join(", ", allowedUrgencies.OrderBy(value => value))}."];

        if (!string.IsNullOrWhiteSpace(sort) && !allowedSorts.Contains(sort))
            errors["sort"] = [$"sort must be one of: {string.Join(", ", allowedSorts.OrderBy(value => value))}."];

        if (!string.IsNullOrWhiteSpace(sortDir)
            && !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase))
        {
            errors["sortDir"] = ["sortDir must be 'asc' or 'desc'."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidatePageQuery(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (page < 1)
            errors["page"] = ["page must be greater than or equal to 1."];

        if (pageSize < 1 || pageSize > 100)
            errors["pageSize"] = ["pageSize must be between 1 and 100."];

        return errors;
    }
}
