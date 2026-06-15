using Nebula.Application.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nebula.Infrastructure.Services;

public class NeuronClient(IHttpClientFactory httpClientFactory) : INeuronClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("neuron");
            using var response = await client.GetAsync("/health/live", ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public Task<BrokerScorecardDto?> GetBrokerScorecardAsync(
        Guid brokerId,
        int windowDays = 90,
        CancellationToken ct = default)
    {
        return GetAsync<BrokerScorecardDto>(
            $"/api/v1/broker-insights/{brokerId}/scorecard?window_days={windowDays}",
            ct);
    }

    public Task<BrokerTrendsDto?> GetBrokerTrendsAsync(
        Guid brokerId,
        int windowDays = 90,
        CancellationToken ct = default)
    {
        return GetAsync<BrokerTrendsDto>(
            $"/api/v1/broker-insights/{brokerId}/trends?window_days={windowDays}",
            ct);
    }

    public Task<BrokerLeaderboardDto?> GetBrokerLeaderboardAsync(
        int limit = 10,
        CancellationToken ct = default)
    {
        return GetAsync<BrokerLeaderboardDto>(
            $"/api/v1/broker-insights/leaderboard?limit={limit}",
            ct);
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("neuron");
            using var response = await client.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
        }
        catch (HttpRequestException)
        {
            return default;
        }
        catch (TaskCanceledException)
        {
            return default;
        }
    }
}
