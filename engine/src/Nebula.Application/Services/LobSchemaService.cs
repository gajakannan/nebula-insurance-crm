using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class LobSchemaService(
    ILobSchemaRepository schemaRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<LobSchemaBundleDto>> ListActiveAsync(
        string? productKey,
        string? lineOfBusiness,
        CancellationToken ct = default)
    {
        var bundles = await schemaRepository.ListBundlesAsync(productKey, lineOfBusiness, activeOnly: true, ct);
        return bundles.Select(MapBundle).ToList();
    }

    public async Task<LobSchemaBundleDto?> ResolveActiveAsync(
        string productKey,
        string productVersion,
        string schemaVersion,
        string? lineOfBusiness,
        CancellationToken ct = default)
    {
        var bundle = await schemaRepository.GetActiveBundleAsync(productKey, productVersion, schemaVersion, lineOfBusiness, ct);
        return bundle is null ? null : MapBundle(bundle);
    }

    public async Task<LobSchemaBundleDto?> GetBundleAsync(Guid bundleId, CancellationToken ct = default)
    {
        var bundle = await schemaRepository.GetBundleByIdAsync(bundleId, track: false, ct);
        return bundle is null ? null : MapBundle(bundle);
    }

    public async Task<LobSchemaBundleDto?> GetBundleByProductVersionAsync(
        Guid productVersionId,
        string stage,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stage))
            return null;

        var bundle = await schemaRepository.GetBundleByProductVersionIdAsync(productVersionId, track: false, ct);
        return bundle is null ? null : MapBundle(bundle);
    }

    public async Task<(LobBundleActivationResultDto? Result, string? ErrorCode)> ActivateProductVersionAsync(
        Guid productVersionId,
        LobBundleActivationRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var bundle = await schemaRepository.GetBundleByProductVersionIdAsync(productVersionId, track: true, ct);
        if (bundle is null)
            return (null, "not_found");

        return await ActivateBundleAsync(bundle, request, user, ct);
    }

    public async Task<(LobBundleActivationResultDto? Result, string? ErrorCode)> ActivateAsync(
        Guid bundleId,
        LobBundleActivationRequestDto request,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var bundle = await schemaRepository.GetBundleByIdAsync(bundleId, track: true, ct);
        if (bundle is null)
            return (null, "not_found");

        return await ActivateBundleAsync(bundle, request, user, ct);
    }

    private async Task<(LobBundleActivationResultDto? Result, string? ErrorCode)> ActivateBundleAsync(
        LobSchemaBundle bundle,
        LobBundleActivationRequestDto request,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var previousStatus = bundle.Status;

        await schemaRepository.DeactivateActiveBundlesAsync(bundle.LobProductVersionId, bundle.Id, now, user.UserId, ct);

        bundle.Status = "Active";
        bundle.ActivatedAt = now;
        bundle.ActivatedByUserId = user.UserId;
        bundle.UpdatedAt = now;
        bundle.UpdatedByUserId = user.UserId;

        var activationEvent = new LobBundleActivationEvent
        {
            LobSchemaBundleId = bundle.Id,
            FromStatus = previousStatus,
            ToStatus = "Active",
            ChangeNote = request.ChangeNote,
            ActorUserId = user.UserId,
            OccurredAt = now,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };
        await schemaRepository.AddActivationEventAsync(activationEvent, ct);
        await unitOfWork.CommitAsync(ct);

        return (new LobBundleActivationResultDto(MapBundle(bundle), activationEvent.Id), null);
    }

    public static LobSchemaBundleDto MapBundle(LobSchemaBundle bundle) => new(
        bundle.Id,
        bundle.LobProductVersion.LobProduct.ProductKey,
        bundle.LobProductVersion.Version,
        bundle.LobProductVersion.LobProduct.LineOfBusiness,
        bundle.SchemaVersion,
        bundle.Status,
        Deserialize(bundle.DataSchemaJson),
        Deserialize(bundle.UiSchemaJson),
        Deserialize(bundle.RulesJson),
        Deserialize(bundle.ProjectionMapJson),
        bundle.ContentHash,
        bundle.ActivatedAt,
        bundle.ActivatedByUserId,
        bundle.RowVersion.ToString());

    private static object? Deserialize(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);
}
