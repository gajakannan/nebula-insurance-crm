using System.Text.Json;
using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class SubmissionEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/submissions")
            .WithTags("Submissions")
            .RequireAuthorization();

        group.MapGet("/", ListSubmissions);
        group.MapPost("/", CreateSubmission);
        group.MapGet("/{submissionId:guid}", GetSubmission);
        group.MapPut("/{submissionId:guid}", UpdateSubmission);
        group.MapPost("/{submissionId:guid}/transitions", PostTransition);
        group.MapPut("/{submissionId:guid}/assignment", AssignSubmission);
        group.MapGet("/{submissionId:guid}/timeline", GetTimeline);

        return app;
    }

    private static async Task<IResult> ListSubmissions(
        string? status,
        Guid? brokerId,
        Guid? accountId,
        string? lineOfBusiness,
        Guid? assignedToUserId,
        bool? stale,
        string? sort,
        string? sortDir,
        int? page,
        int? pageSize,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var errors = ValidateListQuery(sort, sortDir, page, pageSize);
        if (errors.Count > 0)
            return ProblemDetailsHelper.ValidationError(errors);

        var query = new SubmissionListQuery(
            status,
            brokerId,
            accountId,
            lineOfBusiness,
            assignedToUserId,
            stale,
            sort ?? "createdAt",
            sortDir ?? "desc",
            page ?? 1,
            pageSize ?? 25);

        return Results.Ok(await svc.ListAsync(query, user, ct));
    }

    private static async Task<IResult> CreateSubmission(
        SubmissionCreateDto dto,
        IValidator<SubmissionCreateDto> validator,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "create"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "invalid_account" => ProblemDetailsHelper.InvalidAccount(dto.AccountId),
            "invalid_broker" => ProblemDetailsHelper.InvalidBroker(dto.BrokerId),
            "region_mismatch" => ProblemDetailsHelper.RegionMismatch(),
            "invalid_program" => ProblemDetailsHelper.InvalidProgram(dto.ProgramId!.Value),
            "invalid_lob" => ProblemDetailsHelper.InvalidLob(dto.LineOfBusiness!),
            _ => Results.Created($"/submissions/{result!.Id}", result),
        };
    }

    private static async Task<IResult> GetSubmission(
        Guid submissionId,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.GetByIdAsync(submissionId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Submission", submissionId) : Results.Ok(result);
    }

    private static async Task<IResult> UpdateSubmission(
        Guid submissionId,
        IValidator<SubmissionUpdateDto> validator,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "update"))
            return ProblemDetailsHelper.PolicyDenied();

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed();

        httpContext.Request.EnableBuffering();
        using var doc = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: ct);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { [""] = ["Request body must be a JSON object."] });

        var presentFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in root.EnumerateObject())
            presentFields.Add(prop.Name);

        if (presentFields.Count == 0)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { [""] = ["At least one field must be provided."] });

        var dto = root.Deserialize<SubmissionUpdateDto>(JsonOptions);
        if (dto is null)
            return ProblemDetailsHelper.ValidationError(
                new Dictionary<string, string[]> { [""] = ["Invalid request body."] });

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        var (result, error) = await svc.UpdateAsync(submissionId, dto, presentFields, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Submission", submissionId),
            "invalid_program" => ProblemDetailsHelper.InvalidProgram(dto.ProgramId!.Value),
            "invalid_lob" => ProblemDetailsHelper.InvalidLob(dto.LineOfBusiness!),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed(),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> PostTransition(
        Guid submissionId,
        WorkflowTransitionRequestDto dto,
        IValidator<WorkflowTransitionRequestDto> validator,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "transition"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed();

        var (result, error, missingItems) = await svc.TransitionAsync(submissionId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Submission", submissionId),
            "policy_denied" => ProblemDetailsHelper.PolicyDenied(),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", dto.ToState),
            "missing_transition_prerequisite" => ProblemDetailsHelper.MissingTransitionPrerequisite(missingItems ?? []),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed(),
            _ => Results.Created($"/submissions/{submissionId}/transitions", result),
        };
    }

    private static async Task<IResult> AssignSubmission(
        Guid submissionId,
        SubmissionAssignmentRequestDto dto,
        IValidator<SubmissionAssignmentRequestDto> validator,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "assign"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed();

        var (result, error, errorDetail) = await svc.AssignAsync(submissionId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Submission", submissionId),
            "invalid_assignee" => ProblemDetailsHelper.InvalidSubmissionAssignee(errorDetail ?? "The specified assignee is invalid."),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed(),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> GetTimeline(
        Guid submissionId,
        int? page,
        int? pageSize,
        SubmissionService submissionSvc,
        TimelineService timelineSvc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "submission", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var normalizedPage = page ?? 1;
        var normalizedPageSize = pageSize ?? 25;
        var errors = ValidatePageQuery(normalizedPage, normalizedPageSize);
        if (errors.Count > 0)
            return ProblemDetailsHelper.ValidationError(errors);

        if (!await submissionSvc.ExistsAsync(submissionId, user, ct))
            return ProblemDetailsHelper.NotFound("Submission", submissionId);

        return Results.Ok(await timelineSvc.ListEventsPagedAsync(
            "Submission",
            submissionId,
            normalizedPage,
            normalizedPageSize,
            user,
            ct));
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
        string? sort,
        string? sortDir,
        int? page,
        int? pageSize)
    {
        var errors = ValidatePageQuery(page ?? 1, pageSize ?? 25);
        var allowedSorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "createdAt",
            "effectiveDate",
            "accountName",
            "brokerName",
            "currentStatus",
        };

        if (!string.IsNullOrWhiteSpace(sort) && !allowedSorts.Contains(sort))
            errors["sort"] = [$"Sort must be one of: {string.Join(", ", allowedSorts.OrderBy(value => value))}."];

        if (!string.IsNullOrWhiteSpace(sortDir)
            && !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase))
        {
            errors["sortDir"] = ["SortDir must be 'asc' or 'desc'."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidatePageQuery(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (page < 1)
            errors["page"] = ["Page must be greater than or equal to 1."];

        if (pageSize < 1 || pageSize > 100)
            errors["pageSize"] = ["PageSize must be between 1 and 100."];

        return errors;
    }
}
