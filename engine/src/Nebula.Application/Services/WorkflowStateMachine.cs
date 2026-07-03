using Nebula.Domain.Workflow;

namespace Nebula.Application.Services;

public static class WorkflowStateMachine
{
    private static readonly IReadOnlySet<string> SubmissionTerminalStates = OpportunityStatusCatalog.SubmissionTerminalStatusCodes;
    private static readonly IReadOnlySet<string> RenewalTerminalStates = OpportunityStatusCatalog.RenewalTerminalStatusCodes;

    private static readonly Dictionary<string, HashSet<string>> SubmissionTransitions = new()
    {
        ["Received"] = ["Triaging"],
        ["Triaging"] = ["WaitingOnBroker", "ReadyForUWReview"],
        ["WaitingOnBroker"] = ["ReadyForUWReview"],
        ["ReadyForUWReview"] = ["InReview"],
        ["InReview"] = ["Quoted", "Declined"],
        ["Quoted"] = ["BindRequested", "Declined", "Withdrawn"],
        ["BindRequested"] = ["Bound", "Withdrawn"],
    };

    private static readonly Dictionary<string, HashSet<string>> RenewalTransitions = new()
    {
        ["Identified"] = ["Outreach"],
        ["Outreach"] = ["InReview"],
        ["InReview"] = ["Quoted", "Lost"],
        ["Quoted"] = ["Completed", "Lost"],
    };

    private static readonly Dictionary<(string From, string To), HashSet<string>> RenewalTransitionRoles = new()
    {
        [("Identified", "Outreach")] = ["DistributionUser", "DistributionManager", "Admin"],
        [("Outreach", "InReview")] = ["DistributionUser", "DistributionManager", "Underwriter", "Admin"],
        [("InReview", "Quoted")] = ["Underwriter", "Admin"],
        [("InReview", "Lost")] = ["Underwriter", "Admin"],
        [("Quoted", "Completed")] = ["Underwriter", "Admin"],
        [("Quoted", "Lost")] = ["Underwriter", "Admin"],
    };

    // WHY (F0038 / ADR-028 §3): the Underwriter owns the renewal in commercial P&C and is the sole
    // outreach author, but Identified->Outreach is a Distribution-owned move under the general
    // renewal:transition rules above. Rather than widen that Distribution-owned surface for every
    // path (over-grant), F0038 permits the move for the Underwriter (and Admin) ONLY on the
    // outreach mock-send path, which is gated by the dedicated least-privilege renewal:draft_outreach
    // Casbin action. This set is the defense-in-depth role check behind that gate.
    private static readonly HashSet<string> OutreachMockSendRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Underwriter", "Admin" };

    public static bool IsValidTransition(string workflowType, string from, string to) =>
        workflowType switch
        {
            "Submission" => IsValidTransition(SubmissionTransitions, SubmissionTerminalStates, from, to),
            "Renewal" => IsValidTransition(RenewalTransitions, RenewalTerminalStates, from, to),
            _ => false,
        };

    public static bool IsTerminalState(string workflowType, string state) =>
        workflowType switch
        {
            "Submission" => SubmissionTerminalStates.Contains(state),
            "Renewal" => RenewalTerminalStates.Contains(state),
            _ => false,
        };

    public static IReadOnlyList<string> GetAvailableTransitions(string workflowType, string from) =>
        workflowType switch
        {
            "Submission" => GetAvailableTransitions(SubmissionTransitions, SubmissionTerminalStates, from),
            "Renewal" => GetAvailableTransitions(RenewalTransitions, RenewalTerminalStates, from),
            _ => [],
        };

    public static string? ValidateRenewalTransition(string from, string to, IReadOnlyList<string> userRoles)
    {
        if (!IsValidTransition("Renewal", from, to))
            return "invalid_transition";

        if (!RenewalTransitionRoles.TryGetValue((from, to), out var allowedRoles))
            return "invalid_transition";

        return userRoles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            ? null
            : "policy_denied";
    }

    // F0038 / ADR-028 §3: path-scoped exception for the outreach mock-send flow only.
    // Permits Identified -> Outreach for the Underwriter (and Admin) exclusively via mock-send,
    // gated at the endpoint by the renewal:draft_outreach Casbin action. Every other renewal
    // transition path continues to use ValidateRenewalTransition (ownership split unchanged).
    public static string? ValidateOutreachMockSendTransition(string from, string to, IReadOnlyList<string> userRoles)
    {
        if (!string.Equals(from, "Identified", StringComparison.Ordinal)
            || !string.Equals(to, "Outreach", StringComparison.Ordinal))
        {
            return "invalid_transition";
        }

        return userRoles.Any(OutreachMockSendRoles.Contains)
            ? null
            : "policy_denied";
    }

    public static IReadOnlyList<string> GetAvailableRenewalTransitions(string currentStatus, IReadOnlyList<string> userRoles)
    {
        if (!RenewalTransitions.TryGetValue(currentStatus, out var targets))
            return [];

        return targets
            .Where(target =>
                RenewalTransitionRoles.TryGetValue((currentStatus, target), out var allowedRoles)
                && userRoles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsValidTransition(
        IReadOnlyDictionary<string, HashSet<string>> transitions,
        IReadOnlySet<string> terminalStates,
        string from,
        string to)
    {
        if (terminalStates.Contains(from)) return false;
        return transitions.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    private static IReadOnlyList<string> GetAvailableTransitions(
        IReadOnlyDictionary<string, HashSet<string>> transitions,
        IReadOnlySet<string> terminalStates,
        string from)
    {
        if (terminalStates.Contains(from))
            return [];

        return transitions.TryGetValue(from, out var targets)
            ? targets.OrderBy(target => target, StringComparer.Ordinal).ToList()
            : [];
    }
}
