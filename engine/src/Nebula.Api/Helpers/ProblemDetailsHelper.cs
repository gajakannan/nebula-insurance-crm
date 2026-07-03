using System.Diagnostics;

namespace Nebula.Api.Helpers;

public static class ProblemDetailsHelper
{
    public static IResult DuplicateLicense() => Results.Problem(
        title: "Duplicate license number",
        detail: "A broker with the given license number already exists.",
        statusCode: 409,
        extensions: Ext("duplicate_license"));

    public static IResult ActiveDependenciesExist() => Results.Problem(
        title: "Active submissions or renewals exist",
        detail: "Cannot deactivate a broker with active (non-terminal) submissions or renewals.",
        statusCode: 409,
        extensions: Ext("active_dependencies_exist"));

    public static IResult AlreadyActive() => Results.Problem(
        title: "Broker is already active",
        detail: "The broker is currently Active and cannot be reactivated.",
        statusCode: 409,
        extensions: Ext("already_active"));

    public static IResult InvalidTransition(string from, string to) => Results.Problem(
        title: "Invalid workflow transition",
        detail: $"Transition from '{from}' to '{to}' is not allowed.",
        statusCode: 409,
        extensions: Ext("invalid_transition"));

    public static IResult InvalidStatusTransition(string from, string to) => Results.Problem(
        title: "Invalid status transition",
        detail: $"Transition from '{from}' to '{to}' is not allowed.",
        statusCode: 409,
        extensions: Ext("invalid_status_transition"));

    public static IResult ConcurrencyConflict() => Results.Problem(
        title: "Concurrency conflict",
        detail: "The resource was modified by another user. Please refresh and retry.",
        statusCode: 409,
        extensions: Ext("concurrency_conflict"));

    public static IResult NotFound(string resource, Guid id) => Results.Problem(
        title: $"{resource} not found",
        detail: $"{resource} with ID {id} does not exist.",
        statusCode: 404,
        extensions: Ext("not_found"));

