namespace Nebula.Application.DTOs;

public record PolicyListQuery(
    Guid CallerUserId,
    IReadOnlyList<string> CallerRoles,
    IReadOnlyList<string> CallerRegions,
    string? Search,
    string? Status,
    string? LineOfBusiness,
    Guid? CarrierId,
    Guid? BrokerOfRecordId,
    Guid? AccountId,
    DateTime? ExpiringBefore,
    string SortBy,
    string SortDir,
    int Page,
    int PageSize);

public record PolicyCoverageInputDto(
    string CoverageCode,
    string? CoverageName,
    decimal Limit,
    decimal? Deductible,
    decimal Premium,
    string? ExposureBasis,
    decimal? ExposureQuantity);

public record PolicyCreateRequestDto(
    Guid AccountId,
    Guid BrokerOfRecordId,
    string LineOfBusiness,
    Guid CarrierId,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    Guid? PredecessorPolicyId,
    Guid? ProducerUserId,
    decimal? TotalPremium,
    string? PremiumCurrency,
    string? ImportMode,
    string? ExternalPolicyReference,
    IReadOnlyList<PolicyCoverageInputDto>? Coverages);

public record PolicyFromBindRequestDto(
    Guid SubmissionId,
    Guid QuoteId,
    Guid AccountId,
    Guid BrokerOfRecordId,
    string LineOfBusiness,
    Guid CarrierId,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal TotalPremium,
    string PremiumCurrency,
    Guid? ProducerUserId,
    Guid? PredecessorPolicyId,
    string? ExternalPolicyReference,
    IReadOnlyList<PolicyCoverageInputDto> Coverages);

public record PolicyUpdateRequestDto(
    string? LineOfBusiness,
    Guid? CarrierId,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    Guid? ProducerUserId,
    decimal? TotalPremium,
    string? ExternalPolicyReference);

public record PolicyIssueRequestDto(
    DateTime? IssuedAt);

public record PolicyEndorsementRequestDto(
    string EndorsementReasonCode,
    string? EndorsementReasonDetail,
    DateTime EffectiveDate,
    decimal? PremiumDelta,
    IReadOnlyList<PolicyCoverageInputDto> Coverages);

public record PolicyCancelRequestDto(
    string CancellationReasonCode,
    string? CancellationReasonDetail,
    DateTime CancellationEffectiveDate);

public record PolicyReinstateRequestDto(
    string ReinstatementReason,
    string? ReinstatementDetail);

public record PolicyImportRequestDto(
    IReadOnlyList<PolicyCreateRequestDto> Policies);

public record PolicyImportRejectedRowDto(
    int Index,
    string Code,
    string Message);

public record PolicyImportResultDto(
    IReadOnlyList<PolicyDto> Accepted,
    IReadOnlyList<PolicyImportRejectedRowDto> Rejected);

public record PolicyListItemDto(
    Guid Id,
    string PolicyNumber,
    Guid AccountId,
    string? AccountDisplayName,
    string? AccountStatus,
    Guid? AccountSurvivorId,
    Guid BrokerOfRecordId,
    string? BrokerName,
    Guid CarrierId,
    string? CarrierName,
    string LineOfBusiness,
    string Status,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal TotalPremium,
    string PremiumCurrency,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    int VersionCount,
    int EndorsementCount,
    bool HasOpenRenewal,
    DateTime? ReinstatementDeadline,
    string RowVersion);

public record PolicyDto(
    Guid Id,
    Guid AccountId,
    Guid BrokerOfRecordId,
    string PolicyNumber,
    string LineOfBusiness,
    Guid CarrierId,
    string? CarrierName,
    string Status,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    DateTime? BoundAt,
    DateTime? IssuedAt,
    DateTime? CancelledAt,
    DateTime? CancellationEffectiveDate,
    string? CancellationReasonCode,
    string? CancellationReasonDetail,
    DateTime? ReinstatementDeadline,
    DateTime? ExpiredAt,
    Guid? PredecessorPolicyId,
    Guid? SuccessorPolicyId,
    Guid? CurrentVersionId,
    int CurrentVersionNumber,
    int VersionCount,
    decimal TotalPremium,
    string PremiumCurrency,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    string? ImportSource,
    string? ExternalPolicyReference,
    string? AccountDisplayNameAtLink,
    string? AccountStatusAtRead,
    Guid? AccountSurvivorId,
    IReadOnlyList<string> AvailableTransitions,
    string RowVersion,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId);

public record PolicySummaryDto(
    Guid Id,
    string PolicyNumber,
    Guid AccountId,
    string? AccountDisplayName,
    string? AccountStatus,
    Guid? AccountSurvivorId,
    Guid BrokerOfRecordId,
    string? BrokerName,
    Guid CarrierId,
    string? CarrierName,
    string LineOfBusiness,
    string Status,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    DateTime? BoundAt,
    DateTime? CancelledAt,
    DateTime? CancellationEffectiveDate,
    string? CancellationReasonCode,
    DateTime? ReinstatementDeadline,
    DateTime? ExpiredAt,
    Guid? PredecessorPolicyId,
    string? PredecessorPolicyNumber,
    Guid? SuccessorPolicyId,
    string? SuccessorPolicyNumber,
    decimal TotalPremium,
    string PremiumCurrency,
    Guid? CurrentVersionId,
    int CurrentVersionNumber,
    int VersionCount,
    int EndorsementCount,
    int CoverageLineCount,
    int OpenRenewalCount,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    string? ImportSource,
    string? ExternalPolicyReference,
    IReadOnlyList<string> AvailableTransitions,
    string RowVersion,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId);

public record PolicyVersionDto(
    Guid Id,
    Guid PolicyId,
    int VersionNumber,
    string VersionReason,
    Guid? EndorsementId,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal TotalPremium,
    string PremiumCurrency,
    object? ProfileSnapshot,
    object? CoverageSnapshot,
    object? PremiumSnapshot,
    DateTime CreatedAt,
    Guid CreatedByUserId);

public record PolicyEndorsementDto(
    Guid Id,
    Guid PolicyId,
    int EndorsementNumber,
    Guid PolicyVersionId,
    int? VersionNumber,
    string EndorsementReasonCode,
    string? EndorsementReasonDetail,
    DateTime EffectiveDate,
    decimal PremiumDelta,
    string PremiumCurrency,
    DateTime CreatedAt,
    Guid CreatedByUserId);

public record PolicyCoverageLineDto(
    Guid Id,
    Guid PolicyId,
    Guid PolicyVersionId,
    int VersionNumber,
    string CoverageCode,
    string? CoverageName,
    decimal Limit,
    decimal? Deductible,
    decimal Premium,
    string PremiumCurrency,
    string? ExposureBasis,
    decimal? ExposureQuantity,
    bool IsCurrent,
    DateTime CreatedAt);

public record PolicyAccountSummaryDto(
    Guid AccountId,
    int ActivePolicyCount,
    int ExpiredPolicyCount,
    int CancelledPolicyCount,
    int PendingPolicyCount,
    DateTime? NextExpiringDate,
    Guid? NextExpiringPolicyId,
    string? NextExpiringPolicyNumber,
    decimal TotalCurrentPremium,
    string PremiumCurrency,
    DateTime ComputedAt);
