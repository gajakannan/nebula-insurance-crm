namespace Nebula.Application.Interfaces;

public interface INeuronClient
{
    Task<bool> PingAsync(CancellationToken ct = default);

    Task<BrokerScorecardDto?> GetBrokerScorecardAsync(
        Guid brokerId,
        int windowDays = 90,
        CancellationToken ct = default);

    Task<BrokerTrendsDto?> GetBrokerTrendsAsync(
        Guid brokerId,
        int windowDays = 90,
        CancellationToken ct = default);

    Task<BrokerLeaderboardDto?> GetBrokerLeaderboardAsync(
        int limit = 10,
        CancellationToken ct = default);
}