    public static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have permission to perform this action.",
        statusCode: 403,
        type: "https://nebula.local/problems/authz/forbidden",
        extensions: Ext("forbidden"));

    public static IResult PolicyDenied() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have permission to perform this action.",
        statusCode: 403,
        type: "https://nebula.local/problems/authz/forbidden",
        extensions: Ext("policy_denied"));

    public static IResult AuthTokenExpired(string traceId) => Results.Problem(
        title: "Authentication token expired",
        detail: "The access token has expired. Renew the session and retry the request.",
        statusCode: 401,
        type: "https://nebula.local/problems/auth/token-expired",
        extensions: Ext("token_expired", traceId));

    public static IResult AuthInvalidToken(string traceId) => Results.Problem(
        title: "Authentication token invalid",
        detail: "The access token is invalid.",
        statusCode: 401,
        type: "https://nebula.local/problems/auth/invalid-token",
        extensions: Ext("invalid_token", traceId));

    public static IResult AuthSessionRevoked(string traceId) => Results.Problem(
        title: "Session revoked",
        detail: "The session is no longer valid.",
        statusCode: 401,
        type: "https://nebula.local/problems/auth/session-revoked",
        extensions: Ext("session_revoked", traceId));

    public static IResult AuthorizationForbidden(string traceId) => Results.Problem(
        title: "Forbidden",
        detail: "You do not have permission to perform this action.",
        statusCode: 403,
        type: "https://nebula.local/problems/authz/forbidden",
        extensions: Ext("forbidden", traceId));

    public static IResult InactiveAssignee() => Results.Problem(
        title: "Inactive assignee",
        detail: "The specified user is inactive and cannot be assigned tasks.",
        statusCode: 422,
        extensions: Ext("inactive_assignee"));

    public static IResult InvalidAssignee() => Results.Problem(
        title: "Invalid assignee",
        detail: "The specified user does not exist.",
        statusCode: 422,
        extensions: Ext("invalid_assignee"));

    public static IResult DuplicateRenewal() => Results.Problem(
        title: "Duplicate renewal",
        detail: "An active renewal already exists for the specified policy.",
        statusCode: 409,
        extensions: Ext("duplicate_renewal"));

    public static IResult AssignmentNotAllowedInTerminalState() => Results.Problem(
        title: "Assignment not allowed",
        detail: "Completed and lost renewals cannot be reassigned.",
        statusCode: 409,
        extensions: Ext("assignment_not_allowed_in_terminal_state"));

    public static IResult StatusChangeRestricted() => Results.Problem(
        title: "Status change restricted",
        detail: "Only the task assignee can change the task status.",
        statusCode: 403,
        extensions: Ext("status_change_restricted"));

    public static IResult ViewNotAuthorized() => Results.Problem(
        title: "View not authorized",
        detail: "You do not have permission to access this view.",
        statusCode: 403,
        extensions: Ext("view_not_authorized"));

    public static IResult ValidationError(IDictionary<string, string[]> errors) => Results.Problem(
        title: "Validation error",
        detail: "One or more validation errors occurred.",
        statusCode: 400,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "validation_error",
            ["errors"] = errors,
            ["traceId"] = Activity.Current?.Id,
        });

    public static IResult TelemetryValidationError(IDictionary<string, string[]> errors) => Results.Problem(
        title: "Session continuity telemetry validation error",
        detail: "One or more telemetry events failed validation.",
        statusCode: 400,
        type: "https://nebula.local/problems/session-continuity/telemetry-validation",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "validation_error",
            ["errors"] = errors,
            ["traceId"] = Activity.Current?.Id,
        });

    public static IResult NeuronCompanionTelemetryValidationError(IDictionary<string, string[]> errors) => Results.Problem(
        title: "Neuron companion telemetry validation error",
        detail: "One or more telemetry events failed validation.",
        statusCode: 400,
        type: "https://nebula.local/problems/neuron-companion/telemetry-validation",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "validation_error",
            ["errors"] = errors,
            ["traceId"] = Activity.Current?.Id,
        });

    public static IResult LobValidationFailed(object errors) => Results.Problem(
        title: "LOB attribute validation failed",
        detail: "The submitted LOB attributes do not satisfy the active product schema bundle.",
        statusCode: 422,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "lob_validation_failed",
            ["lobErrors"] = errors,
            ["traceId"] = Activity.Current?.Id,
        });

    public static IResult RegionMismatch() => Results.Problem(
        title: "Region mismatch",
        detail: "Account region is not in the broker's licensed region set.",
        statusCode: 400,
        extensions: Ext("region_mismatch"));

    public static IResult InvalidAccount(Guid id) => Results.Problem(
        title: "Invalid account",
        detail: $"Account {id} does not exist or is soft-deleted.",
        statusCode: 400,
        extensions: Ext("invalid_account"));

    public static IResult InvalidBroker(Guid id) => Results.Problem(
        title: "Invalid broker",
        detail: $"Broker {id} does not exist, is soft-deleted, or is inactive.",
        statusCode: 400,
        extensions: Ext("invalid_broker"));

    public static IResult InvalidProgram(Guid id) => Results.Problem(
        title: "Invalid program",
        detail: $"Program {id} does not exist or is soft-deleted.",
        statusCode: 400,
        extensions: Ext("invalid_program"));

    public static IResult InvalidLob(string lob) => Results.Problem(
        title: "Invalid line of business",
        detail: $"'{lob}' is not in the known line of business set.",
        statusCode: 400,
        extensions: Ext("invalid_lob"));

    public static IResult InvalidSubmissionAssignee(string detail) => Results.Problem(
        title: "Invalid assignee",
        detail: detail,
        statusCode: 400,
        extensions: Ext("invalid_assignee"));

    public static IResult MissingTransitionPrerequisite(IReadOnlyList<string> missingItems) => Results.Problem(
        title: "Missing transition prerequisite",
        detail: $"Cannot transition: {string.Join("; ", missingItems)}",
        statusCode: 409,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "missing_transition_prerequisite",
            ["missingItems"] = missingItems,
            ["traceId"] = Activity.Current?.Id,
        });

    public static IResult MergeTooLarge(int linkedCount, int threshold) => Results.Problem(
        title: "Merge too large",
        detail: $"Merge involves {linkedCount} linked records, exceeding the synchronous threshold of {threshold}. Contact an administrator to perform a bulk merge.",
        statusCode: 413,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "merge_too_large",
            ["linkedCount"] = linkedCount,
            ["threshold"] = threshold,
            ["traceId"] = Activity.Current?.Id,
        });

    public static IResult IdempotencyKeyConflict(string key) => Results.Problem(
        title: "Idempotency key conflict",
        detail: $"Idempotency key '{key}' was previously used for a different operation or resource.",
        statusCode: 409,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "idempotency_key_conflict",
            ["key"] = key,
            ["traceId"] = Activity.Current?.Id,
        });

    // ── F0017 distribution hierarchy ───────────────────────────────────────
    public static IResult DistributionNodeSelfParent() => Results.Problem(
        title: "Invalid parent",
        detail: "A distribution node cannot be its own parent.",
        statusCode: 422,
        extensions: Ext("distribution_node_self_parent"));

    public static IResult DistributionNodeCycle() => Results.Problem(
        title: "Hierarchy cycle",
        detail: "The requested parent is a descendant of this node; the move would create a cycle.",
        statusCode: 409,
        extensions: Ext("distribution_node_cycle"));

    public static IResult InvalidDistributionParent() => Results.Problem(
        title: "Invalid distribution parent",
        detail: "The requested parent does not exist, is inactive, or is soft-deleted.",
        statusCode: 422,
        extensions: Ext("invalid_distribution_parent"));

    // ── F0017 effective-dated producer ownership ───────────────────────────
    public static IResult OwnershipPeriodOverlap() => Results.Problem(
        title: "Ownership period overlap",
        detail: "An overlapping active ownership period already exists for this scope.",
        statusCode: 409,
        extensions: Ext("ownership_period_overlap"));

    public static IResult OwnershipPeriodInvalid() => Results.Problem(
        title: "Invalid ownership period",
        detail: "The effective date is invalid: it must be on or after the current open period and form a valid range.",
        statusCode: 422,
        extensions: Ext("ownership_period_invalid"));

    // ── F0017 territory + effective-dated territory assignment ──────────────
    public static IResult TerritoryDuplicateName() => Results.Problem(
        title: "Duplicate territory name",
        detail: "An active territory with the same name already exists.",
        statusCode: 409,
        extensions: Ext("territory_duplicate_name"));

    public static IResult TerritoryAssignmentOverlap() => Results.Problem(
        title: "Territory assignment overlap",
        detail: "A conflicting active territory assignment already exists for this member.",
        statusCode: 409,
        extensions: Ext("territory_assignment_overlap"));

    public static IResult TerritoryAssignmentPeriodInvalid() => Results.Problem(
        title: "Invalid territory assignment period",
        detail: "The effective date is invalid: it must be on or after the current open period and form a valid range.",
        statusCode: 422,
        extensions: Ext("territory_assignment_period_invalid"));

    public static IResult PreconditionFailed() =>
        PreconditionFailed("submission");

    public static IResult PreconditionFailed(string resourceName) => Results.Problem(
        title: "Precondition failed",
        detail: $"The {resourceName} was modified by another user. Refresh the detail view and retry with the current rowVersion.",
        statusCode: 412,
        extensions: Ext("precondition_failed"));

    // ── F0023 global search, saved views, operational reporting ─────────────
    public static IResult SearchQueryTooShort() => Results.Problem(
        title: "Search query too short",
        detail: "The search query must be at least 2 characters after trimming.",
        statusCode: 400,
        extensions: Ext("search_query_too_short"));

    public static IResult SavedViewDuplicateName() => Results.Problem(
        title: "Duplicate saved view name",
        detail: "An active saved view with the same name already exists in this scope.",
        statusCode: 409,
        extensions: Ext("saved_view_duplicate_name"));

    public static IResult SavedViewScopeRequired() => Results.Problem(
        title: "Team scope required",
        detail: "Team saved views require a valid teamScopeType and teamScopeKey.",
        statusCode: 400,
        extensions: Ext("saved_view_scope_required"));

    public static IResult SavedViewScopeDenied() => Results.Problem(
        title: "Saved view scope denied",
        detail: "You do not administer the requested team scope.",
        statusCode: 403,
        extensions: Ext("saved_view_scope_denied"));

    public static IResult SavedViewCriteriaInvalid() => Results.Problem(
        title: "Saved view criteria invalid",
        detail: "The saved view criteria must be a structured JSON object.",
        statusCode: 422,
        extensions: Ext("saved_view_criteria_invalid"));

    private static Dictionary<string, object?> Ext(string code) => new()
    {
        ["code"] = code,
        ["traceId"] = Activity.Current?.Id,
    };

    private static Dictionary<string, object?> Ext(string code, string traceId) => new()
    {
        ["code"] = code,
        ["traceId"] = traceId,
    };
}
