using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Api.Services;

public class HttpCurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db) : ICurrentUserService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HttpContext available.");

    private Guid? _userId;
    public Guid UserId
    {
        get
        {
            if (_userId.HasValue) return _userId.Value;
            var iss = User.FindFirstValue("iss") ?? "";
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            if (string.IsNullOrEmpty(iss) || string.IsNullOrEmpty(sub))
            {
                _userId = Guid.Empty;
                return Guid.Empty;
            }

            var profile = db.UserProfiles.FirstOrDefault(u => u.IdpIssuer == iss && u.IdpSubject == sub);
            if (profile is null)
                profile = UpsertProfile(iss, sub);

            _userId = profile.Id;
            return _userId.Value;
        }
    }

    private UserProfile UpsertProfile(string iss, string sub)
    {
        var now = DateTime.UtcNow;
        var roles = User.FindAll("nebula_roles").Select(c => c.Value).Distinct().ToList();
        var profile = new UserProfile
        {
            IdpIssuer = iss,
            IdpSubject = sub,
            Email = User.FindFirstValue("email") ?? $"{sub}@unknown",
            DisplayName = User.FindFirstValue("name") ?? sub,
            Department = "",
            RolesJson = JsonSerializer.Serialize(roles),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.UserProfiles.Add(profile);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Another concurrent request already inserted this user — discard and re-query.
            db.Entry(profile).State = EntityState.Detached;
            profile = db.UserProfiles.First(u => u.IdpIssuer == iss && u.IdpSubject == sub);
        }
        return profile;
    }

    public string? DisplayName => User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Name);

    // authentik emits nebula_roles as a JSON array; JwtBearer maps each element to a separate Claim
    public IReadOnlyList<string> Roles =>
        User.FindAll("nebula_roles").Select(c => c.Value).Distinct().ToList();

    public IReadOnlyList<string> Regions =>
        User.FindAll("regions").Select(c => c.Value).ToList();

    public string? BrokerTenantId => User.FindFirstValue("broker_tenant_id");
}
