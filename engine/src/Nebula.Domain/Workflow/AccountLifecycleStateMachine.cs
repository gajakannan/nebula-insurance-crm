using Nebula.Domain.Entities;

namespace Nebula.Domain.Workflow;

public static class AccountLifecycleStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.Ordinal)
    {
        [AccountStatuses.Active] = [AccountStatuses.Inactive, AccountStatuses.Merged, AccountStatuses.Deleted],
        [AccountStatuses.Inactive] = [AccountStatuses.Active, AccountStatuses.Merged, AccountStatuses.Deleted],
        [AccountStatuses.Merged] = [],
        [AccountStatuses.Deleted] = [],
    };

    public static bool IsValidTransition(string currentStatus, string nextStatus) =>
        AllowedTransitions.TryGetValue(currentStatus, out var nextStates) && nextStates.Contains(nextStatus);

    public static bool IsTerminal(string status) =>
        string.Equals(status, AccountStatuses.Merged, StringComparison.Ordinal)
        || string.Equals(status, AccountStatuses.Deleted, StringComparison.Ordinal);
}
