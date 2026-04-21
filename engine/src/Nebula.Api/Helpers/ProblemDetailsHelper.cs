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
        extensions: Ext("forbidden"));

    public static IResult PolicyDenied() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have permission to perform this action.",
        statusCode: 403,
        extensions: Ext("policy_denied"));

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

    public static IResult PreconditionFailed() =>
        PreconditionFailed("submission");

    public static IResult PreconditionFailed(string resourceName) => Results.Problem(
        title: "Precondition failed",
        detail: $"The {resourceName} was modified by another user. Refresh the detail view and retry with the current rowVersion.",
        statusCode: 412,
        extensions: Ext("precondition_failed"));

    private static Dictionary<string, object?> Ext(string code) => new()
    {
        ["code"] = code,
        ["traceId"] = Activity.Current?.Id,
    };
}
