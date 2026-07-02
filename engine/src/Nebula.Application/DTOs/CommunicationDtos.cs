namespace Nebula.Application.DTOs;

public record CommunicationLinkDto(string EntityType, Guid EntityId, bool IsPrimary, string? Label = null);

public record CommunicationParticipantDto(
    string DisplayName,
    string? Email,
    string ParticipantType,
    string? Role,
    string? LinkedEntityType,
    Guid? LinkedEntityId);

public record CommunicationEmailReferenceDto(
    string? Provider,
    string? MessageId,
    string? Subject,
    DateTime? SentAt);

public record CommunicationEventDto(
    Guid Id,
    string Type,
    string? Direction,
    string Summary,
    string? Body,
    DateTime OccurredAt,
    string Visibility,
    CommunicationEmailReferenceDto? EmailReference,
    IReadOnlyList<CommunicationParticipantDto> Participants,
    IReadOnlyList<CommunicationLinkDto> Links,
    IReadOnlyList<Guid> FollowUpTaskIds,
    bool IsRedacted,
    DateTime? RedactedAt,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    uint RowVersion);

public record CommunicationEventCreateRequestDto(
    string Type,
    string? Direction,
    string Summary,
    string? Body,
    DateTime OccurredAt,
    CommunicationEmailReferenceDto? EmailReference,
    IReadOnlyList<CommunicationParticipantDto> Participants,
    IReadOnlyList<CommunicationLinkDto> Links,
    CommunicationEventFollowUpRequestDto? FollowUp);

public record CommunicationEventCorrectionRequestDto(
    string Action,
    string Reason,
    string? Summary,
    string? Body,
    IReadOnlyList<CommunicationLinkDto>? Links,
    IReadOnlyList<CommunicationParticipantDto>? Participants);

public record CommunicationEventFollowUpRequestDto(
    string Title,
    string? Description,
    string? Priority,
    DateTime? DueDate,
    Guid AssignedToUserId,
    string LinkedEntityType,
    Guid LinkedEntityId);

public record CommunicationHistoryQuery(string EntityType, Guid EntityId, int Page, int PageSize);

public record CommunicationHistoryResponseDto(
    IReadOnlyList<CommunicationEventDto> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
