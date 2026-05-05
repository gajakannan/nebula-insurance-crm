using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nebula.Application.Services;

namespace Nebula.Infrastructure.Documents;

public sealed class DocumentRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentRetentionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var retention = scope.ServiceProvider.GetRequiredService<DocumentRetentionService>();
                var result = await retention.SweepAsync(dryRun: false, stoppingToken);
                if (result.Swept > 0)
                    logger.LogInformation("Document retention swept {Swept} of {Scanned} candidates", result.Swept, result.Scanned);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Document retention sweep failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
