using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public class UserService(IUserProfileRepository userProfileRepo, IAuthorizationService authz)
{
    /// <summary>
    /// Search for UserProfiles for the assignee picker.
    /// Returns null if the caller is not authorized (caller should return 403).
    /// </summary>
    public async Task<UserSearchResponseDto?> SearchAsync(
        string query, bool activeOnly, int limit,
        ICurrentUserService user, CancellationToken ct = default)
    {
        // Casbin: role, user, search, true — any matching role suffices
        var authorized = false;
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "user", "search"))
            {
                authorized = true;
                break;
            }
        }

        if (!authorized)
            return null;

        var profiles = await userProfileRepo.SearchAsync(query, activeOnly, limit, ct);
        var dtos = profiles.Select(p => new UserSummaryDto(
            p.Id,
            p.DisplayName,
            p.Email,
            JsonSerializer.Deserialize<List<string>>(p.RolesJson) ?? [],
            p.IsActive)).ToList();

        return new UserSearchResponseDto(dtos);
    }
}
