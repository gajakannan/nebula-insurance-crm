using System.Text.Json;
using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Api.Endpoints;

public static class AccountEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static RouteGroupBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapGet("/", ListAccounts);
        group.MapPost("/", CreateAccount);
        group.MapGet("/{accountId:guid}", GetAccount);
        group.MapPut("/{accountId:guid}", UpdateAccount);
        group.MapPost("/{accountId:guid}/lifecycle", TransitionLifecycle);
        group.MapGet("/{accountId:guid}/merge-preview", GetMergePreview);
        group.MapPost("/{accountId:guid}/merge", MergeAccount);
        group.MapGet("/{accountId:guid}/summary", GetSummary);
        group.MapGet("/{accountId:guid}/contacts", ListContacts);
        group.MapPost("/{accountId:guid}/contacts", CreateContact);
        group.MapPut("/{accountId:guid}/contacts/{contactId:guid}", UpdateContact);
        group.MapDelete("/{accountId:guid}/contacts/{contactId:guid}", DeleteContact);
        group.MapPost("/{accountId:guid}/relationships", ChangeRelationship);
        group.MapGet("/{accountId:guid}/submissions", ListSubmissions);
        group.MapGet("/{accountId:guid}/renewals", ListRenewals);
        group.MapGet("/{accountId:guid}/policies", ListPolicies);
        group.MapGet("/{accountId:guid}/timeline", ListTimeline);

        return group;
    }

    private static async Task<IResult> ListAccounts(
        string? q,
        string? status,
        string? territoryCode,
        string? region,
        Guid? brokerOfRecordId,
        string? primaryLineOfBusiness,
        string? include,
        bool? includeRemoved,
        string? sort,
        string? sortDir,
        int? page,
        int? pageSize,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var query = new AccountListQuery(
            q,
            status,
            territoryCode,
            region,
            brokerOfRecordId,
            primaryLineOfBusiness,
            !string.IsNullOrWhiteSpace(include) && include.Split(',').Any(value => value.Trim() == "summary"),
            includeRemoved ?? false,
            sort ?? "displayName",
            sortDir ?? "asc",
            page ?? 1,
            Math.Min(pageSize ?? 25, 100));

        var result = await svc.ListAsync(query, user, ct);
        return Results.Ok(new
        {
            data = result.Data,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
        });
    }

    private static async Task<IResult> CreateAccount(
        AccountCreateRequestDto dto,
        IValidator<AccountCreateRequestDto> validator,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "create"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "duplicate_tax_id" => Results.Problem(title: "Duplicate tax ID", detail: "An active account already uses the supplied tax ID.", statusCode: 409),
            "invalid_broker" => Results.Problem(title: "Invalid broker", detail: "Broker of record must reference an active broker.", statusCode: 400),
            "invalid_producer" => Results.Problem(title: "Invalid producer", detail: "Primary producer must reference an active user.", statusCode: 400),
            _ => Results.Created($"/accounts/{result!.Id}", result),
        };
    }

    private static async Task<IResult> GetAccount(
        Guid accountId,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var (result, error, stableDisplayName, removedAt, reasonCode) = await svc.GetByIdAsync(accountId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "deleted" => Results.Problem(
                title: "Account deleted",
                detail: $"Account \"{stableDisplayName}\" has been deleted.",
                statusCode: 410,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "account_deleted",
                    ["stableDisplayName"] = stableDisplayName,
                    ["removedAt"] = removedAt,
                    ["reasonCode"] = reasonCode,
                }),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> UpdateAccount(
        Guid accountId,
        IValidator<AccountUpdateRequestDto> validator,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "update"))
            return ProblemDetailsHelper.PolicyDenied();

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("account");

        httpContext.Request.EnableBuffering();
        using var doc = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: ct);
        var dto = doc.RootElement.Deserialize<AccountUpdateRequestDto>(JsonOptions);
        if (dto is null)
            return ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { [""] = ["Invalid request body."] });

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        var presentFields = doc.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (result, error) = await svc.UpdateAsync(accountId, dto, presentFields, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("account"),
            "terminal_state" => Results.Problem(title: "Account is read-only", detail: "Merged and deleted accounts cannot be updated.", statusCode: 409),
            "duplicate_tax_id" => Results.Problem(title: "Duplicate tax ID", detail: "An active account already uses the supplied tax ID.", statusCode: 409),
            "display_name_required" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["displayName"] = ["displayName is required."] }),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> TransitionLifecycle(
        Guid accountId,
        AccountLifecycleRequestDto dto,
        IValidator<AccountLifecycleRequestDto> validator,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var action = dto.ToState switch
        {
            AccountStatuses.Active => "reactivate",
            AccountStatuses.Inactive => "deactivate",
            AccountStatuses.Deleted => "delete",
            _ => "update",
        };

        if (!await HasAccessAsync(user, authz, "account", action))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("account");

        var (result, error) = await svc.TransitionAsync(accountId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("account"),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", dto.ToState),
            "reason_required" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["reasonCode"] = ["reasonCode is required when deleting an account."] }),
            "reason_detail_required" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["reasonDetail"] = ["reasonDetail is required when reasonCode is Other."] }),
            _ => Results.Ok(result),
        };
    }

    private const string MergeOperationKey = "account.merge";

    private static async Task<IResult> GetMergePreview(
        Guid accountId,
        Guid survivorId,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "merge"))
            return ProblemDetailsHelper.PolicyDenied();

        var (preview, error) = await svc.GetMergePreviewAsync(accountId, survivorId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "survivor_not_found" => ProblemDetailsHelper.NotFound("Account", survivorId),
            "self_merge" => Results.Problem(title: "Invalid merge", detail: "An account cannot be merged into itself.", statusCode: 409),
            _ => Results.Ok(preview),
        };
    }

    private static async Task<IResult> MergeAccount(
        Guid accountId,
        AccountMergeRequestDto dto,
        IValidator<AccountMergeRequestDto> validator,
        AccountService svc,
        IIdempotencyStore idempotencyStore,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "merge"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("account");

        var idempotencyKey = ReadIdempotencyKey(httpContext);
        if (idempotencyKey is not null)
        {
            var existing = await idempotencyStore.GetAsync(idempotencyKey, MergeOperationKey, ct);
            if (existing is not null)
            {
                if (existing.ResourceId != accountId)
                    return ProblemDetailsHelper.IdempotencyKeyConflict(idempotencyKey);

                return ReplayIdempotentResponse(existing);
            }
        }

        var (result, error, linkedCount) = await svc.MergeAsync(accountId, dto, rowVersion, user, ct);
        var response = error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "survivor_not_found" => ProblemDetailsHelper.NotFound("Account", dto.SurvivorAccountId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("account"),
            "self_merge" => Results.Problem(title: "Invalid merge", detail: "An account cannot be merged into itself.", statusCode: 409),
            "merge_conflict" => Results.Problem(title: "Merge conflict", detail: "Account was already merged into a different survivor.", statusCode: 409),
            "survivor_not_active" => Results.Problem(title: "Invalid survivor", detail: "Survivor account must be Active.", statusCode: 409),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", AccountStatuses.Merged),
            "merge_too_large" => ProblemDetailsHelper.MergeTooLarge(linkedCount ?? 0, AccountService.MergeLinkedRecordsThreshold),
            _ => Results.Ok(result),
        };

        if (idempotencyKey is not null && error is null)
        {
            await idempotencyStore.SaveAsync(new IdempotencyRecord
            {
                IdempotencyKey = idempotencyKey,
                Operation = MergeOperationKey,
                ResourceId = accountId,
                ActorUserId = user.UserId,
                ResponseStatusCode = StatusCodes.Status200OK,
                ResponsePayloadJson = JsonSerializer.Serialize(result),
                CreatedAt = DateTime.UtcNow,
            }, ct);
        }

        return response;
    }

    private static string? ReadIdempotencyKey(HttpContext httpContext)
    {
        var raw = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }

    private static IResult ReplayIdempotentResponse(IdempotencyRecord record)
    {
        if (record.ResponseStatusCode == StatusCodes.Status200OK && record.ResponsePayloadJson is not null)
        {
            var dto = JsonSerializer.Deserialize<AccountDto>(record.ResponsePayloadJson, JsonOptions);
            return Results.Ok(dto);
        }

        return Results.StatusCode(record.ResponseStatusCode);
    }

    private static async Task<IResult> GetSummary(
        Guid accountId,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var (result, error, stableDisplayName, removedAt, reasonCode) = await svc.GetSummaryAsync(accountId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "deleted" => Results.Problem(
                title: "Account deleted",
                detail: $"Account \"{stableDisplayName}\" has been deleted.",
                statusCode: 410,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "account_deleted",
                    ["stableDisplayName"] = stableDisplayName,
                    ["removedAt"] = removedAt,
                    ["reasonCode"] = reasonCode,
                }),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> ListContacts(
        Guid accountId,
        int? page,
        int? pageSize,
        AccountContactService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read"))
            return ProblemDetailsHelper.PolicyDenied();

        var result = await svc.ListAsync(accountId, page ?? 1, pageSize ?? 25, user, ct);
        return result is null
            ? ProblemDetailsHelper.NotFound("Account", accountId)
            : Results.Ok(new
            {
                data = result.Data,
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
            });
    }

    private static async Task<IResult> CreateContact(
        Guid accountId,
        AccountContactRequestDto dto,
        IValidator<AccountContactRequestDto> validator,
        AccountContactService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "contact:manage"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        var (result, error) = await svc.CreateAsync(accountId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "terminal_state" => Results.Problem(title: "Account is read-only", detail: "Merged and deleted accounts cannot be updated.", statusCode: 409),
            "primary_conflict" => Results.Problem(title: "Primary contact conflict", detail: "Another active primary contact already exists.", statusCode: 409),
            _ => Results.Created($"/accounts/{accountId}/contacts/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateContact(
        Guid accountId,
        Guid contactId,
        AccountContactRequestDto dto,
        IValidator<AccountContactRequestDto> validator,
        AccountContactService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "contact:manage"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("account contact");

        var (result, error) = await svc.UpdateAsync(accountId, contactId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "contact_not_found" => ProblemDetailsHelper.NotFound("Contact", contactId),
            "terminal_state" => Results.Problem(title: "Account is read-only", detail: "Merged and deleted accounts cannot be updated.", statusCode: 409),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("account contact"),
            "primary_conflict" => Results.Problem(title: "Primary contact conflict", detail: "Another active primary contact already exists.", statusCode: 409),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> DeleteContact(
        Guid accountId,
        Guid contactId,
        AccountContactService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "contact:manage"))
            return ProblemDetailsHelper.PolicyDenied();

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("account contact");

        var error = await svc.DeleteAsync(accountId, contactId, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "contact_not_found" => ProblemDetailsHelper.NotFound("Contact", contactId),
            "terminal_state" => Results.Problem(title: "Account is read-only", detail: "Merged and deleted accounts cannot be updated.", statusCode: 409),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("account contact"),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> ChangeRelationship(
        Guid accountId,
        AccountRelationshipRequestDto dto,
        IValidator<AccountRelationshipRequestDto> validator,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "relationship:change"))
            return ProblemDetailsHelper.PolicyDenied();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

        if (!TryParseExpectedRowVersion(httpContext, out var rowVersion))
            return ProblemDetailsHelper.PreconditionFailed("account");

        var (result, error) = await svc.ChangeRelationshipAsync(accountId, dto, rowVersion, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Account", accountId),
            "precondition_failed" => ProblemDetailsHelper.PreconditionFailed("account"),
            "terminal_state" => Results.Problem(title: "Account is read-only", detail: "Merged and deleted accounts cannot be updated.", statusCode: 409),
            "invalid_broker" => Results.Problem(title: "Invalid broker", detail: "Broker of record must reference an active broker.", statusCode: 400),
            "invalid_producer" => Results.Problem(title: "Invalid producer", detail: "Primary producer must reference an active user.", statusCode: 400),
            "invalid_relationship" => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["relationshipType"] = ["Unsupported relationshipType."] }),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> ListSubmissions(
        Guid accountId,
        int? page,
        int? pageSize,
        AccountService accountSvc,
        SubmissionService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read") || !await HasAccessAsync(user, authz, "submission", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        if (!await accountSvc.ExistsAccessibleAsync(accountId, user, ct))
            return ProblemDetailsHelper.NotFound("Account", accountId);

        var result = await svc.ListAsync(
            new SubmissionListQuery(null, null, accountId, null, null, null, "createdAt", "desc", page ?? 1, pageSize ?? 25),
            user,
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> ListRenewals(
        Guid accountId,
        int? page,
        int? pageSize,
        AccountService accountSvc,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read") || !await HasAccessAsync(user, authz, "renewal", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        if (!await accountSvc.ExistsAccessibleAsync(accountId, user, ct))
            return ProblemDetailsHelper.NotFound("Account", accountId);

        var result = await svc.ListAsync(
            new RenewalListQuery(user.UserId, user.Roles, user.Regions, null, null, null, null, accountId, null, false, null, "policyExpirationDate", "asc", page ?? 1, pageSize ?? 25),
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> ListPolicies(
        Guid accountId,
        int? page,
        int? pageSize,
        AccountService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        if (!await svc.ExistsAccessibleAsync(accountId, user, ct))
            return ProblemDetailsHelper.NotFound("Account", accountId);

        var result = await svc.ListPoliciesAsync(accountId, page ?? 1, pageSize ?? 25, user, ct);
        return Results.Ok(new
        {
            data = result.Data,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
        });
    }

    private static async Task<IResult> ListTimeline(
        Guid accountId,
        int? page,
        int? pageSize,
        AccountService accountSvc,
        TimelineService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "account", "read"))
            return ProblemDetailsHelper.PolicyDenied();
        if (!await accountSvc.ExistsAccessibleAsync(accountId, user, ct))
            return ProblemDetailsHelper.NotFound("Account", accountId);

        var result = await svc.ListEventsPagedAsync("Account", accountId, page ?? 1, pageSize ?? 25, user, ct);
        return Results.Ok(new
        {
            data = result.Data,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
        });
    }

    private static bool TryParseExpectedRowVersion(HttpContext httpContext, out uint rowVersion)
    {
        var ifMatch = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        return uint.TryParse(ifMatch?.Trim('"'), out rowVersion);
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
}
