namespace Nebula.Application.DTOs;

public record CarrierMarketListQuery(
    string? Search,
    string? Status,
    string? MarketType,
    int Page = 1,
    int PageSize = 20);

public record CarrierMarketDto(
    Guid Id,
    string Code,
    string Name,
    string? NaicCode,
    string? AmBestRating,
    string Status,
    string MarketType,
    Guid? RelationshipOwnerUserId,
    string? WebsiteUrl,
    string? GeneralEmail,
    string? MainPhone,
    string? Notes,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion);

public record CarrierMarketDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? NaicCode,
    string? AmBestRating,
    string Status,
    string MarketType,
    Guid? RelationshipOwnerUserId,
    string? WebsiteUrl,
    string? GeneralEmail,
    string? MainPhone,
    string? Notes,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion,
    IReadOnlyList<CarrierMarketContactDto> Contacts,
    IReadOnlyList<CarrierAppetiteNoteDto> AppetiteNotes,
    IReadOnlyList<CarrierAppointmentDto> Appointments,
    IReadOnlyList<CarrierMarketActivityLinkDto> ActivityLinks);

public record CarrierMarketCreateDto(
    string Code,
    string Name,
    string? NaicCode,
    string? AmBestRating,
    string Status,
    string MarketType,
    Guid? RelationshipOwnerUserId,
    string? WebsiteUrl,
    string? GeneralEmail,
    string? MainPhone,
    string? Notes);

public record CarrierMarketUpdateDto(
    string Name,
    string? NaicCode,
    string? AmBestRating,
    string Status,
    string MarketType,
    Guid? RelationshipOwnerUserId,
    string? WebsiteUrl,
    string? GeneralEmail,
    string? MainPhone,
    string? Notes);

public record CarrierMarketContactDto(
    Guid Id,
    Guid CarrierMarketId,
    string FullName,
    string? Title,
    string? Email,
    string? Phone,
    IReadOnlyList<string> Roles,
    bool IsPrimary,
    string? Notes,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion);

public record CarrierMarketContactUpsertDto(
    string FullName,
    string? Title,
    string? Email,
    string? Phone,
    IReadOnlyList<string> Roles,
    bool IsPrimary,
    string? Notes);

public record CarrierAppetiteNoteDto(
    Guid Id,
    Guid CarrierMarketId,
    string? LineOfBusiness,
    string? Region,
    string AppetiteLevel,
    string Summary,
    string? Detail,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Source,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion);

public record CarrierAppetiteNoteUpsertDto(
    string? LineOfBusiness,
    string? Region,
    string AppetiteLevel,
    string Summary,
    string? Detail,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Source);

public record CarrierAppointmentDto(
    Guid Id,
    Guid CarrierMarketId,
    string AppointmentStatus,
    IReadOnlyList<string> States,
    string? LineOfBusiness,
    string? AppointmentNumber,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate,
    string? Notes,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId,
    uint RowVersion);

public record CarrierAppointmentUpsertDto(
    string AppointmentStatus,
    IReadOnlyList<string> States,
    string? LineOfBusiness,
    string? AppointmentNumber,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate,
    string? Notes);

public record CarrierMarketActivityLinkDto(
    Guid Id,
    Guid CarrierMarketId,
    string RelatedEntityType,
    Guid RelatedEntityId,
    string RelationshipKind,
    string? Note,
    DateTime CreatedAt,
    Guid CreatedByUserId);

public record CarrierMarketActivityLinkCreateDto(
    string RelatedEntityType,
    Guid RelatedEntityId,
    string RelationshipKind,
    string? Note);
