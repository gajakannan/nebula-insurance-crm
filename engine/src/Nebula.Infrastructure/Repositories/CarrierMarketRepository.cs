using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class CarrierMarketRepository(AppDbContext db) : ICarrierMarketRepository
{
    public async Task<CarrierMarket?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.CarrierMarkets.FirstOrDefaultAsync(market => market.Id == id, ct);

    public async Task<CarrierMarket?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default) =>
        await db.CarrierMarkets
            .Include(market => market.Contacts.OrderByDescending(contact => contact.IsPrimary).ThenBy(contact => contact.FullName))
            .Include(market => market.AppetiteNotes.OrderBy(note => note.LineOfBusiness).ThenBy(note => note.Region))
            .Include(market => market.Appointments.OrderBy(appointment => appointment.LineOfBusiness))
            .Include(market => market.ActivityLinks.OrderByDescending(link => link.CreatedAt))
            .FirstOrDefaultAsync(market => market.Id == id, ct);

    public async Task<CarrierMarketContact?> GetContactByIdAsync(Guid carrierMarketId, Guid contactId, CancellationToken ct = default) =>
        await db.CarrierMarketContacts.FirstOrDefaultAsync(contact => contact.CarrierMarketId == carrierMarketId && contact.Id == contactId, ct);

    public async Task<CarrierAppetiteNote?> GetAppetiteNoteByIdAsync(Guid carrierMarketId, Guid noteId, CancellationToken ct = default) =>
        await db.CarrierAppetiteNotes.FirstOrDefaultAsync(note => note.CarrierMarketId == carrierMarketId && note.Id == noteId, ct);

    public async Task<CarrierAppointment?> GetAppointmentByIdAsync(Guid carrierMarketId, Guid appointmentId, CancellationToken ct = default) =>
        await db.CarrierAppointments.FirstOrDefaultAsync(appointment => appointment.CarrierMarketId == carrierMarketId && appointment.Id == appointmentId, ct);

    public async Task<PaginatedResult<CarrierMarket>> ListAsync(CarrierMarketListQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var markets = db.CarrierMarkets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            markets = markets.Where(market =>
                market.Code.ToLower().Contains(search)
                || market.Name.ToLower().Contains(search)
                || (market.NaicCode != null && market.NaicCode.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            markets = markets.Where(market => market.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.MarketType))
            markets = markets.Where(market => market.MarketType == query.MarketType);

        var totalCount = await markets.CountAsync(ct);
        var data = await markets
            .OrderBy(market => market.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<CarrierMarket>(data, page, pageSize, totalCount);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludingId = null, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = db.CarrierMarkets.IgnoreQueryFilters().Where(market => market.Code.ToUpper() == normalized);
        if (excludingId.HasValue)
            query = query.Where(market => market.Id != excludingId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> RelatedSubmissionExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.Submissions.AsNoTracking().AnyAsync(submission => submission.Id == id, ct);

    public async Task<bool> RelatedPolicyExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.Policies.AsNoTracking().AnyAsync(policy => policy.Id == id, ct);

    public Task AddAsync(CarrierMarket carrierMarket, CancellationToken ct = default) =>
        db.CarrierMarkets.AddAsync(carrierMarket, ct).AsTask();

    public Task AddContactAsync(CarrierMarketContact contact, CancellationToken ct = default) =>
        db.CarrierMarketContacts.AddAsync(contact, ct).AsTask();

    public Task AddAppetiteNoteAsync(CarrierAppetiteNote note, CancellationToken ct = default) =>
        db.CarrierAppetiteNotes.AddAsync(note, ct).AsTask();

    public Task AddAppointmentAsync(CarrierAppointment appointment, CancellationToken ct = default) =>
        db.CarrierAppointments.AddAsync(appointment, ct).AsTask();

    public Task AddActivityLinkAsync(CarrierMarketActivityLink link, CancellationToken ct = default) =>
        db.CarrierMarketActivityLinks.AddAsync(link, ct).AsTask();

    public Task UpdateAsync(CarrierMarket carrierMarket, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateContactAsync(CarrierMarketContact contact, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAppetiteNoteAsync(CarrierAppetiteNote note, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAppointmentAsync(CarrierAppointment appointment, CancellationToken ct = default) => Task.CompletedTask;
}
