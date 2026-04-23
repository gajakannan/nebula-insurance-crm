using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/policies")
            .WithTags("Policies")
            .RequireAuthorization();

        group.MapGet("/", ListPolicies);
        group.MapPost("/", CreatePolicy);
        group.MapPost("/from-bind", CreatePolicyFromBind);
        group.MapPost("/import", ImportPolicies);
        group.MapGet("/{policyId:guid}", GetPolicy);
        group.MapPut("/{policyId:guid}", UpdatePolicy);
        group.MapGet("/{policyId:guid}/summary", GetPolicySummary);
        group.MapPost("/{policyId:guid}/issue", IssuePolicy);
        group.MapPost("/{policyId:guid}/endorse", EndorsePolicy);
        group.MapPost("/{policyId:guid}/cancel", CancelPolicy);
        group.MapPost("/{policyId:guid}/reinstate", ReinstatePolicy);
        group.MapGet("/{policyId:guid}/versions", ListVersions);
        group.MapGet("/{policyId:guid}/versions/{versionId:guid}", GetVersion);
        group.MapGet("/{policyId:guid}/endorsements", ListEndorsements);
        group.MapGet("/{policyId:guid}/coverages", ListCoverages);
        group.MapGet("/{policyId:guid}/timeline", ListTimeline);

        return app;
    }

    private static async Task<IResult> ListPolicies(
        string? q,
        string? status,
        string? lineOfBusiness,
        Guid? carrierId,
        Guid? brokerOfRecordId,
        DateTime? expiringBefore,
        string? sort,
        int? page,
        int? pageSize,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var (sortBy, sortDir) = ParseSort(sort);
        var result = await svc.ListAsync(new PolicyListQuery(
            user.UserId,
            user.Roles,
            user.Regions,
            q,
            status,
            lineOfBusiness,
            carrierId,
            brokerOfRecordId,
            null,
            expiringBefore,
            sortBy,
            sortDir,
            page ?? 1,
            Math.Min(pageSize ?? 25, 100)), user, ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePolicy(
        PolicyCreateRequestDto dto,
        IValidator<PolicyCreateRequestDto> validator,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "create"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var (result, error) = await svc.CreateAsync(dto with { ImportMode = "manual" }, user, ct);
        return PolicyWriteResult(error, result, created: true);
    }

    private static async Task<IResult> CreatePolicyFromBind(
        PolicyFromBindRequestDto dto,
        IValidator<PolicyFromBindRequestDto> validator,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "create"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);

        var createRequest = new PolicyCreateRequestDto(
            dto.AccountId,
            dto.BrokerOfRecordId,
            dto.LineOfBusiness,
            dto.CarrierId,
            dto.EffectiveDate,
            dto.ExpirationDate,
            dto.PredecessorPolicyId,
            dto.ProducerUserId,
            dto.TotalPremium,
            dto.PremiumCurrency,
            "manual",
            dto.ExternalPolicyReference,
            dto.Coverages);
        var (result, error) = await svc.CreateAsync(createRequest, user, ct);
        return PolicyWriteResult(error, result, created: true);
    }

    private static async Task<IResult> ImportPolicies(
        PolicyImportRequestDto dto,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "import"))
            return ProblemDetailsHelper.PolicyDenied();

        return Results.Ok(await svc.ImportAsync(dto, user, ct));
    }

    private static async Task<IResult> GetPolicy(
        Guid policyId,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.GetByIdAsync(policyId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Policy", policyId) : Results.Ok(result);
    }

    private static async Task<IResult> UpdatePolicy(
        Guid policyId,
        PolicyUpdateRequestDto dto,
        IValidator<PolicyUpdateRequestDto> validator,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "update"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);
        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("policy");

        var (result, error) = await svc.UpdateAsync(policyId, dto, rowVersion, user, ct);
        return PolicyWriteResult(error, result);
    }

    private static async Task<IResult> GetPolicySummary(
        Guid policyId,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.GetSummaryAsync(policyId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Policy", policyId) : Results.Ok(result);
    }

    private static async Task<IResult> IssuePolicy(
        Guid policyId,
        PolicyIssueRequestDto? dto,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "issue"))
            return ProblemDetailsHelper.PolicyDenied();
        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("policy");

        var (result, error) = await svc.IssueAsync(policyId, dto, rowVersion, user, ct);
        return PolicyWriteResult(error, result);
    }

    private static async Task<IResult> EndorsePolicy(
        Guid policyId,
        PolicyEndorsementRequestDto dto,
        IValidator<PolicyEndorsementRequestDto> validator,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "endorse"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);
        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("policy");

        var (result, error) = await svc.EndorseAsync(policyId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Policy", policyId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("policy"),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", "Endorse"),
            "invalid_effective_date" => Results.Problem(title: "Invalid effective date", detail: "Endorsement effectiveDate must be inside the policy term.", statusCode: 400),
            _ => Results.Created($"/policies/{policyId}/endorsements/{result!.Id}", result),
        };
    }

    private static async Task<IResult> CancelPolicy(
        Guid policyId,
        PolicyCancelRequestDto dto,
        IValidator<PolicyCancelRequestDto> validator,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "cancel"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);
        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("policy");

        var (result, error) = await svc.CancelAsync(policyId, dto, rowVersion, user, ct);
        return PolicyWriteResult(error, result);
    }

    private static async Task<IResult> ReinstatePolicy(
        Guid policyId,
        PolicyReinstateRequestDto dto,
        IValidator<PolicyReinstateRequestDto> validator,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "reinstate"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(validation);
        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("policy");

        var (result, error) = await svc.ReinstateAsync(policyId, dto, rowVersion, user, ct);
        return PolicyWriteResult(error, result);
    }

    private static async Task<IResult> ListVersions(
        Guid policyId,
        int? page,
        int? pageSize,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        var result = await svc.ListVersionsAsync(policyId, page ?? 1, Math.Min(pageSize ?? 25, 100), user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Policy", policyId) : Results.Ok(result);
    }

    private static async Task<IResult> GetVersion(
        Guid policyId,
        Guid versionId,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        var result = await svc.GetVersionAsync(policyId, versionId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("PolicyVersion", versionId) : Results.Ok(result);
    }

    private static async Task<IResult> ListEndorsements(
        Guid policyId,
        int? page,
        int? pageSize,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        var result = await svc.ListEndorsementsAsync(policyId, page ?? 1, Math.Min(pageSize ?? 25, 100), user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Policy", policyId) : Results.Ok(result);
    }

    private static async Task<IResult> ListCoverages(
        Guid policyId,
        PolicyService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        var result = await svc.ListCurrentCoverageLinesAsync(policyId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Policy", policyId) : Results.Ok(result);
    }

    private static async Task<IResult> ListTimeline(
        Guid policyId,
        int? page,
        int? pageSize,
        PolicyService policySvc,
        TimelineService timelineSvc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "policy", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        if (!await policySvc.ExistsAccessibleAsync(policyId, user, ct))
            return ProblemDetailsHelper.NotFound("Policy", policyId);
        return Results.Ok(await timelineSvc.ListEventsPagedAsync("Policy", policyId, page ?? 1, Math.Min(pageSize ?? 25, 100), user, ct));
    }

    private static IResult PolicyWriteResult(string? error, PolicyDto? result, bool created = false) => error switch
    {
        "not_found" => ProblemDetailsHelper.NotFound("Policy", result?.Id ?? Guid.Empty),
        "invalid_account" => Results.Problem(title: "Invalid account", detail: "Account does not exist.", statusCode: 400),
        "invalid_broker" => Results.Problem(title: "Invalid broker", detail: "Broker of record must reference an active broker.", statusCode: 400),
        "invalid_carrier" => Results.Problem(title: "Invalid carrier", detail: "CarrierId must reference an active carrier.", statusCode: 400),
        "invalid_producer" => Results.Problem(title: "Invalid producer", detail: "ProducerUserId must reference an active user.", statusCode: 400),
        "invalid_predecessor" => Results.Problem(title: "Invalid predecessor", detail: "Predecessor policy must be Expired or Cancelled and share the same account.", statusCode: 400),
        "out_of_scope" => ProblemDetailsHelper.PolicyDenied(),
        "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("policy"),
        "must_use_endorse" => Results.Problem(title: "Must use endorsement", detail: "Issued, cancelled, and expired policy terms must be changed via endorsement.", statusCode: 409, extensions: new Dictionary<string, object?> { ["code"] = "must_use_endorse" }),
        "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", "target"),
        "invalid_effective_date" => Results.Problem(title: "Invalid effective date", detail: "Effective date must be inside the policy term.", statusCode: 400),
        "reinstatement_window_expired" => Results.Problem(title: "Reinstatement window expired", detail: "The policy can no longer be reinstated.", statusCode: 409, extensions: new Dictionary<string, object?> { ["code"] = "reinstatement_window_expired" }),
        _ => created ? Results.Created($"/policies/{result!.Id}", result) : Results.Ok(result),
    };

    private static IResult ValidationProblem(FluentValidation.Results.ValidationResult validation) =>
        ProblemDetailsHelper.ValidationError(
            validation.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

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

    private static (string SortBy, string SortDir) ParseSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return ("expirationDate", "asc");

        var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
        return (
            parts[0],
            parts.Length > 1 && string.Equals(parts[1], "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc");
    }
}
