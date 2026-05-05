using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public sealed class DocumentRetentionService(
    IDocumentRepository documents,
    IDocumentConfigurationProvider config)
{
    public async Task<RetentionSweepResultDto> SweepAsync(bool dryRun, CancellationToken ct = default)
    {
        var snapshot = await config.GetSnapshotAsync(ct);
        var candidates = await documents.ListRetentionCandidatesAsync(ct);
        var now = DateTime.UtcNow;
        var expired = candidates.Where(candidate =>
        {
            var days = snapshot.Retention.PerType.TryGetValue(candidate.Type, out var specific)
                ? specific
                : snapshot.Retention.DefaultRetentionDays;
            return candidate.LastUploadedAtUtc.AddDays(days) <= now;
        }).ToList();

        return await documents.SweepAsync(expired, dryRun, ct);
    }
}
