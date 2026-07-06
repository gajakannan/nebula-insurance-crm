using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IBrokerInsightProjectionRepository
{
    Task<IReadOnlyList<BrokerInsightProjection>> QueryAsync(
        BrokerInsightProjectionQuery query,
        ProjectionVisibility visibility,
        CancellationToken ct);

    Task UpsertManyAsync(IReadOnlyList<BrokerInsightProjection> rows, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}
