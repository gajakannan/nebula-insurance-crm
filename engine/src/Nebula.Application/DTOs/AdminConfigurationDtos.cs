using System.Text.Json;
using Nebula.Application.Common;

namespace Nebula.Application.DTOs;

public sealed record AdminConfigurationDomainDto(
    string DomainKey,
    string DisplayName,
    string OwningModule,
    string Status,
    string EditableSchemaRef,
    bool SupportsRollback,
    int? CurrentPublishedVersion,
    string? DraftStatus,
    string? LastValidationStatus,
    string? LastPublishedBy,
    DateTime? LastPublishedAt);

public sealed record AdminConfigurationDomainDetailDto(
    AdminConfigurationDomainDto Domain,
    AdminConfigurationDraftDto? ActiveDraft,
    AdminConfigurationPublishedSetDto? CurrentPublishedSet,
    IReadOnlyList<AdminConfigurationRefreshStatusDto> RefreshStatuses,
    IReadOnlyList<AdminConfigurationPublishedSetDto> PublishedSets);

public sealed record AdminConfigurationDraftDto(
    Guid Id,
    string DomainKey,
    int BasePublishedVersion,
    int DraftVersion,
    string Status,
    JsonElement Payload,
    string PayloadHash,
    string RowVersion,
    AdminConfigurationValidationResultDto? LatestValidation);

public sealed record AdminConfigurationDraftCreateRequestDto(string Reason);

public sealed record AdminConfigurationDraftUpdateRequestDto(JsonElement Payload, string? Reason);

public sealed record AdminConfigurationValidationResultDto(
    Guid Id,
    Guid DraftId,
    string Status,
    string DraftPayloadHash,
    IReadOnlyList<AdminConfigurationValidationIssueDto> BlockingErrors,
    IReadOnlyList<AdminConfigurationValidationIssueDto> Warnings,
    IReadOnlyList<AdminConfigurationChangeSummaryDto> CompareSummary);

public sealed record AdminConfigurationValidationIssueDto(string Code, string Message, string? Path);

public sealed record AdminConfigurationChangeSummaryDto(string Path, string ChangeType, string? Before, string? After);

public sealed record AdminConfigurationPublishRequestDto(string Reason, int? TargetPublishedVersion);

public sealed record AdminConfigurationPublishedSetDto(
    Guid Id,
    string DomainKey,
    int PublishedVersion,
    JsonElement PayloadSnapshot,
    string PayloadHash,
    Guid PublishedByUserId,
    DateTime PublishedAt,
    string PublishReason,
    IReadOnlyList<AdminConfigurationRefreshStatusDto> RefreshStatuses);

public sealed record AdminConfigurationRefreshStatusDto(
    Guid Id,
    string ConsumerKey,
    string Status,
    DateTime? RefreshedAt,
    string? ErrorSummary);

public sealed record AdminConfigurationAuditEventDto(
    Guid Id,
    string DomainKey,
    Guid? DraftId,
    Guid? PublishedSetId,
    string Action,
    string Outcome,
    Guid ActorUserId,
    DateTime CreatedAt,
    JsonElement Summary);

public sealed record AdminConfigurationAuditQuery(
    string? DomainKey,
    string? Action,
    string? Outcome,
    Guid? ActorUserId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50);
