using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class BrokerService(
    IBrokerRepository brokerRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    BrokerScopeResolver scopeResolver,
    ILogger<BrokerService> logger)
{
    private readonly ILogger<BrokerService> _logger = logger;

    public async Task<BrokerDto?> GetByIdAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        // Admin and DistributionManager may view deactivated (soft-deleted) brokers — F0002-S0005/S0008.
        // All other roles get null (→ 404) for deactivated brokers via the global query filter.
        var canSeeDeactivated = user.Roles.Contains("Admin") || user.Roles.Contains("DistributionManager");
        var broker = canSeeDeactivated
            ? await brokerRepo.GetByIdIncludingDeactivatedAsync(id, ct)
            : await brokerRepo.GetByIdAsync(id, ct);

        if (broker is null) return null;
        AuditBrokerUserRead(user, "broker.detail", id, id);
        return MaskPii(MapToDto(broker));
    }

    /// <summary>
    /// BrokerUser variant: scope-isolated GET /brokers/{id} (F0009-S0004).
    /// Verifies the requested broker ID is within the authenticated user's resolved scope.
    /// Throws BrokerScopeUnresolvableException if scope cannot be resolved or doesn't match.
    /// </summary>
    public async Task<BrokerBrokerUserDto?> GetByIdForBrokerUserAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        if (resolvedBrokerId != id)
            throw new BrokerScopeUnresolvableException();

        var broker = await brokerRepo.GetByIdAsync(id, ct);
        if (broker is null) return null;

        AuditBrokerUserRead(user, "broker.detail", id, resolvedBrokerId);
        return MapToBrokerUserDto(broker);
    }

    /// <summary>
    /// BrokerUser variant: scope-isolated GET /brokers (F0009-S0004).
    /// Returns only the single broker within the authenticated user's resolved scope.
    /// Throws BrokerScopeUnresolvableException if scope cannot be resolved.
    /// </summary>
    public async Task<PaginatedResult<BrokerBrokerUserDto>> ListForBrokerUserAsync(
        ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        var broker = await brokerRepo.GetByIdAsync(resolvedBrokerId, ct);
        AuditBrokerUserRead(user, "broker.list", null, resolvedBrokerId);

        var items = broker is null ? [] : new List<BrokerBrokerUserDto> { MapToBrokerUserDto(broker) };
        return new PaginatedResult<BrokerBrokerUserDto>(items, 1, 20, items.Count);
    }

    public async Task<PaginatedResult<BrokerDto>> ListAsync(
        string? search, string? statusFilter, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        var result = await brokerRepo.ListAsync(search, statusFilter, page, pageSize, ct);
        var mapped = result.Data.Select(b => MaskPii(MapToDto(b))).ToList();
        AuditBrokerUserRead(user, "broker.list", null);
        return new PaginatedResult<BrokerDto>(mapped, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<(BrokerDto? Dto, string? ErrorCode)> CreateAsync(
        BrokerCreateDto dto, ICurrentUserService user, CancellationToken ct = default)
    {
        if (await brokerRepo.ExistsByLicenseAsync(dto.LicenseNumber, ct))
            return (null, "duplicate_license");

        var now = DateTime.UtcNow;
        var broker = new Broker
        {
            LegalName = dto.LegalName,
            LicenseNumber = dto.LicenseNumber,
            State = dto.State,
            Status = "Pending",
            Email = dto.Email,
            Phone = dto.Phone,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await brokerRepo.AddAsync(broker, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Broker",
            EntityId = broker.Id,
            EventType = "BrokerCreated",
            EventDescription = $"New broker \"{broker.LegalName}\" added",
            BrokerDescription = BrokerDescriptionTemplates.BrokerCreated,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = broker.Id,
                legalName = broker.LegalName,
                licenseNumber = broker.LicenseNumber,
                state = broker.State,
                status = broker.Status,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return (MapToDto(broker), null);
    }

    public async Task<(BrokerDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid id, BrokerUpdateDto dto, uint rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        var broker = await brokerRepo.GetByIdAsync(id, ct);
        if (broker is null) return (null, "not_found");

        var oldStatus = broker.Status;
        var now = DateTime.UtcNow;

        broker.LegalName = dto.LegalName;
        broker.State = dto.State;
        broker.Status = dto.Status;
        broker.Email = dto.Email;
        broker.Phone = dto.Phone;
        broker.UpdatedAt = now;
        broker.UpdatedByUserId = user.UserId;
        broker.RowVersion = rowVersion;

        await brokerRepo.UpdateAsync(broker, ct);

        var eventType = oldStatus != dto.Status ? "BrokerStatusChanged" : "BrokerUpdated";
        var description = oldStatus != dto.Status
            ? $"Broker \"{broker.LegalName}\" status changed from {oldStatus} to {dto.Status}"
            : $"Broker \"{broker.LegalName}\" updated";
        var brokerDescription = eventType == "BrokerStatusChanged"
            ? string.Format(BrokerDescriptionTemplates.BrokerStatusChanged, broker.Status)
            : BrokerDescriptionTemplates.BrokerUpdated;

        var payloadJson = eventType == "BrokerStatusChanged"
            ? JsonSerializer.Serialize(new
            {
                id = broker.Id,
                legalName = broker.LegalName,
                previousStatus = oldStatus,
                newStatus = broker.Status,
            })
            : JsonSerializer.Serialize(new
            {
                id = broker.Id,
                legalName = broker.LegalName,
                state = broker.State,
                status = broker.Status,
            });

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Broker",
            EntityId = broker.Id,
            EventType = eventType,
            EventDescription = description,
            BrokerDescription = brokerDescription,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = payloadJson,
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return (MaskPii(MapToDto(broker)), null);
    }

    public async Task<string?> DeleteAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var broker = await brokerRepo.GetByIdAsync(id, ct);
        if (broker is null) return "not_found";

        if (await brokerRepo.HasActiveSubmissionsOrRenewalsAsync(id, ct))
            return "active_dependencies_exist";

        var now = DateTime.UtcNow;
        broker.IsDeleted = true;
        broker.Status = "Inactive";
        broker.DeletedAt = now;
        broker.DeletedByUserId = user.UserId;
        broker.UpdatedAt = now;
        broker.UpdatedByUserId = user.UserId;

        await brokerRepo.UpdateAsync(broker, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Broker",
            EntityId = broker.Id,
            EventType = "BrokerDeactivated",
            EventDescription = $"Broker \"{broker.LegalName}\" deactivated",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = broker.Id,
                legalName = broker.LegalName,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return null;
    }

    public async Task<(BrokerDto? Dto, string? ErrorCode)> ReactivateAsync(
        Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var broker = await brokerRepo.GetByIdIncludingDeactivatedAsync(id, ct);
        if (broker is null) return (null, "not_found");

        if (!broker.IsDeleted) return (null, "already_active");

        var now = DateTime.UtcNow;
        broker.IsDeleted = false;
        broker.DeletedAt = null;
        broker.DeletedByUserId = null;
        broker.Status = "Active";
        broker.UpdatedAt = now;
        broker.UpdatedByUserId = user.UserId;

        await brokerRepo.UpdateAsync(broker, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Broker",
            EntityId = broker.Id,
            EventType = "BrokerReactivated",
            EventDescription = $"Broker \"{broker.LegalName}\" reactivated",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = broker.Id,
                legalName = broker.LegalName,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return (MapToDto(broker), null);
    }

    private static BrokerDto MapToDto(Broker b) => new(
        b.Id, b.LegalName, b.LicenseNumber, b.State, b.Status,
        b.Email, b.Phone, b.CreatedAt, b.UpdatedAt, b.RowVersion, b.IsDeleted);

    private static BrokerBrokerUserDto MapToBrokerUserDto(Broker b) => new(
        b.Id, b.LegalName, b.LicenseNumber, b.State, b.Status,
        b.Email, b.Phone, b.CreatedAt, b.UpdatedAt);

    private static BrokerDto MaskPii(BrokerDto dto) =>
        dto.Status == "Inactive" ? dto with { Email = null, Phone = null } : dto;

    private void AuditBrokerUserRead(ICurrentUserService user, string resource, Guid? entityId, Guid? resolvedBrokerId = null)
    {
        if (!user.Roles.Contains("BrokerUser")) return;
        _logger.LogInformation(
            "BrokerUser access: {Resource} by BrokerTenantId={BrokerTenantId} ResolvedBrokerId={ResolvedBrokerId} EntityId={EntityId} OccurredAt={OccurredAt}",
            resource,
            user.BrokerTenantId,
            resolvedBrokerId,
            entityId,
            DateTime.UtcNow);
    }
}
