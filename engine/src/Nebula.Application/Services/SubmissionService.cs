using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;

namespace Nebula.Application.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IBrokerRepository brokerRepo,
    IReferenceDataRepository referenceDataRepo,
    IUserProfileRepository userProfileRepo,
    ISubmissionDocumentChecklistReader submissionDocumentChecklistReader,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<SubmissionListItemDto>> ListAsync(
        SubmissionListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var result = await submissionRepo.ListAsync(query, user, ct);
        var staleFlags = await submissionRepo.GetStaleFlagsAsync(result.Data.Select(submission => submission.Id).ToArray(), ct);

        var mapped = result.Data
            .Select(submission =>
            {
                var fallback = BuildAccountFallback(submission);
                return new SubmissionListItemDto(
                    submission.Id,
                    submission.AccountId,
                    fallback.DisplayName,
                    fallback.Status,
                    fallback.SurvivorAccountId,
                    fallback.DisplayName,
                    submission.Broker.LegalName,
                    submission.LineOfBusiness,
                    submission.CurrentStatus,
                    submission.EffectiveDate,
                    submission.AssignedToUserId,
                    submission.AssignedToUser.DisplayName,
                    submission.CreatedAt,
                    staleFlags.GetValueOrDefault(submission.Id));
            })
            .ToList();

        return new PaginatedResult<SubmissionListItemDto>(mapped, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<SubmissionDto?> GetByIdAsync(
        Guid id,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(id, ct);
        if (submission is null || !CanReadSubmission(user, submission))
            return null;

        return await MapToDtoAsync(submission, user, ct);
    }

    public async Task<bool> ExistsAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(id, ct);
        return submission is not null && CanReadSubmission(user, submission);
    }

    public async Task<IReadOnlyList<WorkflowTransitionRecordDto>> GetTransitionsAsync(
        Guid submissionId,
        CancellationToken ct = default)
    {
        var transitions = await transitionRepo.ListByEntityAsync("Submission", submissionId, ct);
        return transitions.Select(MapTransition).ToList();
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode)> CreateAsync(
        SubmissionCreateDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var account = await referenceDataRepo.GetAccountByIdAsync(dto.AccountId, ct);
        if (account is null)
            return (null, "invalid_account");
        if (IsTerminalAccountState(account.Status))
            return (null, "invalid_account");

        var broker = await brokerRepo.GetByIdAsync(dto.BrokerId, ct);
        if (broker is null || !string.Equals(broker.Status, "Active", StringComparison.Ordinal))
            return (null, "invalid_broker");

        if (!IsBrokerRegionAligned(account.Region, broker.BrokerRegions))
            return (null, "region_mismatch");

        if (dto.ProgramId.HasValue)
        {
            var program = await referenceDataRepo.GetProgramByIdAsync(dto.ProgramId.Value, ct);
            if (program is null)
                return (null, "invalid_program");
        }

        if (!LineOfBusinessCatalog.IsValid(dto.LineOfBusiness))
            return (null, "invalid_lob");

        var now = DateTime.UtcNow;
        var submission = new Submission
        {
            AccountId = dto.AccountId,
            BrokerId = dto.BrokerId,
            ProgramId = dto.ProgramId,
            LineOfBusiness = dto.LineOfBusiness,
            CurrentStatus = "Received",
            EffectiveDate = dto.EffectiveDate,
            ExpirationDate = dto.ExpirationDate ?? dto.EffectiveDate.AddMonths(12),
            PremiumEstimate = dto.PremiumEstimate,
            Description = dto.Description,
            AssignedToUserId = user.UserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };

        await submissionRepo.AddAsync(submission, ct);
        await transitionRepo.AddAsync(new WorkflowTransition
        {
            WorkflowType = "Submission",
            EntityId = submission.Id,
            FromState = null,
            ToState = "Received",
            ActorUserId = user.UserId,
            OccurredAt = now,
        }, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submission.Id,
            EventType = "SubmissionCreated",
            EventDescription = $"Submission created for {account.Name} via {broker.LegalName}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                submissionId = submission.Id,
                accountId = account.Id,
                accountName = account.Name,
                brokerId = broker.Id,
                brokerName = broker.LegalName,
                currentStatus = submission.CurrentStatus,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        var created = await submissionRepo.GetByIdWithIncludesAsync(submission.Id, ct)
            ?? throw new InvalidOperationException("Created submission could not be reloaded.");

        return (await MapToDtoAsync(created, user, ct), null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid id,
        SubmissionUpdateDto dto,
        IReadOnlySet<string> presentFields,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(id, ct);
        if (submission is null || !CanUpdateSubmission(user, submission))
            return (null, "not_found");

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (presentFields.Contains("programId") && dto.ProgramId.HasValue)
        {
            var program = await referenceDataRepo.GetProgramByIdAsync(dto.ProgramId.Value, ct);
            if (program is null)
                return (null, "invalid_program");
        }

        if (presentFields.Contains("lineOfBusiness")
            && dto.LineOfBusiness is not null
            && !LineOfBusinessCatalog.IsValid(dto.LineOfBusiness))
            return (null, "invalid_lob");

        var changedFields = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (presentFields.Contains("programId") && dto.ProgramId != submission.ProgramId)
        {
            TrackChange(changedFields, "programId", submission.ProgramId, dto.ProgramId);
            submission.ProgramId = dto.ProgramId;
        }

        if (presentFields.Contains("lineOfBusiness")
            && !string.Equals(dto.LineOfBusiness, submission.LineOfBusiness, StringComparison.Ordinal))
        {
            TrackChange(changedFields, "lineOfBusiness", submission.LineOfBusiness, dto.LineOfBusiness);
            submission.LineOfBusiness = dto.LineOfBusiness;
        }

        if (presentFields.Contains("effectiveDate")
            && dto.EffectiveDate.HasValue
            && dto.EffectiveDate.Value != submission.EffectiveDate)
        {
            TrackChange(changedFields, "effectiveDate", submission.EffectiveDate, dto.EffectiveDate.Value);
            submission.EffectiveDate = dto.EffectiveDate.Value;
        }

        if (presentFields.Contains("expirationDate") && dto.ExpirationDate != submission.ExpirationDate)
        {
            TrackChange(changedFields, "expirationDate", submission.ExpirationDate, dto.ExpirationDate);
            submission.ExpirationDate = dto.ExpirationDate;
        }

        if (presentFields.Contains("premiumEstimate") && dto.PremiumEstimate != submission.PremiumEstimate)
        {
            TrackChange(changedFields, "premiumEstimate", submission.PremiumEstimate, dto.PremiumEstimate);
            submission.PremiumEstimate = dto.PremiumEstimate;
        }

        if (presentFields.Contains("description")
            && !string.Equals(dto.Description, submission.Description, StringComparison.Ordinal))
        {
            TrackChange(changedFields, "description", submission.Description, dto.Description);
            submission.Description = dto.Description;
        }

        if (changedFields.Count == 0)
            return (await MapToDtoAsync(submission, user, ct), null);

        var now = DateTime.UtcNow;
        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;

        await submissionRepo.UpdateAsync(submission, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submission.Id,
            EventType = "SubmissionUpdated",
            EventDescription = "Submission updated",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                changedFields,
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

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submission.Id, ct)
            ?? throw new InvalidOperationException("Updated submission could not be reloaded.");

        return (await MapToDtoAsync(updated, user, ct), null);
    }

    public async Task<(WorkflowTransitionRecordDto? Dto, string? ErrorCode, IReadOnlyList<string>? MissingItems)> TransitionAsync(
        Guid submissionId,
        WorkflowTransitionRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanTransitionSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (!WorkflowStateMachine.IsValidTransition("Submission", submission.CurrentStatus, dto.ToState))
            return (null, "invalid_transition", null);

        if (!CanPerformTransition(user, submission.CurrentStatus, dto.ToState))
            return (null, "policy_denied", null);

        if (string.Equals(dto.ToState, "ReadyForUWReview", StringComparison.Ordinal))
        {
            var completeness = await EvaluateCompletenessAsync(submission, ct);
            if (!completeness.IsComplete)
                return (null, "missing_transition_prerequisite", completeness.MissingItems);
        }

        var now = DateTime.UtcNow;
        var transition = new WorkflowTransition
        {
            WorkflowType = "Submission",
            EntityId = submissionId,
            FromState = submission.CurrentStatus,
            ToState = dto.ToState,
            Reason = dto.Reason,
            ActorUserId = user.UserId,
            OccurredAt = now,
        };
        var transitionDescription = string.IsNullOrWhiteSpace(transition.Reason)
            ? $"Status changed from {transition.FromState} to {transition.ToState}"
            : $"Status changed from {transition.FromState} to {transition.ToState}. Note: {transition.Reason}";

        submission.CurrentStatus = dto.ToState;
        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;

        await transitionRepo.AddAsync(transition, ct);
        await submissionRepo.UpdateAsync(submission, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submissionId,
            EventType = "SubmissionTransitioned",
            EventDescription = transitionDescription,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                fromState = transition.FromState,
                toState = transition.ToState,
                reason = transition.Reason,
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

    public async Task<(SubmissionDto? Dto, string? ErrorCode, string? ErrorDetail)> AssignAsync(
        Guid id,
        SubmissionAssignmentRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(id, ct);
        if (submission is null || !CanAssignSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (dto.AssignedToUserId == submission.AssignedToUserId)
            return (await MapToDtoAsync(submission, user, ct), null, null);

        var targetUser = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId, ct);
        if (targetUser is null)
            return (null, "invalid_assignee", "The specified user does not exist.");

        if (!targetUser.IsActive)
            return (null, "invalid_assignee", "The specified user is inactive and cannot own submissions.");

        if (string.Equals(submission.CurrentStatus, "ReadyForUWReview", StringComparison.Ordinal)
            && !HasUserProfileRole(targetUser, "Underwriter"))
        {
            return (null, "invalid_assignee", "Ready for UW Review submissions must be assigned to an active underwriter.");
        }

        var previousAssigneeUserId = submission.AssignedToUserId;
        var previousAssigneeName = submission.AssignedToUser.DisplayName;
        var now = DateTime.UtcNow;

        submission.AssignedToUserId = dto.AssignedToUserId;
        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;

        await submissionRepo.UpdateAsync(submission, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submission.Id,
            EventType = "SubmissionAssigned",
            EventDescription = $"Reassigned from \"{previousAssigneeName}\" to \"{targetUser.DisplayName}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                previousAssigneeUserId,
                previousAssigneeName,
                newAssigneeUserId = targetUser.Id,
                newAssigneeName = targetUser.DisplayName,
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

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submission.Id, ct)
            ?? throw new InvalidOperationException("Assigned submission could not be reloaded.");

        return (await MapToDtoAsync(updated, user, ct), null, null);
    }

    public async Task<SubmissionCompletenessDto> EvaluateCompletenessAsync(
        Submission submission,
        CancellationToken ct = default)
    {
        var assignedUser = await userProfileRepo.GetByIdAsync(submission.AssignedToUserId, ct);
        var fieldChecks = new List<SubmissionFieldCheckDto>
        {
            new("AccountId", true, submission.AccountId != Guid.Empty ? "pass" : "missing"),
            new("BrokerId", true, submission.BrokerId != Guid.Empty ? "pass" : "missing"),
            new("EffectiveDate", true, submission.EffectiveDate != default ? "pass" : "missing"),
            new("LineOfBusiness", true, !string.IsNullOrWhiteSpace(submission.LineOfBusiness) ? "pass" : "missing"),
            new(
                "AssignedToUserId",
                true,
                assignedUser is not null && assignedUser.IsActive && HasUserProfileRole(assignedUser, "Underwriter")
                    ? "pass"
                    : "missing"),
        };

        var documentChecks = await submissionDocumentChecklistReader.GetChecklistAsync(submission.Id, ct);
        var missingItems = fieldChecks
            .Where(check => check.Required && string.Equals(check.Status, "missing", StringComparison.Ordinal))
            .Select(check => FieldDisplayName(check.Field))
            .Concat(documentChecks
                .Where(check => check.Required && string.Equals(check.Status, "missing", StringComparison.Ordinal))
                .Select(check => check.Category))
            .ToList();

        var isComplete = fieldChecks.All(check => !check.Required || string.Equals(check.Status, "pass", StringComparison.Ordinal))
            && documentChecks.All(check =>
                !check.Required
                || string.Equals(check.Status, "pass", StringComparison.Ordinal)
                || string.Equals(check.Status, "unavailable", StringComparison.Ordinal));

        return new SubmissionCompletenessDto(isComplete, fieldChecks, documentChecks, missingItems);
    }

    private async Task<SubmissionDto> MapToDtoAsync(
        Submission submission,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var staleFlags = await submissionRepo.GetStaleFlagsAsync([submission.Id], ct);
        var completeness = await EvaluateCompletenessAsync(submission, ct);
        var fallback = BuildAccountFallback(submission);
        var availableTransitions = WorkflowStateMachine.GetAvailableTransitions("Submission", submission.CurrentStatus)
            .Where(target => CanPerformTransition(user, submission.CurrentStatus, target))
            .ToList();

        return new SubmissionDto(
            submission.Id,
            submission.AccountId,
            submission.BrokerId,
            submission.ProgramId,
            submission.LineOfBusiness,
            submission.CurrentStatus,
            submission.EffectiveDate,
            submission.ExpirationDate,
            submission.PremiumEstimate,
            submission.Description,
            submission.AssignedToUserId,
            fallback.DisplayName,
            fallback.Status,
            fallback.SurvivorAccountId,
            fallback.DisplayName,
            submission.Account.Region,
            submission.Account.Industry,
            submission.Broker.LegalName,
            submission.Broker.LicenseNumber,
            submission.Program?.Name,
            submission.AssignedToUser.DisplayName,
            staleFlags.GetValueOrDefault(submission.Id),
            completeness,
            availableTransitions,
            submission.RowVersion.ToString(),
            submission.CreatedAt,
            submission.CreatedByUserId,
            submission.UpdatedAt,
            submission.UpdatedByUserId);
    }

    private static (string DisplayName, string Status, Guid? SurvivorAccountId) BuildAccountFallback(Submission submission)
    {
        var displayName = string.IsNullOrWhiteSpace(submission.AccountDisplayNameAtLink)
            ? submission.Account.StableDisplayName
            : submission.AccountDisplayNameAtLink;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = submission.Account.Name;

        var status = string.IsNullOrWhiteSpace(submission.AccountStatusAtRead)
            ? submission.Account.Status
            : submission.AccountStatusAtRead;

        var survivorAccountId = submission.AccountSurvivorId ?? submission.Account.MergedIntoAccountId;
        return (displayName, status, survivorAccountId);
    }

    private static WorkflowTransitionRecordDto MapTransition(WorkflowTransition transition) => new(
        transition.Id,
        transition.WorkflowType,
        transition.EntityId,
        transition.FromState,
        transition.ToState,
        transition.Reason,
        transition.OccurredAt);

    private static void TrackChange(
        IDictionary<string, object?> changedFields,
        string field,
        object? before,
        object? after) =>
        changedFields[field] = new { before, after };

    private static bool CanReadSubmission(ICurrentUserService user, Submission submission)
    {
        if (HasRole(user, "Admin"))
            return true;

        if ((HasRole(user, "DistributionUser") || HasRole(user, "Underwriter"))
            && submission.AssignedToUserId == user.UserId)
        {
            return true;
        }

        if (HasRole(user, "DistributionManager")
            && NormalizeRegions(user.Regions).Contains(submission.Account.Region, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (HasRole(user, "RelationshipManager") && submission.Broker.ManagedByUserId == user.UserId)
            return true;

        return HasRole(user, "ProgramManager") && submission.Program?.ManagedByUserId == user.UserId;
    }

    private static bool CanUpdateSubmission(ICurrentUserService user, Submission submission) =>
        CanReadSubmission(user, submission)
        && (HasRole(user, "Admin")
            || HasRole(user, "DistributionManager")
            || HasRole(user, "DistributionUser"));

    private static bool IsTerminalAccountState(string status) =>
        string.Equals(status, AccountStatuses.Merged, StringComparison.Ordinal)
        || string.Equals(status, AccountStatuses.Deleted, StringComparison.Ordinal);

    private static bool CanTransitionSubmission(ICurrentUserService user, Submission submission) =>
        CanReadSubmission(user, submission)
        && (HasRole(user, "Admin")
            || HasRole(user, "DistributionManager")
            || HasRole(user, "DistributionUser")
            || HasRole(user, "Underwriter"));

    private static bool CanAssignSubmission(ICurrentUserService user, Submission submission)
    {
        if (HasRole(user, "Admin"))
            return true;

        if (HasRole(user, "DistributionManager")
            && NormalizeRegions(user.Regions).Contains(submission.Account.Region, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return HasRole(user, "DistributionUser") && submission.AssignedToUserId == user.UserId;
    }

    private static bool CanPerformTransition(ICurrentUserService user, string fromState, string toState)
    {
        if (HasRole(user, "Admin"))
            return true;

        var intakeTransition =
            (string.Equals(fromState, "Received", StringComparison.Ordinal) && string.Equals(toState, "Triaging", StringComparison.Ordinal))
            || (string.Equals(fromState, "Triaging", StringComparison.Ordinal)
                && (string.Equals(toState, "WaitingOnBroker", StringComparison.Ordinal)
                    || string.Equals(toState, "ReadyForUWReview", StringComparison.Ordinal)))
            || (string.Equals(fromState, "WaitingOnBroker", StringComparison.Ordinal)
                && string.Equals(toState, "ReadyForUWReview", StringComparison.Ordinal));

        return intakeTransition
            ? HasRole(user, "DistributionUser") || HasRole(user, "DistributionManager")
            : HasRole(user, "Underwriter");
    }

    private static bool HasRole(ICurrentUserService user, string role) =>
        user.Roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static bool HasUserProfileRole(UserProfile userProfile, string role)
    {
        if (string.IsNullOrWhiteSpace(userProfile.RolesJson))
            return false;

        var roles = JsonSerializer.Deserialize<string[]>(userProfile.RolesJson) ?? [];
        return roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBrokerRegionAligned(string? accountRegion, IEnumerable<BrokerRegion> brokerRegions)
    {
        if (string.IsNullOrWhiteSpace(accountRegion))
            return false;

        return brokerRegions.Any(region =>
            string.Equals(region.Region, accountRegion, StringComparison.OrdinalIgnoreCase));
    }

    private static string FieldDisplayName(string field) => field switch
    {
        "AccountId" => "Account",
        "BrokerId" => "Broker",
        "EffectiveDate" => "Effective date",
        "LineOfBusiness" => "Line of business",
        "AssignedToUserId" => "Assigned underwriter",
        _ => field,
    };

    private static string[] NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
