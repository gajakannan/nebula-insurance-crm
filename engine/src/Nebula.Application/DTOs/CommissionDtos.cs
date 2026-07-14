using Nebula.Application.Common;

namespace Nebula.Application.DTOs;

public record CommissionSearchQuery(
    string? Search,
    string? Status,
    string? ExceptionState,
    Guid? PolicyId,
    Guid? ProducerId,
    Guid? BrokerId,
    Guid? CarrierMarketId,
    Guid? TerritoryId,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    int Page = 1,
    int PageSize = 20);

public record CommissionScheduleUpsertDto(
    Guid CarrierMarketId,
    string LineOfBusiness,
    string? State,
    string? ProductCode,
    string Basis,
    decimal? RatePercent,
    decimal? FlatAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceNote);

public record CommissionScheduleDto(
    Guid Id,
    Guid CarrierMarketId,
    string LineOfBusiness,
    string? State,
    string? ProductCode,
    string Basis,
    decimal? RatePercent,
    decimal? FlatAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceNote,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion);

public record ProducerSplitParticipantUpsertDto(Guid ProducerId, decimal SplitPercent);

public record ProducerSplitAssignmentUpsertDto(
    Guid PolicyId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Reason,
    IReadOnlyList<ProducerSplitParticipantUpsertDto> Participants);

public record ProducerSplitParticipantDto(
    Guid Id,
    Guid ProducerId,
    string? ProducerDisplayName,
    decimal SplitPercent,
    string? SourceOwnershipSnapshotJson,
    uint RowVersion);

public record ProducerSplitAssignmentDto(
    Guid Id,
    Guid PolicyId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Reason,
    IReadOnlyList<ProducerSplitParticipantDto> Participants,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion);

public record ExpectedCommissionDto(
    Guid Id,
    Guid PolicyId,
    string? PolicyNumber,
    string? AccountDisplayName,
    Guid? PolicyVersionId,
    Guid CarrierMarketId,
    string? CarrierMarketName,
    string? BrokerName,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    string? LineOfBusiness,
    Guid? CommissionScheduleId,
    Guid? ProducerSplitAssignmentId,
    decimal? PremiumBasisAmount,
    decimal? ExpectedGrossCommission,
    decimal ApprovedAdjustmentTotal,
    decimal? AdjustedExpectedCommission,
    string Status,
    string ExceptionState,
    DateTime? CalculatedAt,
    uint RowVersion);

public record CommissionDisplayContextDto(
    Guid PolicyId,
    string? PolicyNumber,
    string? AccountDisplayName,
    string? CarrierMarketName,
    string? BrokerName,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    string? LineOfBusiness);

public record ExpectedCommissionDetailDto(
    ExpectedCommissionDto Commission,
    IReadOnlyList<CommissionScheduleDto> Schedules,
    IReadOnlyList<ProducerSplitAssignmentDto> Splits,
    IReadOnlyList<CommissionAdjustmentDto> Adjustments);

public record CommissionAdjustmentRequestDto(decimal Amount, DateOnly EffectiveDate, string Reason);

public record CommissionAdjustmentDecisionRequestDto(string Decision, string DecisionNote);

public record CommissionAdjustmentDto(
    Guid Id,
    Guid ExpectedCommissionId,
    decimal Amount,
    DateOnly EffectiveDate,
    string Reason,
    string Status,
    Guid RequestedByUserId,
    DateTime RequestedAt,
    Guid? DecidedByUserId,
    DateTime? DecidedAt,
    string? DecisionNote,
    uint RowVersion);

public record RevenueAttributionRollupQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    string GroupBy,
    Guid? ProducerId,
    Guid? BrokerId,
    Guid? TerritoryId,
    Guid? CarrierMarketId,
    string? Status,
    string? ExceptionState);

public record RevenueAttributionRollupRowDto(
    string GroupKey,
    string GroupLabel,
    decimal ExpectedGrossCommissionTotal,
    decimal ApprovedAdjustmentTotal,
    decimal AdjustedExpectedCommissionTotal,
    decimal ProducerAllocationTotal,
    int RecordCount,
    int ExceptionCount);

public record RevenueAttributionRollupResponseDto(
    RevenueAttributionRollupQuery Query,
    IReadOnlyList<RevenueAttributionRollupRowDto> Rows);

public record CommissionSearchResult(PaginatedResult<ExpectedCommissionDto> Page);
