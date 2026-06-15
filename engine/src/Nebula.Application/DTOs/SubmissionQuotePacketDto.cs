namespace Nebula.Application.DTOs;

public record SubmissionQuotePacketDto(
    Guid? Id,
    Guid SubmissionId,
    string Status,
    IReadOnlyList<Guid> LinkedDocumentRefs,
    decimal? RecordedPremiumAmount,
    string? RecordedLimits,
    string? RecordedDeductibles,
    DateTime? EffectiveDate,
    string? CarrierMarket,
    string ReadinessState,
    DateTime? ReadyAt,
    Guid? ReadyByUserId,
    DateTime? ApprovedAt,
    Guid? ApprovedByUserId,
    string RowVersion);

public record SubmissionQuotePacketUpdateDto(
    IReadOnlyList<Guid>? LinkedDocumentRefs,
    decimal? RecordedPremiumAmount,
    string? RecordedLimits,
    string? RecordedDeductibles,
    DateTime? EffectiveDate,
    string? CarrierMarket,
    bool MarkReady = false);
