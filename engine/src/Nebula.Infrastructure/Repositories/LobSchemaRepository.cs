using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class LobSchemaRepository(AppDbContext db) : ILobSchemaRepository
{
    public async Task<IReadOnlyList<LobSchemaBundle>> ListBundlesAsync(
        string? productKey,
        string? lineOfBusiness,
        bool activeOnly,
        CancellationToken ct = default)
    {
        var query = IncludeProduct(db.LobSchemaBundles.AsNoTracking());

        if (activeOnly)
            query = query.Where(bundle => bundle.Status == "Active");

        if (!string.IsNullOrWhiteSpace(productKey))
            query = query.Where(bundle => bundle.LobProductVersion.LobProduct.ProductKey == productKey);

        if (!string.IsNullOrWhiteSpace(lineOfBusiness))
            query = query.Where(bundle => bundle.LobProductVersion.LobProduct.LineOfBusiness == lineOfBusiness);

        return await query
            .OrderBy(bundle => bundle.LobProductVersion.LobProduct.LineOfBusiness)
            .ThenBy(bundle => bundle.LobProductVersion.LobProduct.ProductKey)
            .ThenBy(bundle => bundle.LobProductVersion.Version)
            .ThenBy(bundle => bundle.SchemaVersion)
            .ToListAsync(ct);
    }

    public async Task<LobSchemaBundle?> GetBundleByIdAsync(
        Guid bundleId,
        bool track,
        CancellationToken ct = default)
    {
        var source = track ? db.LobSchemaBundles : db.LobSchemaBundles.AsNoTracking();
        return await IncludeProduct(source).FirstOrDefaultAsync(bundle => bundle.Id == bundleId, ct);
    }

    public async Task<LobSchemaBundle?> GetBundleByProductVersionIdAsync(
        Guid productVersionId,
        bool track,
        CancellationToken ct = default)
    {
        var source = track ? db.LobSchemaBundles : db.LobSchemaBundles.AsNoTracking();
        return await IncludeProduct(source)
            .Where(bundle => bundle.LobProductVersionId == productVersionId)
            .OrderByDescending(bundle => bundle.Status == "Active")
            .ThenByDescending(bundle => bundle.ActivatedAt)
            .ThenByDescending(bundle => bundle.SchemaVersion)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<LobSchemaBundle?> GetActiveBundleAsync(
        string productKey,
        string productVersion,
        string schemaVersion,
        string? lineOfBusiness,
        CancellationToken ct = default)
    {
        var query = IncludeProduct(db.LobSchemaBundles.AsNoTracking())
            .Where(bundle =>
                bundle.Status == "Active"
                && bundle.SchemaVersion == schemaVersion
                && bundle.LobProductVersion.Version == productVersion
                && bundle.LobProductVersion.LobProduct.ProductKey == productKey);

        if (!string.IsNullOrWhiteSpace(lineOfBusiness))
            query = query.Where(bundle => bundle.LobProductVersion.LobProduct.LineOfBusiness == lineOfBusiness);

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task DeactivateActiveBundlesAsync(
        Guid productVersionId,
        Guid exceptBundleId,
        DateTime now,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await db.LobSchemaBundles
            .Where(bundle =>
                bundle.LobProductVersionId == productVersionId
                && bundle.Id != exceptBundleId
                && bundle.Status == "Active")
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(bundle => bundle.Status, "Deprecated")
                .SetProperty(bundle => bundle.UpdatedAt, now)
                .SetProperty(bundle => bundle.UpdatedByUserId, actorUserId), ct);
    }

    public Task AddActivationEventAsync(
        LobBundleActivationEvent activationEvent,
        CancellationToken ct = default) =>
        db.LobBundleActivationEvents.AddAsync(activationEvent, ct).AsTask();

    private static IQueryable<LobSchemaBundle> IncludeProduct(IQueryable<LobSchemaBundle> source) =>
        source
            .Include(bundle => bundle.LobProductVersion)
            .ThenInclude(version => version.LobProduct);
}
