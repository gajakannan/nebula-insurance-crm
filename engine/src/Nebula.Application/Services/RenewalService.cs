using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;

namespace Nebula.Application.Services;

public class RenewalService(
    IRenewalRepository renewalRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IReferenceDataRepository referenceDataRepo,
    IUserProfileRepository userProfileRepo,
    IWorkflowSlaThresholdRepository workflowSlaThresholdRepo,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<RenewalListItemDto>> ListAsync(
        RenewalListQuery query,
        CancellationToken ct = default)
    {
        var renewals = await renewalRepo.ListAsync(query, ct);
        var items = new List<RenewalListItemDto>(renewals.Data.Count);

        foreach (var renewal in renewals.Data)
        {
            items.Add(await MapListItemAsync(renewal, ct));
        }

        return new PaginatedResult<RenewalListItemDto>(items, renewals.Page, renewals.PageSize, renewals.TotalCount);
    }

    public async Task<RenewalDto?> GetByIdAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var renewal = await renewalRepo.GetByIdWithRelationsAsync(id, ct);
        if (renewal is null || !CanReadRenewal(user, renewal))
            return null;

        return await MapDetailAsync(renewal, user, ct);
    }

    public async Task<bool> ExistsAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var renewal = await renewalRepo.GetByIdWithRelationsAsync(id, ct);
        return renewal is not null && CanReadRenewal(user, renewal);
    }

    public async Task<IReadOnlyList<WorkflowTransitionRecordDto>> GetTransitionsAsync(
        Guid renewalId,
        CancellationToken ct = default)
    {
        var transitions = await transitionRepo.ListByEntityAsync("Renewal", renewalId, ct);
        return transitions.Select(MapTransition).ToList();
    }

    public async Task<(RenewalDto? Dto, string? ErrorCode, string? ErrorDetail)> CreateAsync(
        RenewalCreateDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var policy = await referenceDataRepo.GetPolicyByIdAsync(dto.PolicyId, ct);
        if (policy is null)
            return (null, "not_found", null);

        if (!CanCreateRenewal(user, policy))
            return (null, "policy_denied", null);

        if (await renewalRepo.HasActiveRenewalForPolicyAsync(dto.PolicyId, ct))
            return (null, "duplicate_renewal", null);
        if (IsTerminalAccountState(policy.Account.Status))
            return (null, "policy_denied", "Renewals cannot be created from merged or deleted accounts.");

        var assigneeId = dto.AssignedToUserId ?? user.UserId;
        var assignee = await ResolveAssigneeAsync(assigneeId, user, ct);
        if (assignee.ErrorCode is not null)
            return (null, assignee.ErrorCode, assignee.ErrorDetail);

        var assigneeProfile = assignee.Profile
            ?? throw new InvalidOperationException("Resolved assignee profile was unexpectedly null.");

        if (!CanOwnRenewalStage(assigneeProfile, "Identified"))
        {
            return (null, "invalid_assignee_role", "Target user does not have the required role for this renewal stage.");
        }

        var lineOfBusiness = dto.LineOfBusiness ?? policy.LineOfBusiness;
        var identifiedThreshold = await workflowSlaThresholdRepo.GetThresholdAsync("renewal", "Identified", lineOfBusiness, ct);
        var targetDays = identifiedThreshold?.TargetDays ?? 90;
        var now = DateTime.UtcNow;

        var renewal = new Renewal
        {
            AccountId = policy.AccountId,
            BrokerId = policy.BrokerId,
            PolicyId = policy.Id,
            CurrentStatus = "Identified",
            LineOfBusiness = lineOfBusiness,
            PolicyExpirationDate = policy.ExpirationDate,
            TargetOutreachDate = policy.ExpirationDate.AddDays(-targetDays),
            AssignedToUserId = assigneeProfile.Id,
            AccountDisplayNameAtLink = policy.Account.StableDisplayName,
            AccountStatusAtRead = policy.Account.Status,
            AccountSurvivorId = policy.Account.MergedIntoAccountId,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };

        await renewalRepo.AddAsync(renewal, ct);
        await transitionRepo.AddAsync(new WorkflowTransition
        {
            WorkflowType = "Renewal",
            EntityId = renewal.Id,
            FromState = null,
            ToState = "Identified",
            ActorUserId = user.UserId,
            OccurredAt = now,
        }, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Renewal",
            EntityId = renewal.Id,
            EventType = "RenewalCreated",
            EventDescription = $"Renewal created from policy {policy.PolicyNumber}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                policyId = policy.Id,
                policyNumber = policy.PolicyNumber,
                accountId = policy.AccountId,
                accountName = policy.Account.Name,
                brokerId = policy.BrokerId,
                brokerName = policy.Broker.LegalName,
                lineOfBusiness,
                assignedToUserId = assigneeProfile.Id,
                assignedToDisplayName = assigneeProfile.DisplayName,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        var created = await renewalRepo.GetByIdWithRelationsAsync(renewal.Id, ct)
            ?? throw new InvalidOperationException("Created renewal could not be reloaded.");

        return (await MapDetailAsync(created, user, ct), null, null);
    }

    public async Task<(WorkflowTransitionRecordDto? Dto, string? ErrorCode, IReadOnlyList<string>? MissingItems)> TransitionAsync(
        Guid renewalId,
        RenewalTransitionRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var renewal = await renewalRepo.GetByIdWithRelationsAsync(renewalId, ct);
        if (renewal is null || !CanReadRenewal(user, renewal))
            return (null, "not_found", null);

        if (renewal.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        var error = WorkflowStateMachine.ValidateRenewalTransition(renewal.CurrentStatus, dto.ToState, user.Roles);
        if (error is not null)
            return (null, error, null);

        if (string.Equals(dto.ToState, "Lost", StringComparison.Ordinal))
        {
            var missingItems = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.ReasonCode))
                missingItems.Add("reasonCode is required when transitioning to Lost");
            if (string.Equals(dto.ReasonCode, "Other", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(dto.ReasonDetail))
                missingItems.Add("reasonDetail is required when reasonCode is Other");

            if (missingItems.Count > 0)
                return (null, "missing_transition_prerequisite", missingItems);

            renewal.LostReasonCode = dto.ReasonCode;
            renewal.LostReasonDetail = dto.ReasonDetail;
            renewal.BoundPolicyId = null;
            renewal.RenewalSubmissionId = null;
        }
        else if (string.Equals(dto.ToState, "Completed", StringComparison.Ordinal))
        {
            if (!dto.BoundPolicyId.HasValue && !dto.RenewalSubmissionId.HasValue)
            {
                return (null, "missing_transition_prerequisite", ["boundPolicyId or renewalSubmissionId is required when transitioning to Completed"]);
            }

            renewal.BoundPolicyId = dto.BoundPolicyId;
            renewal.RenewalSubmissionId = dto.RenewalSubmissionId;
        }
        else
        {
            renewal.LostReasonCode = null;
            renewal.LostReasonDetail = null;
            if (!string.Equals(dto.ToState, "Completed", StringComparison.Ordinal))
            {
                renewal.BoundPolicyId = null;
                renewal.RenewalSubmissionId = null;
            }
        }

        var now = DateTime.UtcNow;
        var fromState = renewal.CurrentStatus;
        renewal.CurrentStatus = dto.ToState;
        renewal.UpdatedAt = now;
        renewal.UpdatedByUserId = user.UserId;
        renewal.RowVersion = expectedRowVersion;

        var transition = new WorkflowTransition
        {
            WorkflowType = "Renewal",
            EntityId = renewal.Id,
            FromState = fromState,
            ToState = dto.ToState,
            Reason = dto.Reason ?? dto.ReasonCode,
            ActorUserId = user.UserId,
            OccurredAt = now,
        };

        await transitionRepo.AddAsync(transition, ct);
        await renewalRepo.UpdateAsync(renewal, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Renewal",
            EntityId = renewal.Id,
            EventType = "RenewalTransitioned",
            EventDescription = $"Renewal transitioned from {fromState} to {dto.ToState}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                fromState,
                toState = dto.ToState,
                reason = dto.Reason,
                reasonCode = dto.ReasonCode,
                reasonDetail = dto.ReasonDetail,
                boundPolicyId = dto.BoundPolicyId,
                renewalSubmissionId = dto.RenewalSubmissionId,
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

        return (MapTransition(transition), null, null);
    }

    public async Task<(RenewalDto? Dto, string? ErrorCode, string? ErrorDetail)> AssignAsync(
        Guid renewalId,
        RenewalAssignmentRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var renewal = await renewalRepo.GetByIdWithRelationsAsync(renewalId, ct);
        if (renewal is null || !CanReadRenewal(user, renewal))
            return (null, "not_found", null);

        if (renewal.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (WorkflowStateMachine.IsTerminalState("Renewal", renewal.CurrentStatus))
            return (null, "assignment_not_allowed_in_terminal_state", null);

        var assignee = await ResolveAssigneeAsync(dto.AssignedToUserId, user, ct);
        if (assignee.ErrorCode is not null)
            return (null, assignee.ErrorCode, assignee.ErrorDetail);

        var assigneeProfile = assignee.Profile
            ?? throw new InvalidOperationException("Resolved assignee profile was unexpectedly null.");

        if (!CanOwnRenewalStage(assigneeProfile, renewal.CurrentStatus))
        {
            return (null, "invalid_assignee_role", "Target user does not have the required role for this renewal stage.");
        }

        if (renewal.AssignedToUserId == assigneeProfile.Id)
            return (await MapDetailAsync(renewal, user, ct), null, null);

        var previousAssignee = await userProfileRepo.GetByIdAsync(renewal.AssignedToUserId, ct);
        var now = DateTime.UtcNow;
        renewal.AssignedToUserId = assigneeProfile.Id;
        renewal.UpdatedAt = now;
        renewal.UpdatedByUserId = user.UserId;
        renewal.RowVersion = expectedRowVersion;

        await renewalRepo.UpdateAsync(renewal, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Renewal",
            EntityId = renewal.Id,
            EventType = "RenewalAssigned",
            EventDescription = $"Renewal reassigned from {previousAssignee?.DisplayName ?? "Unknown User"} to {assigneeProfile.DisplayName}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                previousAssigneeUserId = previousAssignee?.Id,
                previousAssigneeName = previousAssignee?.DisplayName,
                newAssigneeUserId = assigneeProfile.Id,
                newAssigneeName = assigneeProfile.DisplayName,
                assignedByUserId = user.UserId,
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

        var updated = await renewalRepo.GetByIdWithRelationsAsync(renewal.Id, ct)
            ?? throw new InvalidOperationException("Updated renewal could not be reloaded.");

        return (await MapDetailAsync(updated, user, ct), null, null);
    }

    private async Task<RenewalListItemDto> MapListItemAsync(Renewal renewal, CancellationToken ct)
    {
        var urgency = await ComputeUrgencyAsync(renewal.CurrentStatus, renewal.PolicyExpirationDate, renewal.LineOfBusiness, ct);
        var fallback = BuildAccountFallback(renewal);

        return new RenewalListItemDto(
            renewal.Id,
            renewal.AccountId,
            fallback.DisplayName,
            fallback.Status,
            fallback.SurvivorAccountId,
            fallback.DisplayName,
            renewal.Account.Industry ?? string.Empty,
            renewal.Account.PrimaryState,
            renewal.Broker.LegalName,
            renewal.Broker.LicenseNumber ?? string.Empty,
            renewal.Broker.State ?? string.Empty,
            renewal.Policy.PolicyNumber,
            renewal.Policy.Carrier,
            renewal.LineOfBusiness,
            renewal.CurrentStatus,
            renewal.PolicyExpirationDate,
            renewal.TargetOutreachDate,
            renewal.AssignedToUserId,
            renewal.AssignedToUser.DisplayName,
            urgency,
            renewal.RowVersion.ToString());
    }

    private async Task<RenewalDto> MapDetailAsync(Renewal renewal, ICurrentUserService user, CancellationToken ct)
    {
        var urgency = await ComputeUrgencyAsync(renewal.CurrentStatus, renewal.PolicyExpirationDate, renewal.LineOfBusiness, ct);
        var fallback = BuildAccountFallback(renewal);

        return new RenewalDto(
            renewal.Id,
            renewal.AccountId,
            renewal.BrokerId,
            renewal.PolicyId,
            renewal.CurrentStatus,
            renewal.LineOfBusiness,
            renewal.PolicyExpirationDate,
            renewal.TargetOutreachDate,
            renewal.AssignedToUserId,
            renewal.LostReasonCode,
            renewal.LostReasonDetail,
            renewal.BoundPolicyId,
            renewal.RenewalSubmissionId,
            urgency,
            WorkflowStateMachine.GetAvailableRenewalTransitions(renewal.CurrentStatus, user.Roles),
            renewal.AssignedToUser.DisplayName,
            fallback.DisplayName,
            fallback.Status,
            fallback.SurvivorAccountId,
            fallback.DisplayName,
            renewal.Account.Industry,
            renewal.Account.PrimaryState,
            renewal.Broker.LegalName,
            renewal.Broker.LicenseNumber,
            renewal.Broker.State,
            renewal.Policy.PolicyNumber,
            renewal.Policy.Carrier,
            renewal.Policy.EffectiveDate,
            renewal.Policy.Premium,
            renewal.RowVersion.ToString(),
            renewal.CreatedAt,
            renewal.CreatedByUserId,
            renewal.UpdatedAt,
            renewal.UpdatedByUserId);
    }

    private static (string DisplayName, string Status, Guid? SurvivorAccountId) BuildAccountFallback(Renewal renewal)
    {
        var displayName = string.IsNullOrWhiteSpace(renewal.AccountDisplayNameAtLink)
            ? renewal.Account.StableDisplayName
            : renewal.AccountDisplayNameAtLink;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = renewal.Account.Name;

        var status = string.IsNullOrWhiteSpace(renewal.AccountStatusAtRead)
            ? renewal.Account.Status
            : renewal.AccountStatusAtRead;

        var survivorAccountId = renewal.AccountSurvivorId ?? renewal.Account.MergedIntoAccountId;
        return (displayName, status, survivorAccountId);
    }

    private async Task<string?> ComputeUrgencyAsync(
        string currentStatus,
        DateTime policyExpirationDate,
        string? lineOfBusiness,
        CancellationToken ct)
    {
        if (!string.Equals(currentStatus, "Identified", StringComparison.Ordinal))
            return null;

        var threshold = await workflowSlaThresholdRepo.GetThresholdAsync("renewal", "Identified", lineOfBusiness, ct);
        var warningDays = threshold?.WarningDays ?? 60;
        var targetDays = threshold?.TargetDays ?? 90;
        var today = DateTime.UtcNow.Date;

        var overdueDate = policyExpirationDate.Date.AddDays(-targetDays);
        var approachingDate = policyExpirationDate.Date.AddDays(-(targetDays + warningDays));

        if (today > overdueDate)
            return "overdue";

        return today > approachingDate ? "approaching" : null;
    }

    private async Task<(UserProfile? Profile, string? ErrorCode, string? ErrorDetail)> ResolveAssigneeAsync(
        Guid assigneeId,
        ICurrentUserService user,
        CancellationToken ct)
    {
        if (assigneeId == user.UserId)
        {
            return (new UserProfile
            {
                Id = user.UserId,
                DisplayName = user.DisplayName ?? "Current User",
                Email = string.Empty,
                Department = string.Empty,
                IdpIssuer = string.Empty,
                IdpSubject = string.Empty,
                RolesJson = JsonSerializer.Serialize(user.Roles),
                RegionsJson = JsonSerializer.Serialize(user.Regions),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, null, null);
        }

        var assignee = await userProfileRepo.GetByIdAsync(assigneeId, ct);
        if (assignee is null)
            return (null, "invalid_assignee", null);

        if (!assignee.IsActive)
            return (null, "inactive_assignee", null);

        return (assignee, null, null);
    }

    private static WorkflowTransitionRecordDto MapTransition(WorkflowTransition transition) => new(
        transition.Id,
        transition.WorkflowType,
        transition.EntityId,
        transition.FromState,
        transition.ToState,
        transition.Reason,
        transition.OccurredAt);

    private static bool CanReadRenewal(ICurrentUserService user, Renewal renewal)
    {
        if (HasRole(user, "Admin"))
            return true;

        if ((HasRole(user, "DistributionUser") || HasRole(user, "Underwriter")) && renewal.AssignedToUserId == user.UserId)
            return true;

        if (HasRole(user, "DistributionManager")
            && NormalizeRegions(user.Regions).Contains(renewal.Account.Region, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (HasRole(user, "RelationshipManager") && renewal.Broker.ManagedByUserId == user.UserId)
            return true;

        return HasRole(user, "ProgramManager");
    }

    private static bool CanCreateRenewal(ICurrentUserService user, Policy policy)
    {
        if (HasRole(user, "Admin"))
            return true;

        if (HasRole(user, "DistributionManager"))
            return NormalizeRegions(user.Regions).Contains(policy.Account.Region, StringComparer.OrdinalIgnoreCase);

        return HasRole(user, "DistributionUser")
            && NormalizeRegions(user.Regions).Contains(policy.Account.Region, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsTerminalAccountState(string status) =>
        string.Equals(status, AccountStatuses.Merged, StringComparison.Ordinal)
        || string.Equals(status, AccountStatuses.Deleted, StringComparison.Ordinal);

    private static bool CanOwnRenewalStage(UserProfile assignee, string currentStatus)
    {
        return currentStatus switch
        {
            "Identified" or "Outreach" =>
                HasUserProfileRole(assignee, "DistributionUser")
                || HasUserProfileRole(assignee, "DistributionManager")
                || HasUserProfileRole(assignee, "Admin"),
            "InReview" or "Quoted" =>
                HasUserProfileRole(assignee, "Underwriter")
                || HasUserProfileRole(assignee, "Admin"),
            _ => false,
        };
    }

    private static bool HasRole(ICurrentUserService user, string role) =>
        user.Roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static bool HasUserProfileRole(UserProfile profile, string role)
    {
        if (string.IsNullOrWhiteSpace(profile.RolesJson))
            return false;

        var roles = JsonSerializer.Deserialize<string[]>(profile.RolesJson) ?? [];
        return roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
