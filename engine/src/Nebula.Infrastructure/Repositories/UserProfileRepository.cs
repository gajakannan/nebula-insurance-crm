using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class UserProfileRepository(AppDbContext db) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId, ct);

    /// <summary>
    /// Case-insensitive search on DisplayName and Email using PostgreSQL ILIKE.
    /// </summary>
    public async Task<IReadOnlyList<UserProfile>> SearchAsync(
        string query, bool activeOnly, int limit, CancellationToken ct = default)
    {
        var q = db.UserProfiles.AsQueryable();

        if (activeOnly)
            q = q.Where(u => u.IsActive);

        // EF Core translates .ToLower() to LOWER() in SQL — works cross-platform.
        // For PostgreSQL specifically we use EF.Functions.ILike for proper ILIKE behaviour.
        var lowerQuery = "%" + query.ToLower() + "%";
        q = q.Where(u =>
            EF.Functions.ILike(u.DisplayName, lowerQuery) ||
            EF.Functions.ILike(u.Email, lowerQuery));

        return await q
            .OrderBy(u => u.DisplayName)
            .Take(limit)
            .ToListAsync(ct);
    }
}
