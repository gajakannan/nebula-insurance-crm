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
    ISubmissionQuotePacketRepository quotePacketRepo,
    ISubmissionApprovalDecisionRepository approvalDecisionRepo,
    ISubmissionBindHandoffRepository bindHandoffRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IBrokerRepository brokerRepo,
    IReferenceDataRepository referenceDataRepo,
    IUserProfileRepository userProfileRepo,
    ISubmissionDocumentChecklistReader submissionDocumentChecklistReader,
    LobAttributeService lobAttributeService,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<SubmissionListItemDto>> ListAsync(
        SubmissionListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var result = await submissionRepo.ListAsync(query, user, ct);
        var submissionIds = result.Data.Select(submission => submission.Id).ToArray();
        var staleFlags = await submissionRepo.GetStaleFlagsAsync(submissionIds, ct);
        var ageDays = await submissionRepo.GetAgeDaysInStateAsync(submissionIds, ct);

        var mapped = result.Data
            .Select(submission =>
            {
                var fallback = BuildAccountFallback(submission);
                var isStale = staleFlags.GetValueOrDefault(submission.Id);
                var approvalStatus = ResolveApprovalStatus(submission);
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
                    isStale,
                    ageDays.GetValueOrDefault(submission.Id),
                    approvalStatus,
                    string.Equals(approvalStatus, "Pending", StringComparison.Ordinal),
                    submission.IsArchived,
                    isStale);
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

    public async Task<(SubmissionQuotePacketDto? Dto, string? ErrorCode)> GetQuotePacketAsync(
        Guid submissionId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanReadSubmission(user, submission))
            return (null, "not_found");

        return (MapQuotePacket(submission.QuotePackets.OrderByDescending(packet => packet.UpdatedAt).FirstOrDefault(), submission.Id), null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode, IReadOnlyList<string>? MissingItems)> UpdateQuotePacketAsync(
        Guid submissionId,
        SubmissionQuotePacketUpdateDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanManageDownstreamSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (submission.IsArchived)
            return (null, "archived_submission", null);

        if (!IsAnyStatus(submission.CurrentStatus, "InReview", "Quoted"))
            return (null, "invalid_transition", null);

        var now = DateTime.UtcNow;
        var packet = await quotePacketRepo.GetBySubmissionIdAsync(submissionId, ct);
        var isNewPacket = packet is null;
        packet ??= new SubmissionQuotePacket
        {
            SubmissionId = submissionId,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
        };

        if (dto.LinkedDocumentRefs is not null)
            packet.LinkedDocumentRefsJson = JsonSerializer.Serialize(dto.LinkedDocumentRefs);

        packet.RecordedPremiumAmount = dto.RecordedPremiumAmount;
        packet.RecordedLimits = dto.RecordedLimits;
        packet.RecordedDeductibles = dto.RecordedDeductibles;
        packet.EffectiveDate = dto.EffectiveDate;
        packet.CarrierMarket = dto.CarrierMarket;
        packet.UpdatedAt = now;
        packet.UpdatedByUserId = user.UserId;

        if (dto.MarkReady)
        {
            var missingItems = GetQuotePacketMissingItems(packet);
            if (missingItems.Count > 0)
                return (null, "missing_transition_prerequisite", missingItems);

            packet.Status = "ReadyForApproval";
            packet.ReadinessState = "ReadyForApproval";
            packet.ReadyAt ??= now;
            packet.ReadyByUserId ??= user.UserId;
        }

        if (isNewPacket)
            await quotePacketRepo.AddAsync(packet, ct);
        else
            await quotePacketRepo.UpdateAsync(packet, ct);

        if (submission.QuotePackets.All(existing => existing.Id != packet.Id))
            submission.QuotePackets.Add(packet);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submissionId,
            EventType = "SubmissionPacketUpdated",
            EventDescription = dto.MarkReady
                ? "Quote/proposal packet marked ready for approval"
                : "Quote/proposal packet updated",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                packetStatus = packet.Status,
                packetReadiness = packet.ReadinessState,
                linkedDocumentRefs = DeserializeGuidList(packet.LinkedDocumentRefsJson),
                recordedPremiumAmount = packet.RecordedPremiumAmount,
                packet.RecordedLimits,
                packet.RecordedDeductibles,
                packet.EffectiveDate,
                packet.CarrierMarket,
            }),
        }, ct);

        if (dto.MarkReady && string.Equals(submission.CurrentStatus, "InReview", StringComparison.Ordinal))
            await AddSubmissionTransitionAsync(submission, "Quoted", "Quote/proposal packet ready for approval", user, now, ct);

        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;
        await submissionRepo.UpdateAsync(submission, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed", null);
        }

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct)
            ?? throw new InvalidOperationException("Updated submission could not be reloaded.");
        return (await MapToDtoAsync(updated, user, ct), null, null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode, IReadOnlyList<string>? MissingItems)> ApproveSubmissionAsync(
        Guid submissionId,
        SubmissionApprovalRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanApproveSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (submission.IsArchived)
            return (null, "archived_submission", null);

        if (!string.Equals(submission.CurrentStatus, "Quoted", StringComparison.Ordinal))
            return (null, "invalid_transition", null);

        var decision = NormalizeApprovalDecision(dto.Decision);
        if (decision is null)
            return (null, "invalid_approval_decision", null);

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, "missing_reason", null);

        var packet = await quotePacketRepo.GetBySubmissionIdAsync(submissionId, ct);
        if (packet is null || !IsPacketReadyForApproval(packet))
            return (null, "missing_transition_prerequisite", ["Ready quote/proposal packet"]);

        if (string.Equals(decision, "Granted", StringComparison.Ordinal)
            && await approvalDecisionRepo.GetLatestGrantedAsync(submissionId, ct) is not null)
        {
            return (null, "duplicate_approval", null);
        }

        var now = DateTime.UtcNow;
        var approval = new SubmissionApprovalDecision
        {
            SubmissionId = submissionId,
            Decision = decision,
            ApproverUserId = user.UserId,
            Reason = dto.Reason.Trim(),
            AuthorityContextJson = JsonSerializer.Serialize(new
            {
                user.UserId,
                user.DisplayName,
                user.Roles,
            }),
            BlockingConditionsJson = JsonSerializer.Serialize(dto.BlockingConditions ?? []),
            DecidedAt = now,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };

        if (string.Equals(decision, "Granted", StringComparison.Ordinal))
        {
            packet.Status = "Approved";
            packet.ReadinessState = "Approved";
            packet.ApprovedAt = now;
            packet.ApprovedByUserId = user.UserId;
            packet.UpdatedAt = now;
            packet.UpdatedByUserId = user.UserId;
            await quotePacketRepo.UpdateAsync(packet, ct);
        }

        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;

        await approvalDecisionRepo.AddAsync(approval, ct);
        await submissionRepo.UpdateAsync(submission, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submissionId,
            EventType = string.Equals(decision, "Granted", StringComparison.Ordinal)
                ? "SubmissionApprovalGranted"
                : "SubmissionApprovalDeclined",
            EventDescription = string.Equals(decision, "Granted", StringComparison.Ordinal)
                ? "Underwriting approval granted"
                : "Underwriting approval declined",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                decision,
                approval.Reason,
                blockingConditions = dto.BlockingConditions ?? [],
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

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct)
            ?? throw new InvalidOperationException("Approved submission could not be reloaded.");
        return (await MapToDtoAsync(updated, user, ct), null, null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode, IReadOnlyList<string>? MissingItems)> RequestBindAsync(
        Guid submissionId,
        SubmissionBindRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanManageDownstreamSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (submission.IsArchived)
            return (null, "archived_submission", null);

        if (!string.Equals(submission.CurrentStatus, "Quoted", StringComparison.Ordinal))
            return (null, "invalid_transition", null);

        var packet = await quotePacketRepo.GetBySubmissionIdAsync(submissionId, ct);
        if (packet is null || !IsPacketApproved(packet))
            return (null, "missing_transition_prerequisite", ["Approved quote/proposal packet"]);

        var granted = await approvalDecisionRepo.GetLatestGrantedAsync(submissionId, ct);
        if (granted is null)
            return (null, "missing_transition_prerequisite", ["Granted underwriting approval"]);

        var idempotencyKey = NormalizeIdempotencyKey(dto.IdempotencyKey) ?? $"submission-bind:{submissionId:N}";
        var existing = await bindHandoffRepo.GetByIdempotencyKeyAsync(submissionId, idempotencyKey, ct);
        if (existing is not null)
            return (await MapToDtoAsync(submission, user, ct), null, null);

        var now = DateTime.UtcNow;
        var handoff = new SubmissionBindHandoff
        {
            SubmissionId = submissionId,
            IdempotencyKey = idempotencyKey,
            Status = "Pending",
            CorrelationId = Guid.NewGuid(),
            PayloadSnapshotJson = JsonSerializer.Serialize(new
            {
                submission.Id,
                submission.AccountId,
                submission.BrokerId,
                submission.LineOfBusiness,
                packet.RecordedPremiumAmount,
                packet.RecordedLimits,
                packet.RecordedDeductibles,
                packet.EffectiveDate,
                packet.CarrierMarket,
                linkedDocumentRefs = DeserializeGuidList(packet.LinkedDocumentRefsJson),
                approvalDecisionId = granted.Id,
            }),
            RequestedAt = now,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };

        await bindHandoffRepo.AddAsync(handoff, ct);
        await AddSubmissionTransitionAsync(submission, "BindRequested", "Approved submission sent for bind handoff", user, now, ct);

        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;
        await submissionRepo.UpdateAsync(submission, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed", null);
        }

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct)
            ?? throw new InvalidOperationException("Bind-requested submission could not be reloaded.");
        return (await MapToDtoAsync(updated, user, ct), null, null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode, IReadOnlyList<string>? MissingItems)> ConfirmBindAsync(
        Guid submissionId,
        SubmissionBindRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanManageDownstreamSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (submission.IsArchived)
            return (null, "archived_submission", null);

        if (!string.Equals(submission.CurrentStatus, "BindRequested", StringComparison.Ordinal))
            return (null, "invalid_transition", null);

        var idempotencyKey = NormalizeIdempotencyKey(dto.IdempotencyKey);
        var handoff = idempotencyKey is not null
            ? await bindHandoffRepo.GetByIdempotencyKeyAsync(submissionId, idempotencyKey, ct)
            : await bindHandoffRepo.GetLatestBySubmissionIdAsync(submissionId, ct);

        if (handoff is null)
            return (null, "missing_transition_prerequisite", ["Pending bind handoff"]);

        var now = DateTime.UtcNow;
        handoff.Status = "Completed";
        handoff.CompletedAt ??= now;
        handoff.UpdatedAt = now;
        handoff.UpdatedByUserId = user.UserId;

        await bindHandoffRepo.UpdateAsync(handoff, ct);
        await AddSubmissionTransitionAsync(submission, "Bound", "Bind handoff confirmed", user, now, ct);

        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;
        await submissionRepo.UpdateAsync(submission, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed", null);
        }

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct)
            ?? throw new InvalidOperationException("Bound submission could not be reloaded.");
        return (await MapToDtoAsync(updated, user, ct), null, null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode)> ArchiveAsync(
        Guid submissionId,
        SubmissionArchiveRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanArchiveSubmission(user, submission))
            return (null, "not_found");

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, "missing_reason");

        if (!IsAnyStatus(submission.CurrentStatus, "Bound", "Declined", "Withdrawn"))
            return (null, "invalid_transition");

        if (submission.IsArchived)
            return (await MapToDtoAsync(submission, user, ct), null);

        var now = DateTime.UtcNow;
        submission.IsArchived = true;
        submission.ArchivedAt = now;
        submission.ArchivedByUserId = user.UserId;
        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;

        await submissionRepo.UpdateAsync(submission, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submissionId,
            EventType = "SubmissionArchived",
            EventDescription = "Submission archived",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                reason = dto.Reason,
                archivedAt = now,
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

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct)
            ?? throw new InvalidOperationException("Archived submission could not be reloaded.");
        return (await MapToDtoAsync(updated, user, ct), null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode)> ReactivateAsync(
        Guid submissionId,
        SubmissionArchiveRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct);
        if (submission is null || !CanArchiveSubmission(user, submission))
            return (null, "not_found");

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, "missing_reason");

        if (!submission.IsArchived)
            return (await MapToDtoAsync(submission, user, ct), null);

        var now = DateTime.UtcNow;
        submission.IsArchived = false;
        submission.ArchivedAt = null;
        submission.ArchivedByUserId = null;
        submission.UpdatedAt = now;
        submission.UpdatedByUserId = user.UserId;
        submission.RowVersion = expectedRowVersion;

        await submissionRepo.UpdateAsync(submission, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submissionId,
            EventType = "SubmissionReactivated",
            EventDescription = "Archived submission reactivated",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                reason = dto.Reason,
                reactivatedAt = now,
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

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submissionId, ct)
            ?? throw new InvalidOperationException("Reactivated submission could not be reloaded.");
        return (await MapToDtoAsync(updated, user, ct), null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode, IReadOnlyList<LobValidationIssueDto>? LobErrors)> CreateAsync(
        SubmissionCreateDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var account = await referenceDataRepo.GetAccountByIdAsync(dto.AccountId, ct);
        if (account is null)
            return (null, "invalid_account", null);
        if (IsTerminalAccountState(account.Status))
            return (null, "invalid_account", null);

        var broker = await brokerRepo.GetByIdAsync(dto.BrokerId, ct);
        if (broker is null || !string.Equals(broker.Status, "Active", StringComparison.Ordinal))
            return (null, "invalid_broker", null);

        if (!IsBrokerRegionAligned(account.Region, broker.BrokerRegions))
            return (null, "region_mismatch", null);

        if (dto.ProgramId.HasValue)
        {
            var program = await referenceDataRepo.GetProgramByIdAsync(dto.ProgramId.Value, ct);
            if (program is null)
                return (null, "invalid_program", null);
        }

        if (!LineOfBusinessCatalog.IsValid(dto.LineOfBusiness))
            return (null, "invalid_lob", null);

        var lobAttributes = await lobAttributeService.ValidateAndSerializeAsync(dto.LobAttributes, dto.LineOfBusiness, ct);
        if (!lobAttributes.IsValid)
            return (null, "lob_validation_failed", lobAttributes.Errors);

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
            LobProductVersionId = lobAttributes.RequiredLobProductVersionId,
            LobAttributesJson = lobAttributes.RequiredAttributesJson,
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
                lobProductVersionId = submission.LobProductVersionId,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        var created = await submissionRepo.GetByIdWithIncludesAsync(submission.Id, ct)
            ?? throw new InvalidOperationException("Created submission could not be reloaded.");

        return (await MapToDtoAsync(created, user, ct), null, null);
    }

    public async Task<(SubmissionDto? Dto, string? ErrorCode, IReadOnlyList<LobValidationIssueDto>? LobErrors)> UpdateAsync(
        Guid id,
        SubmissionUpdateDto dto,
        IReadOnlySet<string> presentFields,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var submission = await submissionRepo.GetByIdWithIncludesAsync(id, ct);
        if (submission is null || !CanUpdateSubmission(user, submission))
            return (null, "not_found", null);

        if (submission.RowVersion != expectedRowVersion)
            return (null, "precondition_failed", null);

        if (submission.IsArchived)
            return (null, "archived_submission", null);

        if (presentFields.Contains("programId") && dto.ProgramId.HasValue)
        {
            var program = await referenceDataRepo.GetProgramByIdAsync(dto.ProgramId.Value, ct);
            if (program is null)
                return (null, "invalid_program", null);
        }

        if (presentFields.Contains("lineOfBusiness")
            && dto.LineOfBusiness is not null
            && !LineOfBusinessCatalog.IsValid(dto.LineOfBusiness))
            return (null, "invalid_lob", null);

        var targetLineOfBusiness = presentFields.Contains("lineOfBusiness")
            ? dto.LineOfBusiness
            : submission.LineOfBusiness;
        LobAttributeStorageResult? lobAttributes = null;
        if (presentFields.Contains("lobAttributes"))
        {
            lobAttributes = await lobAttributeService.ValidateAndSerializeAsync(dto.LobAttributes, targetLineOfBusiness, ct);
            if (!lobAttributes.IsValid)
                return (null, "lob_validation_failed", lobAttributes.Errors);
        }

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
            if (lobAttributes is null)
            {
                var defaultProductVersionId = LobSchemaDefaults.ResolveDefaultProductVersionId(dto.LineOfBusiness);
                if (submission.LobProductVersionId != defaultProductVersionId
                    || !string.Equals(submission.LobAttributesJson, LobSchemaDefaults.EmptyAttributesJson, StringComparison.Ordinal))
                {
                    TrackChange(changedFields, "lobAttributes", submission.LobAttributesJson, LobSchemaDefaults.EmptyAttributesJson);
                    submission.LobProductVersionId = defaultProductVersionId;
                    submission.LobAttributesJson = LobSchemaDefaults.EmptyAttributesJson;
                }
            }
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

        if (lobAttributes is not null
            && (!string.Equals(lobAttributes.RequiredAttributesJson, submission.LobAttributesJson, StringComparison.Ordinal)
                || lobAttributes.RequiredLobProductVersionId != submission.LobProductVersionId))
        {
            TrackChange(changedFields, "lobAttributes", submission.LobAttributesJson, lobAttributes.RequiredAttributesJson);
            submission.LobAttributesJson = lobAttributes.RequiredAttributesJson;
            submission.LobProductVersionId = lobAttributes.RequiredLobProductVersionId;
        }

        if (changedFields.Count == 0)
            return (await MapToDtoAsync(submission, user, ct), null, null);

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
            return (null, "precondition_failed", null);
        }

        var updated = await submissionRepo.GetByIdWithIncludesAsync(submission.Id, ct)
            ?? throw new InvalidOperationException("Updated submission could not be reloaded.");

        return (await MapToDtoAsync(updated, user, ct), null, null);
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

        if (submission.IsArchived)
            return (null, "archived_submission", null);

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

        if (string.Equals(dto.ToState, "Quoted", StringComparison.Ordinal))
        {
            var packet = await quotePacketRepo.GetBySubmissionIdAsync(submissionId, ct);
            if (packet is null || !IsPacketReadyForApproval(packet))
                return (null, "missing_transition_prerequisite", ["Ready quote/proposal packet"]);
        }

        if (string.Equals(dto.ToState, "BindRequested", StringComparison.Ordinal)
            || string.Equals(dto.ToState, "Bound", StringComparison.Ordinal))
        {
            return (null, "missing_transition_prerequisite", ["Use the bind handoff endpoint for bind transitions"]);
        }

        if (IsAnyStatus(dto.ToState, "Declined", "Withdrawn"))
        {
            if (string.IsNullOrWhiteSpace(dto.ReasonCode))
                return (null, "missing_transition_prerequisite", ["Reason code"]);

            if (string.Equals(dto.ReasonCode, "Other", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(dto.ReasonDetail))
            {
                return (null, "missing_transition_prerequisite", ["Reason detail"]);
            }
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

        if (submission.IsArchived)
            return (null, "archived_submission", null);

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
        var ageDays = await submissionRepo.GetAgeDaysInStateAsync([submission.Id], ct);
        var completeness = await EvaluateCompletenessAsync(submission, ct);
        var fallback = BuildAccountFallback(submission);
        var availableTransitions = WorkflowStateMachine.GetAvailableTransitions("Submission", submission.CurrentStatus)
            .Where(target => CanShowTransitionAction(user, submission, target))
            .ToList();
        var quotePacket = submission.QuotePackets
            .OrderByDescending(packet => packet.UpdatedAt)
            .FirstOrDefault();
        var approvalDecisions = submission.ApprovalDecisions
            .OrderByDescending(decision => decision.DecidedAt)
            .Select(MapApprovalDecision)
            .ToList();
        var bindHandoff = submission.BindHandoffs
            .OrderByDescending(handoff => handoff.RequestedAt)
            .Select(MapBindHandoff)
            .FirstOrDefault();
        var approvalStatus = ResolveApprovalStatus(submission);

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
            lobAttributeService.Deserialize(submission.LobAttributesJson),
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
            ageDays.GetValueOrDefault(submission.Id),
            approvalStatus,
            string.Equals(approvalStatus, "Pending", StringComparison.Ordinal),
            submission.IsArchived,
            submission.ArchivedAt,
            submission.ArchivedByUserId,
            completeness,
            MapQuotePacket(quotePacket, submission.Id),
            approvalDecisions,
            bindHandoff,
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

    private async Task<WorkflowTransition> AddSubmissionTransitionAsync(
        Submission submission,
        string toState,
        string? reason,
        ICurrentUserService user,
        DateTime occurredAt,
        CancellationToken ct)
    {
        var transition = new WorkflowTransition
        {
            WorkflowType = "Submission",
            EntityId = submission.Id,
            FromState = submission.CurrentStatus,
            ToState = toState,
            Reason = reason,
            ActorUserId = user.UserId,
            OccurredAt = occurredAt,
        };

        var transitionDescription = string.IsNullOrWhiteSpace(reason)
            ? $"Status changed from {transition.FromState} to {transition.ToState}"
            : $"Status changed from {transition.FromState} to {transition.ToState}. Note: {reason}";

        submission.CurrentStatus = toState;

        await transitionRepo.AddAsync(transition, ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Submission",
            EntityId = submission.Id,
            EventType = "SubmissionTransitioned",
            EventDescription = transitionDescription,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                fromState = transition.FromState,
                toState = transition.ToState,
                reason,
            }),
        }, ct);

        return transition;
    }

    private static SubmissionQuotePacketDto MapQuotePacket(SubmissionQuotePacket? packet, Guid submissionId) =>
        packet is null
            ? new SubmissionQuotePacketDto(
                null,
                submissionId,
                "Draft",
                [],
                null,
                null,
                null,
                null,
                null,
                "Draft",
                null,
                null,
                null,
                null,
                string.Empty)
            : new SubmissionQuotePacketDto(
                packet.Id,
                packet.SubmissionId,
                packet.Status,
                DeserializeGuidList(packet.LinkedDocumentRefsJson),
                packet.RecordedPremiumAmount,
                packet.RecordedLimits,
                packet.RecordedDeductibles,
                packet.EffectiveDate,
                packet.CarrierMarket,
                packet.ReadinessState,
                packet.ReadyAt,
                packet.ReadyByUserId,
                packet.ApprovedAt,
                packet.ApprovedByUserId,
                packet.RowVersion.ToString());

    private static SubmissionApprovalDecisionDto MapApprovalDecision(SubmissionApprovalDecision decision) => new(
        decision.Id,
        decision.SubmissionId,
        decision.Decision,
        decision.ApproverUserId,
        decision.Reason,
        DeserializeStringList(decision.BlockingConditionsJson),
        decision.DecidedAt);

    private static SubmissionBindHandoffDto MapBindHandoff(SubmissionBindHandoff handoff) => new(
        handoff.Id,
        handoff.SubmissionId,
        handoff.IdempotencyKey,
        handoff.Status,
        handoff.CorrelationId,
        handoff.RequestedAt,
        handoff.CompletedAt);

    private static IReadOnlyList<Guid> DeserializeGuidList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> GetQuotePacketMissingItems(SubmissionQuotePacket packet)
    {
        var missing = new List<string>();
        if (DeserializeGuidList(packet.LinkedDocumentRefsJson).Count == 0)
            missing.Add("Linked document refs");
        if (!packet.RecordedPremiumAmount.HasValue)
            missing.Add("Recorded premium amount");
        if (string.IsNullOrWhiteSpace(packet.RecordedLimits))
            missing.Add("Recorded limits");
        if (string.IsNullOrWhiteSpace(packet.RecordedDeductibles))
            missing.Add("Recorded deductibles");
        if (!packet.EffectiveDate.HasValue)
            missing.Add("Effective date");
        if (string.IsNullOrWhiteSpace(packet.CarrierMarket))
            missing.Add("Carrier market");
        return missing;
    }

    private static bool IsPacketReadyForApproval(SubmissionQuotePacket packet) =>
        IsAnyStatus(packet.ReadinessState, "ReadyForApproval", "Approved")
        || IsAnyStatus(packet.Status, "ReadyForApproval", "Approved");

    private static bool IsPacketApproved(SubmissionQuotePacket packet) =>
        string.Equals(packet.ReadinessState, "Approved", StringComparison.Ordinal)
        || string.Equals(packet.Status, "Approved", StringComparison.Ordinal);

    private static string ResolveApprovalStatus(Submission submission)
    {
        var latestDecision = submission.ApprovalDecisions
            .OrderByDescending(decision => decision.DecidedAt)
            .FirstOrDefault();
        if (latestDecision is not null)
            return latestDecision.Decision;

        var packet = submission.QuotePackets
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        if (packet is not null && IsPacketApproved(packet))
            return "Granted";

        if (string.Equals(submission.CurrentStatus, "Quoted", StringComparison.Ordinal)
            && packet is not null
            && IsPacketReadyForApproval(packet))
        {
            return "Pending";
        }

        return "NotRequired";
    }

    private static string? NormalizeApprovalDecision(string decision)
    {
        if (string.Equals(decision, "Granted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return "Granted";
        }

        if (string.Equals(decision, "Declined", StringComparison.OrdinalIgnoreCase)
            || string.Equals(decision, "Denied", StringComparison.OrdinalIgnoreCase))
        {
            return "Declined";
        }

        return null;
    }

    private static string? NormalizeIdempotencyKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool CanShowTransitionAction(ICurrentUserService user, Submission submission, string target)
    {
        if (submission.IsArchived || !CanPerformTransition(user, submission.CurrentStatus, target))
            return false;

        if (IsAnyStatus(target, "BindRequested", "Bound"))
            return false;

        if (string.Equals(target, "Quoted", StringComparison.Ordinal))
        {
            var packet = submission.QuotePackets
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefault();
            return packet is not null && IsPacketReadyForApproval(packet);
        }

        return true;
    }

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

    private static bool CanManageDownstreamSubmission(ICurrentUserService user, Submission submission) =>
        CanReadSubmission(user, submission)
        && (HasRole(user, "Admin") || HasRole(user, "Underwriter"));

    private static bool CanApproveSubmission(ICurrentUserService user, Submission submission) =>
        CanReadSubmission(user, submission)
        && (HasRole(user, "Admin") || HasRole(user, "Underwriter"));

    private static bool CanArchiveSubmission(ICurrentUserService user, Submission submission) =>
        CanReadSubmission(user, submission)
        && (HasRole(user, "Admin") || HasRole(user, "Underwriter"));

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

    private static bool IsAnyStatus(string value, params string[] statuses) =>
        statuses.Any(status => string.Equals(value, status, StringComparison.Ordinal));

    private static string[] NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
