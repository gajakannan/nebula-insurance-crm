using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IBrokerInsightService
{
    Task<PaginatedResult<BrokerInsightScorecardDto>> GetScorecardsAsync(
        BrokerInsightScorecardQuery query,
        ICurrentUserService user,
        CancellationToken ct);

    Task<BrokerInsightTrendDto?> GetTrendAsync(
        BrokerInsightTrendQuery query,
        ICurrentUserService user,
        CancellationToken ct);

    Task<BrokerInsightBenchmarkDto?> GetBenchmarkAsync(
        BrokerInsightBenchmarkQuery query,
        ICurrentUserService user,
        CancellationToken ct);

    Task<BrokerInsightSnapshotDto?> GetSnapshotAsync(
        BrokerInsightSnapshotQuery query,
        ICurrentUserService user,
        CancellationToken ct);
}
