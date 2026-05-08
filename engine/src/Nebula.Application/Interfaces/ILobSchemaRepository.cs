using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ILobSchemaRepository
{
    Task<IReadOnlyList<LobSchemaBundle>> ListBundlesAsync(
        string? productKey,
        string? lineOfBusiness,
        bool activeOnly,
        CancellationToken ct = default);

    Task<LobSchemaBundle?> GetBundleByIdAsync(
        Guid bundleId,
        bool track,
        CancellationToken ct = default);

    Task<LobSchemaBundle?> GetBundleByProductVersionIdAsync(
        Guid productVersionId,
        bool track,
        CancellationToken ct = default);

    Task<LobSchemaBundle?> GetActiveBundleAsync(
        string productKey,
        string productVersion,
        string schemaVersion,
        string? lineOfBusiness,
        CancellationToken ct = default);

    Task DeactivateActiveBundlesAsync(
        Guid productVersionId,
        Guid exceptBundleId,
        DateTime now,
        Guid actorUserId,
        CancellationToken ct = default);

    Task AddActivationEventAsync(
        LobBundleActivationEvent activationEvent,
        CancellationToken ct = default);
}
