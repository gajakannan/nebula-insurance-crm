using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nebula.Application.Services;

namespace Nebula.Infrastructure.Services;

public class PolicyExpirationHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PolicyExpirationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultRunAtUtc = new(0, 15, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!ResolveEnabled(configuration["PolicyExpiration:Enabled"]))
        {
            logger.LogInformation("Policy expiration sweep is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var runAtUtc = ResolveRunAtUtc(configuration["PolicyExpiration:RunAtUtc"]);
            var delay = DelayUntilNextRun(DateTimeOffset.UtcNow, runAtUtc);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunSweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Policy expiration sweep failed.");
            }
        }
    }

    internal static TimeSpan DelayUntilNextRun(DateTimeOffset nowUtc, TimeSpan runAtUtc)
    {
        var todayRun = new DateTimeOffset(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            runAtUtc.Hours,
            runAtUtc.Minutes,
            runAtUtc.Seconds,
            TimeSpan.Zero);

        var nextRun = nowUtc < todayRun ? todayRun : todayRun.AddDays(1);
        return nextRun - nowUtc;
    }

    private static TimeSpan ResolveRunAtUtc(string? value) =>
        TimeSpan.TryParse(value, out var parsed) ? parsed : DefaultRunAtUtc;

    private static bool ResolveEnabled(string? value) =>
        !bool.TryParse(value, out var parsed) || parsed;

    private static int ResolveBatchSize(string? value) =>
        int.TryParse(value, out var parsed) ? Math.Clamp(parsed, 1, 1000) : 250;

    private async Task RunSweepAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var policyService = scope.ServiceProvider.GetRequiredService<PolicyService>();
        var batchSize = ResolveBatchSize(configuration["PolicyExpiration:BatchSize"]);
        var expiredCount = await policyService.ExpireIssuedPoliciesAsync(DateTime.UtcNow, batchSize, ct);

        if (expiredCount > 0)
            logger.LogInformation("Policy expiration sweep expired {PolicyCount} policies.", expiredCount);
    }
}
