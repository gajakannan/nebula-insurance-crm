using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nebula.Application.Interfaces;
using Nebula.Domain.Documents;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Documents;

public sealed class QuarantinePromotionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<QuarantinePromotionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var config = scope.ServiceProvider.GetRequiredService<IDocumentConfigurationProvider>();
            var scanner = scope.ServiceProvider.GetRequiredService<IQuarantineScanner>();
            var timeline = scope.ServiceProvider.GetRequiredService<ITimelineRepository>();
            var snapshot = await config.GetSnapshotAsync(stoppingToken);
            await PromoteDueEntriesAsync(documents, scanner, timeline, snapshot.Retention.HoldSeconds, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(snapshot.Retention.WorkerTickSeconds), stoppingToken);
        }
    }

    internal async Task PromoteDueEntriesAsync(
        IDocumentRepository documents,
        IQuarantineScanner scanner,
        ITimelineRepository timeline,
        int holdSeconds,
        CancellationToken ct)
    {
        var entries = await documents.ListPromotableQuarantineEntriesAsync(DateTime.UtcNow, TimeSpan.FromSeconds(holdSeconds), ct);
        foreach (var entry in entries)
        {
            var scan = await scanner.ScanAsync(entry, ct);
            if (scan is not ScanResult.Clean)
            {
                logger.LogWarning("Document quarantine scan did not return clean for {DocumentId} version {Version}", entry.DocumentId, entry.Version);
                continue;
            }

            var result = await documents.PromoteAsync(entry, ct);
            if (!result.Promoted)
            {
                if (result.ErrorCode is not null)
                    logger.LogWarning("Document promotion failed for {DocumentId} version {Version}: {ErrorCode}", entry.DocumentId, entry.Version, result.ErrorCode);
                continue;
            }

            var sidecar = await documents.FindSidecarAsync(entry.DocumentId, ct);
            if (sidecar is null)
                continue;

            await timeline.AddEventAsync(new ActivityTimelineEvent
            {
                EntityType = "Document",
                EntityId = DocumentIds.StableGuid(entry.DocumentId),
                EventType = "DocumentPromoted",
                EventPayloadJson = JsonSerializer.Serialize(new { entry.DocumentId, sidecar.Parent, entry.Version, status = "available" }),
                EventDescription = $"Document {sidecar.LogicalName} promoted from quarantine",
                BrokerDescription = null,
                ActorUserId = Guid.Empty,
                ActorDisplayName = DocumentConstants.SystemQuarantineWorker,
                OccurredAt = DateTime.UtcNow,
            }, ct);
        }
    }
}
