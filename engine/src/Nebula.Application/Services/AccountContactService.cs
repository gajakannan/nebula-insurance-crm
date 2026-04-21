using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class AccountContactService(
    IAccountRepository accountRepo,
    IAccountContactRepository contactRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    BrokerScopeResolver scopeResolver)
{
    public async Task<PaginatedResult<AccountContactDto>?> ListAsync(
        Guid accountId,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(accountId, user, brokerScopeId, ct);
        if (accessible is null)
            return null;

        var result = await contactRepo.ListAsync(accountId, page, pageSize, ct);
        return new PaginatedResult<AccountContactDto>(
            result.Data.Select(MapToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<(AccountContactDto? Dto, string? ErrorCode)> CreateAsync(
        Guid accountId,
        AccountContactRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var account = await accountRepo.GetAccessibleByIdAsync(accountId, user, brokerScopeId, ct);
        if (account is null)
            return (null, "not_found");
        if (IsTerminalAccountState(account.Status))
            return (null, "terminal_state");

        if (dto.IsPrimary && await contactRepo.HasAnotherPrimaryAsync(accountId, null, ct))
            return (null, "primary_conflict");

        var now = DateTime.UtcNow;
        var contact = new AccountContact
        {
            AccountId = accountId,
            FullName = dto.FullName,
            Role = dto.Role,
            Email = dto.Email,
            Phone = dto.Phone,
            IsPrimary = dto.IsPrimary,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await contactRepo.AddAsync(contact, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = accountId,
            EventType = "AccountContactAdded",
            EventDescription = $"Contact \"{contact.FullName}\" added to account \"{account.DisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                contactId = contact.Id,
                accountId,
                fullName = contact.FullName,
                isPrimary = contact.IsPrimary,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);
        return (MapToDto(contact), null);
    }

    public async Task<(AccountContactDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid accountId,
        Guid contactId,
        AccountContactRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(accountId, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found");
        if (IsTerminalAccountState(accessible.Status))
            return (null, "terminal_state");

        var contact = await contactRepo.GetByIdAsync(contactId, ct);
        if (contact is null || contact.AccountId != accountId)
            return (null, "contact_not_found");

        if (contact.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (dto.IsPrimary && await contactRepo.HasAnotherPrimaryAsync(accountId, contactId, ct))
            return (null, "primary_conflict");

        var now = DateTime.UtcNow;
        contact.FullName = dto.FullName;
        contact.Role = dto.Role;
        contact.Email = dto.Email;
        contact.Phone = dto.Phone;
        contact.IsPrimary = dto.IsPrimary;
        contact.UpdatedAt = now;
        contact.UpdatedByUserId = user.UserId;
        contact.RowVersion = expectedRowVersion;

        await contactRepo.UpdateAsync(contact, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = accountId,
            EventType = "AccountContactUpdated",
            EventDescription = $"Contact \"{contact.FullName}\" updated on account \"{accessible.DisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                contactId = contact.Id,
                accountId,
                fullName = contact.FullName,
                isPrimary = contact.IsPrimary,
            }),
        }, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed");
        }

        return (MapToDto(contact), null);
    }

    public async Task<string?> DeleteAsync(
        Guid accountId,
        Guid contactId,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(accountId, user, brokerScopeId, ct);
        if (accessible is null)
            return "not_found";
        if (IsTerminalAccountState(accessible.Status))
            return "terminal_state";

        var contact = await contactRepo.GetByIdAsync(contactId, ct);
        if (contact is null || contact.AccountId != accountId)
            return "contact_not_found";

        if (contact.RowVersion != expectedRowVersion)
            return "precondition_failed";

        var now = DateTime.UtcNow;
        contact.IsDeleted = true;
        contact.DeletedAt = now;
        contact.DeletedByUserId = user.UserId;
        contact.UpdatedAt = now;
        contact.UpdatedByUserId = user.UserId;
        contact.RowVersion = expectedRowVersion;

        await contactRepo.UpdateAsync(contact, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = accountId,
            EventType = "AccountContactDeleted",
            EventDescription = $"Contact \"{contact.FullName}\" removed from account \"{accessible.DisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                contactId = contact.Id,
                accountId,
                fullName = contact.FullName,
            }),
        }, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return "precondition_failed";
        }

        return null;
    }

    private async Task<Guid?> ResolveBrokerScopeAsync(ICurrentUserService user, CancellationToken ct)
    {
        if (!user.Roles.Contains("BrokerUser"))
            return null;

        return await scopeResolver.ResolveAsync(user, ct);
    }

    private static AccountContactDto MapToDto(AccountContact contact) => new(
        contact.Id,
        contact.AccountId,
        contact.FullName,
        contact.Role,
        contact.Email,
        contact.Phone,
        contact.IsPrimary,
        contact.RowVersion.ToString(),
        contact.CreatedAt,
        contact.UpdatedAt);

    private static bool IsTerminalAccountState(string status) =>
        string.Equals(status, AccountStatuses.Merged, StringComparison.Ordinal)
        || string.Equals(status, AccountStatuses.Deleted, StringComparison.Ordinal);
}
