using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> SearchAsync(string query, bool activeOnly, int limit, CancellationToken ct = default);
}
