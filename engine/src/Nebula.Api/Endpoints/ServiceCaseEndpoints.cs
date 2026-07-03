using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class ServiceCaseEndpoints
{
    public static IEndpointRouteBuilder MapServiceCaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/service-cases")
            .WithTags("Service Cases")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", ListServiceCases);
        group.MapPost("/", CreateServiceCase);
        group.MapGet("/{serviceCaseId:guid}", GetServiceCase);
        group.MapPatch("/{serviceCaseId:guid}", UpdateServiceCase);
        group.MapPost("/{serviceCaseId:guid}/transition", TransitionServiceCase);
        group.MapPatch("/{serviceCaseId:guid}/claim-reference", UpdateClaimReference);
        group.MapPost("/{serviceCaseId:guid}/communication-links", LinkCommunication);
        group.MapPost("/{serviceCaseId:guid}/follow-up-task", CreateFollowUpTask);

        return app;
    }

    private static async Task<IResult> ListServiceCases(
        Guid? accountId,
        Guid? policyId,
        Guid? ownerUserId,
        string? status,
        string? priority,
        string? q,
        DateOnly? dueBefore,
        DateOnly? dueAfter,
        bool? includeClosed,
        int? page,
        int? pageSize,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var query = new ServiceCaseListQuery(accountId, policyId, ownerUserId, status, priority, q, dueBefore, dueAfter, includeClosed ?? false, page ?? 1, pageSize ?? 20);
        var (result, error) = await svc.ListAsync(query, user, ct);
        return MapServiceCaseResult(result, error, Guid.Empty);
    }

    private static async Task<IResult> CreateServiceCase(
        ServiceCaseCreateRequestDto dto,
        IValidator<ServiceCaseCreateRequestDto> validator,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        if (error is not null)
            return MapServiceCaseResult(result, error, Guid.Empty);

        return Results.Created($"/service-cases/{result!.Id}", result);
    }

    private static async Task<IResult> GetServiceCase(
        Guid serviceCaseId,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (result, error) = await svc.GetByIdAsync(serviceCaseId, user, ct);
        return MapServiceCaseResult(result, error, serviceCaseId);
    }

    private static async Task<IResult> UpdateServiceCase(
        Guid serviceCaseId,
        ServiceCaseUpdateRequestDto dto,
        IValidator<ServiceCaseUpdateRequestDto> validator,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.UpdateAsync(serviceCaseId, dto, user, ct);
        return MapServiceCaseResult(result, error, serviceCaseId);
    }

    private static async Task<IResult> TransitionServiceCase(
        Guid serviceCaseId,
        ServiceCaseTransitionRequestDto dto,
        IValidator<ServiceCaseTransitionRequestDto> validator,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.TransitionAsync(serviceCaseId, dto, user, ct);
        return MapServiceCaseResult(result, error, serviceCaseId);
    }

    private static async Task<IResult> UpdateClaimReference(
        Guid serviceCaseId,
        ServiceCaseClaimReferenceUpdateRequestDto dto,
        IValidator<ServiceCaseClaimReferenceUpdateRequestDto> validator,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.UpdateClaimReferenceAsync(serviceCaseId, dto, user, ct);
        return MapServiceCaseResult(result, error, serviceCaseId);
    }

    private static async Task<IResult> LinkCommunication(
        Guid serviceCaseId,
        ServiceCaseCommunicationLinkRequestDto dto,
        IValidator<ServiceCaseCommunicationLinkRequestDto> validator,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.LinkCommunicationAsync(serviceCaseId, dto, user, ct);
        return MapServiceCaseResult(result, error, serviceCaseId);
    }

    private static async Task<IResult> CreateFollowUpTask(
        Guid serviceCaseId,
        ServiceCaseFollowUpTaskRequestDto dto,
        IValidator<ServiceCaseFollowUpTaskRequestDto> validator,
        ServiceCaseService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.CreateFollowUpTaskAsync(serviceCaseId, dto, user, ct);
        return error switch
        {
            null => Results.Created($"/tasks/{result!.Id}", result),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            _ => MapServiceCaseResult(result, error, serviceCaseId),
        };
    }

    private static IResult MapServiceCaseResult<T>(T? result, string? error, Guid resourceId)
    {
        return error switch
        {
            null => Results.Ok(result),
            "forbidden" => ProblemDetailsHelper.PolicyDenied(),
            "not_found" => ProblemDetailsHelper.NotFound("Service case", resourceId),
            "account_not_found" => ProblemDetailsHelper.NotFound("Account", resourceId),
            "policy_not_found" => ProblemDetailsHelper.NotFound("Policy", resourceId),
            "communication_not_found" => ProblemDetailsHelper.NotFound("Communication", resourceId),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            "invalid_transition" => ProblemDetailsHelper.InvalidStatusTransition("current", "requested"),
            "closed_service_case" => Results.Problem(
                title: "Closed service case",
                detail: "Closed service cases cannot be modified.",
                statusCode: 409,
                extensions: new Dictionary<string, object?> { ["code"] = "closed_service_case" }),
            "missing_resolution_summary" => Results.Problem(
                title: "Resolution summary required",
                detail: "A resolution summary is required before resolving or closing a service case.",
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["code"] = "missing_resolution_summary" }),
            "missing_waiting_reason" => Results.Problem(
                title: "Waiting reason required",
                detail: "Moving a service case to Waiting requires a reason or note.",
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["code"] = "missing_waiting_reason" }),
            "policy_account_mismatch" => Results.Problem(
                title: "Policy/account mismatch",
                detail: "The specified policy is not attached to the specified account.",
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["code"] = "policy_account_mismatch" }),
            "duplicate_communication_link" => Results.Problem(
                title: "Duplicate communication link",
                detail: "The communication is already linked to this service case.",
                statusCode: 409,
                extensions: new Dictionary<string, object?> { ["code"] = "duplicate_communication_link" }),
            _ => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["serviceCase"] = [error] }),
        };
    }
}
