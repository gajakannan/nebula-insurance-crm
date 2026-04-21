using Casbin;
using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Authorization;

/// <summary>
/// Native Casbin enforcer implementation of <see cref="IAuthorizationService"/>.
/// Replaces the hand-rolled <see cref="PolicyAuthorizationService"/> per ADR-008.
/// Loads model.conf and policy.csv from embedded resources at construction time.
/// </summary>
public sealed class CasbinAuthorizationService : IAuthorizationService
{
    private readonly Enforcer _enforcer;

    public CasbinAuthorizationService()
    {
        var modelText = LoadEmbeddedResource("Nebula.Infrastructure.Authorization.model.conf");
        var policyText = LoadEmbeddedResource("Nebula.Infrastructure.Authorization.policy.csv");

        // Write to temp files for Casbin file-based loading (enforcer reads once at init)
        var modelPath = Path.Combine(Path.GetTempPath(), $"nebula_casbin_model_{Guid.NewGuid():N}.conf");
        var policyPath = Path.Combine(Path.GetTempPath(), $"nebula_casbin_policy_{Guid.NewGuid():N}.csv");

        try
        {
            File.WriteAllText(modelPath, modelText);
            File.WriteAllText(policyPath, policyText);

            _enforcer = new Enforcer(modelPath, policyPath);
        }
        finally
        {
            // Clean up temp files — enforcer has already loaded everything into memory
            TryDelete(modelPath);
            TryDelete(policyPath);
        }
    }

    public Task<bool> AuthorizeAsync(
        string userRole, string resourceType, string action,
        IDictionary<string, object>? resourceAttributes = null)
    {
        // When no attributes are provided, use distinct sentinel values for sub.id, obj.assignee,
        // and obj.creator so that condition expressions like "r.obj.assignee == r.sub.id" and
        // "r.obj.creator == r.sub.id" evaluate to false (deny-by-default).
        // Using empty strings would cause "" == "" → true.
        const string noSubject = "__no_subject__";
        const string noAssignee = "__no_assignee__";
        const string noCreator = "__no_creator__";

        var subId = resourceAttributes is not null
            && resourceAttributes.TryGetValue("subjectId", out var sid)
                ? sid?.ToString() ?? noSubject
                : noSubject;

        var objAssignee = resourceAttributes is not null
            && resourceAttributes.TryGetValue("assignee", out var asg)
                ? asg?.ToString() ?? noAssignee
                : noAssignee;

        var objCreator = resourceAttributes is not null
            && resourceAttributes.TryGetValue("creator", out var crt)
                ? crt?.ToString() ?? noCreator
                : noCreator;

        var sub = new CasbinSubject(userRole, subId);
        var obj = new CasbinObject(resourceType, objAssignee, objCreator);

        var result = _enforcer.Enforce(sub, obj, action);
        return Task.FromResult(result);
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(CasbinAuthorizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                "Ensure the file is included as EmbeddedResource in Nebula.Infrastructure.csproj.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best-effort cleanup */ }
    }

    /// <summary>Maps to r.sub in model.conf: r.sub.role, r.sub.id</summary>
    private sealed record CasbinSubject(string role, string id);

    /// <summary>Maps to r.obj in model.conf: r.obj.type, r.obj.assignee, r.obj.creator</summary>
    private sealed record CasbinObject(string type, string assignee, string creator);
}
