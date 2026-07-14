using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class CommissionRevenueService(
    ICommissionRepository commissionRepo,
    IRevenueAttributionRepository revenueRepo,
    BrokerScopeResolver scopeResolver,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<ExpectedCommissionDto>> SearchAsync(CommissionSearchQuery query, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var result = await commissionRepo.SearchExpectedCommissionsAsync(query, user, brokerScopeId, ct);
        var mapped = await MapExpectedListAsync(result.Data, ct);
        return new PaginatedResult<ExpectedCommissionDto>(mapped, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<ExpectedCommissionDetailDto?> GetDetailAsync(Guid expectedCommissionId, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var expected = await commissionRepo.GetExpectedCommissionAsync(expectedCommissionId, user, brokerScopeId, ct);
        if (expected is null) return null;

        var schedules = await commissionRepo.ListSchedulesAsync(expected.CarrierMarketId, ct);
        var splits = await commissionRepo.ListPolicySplitsAsync(expected.PolicyId, ct);
        var adjustments = await commissionRepo.ListAdjustmentsAsync(expected.Id, user, brokerScopeId, ct);

        return new ExpectedCommissionDetailDto(
            (await MapExpectedListAsync([expected], ct)).Single(),
            schedules.Select(MapSchedule).ToList(),
            await MapSplitListAsync(splits, ct),
            adjustments.Select(MapAdjustment).ToList());
    }

    public async Task<(CommissionScheduleDto? Result, string? Error)> CreateScheduleAsync(
        CommissionScheduleUpsertDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var schedule = new CommissionSchedule
        {
            CarrierMarketId = dto.CarrierMarketId,
            LineOfBusiness = dto.LineOfBusiness.Trim(),
            State = Normalize(dto.State)?.ToUpperInvariant(),
            ProductCode = Normalize(dto.ProductCode),
            Basis = dto.Basis.Trim(),
            RatePercent = dto.RatePercent,
            FlatAmount = dto.FlatAmount,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            SourceNote = dto.SourceNote.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        if (await commissionRepo.ScheduleOverlapsAsync(schedule, null, ct))
            return (null, "schedule_overlap");

        await commissionRepo.AddScheduleAsync(schedule, ct);
        await AddTimelineAsync("CommissionSchedule", schedule.Id, "CommissionScheduleCreated", "Commission schedule created", user, now, new { schedule.Id, schedule.CarrierMarketId, schedule.LineOfBusiness }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapSchedule(schedule), null);
    }

    public async Task<(CommissionScheduleDto? Result, string? Error)> UpdateScheduleAsync(
        Guid scheduleId,
        CommissionScheduleUpsertDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var schedule = await commissionRepo.GetScheduleAsync(scheduleId, ct);
        if (schedule is null) return (null, "not_found");

        schedule.CarrierMarketId = dto.CarrierMarketId;
        schedule.LineOfBusiness = dto.LineOfBusiness.Trim();
        schedule.State = Normalize(dto.State)?.ToUpperInvariant();
        schedule.ProductCode = Normalize(dto.ProductCode);
        schedule.Basis = dto.Basis.Trim();
        schedule.RatePercent = dto.RatePercent;
        schedule.FlatAmount = dto.FlatAmount;
        schedule.EffectiveFrom = dto.EffectiveFrom;
        schedule.EffectiveTo = dto.EffectiveTo;
        schedule.SourceNote = dto.SourceNote.Trim();
        schedule.UpdatedAt = DateTime.UtcNow;
        schedule.UpdatedByUserId = user.UserId;
        schedule.RowVersion = rowVersion;

        if (await commissionRepo.ScheduleOverlapsAsync(schedule, scheduleId, ct))
            return (null, "schedule_overlap");

        await AddTimelineAsync("CommissionSchedule", schedule.Id, "CommissionScheduleUpdated", "Commission schedule updated", user, schedule.UpdatedAt, new { schedule.Id, schedule.CarrierMarketId, schedule.LineOfBusiness }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapSchedule(schedule), null);
    }

    public async Task<(ProducerSplitAssignmentDto? Result, string? Error)> UpsertSplitAsync(
        ProducerSplitAssignmentUpsertDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        if (!await commissionRepo.PolicyIsVisibleAsync(dto.PolicyId, user, brokerScopeId, ct))
            return (null, "not_found");

        if (await commissionRepo.SplitOverlapsAsync(dto.PolicyId, dto.EffectiveFrom, dto.EffectiveTo, null, ct))
            return (null, "split_overlap");

        var now = DateTime.UtcNow;
        var assignment = new ProducerSplitAssignment
        {
            PolicyId = dto.PolicyId,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Reason = dto.Reason.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
            Participants = dto.Participants.Select(p => new ProducerSplitParticipant
            {
                ProducerId = p.ProducerId,
                SplitPercent = p.SplitPercent,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = user.UserId,
                UpdatedByUserId = user.UserId,
            }).ToList(),
        };

        await commissionRepo.AddSplitAsync(assignment, ct);
        await AddTimelineAsync("ProducerSplitAssignment", assignment.Id, "ProducerSplitAssigned", "Producer split assignment saved", user, now, new { assignment.Id, assignment.PolicyId, ParticipantCount = assignment.Participants.Count }, ct);
        await unitOfWork.CommitAsync(ct);
        return ((await MapSplitListAsync([assignment], ct)).Single(), null);
    }

    public async Task<(ExpectedCommissionDto? Result, string? Error)> CalculateAsync(
        Guid expectedCommissionId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var expected = await commissionRepo.GetExpectedCommissionForMutationAsync(expectedCommissionId, user, brokerScopeId, ct);
        if (expected is null) return (null, "not_found");

        var schedules = await commissionRepo.ListSchedulesAsync(expected.CarrierMarketId, ct);
        var schedule = schedules
            .Where(s => s.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= DateOnly.FromDateTime(DateTime.UtcNow)))
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefault();
        var split = await commissionRepo.GetActiveSplitAsync(expected.PolicyId, DateOnly.FromDateTime(DateTime.UtcNow), null, ct);
        var now = DateTime.UtcNow;

        if (expected.PremiumBasisAmount is null)
        {
            ApplyException(expected, "MissingInputs", "missing_premium", now);
        }
        else if (schedule is null)
        {
            ApplyException(expected, "MissingInputs", "missing_schedule", now);
        }
        else if (split is null)
        {
            ApplyException(expected, "MissingInputs", "missing_split", now);
        }
        else
        {
            var gross = schedule.RatePercent.HasValue
                ? Math.Round(expected.PremiumBasisAmount.Value * (schedule.RatePercent.Value / 100m), 2)
                : Math.Round(schedule.FlatAmount ?? 0m, 2);
            expected.CommissionScheduleId = schedule.Id;
            expected.ProducerSplitAssignmentId = split.Id;
            expected.ExpectedGrossCommission = gross;
            expected.ApprovedAdjustmentTotal = expected.Adjustments.Where(a => a.Status == "Approved").Sum(a => a.Amount);
            expected.AdjustedExpectedCommission = gross + expected.ApprovedAdjustmentTotal;
            expected.Status = "ReviewReady";
            expected.ExceptionState = "none";
            expected.CalculatedAt = now;
            expected.UpdatedAt = now;
        }

        await RefreshProjectionAsync(expected, schedule, split, user, now, ct);
        await AddTimelineAsync("ExpectedCommission", expected.Id, "ExpectedCommissionCalculated", "Expected commission recalculated", user, now, new { expected.Id, expected.Status, expected.ExceptionState, expected.ExpectedGrossCommission }, ct);
        await unitOfWork.CommitAsync(ct);
        return ((await MapExpectedListAsync([expected], ct)).Single(), null);
    }

    public async Task<IReadOnlyList<ProducerSplitAssignmentDto>> ListPolicySplitsAsync(Guid policyId, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        if (!await commissionRepo.PolicyIsVisibleAsync(policyId, user, brokerScopeId, ct))
            return [];

        var splits = await commissionRepo.ListPolicySplitsAsync(policyId, ct);
        return await MapSplitListAsync(splits, ct);
    }

    public async Task<IReadOnlyList<CommissionAdjustmentDto>> ListAdjustmentsAsync(Guid expectedCommissionId, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        if (await commissionRepo.GetExpectedCommissionAsync(expectedCommissionId, user, brokerScopeId, ct) is null)
            return [];

        var adjustments = await commissionRepo.ListAdjustmentsAsync(expectedCommissionId, user, brokerScopeId, ct);
        return adjustments.Select(MapAdjustment).ToList();
    }

    public async Task<(CommissionAdjustmentDto? Result, string? Error)> RequestAdjustmentAsync(
        Guid expectedCommissionId,
        CommissionAdjustmentRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var expected = await commissionRepo.GetExpectedCommissionForMutationAsync(expectedCommissionId, user, brokerScopeId, ct);
        if (expected is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        var adjustment = new CommissionAdjustment
        {
            ExpectedCommissionId = expectedCommissionId,
            Amount = dto.Amount,
            EffectiveDate = dto.EffectiveDate,
            Reason = dto.Reason.Trim(),
            Status = "Pending",
            RequestedByUserId = user.UserId,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await commissionRepo.AddAdjustmentAsync(adjustment, ct);
        await AddTimelineAsync("ExpectedCommission", expected.Id, "CommissionAdjustmentRequested", "Commission adjustment requested", user, now, new { adjustment.Id, adjustment.ExpectedCommissionId, adjustment.Amount }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapAdjustment(adjustment), null);
    }

    public async Task<(CommissionAdjustmentDto? Result, string? Error)> DecideAdjustmentAsync(
        Guid adjustmentId,
        CommissionAdjustmentDecisionRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var adjustment = await commissionRepo.GetAdjustmentAsync(adjustmentId, user, brokerScopeId, ct);
        if (adjustment is null) return (null, "not_found");
        if (adjustment.Status != "Pending") return (null, "adjustment_not_pending");
        if (adjustment.RequestedByUserId == user.UserId) return (null, "same_user_approval_denied");

        var now = DateTime.UtcNow;
        adjustment.Status = dto.Decision;
        adjustment.DecisionNote = dto.DecisionNote.Trim();
        adjustment.DecidedByUserId = user.UserId;
        adjustment.DecidedAt = now;
        adjustment.UpdatedByUserId = user.UserId;
        adjustment.UpdatedAt = now;

        if (adjustment.ExpectedCommission is not null && dto.Decision == "Approved")
        {
            adjustment.ExpectedCommission.ApprovedAdjustmentTotal = adjustment.ExpectedCommission.Adjustments
                .Where(a => a.Id == adjustment.Id || a.Status == "Approved")
                .Sum(a => a.Amount);
            adjustment.ExpectedCommission.AdjustedExpectedCommission = (adjustment.ExpectedCommission.ExpectedGrossCommission ?? 0m) + adjustment.ExpectedCommission.ApprovedAdjustmentTotal;
            adjustment.ExpectedCommission.UpdatedAt = now;
            adjustment.ExpectedCommission.UpdatedByUserId = user.UserId;
            await RefreshProjectionAsync(adjustment.ExpectedCommission, null, null, user, now, ct);
        }

        await AddTimelineAsync("ExpectedCommission", adjustment.ExpectedCommissionId, dto.Decision == "Approved" ? "CommissionAdjustmentApproved" : "CommissionAdjustmentRejected", $"Commission adjustment {dto.Decision.ToLowerInvariant()}", user, now, new { adjustment.Id, adjustment.ExpectedCommissionId, adjustment.Status }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapAdjustment(adjustment), null);
    }

    public async Task<RevenueAttributionRollupResponseDto> GetRollupsAsync(RevenueAttributionRollupQuery query, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        return await revenueRepo.GetRollupsAsync(query, user, brokerScopeId, ct);
    }

    private async Task<Guid?> ResolveBrokerScopeAsync(ICurrentUserService user, CancellationToken ct) =>
        HasRole(user.Roles, "BrokerUser") ? await scopeResolver.ResolveAsync(user, ct) : null;

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static void ApplyException(ExpectedCommission expected, string status, string exceptionState, DateTime now)
    {
        expected.Status = status;
        expected.ExceptionState = exceptionState;
        expected.ExpectedGrossCommission = null;
        expected.AdjustedExpectedCommission = null;
        expected.CalculatedAt = now;
        expected.UpdatedAt = now;
    }

    private Task AddTimelineAsync(string entityType, Guid entityId, string eventType, string description, ICurrentUserService user, DateTime occurredAt, object payload, CancellationToken ct) =>
        timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            EventDescription = description,
            EventPayloadJson = JsonSerializer.Serialize(payload),
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
        }, ct);

    private async Task RefreshProjectionAsync(
        ExpectedCommission expected,
        CommissionSchedule? schedule,
        ProducerSplitAssignment? split,
        ICurrentUserService user,
        DateTime now,
        CancellationToken ct)
    {
        schedule ??= expected.CommissionScheduleId.HasValue
            ? await commissionRepo.GetScheduleAsync(expected.CommissionScheduleId.Value, ct)
            : null;
        split ??= expected.ProducerSplitAssignmentId.HasValue
            ? await commissionRepo.GetActiveSplitAsync(expected.PolicyId, DateOnly.FromDateTime(now), null, ct)
            : null;

        var primaryParticipant = split?.Participants
            .OrderByDescending(participant => participant.SplitPercent)
            .FirstOrDefault();
        var policyPeriodStart = schedule?.EffectiveFrom ?? DateOnly.FromDateTime(now);
        var policyPeriodEnd = schedule?.EffectiveTo ?? policyPeriodStart.AddYears(1).AddDays(-1);
        var adjustedExpected = expected.AdjustedExpectedCommission ?? 0m;
        var producerAllocation = primaryParticipant is null
            ? 0m
            : Math.Round(adjustedExpected * (primaryParticipant.SplitPercent / 100m), 2);

        await commissionRepo.UpsertProjectionAsync(new RevenueAttributionProjection
        {
            ExpectedCommissionId = expected.Id,
            PolicyId = expected.PolicyId,
            ProducerId = primaryParticipant?.ProducerId,
            CarrierMarketId = expected.CarrierMarketId,
            PolicyPeriodStart = policyPeriodStart,
            PolicyPeriodEnd = policyPeriodEnd,
            LineOfBusiness = schedule?.LineOfBusiness ?? "Unassigned",
            ExpectedGrossCommission = expected.ExpectedGrossCommission ?? 0m,
            ApprovedAdjustmentTotal = expected.ApprovedAdjustmentTotal,
            AdjustedExpectedCommission = adjustedExpected,
            ProducerAllocationAmount = producerAllocation,
            UnresolvedExceptionCount = expected.ExceptionState == "none" ? 0 : 1,
            SourceRefreshedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        }, ct);
    }

    public static CommissionScheduleDto MapSchedule(CommissionSchedule s) =>
        new(s.Id, s.CarrierMarketId, s.LineOfBusiness, s.State, s.ProductCode, s.Basis, s.RatePercent, s.FlatAmount, s.EffectiveFrom, s.EffectiveTo, s.SourceNote, s.CreatedAt, s.CreatedByUserId, s.UpdatedAt, s.UpdatedByUserId, s.RowVersion);

    private async Task<IReadOnlyList<ProducerSplitAssignmentDto>> MapSplitListAsync(IReadOnlyList<ProducerSplitAssignment> splits, CancellationToken ct)
    {
        var producerIds = splits
            .SelectMany(split => split.Participants)
            .Select(participant => participant.ProducerId)
            .Distinct()
            .ToList();
        var producerNames = await commissionRepo.GetProducerDisplayNamesAsync(producerIds, ct);

        return splits.Select(split => new ProducerSplitAssignmentDto(
            split.Id,
            split.PolicyId,
            split.EffectiveFrom,
            split.EffectiveTo,
            split.Reason,
            split.Participants.Select(participant => new ProducerSplitParticipantDto(
                participant.Id,
                participant.ProducerId,
                producerNames.GetValueOrDefault(participant.ProducerId),
                participant.SplitPercent,
                participant.SourceOwnershipSnapshotJson,
                participant.RowVersion)).ToList(),
            split.CreatedAt,
            split.CreatedByUserId,
            split.UpdatedAt,
            split.UpdatedByUserId,
            split.RowVersion)).ToList();
    }

    private async Task<IReadOnlyList<ExpectedCommissionDto>> MapExpectedListAsync(IReadOnlyList<ExpectedCommission> commissions, CancellationToken ct)
    {
        var contexts = await commissionRepo.GetDisplayContextsAsync(commissions.Select(e => e.PolicyId).Distinct().ToList(), ct);

        return commissions.Select(e =>
        {
            contexts.TryGetValue(e.PolicyId, out var context);

            return new ExpectedCommissionDto(
                e.Id,
                e.PolicyId,
                context?.PolicyNumber,
                context?.AccountDisplayName,
                e.PolicyVersionId,
                e.CarrierMarketId,
                context?.CarrierMarketName,
                context?.BrokerName,
                context?.ProducerUserId,
                context?.ProducerDisplayName,
                context?.LineOfBusiness,
                e.CommissionScheduleId,
                e.ProducerSplitAssignmentId,
                e.PremiumBasisAmount,
                e.ExpectedGrossCommission,
                e.ApprovedAdjustmentTotal,
                e.AdjustedExpectedCommission,
                e.Status,
                e.ExceptionState,
                e.CalculatedAt,
                e.RowVersion);
        }).ToList();
    }

    public static CommissionAdjustmentDto MapAdjustment(CommissionAdjustment a) =>
        new(a.Id, a.ExpectedCommissionId, a.Amount, a.EffectiveDate, a.Reason, a.Status, a.RequestedByUserId, a.RequestedAt, a.DecidedByUserId, a.DecidedAt, a.DecisionNote, a.RowVersion);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
