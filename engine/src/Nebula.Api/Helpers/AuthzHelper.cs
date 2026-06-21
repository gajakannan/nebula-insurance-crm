using Nebula.Application.Common;
using Nebula.Application.Interfaces;

namespace Nebula.Api.Helpers;

/// <summary>Shared endpoint helpers for Casbin role-loop checks and comma-separated query parsing.</summary>
public static class AuthzHelper
{
    public static async Task<bool> HasPermissionAsync(
        IAuthorizationService authz, ICurrentUserService user, string resource, string action,
        IDictionary<string, object>? attrs = null)
    {
        attrs ??= new Dictionary<string, object>();
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, resource, action, attrs))
                return true;
        }
        return false;
    }

    public static IReadOnlyList<string> ParseMulti(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
