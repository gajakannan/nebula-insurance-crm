using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class CarrierMarketService(
    ICarrierMarketRepository carrierMarketRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<CarrierMarketDto>> ListAsync(
        CarrierMarketListQuery query,
        CancellationToken ct = default)
    {
        var result = await carrierMarketRepo.ListAsync(query, ct);
        return new PaginatedResult<CarrierMarketDto>(
            result.Data.Select(MapMarket).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<CarrierMarketDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdWithChildrenAsync(id, ct);
        return market is null ? null : MapDetail(market);
    }

    public async Task<(CarrierMarketDto? Dto, string? ErrorCode)> CreateAsync(
        CarrierMarketCreateDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (await carrierMarketRepo.CodeExistsAsync(dto.Code, null, ct))
            return (null, "duplicate_code");

        var now = DateTime.UtcNow;
        var market = new CarrierMarket
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            NaicCode = Normalize(dto.NaicCode),
            AmBestRating = Normalize(dto.AmBestRating),
            Status = dto.Status,
            MarketType = dto.MarketType,
            RelationshipOwnerUserId = dto.RelationshipOwnerUserId,
            WebsiteUrl = Normalize(dto.WebsiteUrl),
            GeneralEmail = Normalize(dto.GeneralEmail),
            MainPhone = Normalize(dto.MainPhone),
            Notes = Normalize(dto.Notes),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await carrierMarketRepo.AddAsync(market, ct);
        await AddTimelineAsync(market.Id, "CarrierMarketCreated", $"Carrier market \"{market.Name}\" created", user, now, new
        {
            market.Id,
            market.Code,
            market.Name,
            market.Status,
            market.MarketType,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapMarket(market), null);
    }

    public async Task<(CarrierMarketDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid id,
        CarrierMarketUpdateDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdAsync(id, ct);
        if (market is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        market.Name = dto.Name.Trim();
        market.NaicCode = Normalize(dto.NaicCode);
        market.AmBestRating = Normalize(dto.AmBestRating);
        market.Status = dto.Status;
        market.MarketType = dto.MarketType;
        market.RelationshipOwnerUserId = dto.RelationshipOwnerUserId;
        market.WebsiteUrl = Normalize(dto.WebsiteUrl);
        market.GeneralEmail = Normalize(dto.GeneralEmail);
        market.MainPhone = Normalize(dto.MainPhone);
        market.Notes = Normalize(dto.Notes);
        market.UpdatedAt = now;
        market.UpdatedByUserId = user.UserId;
        market.RowVersion = rowVersion;

        await carrierMarketRepo.UpdateAsync(market, ct);
        await AddTimelineAsync(market.Id, "CarrierMarketUpdated", $"Carrier market \"{market.Name}\" updated", user, now, new
        {
            market.Id,
            market.Code,
            market.Name,
            market.Status,
            market.MarketType,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapMarket(market), null);
    }

    public async Task<string?> DeleteAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdAsync(id, ct);
        if (market is null) return "not_found";

        var now = DateTime.UtcNow;
        market.IsDeleted = true;
        market.DeletedAt = now;
        market.DeletedByUserId = user.UserId;
        market.UpdatedAt = now;
        market.UpdatedByUserId = user.UserId;

        await carrierMarketRepo.UpdateAsync(market, ct);
        await AddTimelineAsync(market.Id, "CarrierMarketDeleted", $"Carrier market \"{market.Name}\" deleted", user, now, new
        {
            market.Id,
            market.Code,
            market.Name,
        }, ct);
        await unitOfWork.CommitAsync(ct);
        return null;
    }

    public async Task<(CarrierMarketContactDto? Dto, string? ErrorCode)> AddContactAsync(
        Guid marketId,
        CarrierMarketContactUpsertDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdAsync(marketId, ct);
        if (market is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        var contact = new CarrierMarketContact
        {
            CarrierMarketId = marketId,
            FullName = dto.FullName.Trim(),
            Title = Normalize(dto.Title),
            Email = Normalize(dto.Email),
            Phone = Normalize(dto.Phone),
            Roles = NormalizeStrings(dto.Roles),
            IsPrimary = dto.IsPrimary,
            Notes = Normalize(dto.Notes),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await carrierMarketRepo.AddContactAsync(contact, ct);
        await AddTimelineAsync(marketId, "CarrierMarketContactAdded", $"Contact \"{contact.FullName}\" added to carrier market \"{market.Name}\"", user, now, new
        {
            contact.Id,
            contact.CarrierMarketId,
            contact.FullName,
            contact.Roles,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapContact(contact), null);
    }

    public async Task<(CarrierMarketContactDto? Dto, string? ErrorCode)> UpdateContactAsync(
        Guid marketId,
        Guid contactId,
        CarrierMarketContactUpsertDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var contact = await carrierMarketRepo.GetContactByIdAsync(marketId, contactId, ct);
        if (contact is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        contact.FullName = dto.FullName.Trim();
        contact.Title = Normalize(dto.Title);
        contact.Email = Normalize(dto.Email);
        contact.Phone = Normalize(dto.Phone);
        contact.Roles = NormalizeStrings(dto.Roles);
        contact.IsPrimary = dto.IsPrimary;
        contact.Notes = Normalize(dto.Notes);
        contact.UpdatedAt = now;
        contact.UpdatedByUserId = user.UserId;
        contact.RowVersion = rowVersion;

        await carrierMarketRepo.UpdateContactAsync(contact, ct);
        await AddTimelineAsync(marketId, "CarrierMarketContactUpdated", $"Contact \"{contact.FullName}\" updated", user, now, new
        {
            contact.Id,
            contact.CarrierMarketId,
            contact.FullName,
            contact.Roles,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapContact(contact), null);
    }

    public async Task<string?> DeleteContactAsync(Guid marketId, Guid contactId, ICurrentUserService user, CancellationToken ct = default)
    {
        var contact = await carrierMarketRepo.GetContactByIdAsync(marketId, contactId, ct);
        if (contact is null) return "not_found";

        var now = DateTime.UtcNow;
        contact.IsDeleted = true;
        contact.DeletedAt = now;
        contact.DeletedByUserId = user.UserId;
        contact.UpdatedAt = now;
        contact.UpdatedByUserId = user.UserId;

        await carrierMarketRepo.UpdateContactAsync(contact, ct);
        await AddTimelineAsync(marketId, "CarrierMarketContactDeleted", $"Contact \"{contact.FullName}\" deleted", user, now, new
        {
            contact.Id,
            contact.CarrierMarketId,
            contact.FullName,
        }, ct);
        await unitOfWork.CommitAsync(ct);
        return null;
    }

    public async Task<(CarrierAppetiteNoteDto? Dto, string? ErrorCode)> AddAppetiteNoteAsync(
        Guid marketId,
        CarrierAppetiteNoteUpsertDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdAsync(marketId, ct);
        if (market is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        var note = new CarrierAppetiteNote
        {
            CarrierMarketId = marketId,
            LineOfBusiness = Normalize(dto.LineOfBusiness),
            Region = Normalize(dto.Region),
            AppetiteLevel = dto.AppetiteLevel,
            Summary = dto.Summary.Trim(),
            Detail = Normalize(dto.Detail),
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Source = Normalize(dto.Source),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await carrierMarketRepo.AddAppetiteNoteAsync(note, ct);
        await AddTimelineAsync(marketId, "CarrierAppetiteNoteAdded", $"Appetite note \"{note.Summary}\" added to carrier market \"{market.Name}\"", user, now, new
        {
            note.Id,
            note.CarrierMarketId,
            note.LineOfBusiness,
            note.Region,
            note.AppetiteLevel,
            note.Summary,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapAppetiteNote(note), null);
    }

    public async Task<(CarrierAppetiteNoteDto? Dto, string? ErrorCode)> UpdateAppetiteNoteAsync(
        Guid marketId,
        Guid noteId,
        CarrierAppetiteNoteUpsertDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var note = await carrierMarketRepo.GetAppetiteNoteByIdAsync(marketId, noteId, ct);
        if (note is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        note.LineOfBusiness = Normalize(dto.LineOfBusiness);
        note.Region = Normalize(dto.Region);
        note.AppetiteLevel = dto.AppetiteLevel;
        note.Summary = dto.Summary.Trim();
        note.Detail = Normalize(dto.Detail);
        note.EffectiveFrom = dto.EffectiveFrom;
        note.EffectiveTo = dto.EffectiveTo;
        note.Source = Normalize(dto.Source);
        note.UpdatedAt = now;
        note.UpdatedByUserId = user.UserId;
        note.RowVersion = rowVersion;

        await carrierMarketRepo.UpdateAppetiteNoteAsync(note, ct);
        await AddTimelineAsync(marketId, "CarrierAppetiteNoteUpdated", $"Appetite note \"{note.Summary}\" updated", user, now, new
        {
            note.Id,
            note.CarrierMarketId,
            note.LineOfBusiness,
            note.Region,
            note.AppetiteLevel,
            note.Summary,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapAppetiteNote(note), null);
    }

    public async Task<string?> DeleteAppetiteNoteAsync(Guid marketId, Guid noteId, ICurrentUserService user, CancellationToken ct = default)
    {
        var note = await carrierMarketRepo.GetAppetiteNoteByIdAsync(marketId, noteId, ct);
        if (note is null) return "not_found";

        var now = DateTime.UtcNow;
        note.IsDeleted = true;
        note.DeletedAt = now;
        note.DeletedByUserId = user.UserId;
        note.UpdatedAt = now;
        note.UpdatedByUserId = user.UserId;

        await carrierMarketRepo.UpdateAppetiteNoteAsync(note, ct);
        await AddTimelineAsync(marketId, "CarrierAppetiteNoteDeleted", $"Appetite note \"{note.Summary}\" deleted", user, now, new
        {
            note.Id,
            note.CarrierMarketId,
            note.Summary,
        }, ct);
        await unitOfWork.CommitAsync(ct);
        return null;
    }

    public async Task<(CarrierAppointmentDto? Dto, string? ErrorCode)> AddAppointmentAsync(
        Guid marketId,
        CarrierAppointmentUpsertDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdAsync(marketId, ct);
        if (market is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        var appointment = new CarrierAppointment
        {
            CarrierMarketId = marketId,
            AppointmentStatus = dto.AppointmentStatus,
            States = NormalizeStrings(dto.States),
            LineOfBusiness = Normalize(dto.LineOfBusiness),
            AppointmentNumber = Normalize(dto.AppointmentNumber),
            EffectiveDate = dto.EffectiveDate,
            ExpirationDate = dto.ExpirationDate,
            Notes = Normalize(dto.Notes),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await carrierMarketRepo.AddAppointmentAsync(appointment, ct);
        await AddTimelineAsync(marketId, "CarrierAppointmentAdded", $"Appointment \"{appointment.AppointmentStatus}\" added to carrier market \"{market.Name}\"", user, now, new
        {
            appointment.Id,
            appointment.CarrierMarketId,
            appointment.AppointmentStatus,
            appointment.States,
            appointment.LineOfBusiness,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapAppointment(appointment), null);
    }

    public async Task<(CarrierAppointmentDto? Dto, string? ErrorCode)> UpdateAppointmentAsync(
        Guid marketId,
        Guid appointmentId,
        CarrierAppointmentUpsertDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var appointment = await carrierMarketRepo.GetAppointmentByIdAsync(marketId, appointmentId, ct);
        if (appointment is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        appointment.AppointmentStatus = dto.AppointmentStatus;
        appointment.States = NormalizeStrings(dto.States);
        appointment.LineOfBusiness = Normalize(dto.LineOfBusiness);
        appointment.AppointmentNumber = Normalize(dto.AppointmentNumber);
        appointment.EffectiveDate = dto.EffectiveDate;
        appointment.ExpirationDate = dto.ExpirationDate;
        appointment.Notes = Normalize(dto.Notes);
        appointment.UpdatedAt = now;
        appointment.UpdatedByUserId = user.UserId;
        appointment.RowVersion = rowVersion;

        await carrierMarketRepo.UpdateAppointmentAsync(appointment, ct);
        await AddTimelineAsync(marketId, "CarrierAppointmentUpdated", $"Appointment \"{appointment.AppointmentStatus}\" updated", user, now, new
        {
            appointment.Id,
            appointment.CarrierMarketId,
            appointment.AppointmentStatus,
            appointment.States,
            appointment.LineOfBusiness,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapAppointment(appointment), null);
    }

    public async Task<string?> DeleteAppointmentAsync(Guid marketId, Guid appointmentId, ICurrentUserService user, CancellationToken ct = default)
    {
        var appointment = await carrierMarketRepo.GetAppointmentByIdAsync(marketId, appointmentId, ct);
        if (appointment is null) return "not_found";

        var now = DateTime.UtcNow;
        appointment.IsDeleted = true;
        appointment.DeletedAt = now;
        appointment.DeletedByUserId = user.UserId;
        appointment.UpdatedAt = now;
        appointment.UpdatedByUserId = user.UserId;

        await carrierMarketRepo.UpdateAppointmentAsync(appointment, ct);
        await AddTimelineAsync(marketId, "CarrierAppointmentDeleted", $"Appointment \"{appointment.AppointmentStatus}\" deleted", user, now, new
        {
            appointment.Id,
            appointment.CarrierMarketId,
            appointment.AppointmentStatus,
            appointment.States,
        }, ct);
        await unitOfWork.CommitAsync(ct);
        return null;
    }

    public async Task<(CarrierMarketActivityLinkDto? Dto, string? ErrorCode)> AddActivityLinkAsync(
        Guid marketId,
        CarrierMarketActivityLinkCreateDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var market = await carrierMarketRepo.GetByIdAsync(marketId, ct);
        if (market is null) return (null, "not_found");

        var relatedExists = dto.RelatedEntityType switch
        {
            "Submission" => await carrierMarketRepo.RelatedSubmissionExistsAsync(dto.RelatedEntityId, ct),
            "Policy" => await carrierMarketRepo.RelatedPolicyExistsAsync(dto.RelatedEntityId, ct),
            _ => false,
        };
        if (!relatedExists) return (null, "related_not_found");

        var now = DateTime.UtcNow;
        var link = new CarrierMarketActivityLink
        {
            CarrierMarketId = marketId,
            RelatedEntityType = dto.RelatedEntityType,
            RelatedEntityId = dto.RelatedEntityId,
            RelationshipKind = dto.RelationshipKind,
            Note = Normalize(dto.Note),
            CreatedAt = now,
            CreatedByUserId = user.UserId,
        };

        await carrierMarketRepo.AddActivityLinkAsync(link, ct);
        await AddTimelineAsync(marketId, "CarrierMarketActivityLinked", $"Carrier market \"{market.Name}\" linked to {link.RelatedEntityType}", user, now, new
        {
            link.Id,
            link.CarrierMarketId,
            link.RelatedEntityType,
            link.RelatedEntityId,
            link.RelationshipKind,
        }, ct);
        await unitOfWork.CommitAsync(ct);

        return (MapActivityLink(link), null);
    }

    private async Task AddTimelineAsync(
        Guid carrierMarketId,
        string eventType,
        string description,
        ICurrentUserService user,
        DateTime occurredAt,
        object payload,
        CancellationToken ct)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "CarrierMarket",
            EntityId = carrierMarketId,
            EventType = eventType,
            EventDescription = description,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(payload),
        }, ct);
    }

    private static CarrierMarketDto MapMarket(CarrierMarket market) => new(
        market.Id,
        market.Code,
        market.Name,
        market.NaicCode,
        market.AmBestRating,
        market.Status,
        market.MarketType,
        market.RelationshipOwnerUserId,
        market.WebsiteUrl,
        market.GeneralEmail,
        market.MainPhone,
        market.Notes,
        market.CreatedAt,
        market.CreatedByUserId,
        market.UpdatedAt,
        market.UpdatedByUserId,
        market.RowVersion);

    private static CarrierMarketDetailDto MapDetail(CarrierMarket market) => new(
        market.Id,
        market.Code,
        market.Name,
        market.NaicCode,
        market.AmBestRating,
        market.Status,
        market.MarketType,
        market.RelationshipOwnerUserId,
        market.WebsiteUrl,
        market.GeneralEmail,
        market.MainPhone,
        market.Notes,
        market.CreatedAt,
        market.CreatedByUserId,
        market.UpdatedAt,
        market.UpdatedByUserId,
        market.RowVersion,
        market.Contacts.Select(MapContact).ToList(),
        market.AppetiteNotes.Select(MapAppetiteNote).ToList(),
        market.Appointments.Select(MapAppointment).ToList(),
        market.ActivityLinks.Select(MapActivityLink).ToList());

    private static CarrierMarketContactDto MapContact(CarrierMarketContact contact) => new(
        contact.Id,
        contact.CarrierMarketId,
        contact.FullName,
        contact.Title,
        contact.Email,
        contact.Phone,
        contact.Roles,
        contact.IsPrimary,
        contact.Notes,
        contact.CreatedAt,
        contact.CreatedByUserId,
        contact.UpdatedAt,
        contact.UpdatedByUserId,
        contact.RowVersion);

    private static CarrierAppetiteNoteDto MapAppetiteNote(CarrierAppetiteNote note) => new(
        note.Id,
        note.CarrierMarketId,
        note.LineOfBusiness,
        note.Region,
        note.AppetiteLevel,
        note.Summary,
        note.Detail,
        note.EffectiveFrom,
        note.EffectiveTo,
        note.Source,
        note.CreatedAt,
        note.CreatedByUserId,
        note.UpdatedAt,
        note.UpdatedByUserId,
        note.RowVersion);

    private static CarrierAppointmentDto MapAppointment(CarrierAppointment appointment) => new(
        appointment.Id,
        appointment.CarrierMarketId,
        appointment.AppointmentStatus,
        appointment.States,
        appointment.LineOfBusiness,
        appointment.AppointmentNumber,
        appointment.EffectiveDate,
        appointment.ExpirationDate,
        appointment.Notes,
        appointment.CreatedAt,
        appointment.CreatedByUserId,
        appointment.UpdatedAt,
        appointment.UpdatedByUserId,
        appointment.RowVersion);

    private static CarrierMarketActivityLinkDto MapActivityLink(CarrierMarketActivityLink link) => new(
        link.Id,
        link.CarrierMarketId,
        link.RelatedEntityType,
        link.RelatedEntityId,
        link.RelationshipKind,
        link.Note,
        link.CreatedAt,
        link.CreatedByUserId);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string[] NormalizeStrings(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
