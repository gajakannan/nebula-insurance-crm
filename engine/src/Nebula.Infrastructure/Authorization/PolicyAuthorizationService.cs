using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Authorization;

/// <summary>
/// Implements ABAC authorization matching the Casbin model.conf and policy.csv semantics.
/// Evaluates: role == policy.sub AND resourceType == policy.obj AND action == policy.act AND eval(policy.cond)
/// </summary>
public class PolicyAuthorizationService : IAuthorizationService
{
    private static readonly List<PolicyRule> Rules = LoadPolicies();

    public Task<bool> AuthorizeAsync(
        string userRole, string resourceType, string action,
        IDictionary<string, object>? resourceAttributes = null)
    {
        foreach (var rule in Rules)
        {
            if (rule.Role == userRole && rule.Resource == resourceType && rule.Action == action)
            {
                if (EvaluateCondition(rule.Condition, resourceAttributes))
                    return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private static bool EvaluateCondition(string condition, IDictionary<string, object>? attrs)
    {
        if (condition == "true") return true;

        // r.obj.assignee == r.sub.id — task ownership check
        if (condition == "r.obj.assignee == r.sub.id")
        {
            if (attrs is null) return false;
            return attrs.TryGetValue("assignee", out var assignee)
                && attrs.TryGetValue("subjectId", out var subjectId)
                && string.Equals(assignee?.ToString(), subjectId?.ToString(), StringComparison.Ordinal);
        }

        return false;
    }

    private static List<PolicyRule> LoadPolicies()
    {
        var assembly = typeof(PolicyAuthorizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream("Nebula.Infrastructure.Authorization.policy.csv")
            ?? throw new InvalidOperationException(
                "Embedded resource 'Nebula.Infrastructure.Authorization.policy.csv' not found. " +
                "Ensure policy.csv is included as EmbeddedResource in Nebula.Infrastructure.csproj.");

        using var reader = new StreamReader(stream);
        var rules = new List<PolicyRule>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 4 || parts[0].Trim() != "p")
                continue;

            var role = parts[1].Trim();
            var resource = parts[2].Trim();
            var action = parts[3].Trim();
            var condition = parts.Length >= 5 ? parts[4].Trim() : "true";
            rules.Add(new PolicyRule(role, resource, action, condition));
        }
        return rules;
    }

    private record PolicyRule(string Role, string Resource, string Action, string Condition = "true");
}
