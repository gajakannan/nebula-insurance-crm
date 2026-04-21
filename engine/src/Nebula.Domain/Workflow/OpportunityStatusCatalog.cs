namespace Nebula.Domain.Workflow;

public sealed record WorkflowStatusDefinition(
    string Code,
    string DisplayName,
    string Description,
    bool IsTerminal,
    short DisplayOrder,
    string? ColorGroup);

public static class OpportunityStatusCatalog
{
    public static readonly IReadOnlyList<WorkflowStatusDefinition> SubmissionStatuses =
    [
        new("Received", "Received", "Initial state when submission is created", false, 1, "intake"),
        new("Triaging", "Triaging", "Initial triage and data validation", false, 2, "triage"),
        new("WaitingOnBroker", "Waiting on Broker", "Awaiting additional information from broker", false, 3, "waiting"),
        new("ReadyForUWReview", "Ready for UW Review", "All data received, queued for underwriter", false, 4, "review"),
        new("InReview", "In Review", "Under active underwriter review", false, 5, "review"),
        new("Quoted", "Quoted", "Quote issued, awaiting broker response", false, 6, "decision"),
        new("BindRequested", "Bind Requested", "Broker accepted quote, bind in progress", false, 7, "decision"),
        new("Bound", "Bound", "Policy bound and issued", true, 8, "won"),
        new("Declined", "Declined", "Submission declined by underwriter", true, 9, "lost"),
        new("Withdrawn", "Withdrawn", "Broker or insured withdrew submission", true, 10, "lost"),
    ];

    public static readonly IReadOnlyList<WorkflowStatusDefinition> RenewalStatuses =
    [
        new("Identified", "Identified", "Renewal created from expiring policy; not yet worked", false, 1, "intake"),
        new("Outreach", "Outreach", "Distribution has initiated broker/account contact", false, 2, "waiting"),
        new("InReview", "In Review", "Underwriting is reviewing the renewal", false, 3, "review"),
        new("Quoted", "Quoted", "Quote has been prepared and shared", false, 4, "decision"),
        new("Completed", "Completed", "Renewal successfully bound; linked to a policy or submission", true, 5, "won"),
        new("Lost", "Lost", "Renewal not retained", true, 6, "lost"),
    ];

    public static readonly IReadOnlySet<string> SubmissionTerminalStatusCodes = SubmissionStatuses
        .Where(s => s.IsTerminal)
        .Select(s => s.Code)
        .ToHashSet(StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> RenewalTerminalStatusCodes = RenewalStatuses
        .Where(s => s.IsTerminal)
        .Select(s => s.Code)
        .ToHashSet(StringComparer.Ordinal);
}
