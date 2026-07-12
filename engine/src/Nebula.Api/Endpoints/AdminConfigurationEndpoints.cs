using System.Text.Json;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class AdminConfigurationEndpoints
{
    private const string Resource = "admin-configuration";

    public static IEndpointRouteBuilder MapAdminConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("AdminConfiguration")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/configuration-domains", ListDomains);
        group.MapGet("/configuration-domains/{domainKey}", GetDomain);
        group.MapPost("/configuration-domains/{domainKey}/drafts", CreateDraft);
        group.MapPatch("/configuration-drafts/{draftId:guid}", UpdateDraft);
        group.MapPost("/configuration-drafts/{draftId:guid}/validation", ValidateDraft);
        group.MapGet("/configuration-drafts/{draftId:guid}/comparison", CompareDraft);
        group.MapPost("/configuration-drafts/{draftId:guid}/publish", PublishDraft);
        group.MapPost("/configuration-domains/{domainKey}/rollback", Rollback);
        group.MapGet("/configuration-audit-events", ListAuditEvents);

        return app;
    }

    private static async Task<IResult> ListDomains(AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "read"))
            return ProblemDetailsHelper.PolicyDenied();
        return Results.Ok(await service.ListDomainsAsync(user, ct));
    }

    private static async Task<IResult> GetDomain(string domainKey, AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "read"))
            return ProblemDetailsHelper.PolicyDenied();
        var (result, error) = await service.GetDomainAsync(domainKey, user, ct);
        return error switch
        {
            "domain_not_found" => NotFound("configuration_domain_not_found", "Configuration domain not found."),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> CreateDraft(string domainKey, AdminConfigurationDraftCreateRequestDto request, AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "draft"))
            return ProblemDetailsHelper.PolicyDenied();
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("reason_required", "A change reason is required to create a draft.");
        var (result, error) = await service.CreateDraftAsync(domainKey, request, user, ct);
        return error switch
        {
            "domain_not_found" => NotFound("configuration_domain_not_found", "Configuration domain not found."),
            "domain_unsupported" => Conflict("configuration_domain_unsupported", "The requested configuration domain is not supported for drafting."),
            "active_draft_exists" => Conflict("active_draft_exists", "An active draft already exists for this configuration domain."),
            _ => Results.Created($"/admin/configuration-drafts/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateDraft(
        Guid draftId,
        AdminConfigurationDraftUpdateRequestDto request,
        HttpContext httpContext,
        AdminConfigurationService service,
        IAuthorizationService authz,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "draft"))
            return ProblemDetailsHelper.PolicyDenied();
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("reason_required", "A change reason is required to save a draft.");
        if (!TryRowVersion(httpContext, out var rowVersion, out var rowVersionProblem))
            return rowVersionProblem!;
        var (result, error) = await service.UpdateDraftAsync(draftId, request, rowVersion, user, ct);
        return error switch
        {
            "draft_not_found" => ProblemDetailsHelper.NotFound("ConfigurationDraft", draftId),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            "draft_not_editable" => Conflict("draft_not_editable", "Published or superseded drafts cannot be edited."),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> ValidateDraft(Guid draftId, AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "validate"))
            return ProblemDetailsHelper.PolicyDenied();
        var (result, error) = await service.ValidateDraftAsync(draftId, user, ct);
        return error switch
        {
            "draft_not_found" => ProblemDetailsHelper.NotFound("ConfigurationDraft", draftId),
            "domain_unsupported" => Conflict("configuration_domain_unsupported", "The requested configuration domain is not supported for validation."),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> CompareDraft(Guid draftId, AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "validate"))
            return ProblemDetailsHelper.PolicyDenied();
        var (result, error) = await service.CompareDraftAsync(draftId, user, ct);
        return error switch
        {
            "draft_not_found" => ProblemDetailsHelper.NotFound("ConfigurationDraft", draftId),
            "domain_unsupported" => Conflict("configuration_domain_unsupported", "The requested configuration domain is not supported for comparison."),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> PublishDraft(Guid draftId, AdminConfigurationPublishRequestDto request, AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "publish"))
            return ProblemDetailsHelper.PolicyDenied();
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("reason_required", "A publish reason is required.");
        var (result, error) = await service.PublishDraftAsync(draftId, request, user, ct);
        return error switch
        {
            "draft_not_found" => ProblemDetailsHelper.NotFound("ConfigurationDraft", draftId),
            "validation_required" => Conflict("validation_required", "The latest validation result must be passing and match the current draft payload before publish."),
            "stale_published_version" => Conflict("stale_published_version", "The draft base version is stale. Create a new draft from the current published version."),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> Rollback(string domainKey, AdminConfigurationPublishRequestDto request, AdminConfigurationService service, IAuthorizationService authz, ICurrentUserService user, CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "rollback"))
            return ProblemDetailsHelper.PolicyDenied();
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("reason_required", "A rollback reason is required.");
        var (result, error) = await service.RollbackAsync(domainKey, request, user, ct);
        return error switch
        {
            "domain_not_found" => NotFound("configuration_domain_not_found", "Configuration domain not found."),
            "rollback_not_supported" => Conflict("rollback_not_supported", "Rollback is not supported for this configuration domain."),
            "rollback_target_required" => Results.Problem(title: "Rollback target required", detail: "targetPublishedVersion is required.", statusCode: 400, extensions: Ext("rollback_target_required")),
            "published_set_not_found" => NotFound("published_set_not_found", "The requested rollback target was not found."),
            _ => Results.Ok(result),
        };
    }

    private static async Task<IResult> ListAuditEvents(
        string? domainKey,
        string? action,
        string? outcome,
        Guid? actorUserId,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        AdminConfigurationService service,
        IAuthorizationService authz,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (!await AuthzHelper.HasPermissionAsync(authz, user, Resource, "audit"))
            return ProblemDetailsHelper.PolicyDenied();
        var result = await service.ListAuditEventsAsync(new AdminConfigurationAuditQuery(domainKey, action, outcome, actorUserId, from, to, page ?? 1, pageSize ?? 50), user, ct);
        return Results.Ok(new
        {
            items = result.Data.Select(AdminConfigurationService.MapAuditEvent),
            result.TotalCount,
            result.Page,
            result.PageSize,
        });
    }

    private static bool TryRowVersion(HttpContext httpContext, out uint rowVersion, out IResult? problem)
    {
        rowVersion = 0;
        problem = null;
        if (!httpContext.Request.Headers.TryGetValue("If-Match", out var values) || values.Count == 0)
        {
            problem = Results.Problem(title: "Missing If-Match header", detail: "If-Match is required for draft updates.", statusCode: 428, extensions: Ext("if_match_required"));
            return false;
        }

        var raw = values[0]?.Trim().Trim('"');
        if (!uint.TryParse(raw, out rowVersion))
        {
            problem = Results.Problem(title: "Invalid If-Match header", detail: "If-Match must contain the current row version.", statusCode: 400, extensions: Ext("invalid_if_match"));
            return false;
        }
        return true;
    }

    private static IResult Conflict(string code, string detail) =>
        Results.Problem(title: "Configuration conflict", detail: detail, statusCode: 409, extensions: Ext(code));

    private static IResult BadRequest(string code, string detail) =>
        Results.Problem(title: "Invalid configuration request", detail: detail, statusCode: 400, extensions: Ext(code));

    private static IResult NotFound(string code, string detail) =>
        Results.Problem(title: "Not found", detail: detail, statusCode: 404, extensions: Ext(code));

    private static Dictionary<string, object?> Ext(string code) =>
        new()
        {
            ["code"] = code,
            ["traceId"] = System.Diagnostics.Activity.Current?.Id,
        };
}
