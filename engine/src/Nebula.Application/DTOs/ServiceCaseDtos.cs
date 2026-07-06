namespace Nebula.Application.DTOs;

public record ServiceCaseClaimReferenceDto(
    string? CarrierClaimNumber,
    DateOnly? DateOfLoss,
    string? ClaimantDisplayName,
    string? LossSummary,
    string? CarrierContactReference,
    Guid? UpdatedByUserId,
    DateTime? UpdatedAt);

public record ServiceCaseCommunicationLinkDto(
    Guid CommunicationEventId,
    string LinkType,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record ServiceCaseTaskLinkDto(
    Guid TaskId,
    string Relationship,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record ServiceCaseTransitionDto(
    string? FromStatus,
    string ToStatus,
    Guid ActorUserId,
    DateTime OccurredAt,
    string? ReasonCode,
    string? Note);

public record ServiceCaseDto(
    Guid Id,
    string CaseNumber,
    Guid AccountId,
    Guid? PolicyId,
    string Summary,
    string? Description,
    string Type,
    string Status,
    string Priority,
    Guid OwnerUserId,
    string? OwnerDisplayName,
    string? AccountDisplayName,
    string? PolicyNumber,
    DateOnly? DueDate,
    string? FollowUpSummary,
    bool HasClaimReference,
    DateTime? LastActivityAt,
    ServiceCaseClaimReferenceDto? ClaimReference,
    IReadOnlyList<ServiceCaseCommunicationLinkDto> CommunicationLinks,
    IReadOnlyList<ServiceCaseTaskLinkDto> TaskLinks,
    IReadOnlyList<ServiceCaseTransitionDto> Transitions,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string? ResolutionSummary,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    uint RowVersion);

public record ServiceCaseCreateRequestDto(
    Guid AccountId,
    Guid? PolicyId,
    string Summary,
    string? Description,
    string Type,
    string Priority,
    Guid OwnerUserId,
    DateOnly? DueDate,
    string? FollowUpSummary,
    ServiceCaseClaimReferenceUpdateRequestDto? ClaimReference);

public record ServiceCaseUpdateRequestDto(
    string? Summary,
    string? Description,
    string? Priority,
    Guid? OwnerUserId,
    DateOnly? DueDate,
    string? FollowUpSummary,
    string? ResolutionSummary,
    uint? RowVersion);

public record ServiceCaseTransitionRequestDto(
    string ToStatus,
    string? ReasonCode,
    string? Note,
    string? ResolutionSummary,
    uint? RowVersion);

public record ServiceCaseClaimReferenceUpdateRequestDto(
    string? CarrierClaimNumber,
    DateOnly? DateOfLoss,
    string? ClaimantDisplayName,
    string? LossSummary,
    string? CarrierContactReference,
    uint? RowVersion);

public record ServiceCaseCommunicationLinkRequestDto(
    Guid CommunicationEventId,
    string? LinkType,
    uint? RowVersion);

public record ServiceCaseFollowUpTaskRequestDto(
    string Title,
    string? Description,
    Guid AssignedToUserId,
    DateOnly? DueDate,
    string? Priority,
    uint? RowVersion);

public record ServiceCaseListQuery(
    Guid? AccountId,
    Guid? PolicyId,
    Guid? OwnerUserId,
    string? Status,
    string? Priority,
    string? Search,
    DateOnly? DueBefore,
    DateOnly? DueAfter,
    bool IncludeClosed,
    int Page,
    int PageSize);

public record ServiceCaseListResponseDto(
    IReadOnlyList<ServiceCaseDto> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
