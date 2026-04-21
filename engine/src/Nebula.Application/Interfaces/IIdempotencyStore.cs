using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, string operation, CancellationToken ct = default);

    Task SaveAsync(IdempotencyRecord record, CancellationToken ct = default);
}
