using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;

namespace Nebula.Application.Services;

public class AccountService(
    IAccountRepository accountRepo,
    IAccountRelationshipHistoryRepository relationshipHistoryRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IBrokerRepository brokerRepo,
    IUserProfileRepository userProfileRepo,
    IUnitOfWork unitOfWork,
    BrokerScopeResolver scopeResolver)
{
    public const int MergeLinkedRecordsThreshold = 500;

    private static string? NormalizeTaxId(string? taxId) =>
        string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim().ToUpperInvariant();

    public async Task<PaginatedResult<AccountListItemDto>> ListAsync(
        AccountListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var result = await accountRepo.ListAsync(query, user, brokerScopeId, ct);
        var summaries = query.IncludeSummary
            ? await accountRepo.GetSummaryProjectionAsync(result.Data.Select(account => account.Id).ToArray(), user, brokerScopeId, ct)
            : new Dictionary<Guid, AccountSummaryProjection>();

        var items = result.Data
            .Select(account =>
            {
                var summary = summaries.GetValueOrDefault(account.Id);
                return new AccountListItemDto(
                    account.Id,
                    account.DisplayName,
                    account.LegalName,
                    account.TaxId,
                    account.Status,
                    account.BrokerOfRecordId,
                    account.BrokerOfRecord?.LegalName,
                    account.TerritoryCode,
                    account.Region,
                    account.PrimaryLineOfBusiness,
                    summary?.LastActivityAt,
                    summary?.ActivePolicyCount,
                    summary?.OpenSubmissionCount,
                    summary?.RenewalDueCount,
                    account.RowVersion.ToString());
            })
            .ToList();

        return new PaginatedResult<AccountListItemDto>(items, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<(AccountDto? Dto, string? ErrorCode, string? StableDisplayName, DateTime? RemovedAt, string? ReasonCode)> GetByIdAsync(
        Guid id,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found", null, null, null);

        var account = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        if (account.Status == AccountStatuses.Deleted)
        {
            return (null, "deleted", account.StableDisplayName, account.RemovedAt, account.DeleteReasonCode);
        }

        return (MapToDto(account), null, null, null, null);
    }

    public async Task<(AccountDto? Dto, string? ErrorCode)> CreateAsync(
        AccountCreateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.TaxId)
            && await accountRepo.ExistsActiveTaxIdAsync(dto.TaxId, null, ct))
        {
            return (null, "duplicate_tax_id");
        }

        if (dto.BrokerOfRecordId.HasValue)
        {
            var broker = await brokerRepo.GetByIdAsync(dto.BrokerOfRecordId.Value, ct);
            if (broker is null || !string.Equals(broker.Status, "Active", StringComparison.Ordinal))
                return (null, "invalid_broker");
        }

        if (dto.PrimaryProducerUserId.HasValue)
        {
            var producer = await userProfileRepo.GetByIdAsync(dto.PrimaryProducerUserId.Value, ct);
            if (producer is null || !producer.IsActive)
                return (null, "invalid_producer");
        }

        var now = DateTime.UtcNow;
        var name = dto.DisplayName.Trim();
        var account = new Account
        {
            Name = name,
            LegalName = dto.LegalName,
            TaxId = NormalizeTaxId(dto.TaxId),
            Industry = dto.Industry,
            PrimaryLineOfBusiness = dto.PrimaryLineOfBusiness,
            Status = AccountStatuses.Active,
            BrokerOfRecordId = dto.BrokerOfRecordId,
            PrimaryProducerUserId = dto.PrimaryProducerUserId,
            TerritoryCode = dto.TerritoryCode,
            Region = dto.Region,
            PrimaryState = dto.State ?? string.Empty,
            Address1 = dto.Address1,
            Address2 = dto.Address2,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            StableDisplayName = name,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await accountRepo.AddAsync(account, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = account.Id,
            EventType = "AccountCreated",
            EventDescription = $"Account \"{account.DisplayName}\" created",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                accountId = account.Id,
                displayName = account.DisplayName,
                brokerOfRecordId = account.BrokerOfRecordId,
                territoryCode = account.TerritoryCode,
                region = account.Region,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        var created = await accountRepo.GetByIdWithRelationsAsync(account.Id, ct)
            ?? throw new InvalidOperationException("Created account could not be reloaded.");

        return (MapToDto(created), null);
    }

    public async Task<(AccountDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid id,
        AccountUpdateRequestDto dto,
        IReadOnlySet<string> presentFields,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found");

        var account = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        if (account.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (AccountLifecycleStateMachine.IsTerminal(account.Status))
            return (null, "terminal_state");

        if (presentFields.Contains("displayName") && string.IsNullOrWhiteSpace(dto.DisplayName))
            return (null, "display_name_required");

        if (presentFields.Contains("taxId")
            && !string.IsNullOrWhiteSpace(dto.TaxId)
            && await accountRepo.ExistsActiveTaxIdAsync(dto.TaxId, account.Id, ct))
        {
            return (null, "duplicate_tax_id");
        }

        var changedFields = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (presentFields.Contains("displayName") && dto.DisplayName is not null && !string.Equals(dto.DisplayName, account.DisplayName, StringComparison.Ordinal))
        {
            changedFields["displayName"] = dto.DisplayName;
            account.Name = dto.DisplayName.Trim();
            account.StableDisplayName = account.Name;
        }

        ApplyNullableField("legalName", dto.LegalName, account.LegalName, presentFields, changedFields, value => account.LegalName = value);
        ApplyNullableField("taxId", NormalizeTaxId(dto.TaxId), account.TaxId, presentFields, changedFields, value => account.TaxId = value);
        ApplyNullableField("industry", dto.Industry, account.Industry, presentFields, changedFields, value => account.Industry = value);
        ApplyNullableField("primaryLineOfBusiness", dto.PrimaryLineOfBusiness, account.PrimaryLineOfBusiness, presentFields, changedFields, value => account.PrimaryLineOfBusiness = value);
        ApplyNullableField("territoryCode", dto.TerritoryCode, account.TerritoryCode, presentFields, changedFields, value => account.TerritoryCode = value);
        ApplyNullableField("region", dto.Region, account.Region, presentFields, changedFields, value => account.Region = value);
        ApplyNullableField("address1", dto.Address1, account.Address1, presentFields, changedFields, value => account.Address1 = value);
        ApplyNullableField("address2", dto.Address2, account.Address2, presentFields, changedFields, value => account.Address2 = value);
        ApplyNullableField("city", dto.City, account.City, presentFields, changedFields, value => account.City = value);
        if (presentFields.Contains("state") && !string.Equals(dto.State, account.State, StringComparison.Ordinal))
        {
            changedFields["state"] = dto.State;
            account.PrimaryState = dto.State ?? string.Empty;
        }
        ApplyNullableField("postalCode", dto.PostalCode, account.PostalCode, presentFields, changedFields, value => account.PostalCode = value);
        ApplyNullableField("country", dto.Country, account.Country, presentFields, changedFields, value => account.Country = value);

        if (changedFields.Count == 0)
            return (MapToDto(account), null);

        var now = DateTime.UtcNow;
        account.UpdatedAt = now;
        account.UpdatedByUserId = user.UserId;
        account.RowVersion = expectedRowVersion;

        await accountRepo.UpdateAsync(account, ct);
        if (changedFields.ContainsKey("displayName"))
        {
            await accountRepo.PropagateFallbackStateAsync(
                account.Id,
                account.StableDisplayName,
                account.Status,
                account.MergedIntoAccountId,
                ct);
        }
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = account.Id,
            EventType = "AccountUpdated",
            EventDescription = $"Account \"{account.DisplayName}\" updated",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new { changedFields }),
        }, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed");
        }

        return (MapToDto(account), null);
    }

    public async Task<(AccountDto? Dto, string? ErrorCode)> ChangeRelationshipAsync(
        Guid id,
        AccountRelationshipRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found");

        var account = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        if (account.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (AccountLifecycleStateMachine.IsTerminal(account.Status))
            return (null, "terminal_state");

        string? previousValue;
        switch (dto.RelationshipType)
        {
            case "BrokerOfRecord":
            {
                previousValue = account.BrokerOfRecordId?.ToString();
                if (!Guid.TryParse(dto.NewValue, out var brokerId))
                    return (null, "invalid_broker");

                var broker = await brokerRepo.GetByIdAsync(brokerId, ct);
                if (broker is null || !string.Equals(broker.Status, "Active", StringComparison.Ordinal))
                    return (null, "invalid_broker");

                account.BrokerOfRecordId = brokerId;
                break;
            }
            case "PrimaryProducer":
            {
                previousValue = account.PrimaryProducerUserId?.ToString();
                if (!Guid.TryParse(dto.NewValue, out var producerId))
                    return (null, "invalid_producer");

                var producer = await userProfileRepo.GetByIdAsync(producerId, ct);
                if (producer is null || !producer.IsActive)
                    return (null, "invalid_producer");

                account.PrimaryProducerUserId = producerId;
                break;
            }
            case "Territory":
            {
                previousValue = account.TerritoryCode;
                account.TerritoryCode = dto.NewValue.Trim();
                break;
            }
            default:
                return (null, "invalid_relationship");
        }

        var now = DateTime.UtcNow;
        var transitionFromState = previousValue is null ? null : $"Previous{dto.RelationshipType}";
        var transitionToState = $"Updated{dto.RelationshipType}";
        account.UpdatedAt = now;
        account.UpdatedByUserId = user.UserId;
        account.RowVersion = expectedRowVersion;

        await accountRepo.UpdateAsync(account, ct);
        await relationshipHistoryRepo.AddAsync(new AccountRelationshipHistory
        {
            AccountId = account.Id,
            RelationshipType = dto.RelationshipType,
            PreviousValue = previousValue,
            NewValue = dto.NewValue,
            EffectiveAt = now,
            ActorUserId = user.UserId,
            Notes = dto.Notes,
        }, ct);
        await transitionRepo.AddAsync(new WorkflowTransition
        {
            WorkflowType = "AccountRelationship",
            EntityId = account.Id,
            FromState = transitionFromState,
            ToState = transitionToState,
            Reason = dto.Notes,
            ActorUserId = user.UserId,
            OccurredAt = now,
        }, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = account.Id,
            EventType = "AccountRelationshipChanged",
            EventDescription = $"{dto.RelationshipType} updated for \"{account.DisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                relationshipType = dto.RelationshipType,
                previousValue,
                newValue = dto.NewValue,
                notes = dto.Notes,
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

        return (MapToDto(account), null);
    }

    public async Task<(AccountDto? Dto, string? ErrorCode)> TransitionAsync(
        Guid id,
        AccountLifecycleRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found");

        var account = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        if (account.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (!AccountLifecycleStateMachine.IsValidTransition(account.Status, dto.ToState))
            return (null, "invalid_transition");

        if (dto.ToState == AccountStatuses.Deleted && string.IsNullOrWhiteSpace(dto.ReasonCode))
            return (null, "reason_required");

        if (dto.ToState == AccountStatuses.Deleted
            && string.Equals(dto.ReasonCode, "Other", StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(dto.ReasonDetail))
        {
            return (null, "reason_detail_required");
        }

        var now = DateTime.UtcNow;
        var fromState = account.Status;
        account.Status = dto.ToState;
        account.UpdatedAt = now;
        account.UpdatedByUserId = user.UserId;
        account.RowVersion = expectedRowVersion;

        if (dto.ToState == AccountStatuses.Deleted)
        {
            account.DeleteReasonCode = dto.ReasonCode;
            account.DeleteReasonDetail = dto.ReasonDetail;
            account.RemovedAt = now;
        }
        else
        {
            account.DeleteReasonCode = null;
            account.DeleteReasonDetail = null;
            if (dto.ToState == AccountStatuses.Active)
                account.RemovedAt = null;
        }

        var transition = new WorkflowTransition
        {
            WorkflowType = "AccountLifecycle",
            EntityId = account.Id,
            FromState = fromState,
            ToState = dto.ToState,
            Reason = dto.ReasonCode ?? dto.ReasonDetail,
            ActorUserId = user.UserId,
            OccurredAt = now,
        };

        await transitionRepo.AddAsync(transition, ct);
        await accountRepo.UpdateAsync(account, ct);
        await accountRepo.PropagateFallbackStateAsync(
            account.Id,
            account.StableDisplayName,
            account.Status,
            account.MergedIntoAccountId,
            ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = account.Id,
            EventType = "AccountLifecycleTransitioned",
            EventDescription = $"Account \"{account.DisplayName}\" transitioned from {fromState} to {dto.ToState}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                fromState,
                toState = dto.ToState,
                reasonCode = dto.ReasonCode,
                reasonDetail = dto.ReasonDetail,
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

        return (MapToDto(account), null);
    }

    public async Task<(AccountMergePreviewDto? Dto, string? ErrorCode)> GetMergePreviewAsync(
        Guid id,
        Guid survivorId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (id == survivorId)
            return (null, "self_merge");

        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessibleSource = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessibleSource is null)
            return (null, "not_found");

        var source = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        var accessibleSurvivor = await accountRepo.GetAccessibleByIdAsync(survivorId, user, brokerScopeId, ct);
        if (accessibleSurvivor is null)
            return (null, "survivor_not_found");

        var survivor = await accountRepo.GetByIdWithRelationsAsync(survivorId, ct)
            ?? throw new InvalidOperationException("Accessible survivor account could not be loaded with relations.");

        var impact = await accountRepo.GetMergeImpactAsync(source.Id, ct);
        return (new AccountMergePreviewDto(
            source.Id,
            survivor.Id,
            source.DisplayName,
            survivor.DisplayName,
            impact.SubmissionCount,
            impact.PolicyCount,
            impact.RenewalCount,
            impact.ContactCount,
            impact.TimelineCount,
            impact.TotalLinked,
            MergeLinkedRecordsThreshold), null);
    }

    public async Task<(AccountDto? Dto, string? ErrorCode, int? LinkedCount)> MergeAsync(
        Guid id,
        AccountMergeRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (id == dto.SurvivorAccountId)
            return (null, "self_merge", null);

        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found", null);

        var source = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        if (source.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        var accessibleSurvivor = await accountRepo.GetAccessibleByIdAsync(dto.SurvivorAccountId, user, brokerScopeId, ct);
        if (accessibleSurvivor is null)
            return (null, "survivor_not_found", null);
        var survivor = await accountRepo.GetByIdWithRelationsAsync(dto.SurvivorAccountId, ct)
            ?? throw new InvalidOperationException("Accessible survivor account could not be loaded with relations.");

        if (source.Status == AccountStatuses.Merged)
        {
            return source.MergedIntoAccountId == dto.SurvivorAccountId
                ? (MapToDto(source), null, null)
                : (null, "merge_conflict", null);
        }

        if (AccountLifecycleStateMachine.IsTerminal(source.Status))
            return (null, "invalid_transition", null);

        if (!string.Equals(survivor.Status, AccountStatuses.Active, StringComparison.Ordinal))
            return (null, "survivor_not_active", null);

        var impact = await accountRepo.GetMergeImpactAsync(source.Id, ct);
        if (impact.TotalLinked > MergeLinkedRecordsThreshold)
            return (null, "merge_too_large", impact.TotalLinked);

        var now = DateTime.UtcNow;
        var fromState = source.Status;
        source.Status = AccountStatuses.Merged;
        source.MergedIntoAccountId = survivor.Id;
        source.RemovedAt = now;
        source.UpdatedAt = now;
        source.UpdatedByUserId = user.UserId;
        source.RowVersion = expectedRowVersion;

        await transitionRepo.AddAsync(new WorkflowTransition
        {
            WorkflowType = "AccountLifecycle",
            EntityId = source.Id,
            FromState = fromState,
            ToState = AccountStatuses.Merged,
            Reason = dto.Notes,
            ActorUserId = user.UserId,
            OccurredAt = now,
        }, ct);
        await accountRepo.UpdateAsync(source, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = source.Id,
            EventType = "AccountMerged",
            EventDescription = $"Account \"{source.StableDisplayName}\" merged into \"{survivor.DisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                sourceAccountId = source.Id,
                survivorAccountId = survivor.Id,
                notes = dto.Notes,
            }),
        }, ct);
        await accountRepo.PropagateFallbackStateAsync(
            source.Id,
            source.StableDisplayName,
            source.Status,
            source.MergedIntoAccountId,
            ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Account",
            EntityId = survivor.Id,
            EventType = "AccountMergeIn",
            EventDescription = $"Merged in account \"{source.StableDisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                sourceAccountId = source.Id,
                survivorAccountId = survivor.Id,
                notes = dto.Notes,
            }),
        }, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed", null);
        }

        return (MapToDto(source), null, impact.TotalLinked);
    }

    public async Task<(AccountSummaryDto? Dto, string? ErrorCode, string? StableDisplayName, DateTime? RemovedAt, string? ReasonCode)> GetSummaryAsync(
        Guid id,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return (null, "not_found", null, null, null);

        var account = await accountRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Accessible account could not be loaded with relations.");

        if (account.Status == AccountStatuses.Deleted)
        {
            return (null, "deleted", account.StableDisplayName, account.RemovedAt, account.DeleteReasonCode);
        }

        var summary = (await accountRepo.GetSummaryProjectionAsync([id], user, brokerScopeId, ct)).GetValueOrDefault(id)
            ?? new AccountSummaryProjection(0, 0, 0, null);

        return (new AccountSummaryDto(
            account.Id,
            account.DisplayName,
            account.Status,
            account.BrokerOfRecord?.LegalName,
            account.PrimaryProducer?.DisplayName,
            account.TerritoryCode,
            account.Region,
            summary.ActivePolicyCount,
            summary.OpenSubmissionCount,
            summary.RenewalDueCount,
            summary.LastActivityAt,
            account.RowVersion.ToString()), null, null, null, null);
    }

    public async Task<bool> ExistsAccessibleAsync(
        Guid id,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        return await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct) is not null;
    }

    public async Task<PaginatedResult<AccountPolicyListItemDto>> ListPoliciesAsync(
        Guid id,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var accessible = await accountRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        if (accessible is null)
            return new PaginatedResult<AccountPolicyListItemDto>([], page, pageSize, 0);

        var policies = await accountRepo.ListPoliciesAsync(id, page, pageSize, ct);
        return new PaginatedResult<AccountPolicyListItemDto>(
            policies.Data.Select(policy => new AccountPolicyListItemDto(
                policy.Id,
                policy.PolicyNumber,
                policy.Carrier?.Name,
                policy.LineOfBusiness,
                policy.EffectiveDate,
                policy.ExpirationDate,
                policy.TotalPremium,
                policy.CurrentStatus)).ToList(),
            policies.Page,
            policies.PageSize,
            policies.TotalCount);
    }

    private async Task<Guid?> ResolveBrokerScopeAsync(ICurrentUserService user, CancellationToken ct)
    {
        if (!user.Roles.Contains("BrokerUser"))
            return null;

        return await scopeResolver.ResolveAsync(user, ct);
    }

    private static AccountDto MapToDto(Account account) => new(
        account.Id,
        account.DisplayName,
        account.StableDisplayName,
        account.LegalName,
        account.TaxId,
        account.Industry,
        account.PrimaryLineOfBusiness,
        account.Status,
        account.BrokerOfRecordId,
        account.BrokerOfRecord?.LegalName,
        account.PrimaryProducerUserId,
        account.PrimaryProducer?.DisplayName,
        account.TerritoryCode,
        account.Region,
        account.Address1,
        account.Address2,
        account.City,
        account.State,
        account.PostalCode,
        account.Country,
        account.MergedIntoAccountId,
        account.SurvivorAccountId,
        account.DeleteReasonCode,
        account.DeleteReasonDetail,
        account.RemovedAt,
        account.RowVersion.ToString(),
        account.CreatedAt,
        account.CreatedByUserId,
        account.UpdatedAt,
        account.UpdatedByUserId);

    private static void ApplyNullableField(
        string fieldName,
        string? nextValue,
        string? currentValue,
        IReadOnlySet<string> presentFields,
        IDictionary<string, object?> changedFields,
        Action<string?> apply)
    {
        if (!presentFields.Contains(fieldName) || string.Equals(nextValue, currentValue, StringComparison.Ordinal))
            return;

        changedFields[fieldName] = nextValue;
        apply(nextValue);
    }
}
