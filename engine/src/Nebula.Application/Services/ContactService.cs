using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class ContactService(
    IContactRepository contactRepo,
    IBrokerRepository brokerRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    BrokerScopeResolver scopeResolver,
    ILogger<ContactService> logger)
{
    private readonly ILogger<ContactService> _logger = logger;

    public async Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await contactRepo.GetByIdAsync(id, ct);
        if (contact is null) return null;
        return MaskPii(MapToDto(contact), contact.Broker?.Status);
    }

    /// <summary>
    /// BrokerUser variant: scope-isolated contact read (F0009-S0004).
    /// Verifies the contact's broker is within the authenticated user's resolved scope.
    /// Throws BrokerScopeUnresolvableException if scope cannot be resolved or contact belongs to another broker.
    /// </summary>
    public async Task<ContactBrokerUserDto?> GetByIdForBrokerUserAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        var contact = await contactRepo.GetByIdAsync(id, ct);
        if (contact is null) return null;
        if (contact.BrokerId != resolvedBrokerId)
            throw new BrokerScopeUnresolvableException();

        AuditBrokerUserRead(user, "broker.contacts", id, resolvedBrokerId);
        return MapToBrokerUserDto(contact);
    }

    /// <summary>
    /// BrokerUser variant: scope-isolated contact list (F0009-S0004).
    /// Scopes results to the resolved broker only.
    /// Throws BrokerScopeUnresolvableException if scope cannot be resolved.
    /// </summary>
    public async Task<PaginatedResult<ContactBrokerUserDto>> ListForBrokerUserAsync(
        int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        var result = await contactRepo.ListAsync(resolvedBrokerId, page, pageSize, ct);
        var mapped = result.Data.Select(MapToBrokerUserDto).ToList();
        AuditBrokerUserRead(user, "broker.contacts", resolvedBrokerId, resolvedBrokerId);
        return new PaginatedResult<ContactBrokerUserDto>(mapped, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<PaginatedResult<ContactDto>> ListAsync(
        Guid? brokerId, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        var result = await contactRepo.ListAsync(brokerId, page, pageSize, ct);
        var mapped = result.Data.Select(c => MaskPii(MapToDto(c), c.Broker?.Status)).ToList();
        AuditBrokerUserRead(user, "broker.contacts", brokerId);
        return new PaginatedResult<ContactDto>(mapped, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<(ContactDto? Dto, string? ErrorCode)> CreateAsync(
        ContactCreateDto dto, ICurrentUserService user, CancellationToken ct = default)
    {
        var broker = await brokerRepo.GetByIdAsync(dto.BrokerId, ct);
        if (broker is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        var contact = new Contact
        {
            BrokerId = dto.BrokerId,
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Role = dto.Role ?? "Primary",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await contactRepo.AddAsync(contact, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Broker",
            EntityId = dto.BrokerId,
            EventType = "ContactAdded",
            EventDescription = $"Contact \"{contact.FullName}\" added to broker \"{broker.LegalName}\"",
            BrokerDescription = BrokerDescriptionTemplates.ContactAdded,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = contact.Id,
                fullName = contact.FullName,
                brokerId = dto.BrokerId,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return (MaskPii(MapToDto(contact), broker.Status), null);
    }

    public async Task<(ContactDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid id, ContactUpdateDto dto, uint rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        var contact = await contactRepo.GetByIdAsync(id, ct);
        if (contact is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        contact.FullName = dto.FullName;
        contact.Email = dto.Email;
        contact.Phone = dto.Phone;
        contact.Role = dto.Role ?? contact.Role;
        contact.UpdatedAt = now;
        contact.UpdatedByUserId = user.UserId;
        contact.RowVersion = rowVersion;

        await contactRepo.UpdateAsync(contact, ct);

        if (contact.BrokerId.HasValue)
        {
            await timelineRepo.AddEventAsync(new ActivityTimelineEvent
            {
                EntityType = "Broker",
                EntityId = contact.BrokerId.Value,
                EventType = "ContactUpdated",
                EventDescription = $"Contact \"{contact.FullName}\" updated",
                BrokerDescription = BrokerDescriptionTemplates.ContactUpdated,
                ActorUserId = user.UserId,
                ActorDisplayName = user.DisplayName,
                OccurredAt = now,
                EventPayloadJson = JsonSerializer.Serialize(new
                {
                    id = contact.Id,
                    fullName = contact.FullName,
                    brokerId = contact.BrokerId.Value,
                }),
            }, ct);
        }

        await unitOfWork.CommitAsync(ct);

        return (MaskPii(MapToDto(contact), contact.Broker?.Status), null);
    }

    public async Task<string?> DeleteAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var contact = await contactRepo.GetByIdAsync(id, ct);
        if (contact is null) return "not_found";

        var now = DateTime.UtcNow;
        contact.IsDeleted = true;
        contact.DeletedAt = now;
        contact.DeletedByUserId = user.UserId;
        contact.UpdatedAt = now;
        contact.UpdatedByUserId = user.UserId;

        await contactRepo.UpdateAsync(contact, ct);

        if (contact.BrokerId.HasValue)
        {
            await timelineRepo.AddEventAsync(new ActivityTimelineEvent
            {
                EntityType = "Broker",
                EntityId = contact.BrokerId.Value,
                EventType = "ContactDeleted",
                EventDescription = $"Contact \"{contact.FullName}\" deleted",
                ActorUserId = user.UserId,
                ActorDisplayName = user.DisplayName,
                OccurredAt = now,
                EventPayloadJson = JsonSerializer.Serialize(new
                {
                    id = contact.Id,
                    fullName = contact.FullName,
                    brokerId = contact.BrokerId.Value,
                }),
            }, ct);
        }

        await unitOfWork.CommitAsync(ct);

        return null;
    }

    private static ContactDto MapToDto(Contact c) => new(
        c.Id, c.BrokerId, c.AccountId, c.FullName, c.Email, c.Phone, c.Role, c.RowVersion);

    private static ContactBrokerUserDto MapToBrokerUserDto(Contact c) => new(
        c.Id, c.BrokerId ?? Guid.Empty, c.FullName, c.Email, c.Phone, c.Role, c.CreatedAt, c.UpdatedAt);

    private static ContactDto MaskPii(ContactDto dto, string? brokerStatus) =>
        brokerStatus == "Inactive" ? dto with { Email = null, Phone = null } : dto;

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
