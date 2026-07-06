using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ICarrierMarketRepository
{
    Task<CarrierMarket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CarrierMarket?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);
    Task<CarrierMarketContact?> GetContactByIdAsync(Guid carrierMarketId, Guid contactId, CancellationToken ct = default);
    Task<CarrierAppetiteNote?> GetAppetiteNoteByIdAsync(Guid carrierMarketId, Guid noteId, CancellationToken ct = default);
    Task<CarrierAppointment?> GetAppointmentByIdAsync(Guid carrierMarketId, Guid appointmentId, CancellationToken ct = default);
    Task<PaginatedResult<CarrierMarket>> ListAsync(CarrierMarketListQuery query, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludingId = null, CancellationToken ct = default);
    Task<bool> RelatedSubmissionExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> RelatedPolicyExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(CarrierMarket carrierMarket, CancellationToken ct = default);
    Task AddContactAsync(CarrierMarketContact contact, CancellationToken ct = default);
    Task AddAppetiteNoteAsync(CarrierAppetiteNote note, CancellationToken ct = default);
    Task AddAppointmentAsync(CarrierAppointment appointment, CancellationToken ct = default);
    Task AddActivityLinkAsync(CarrierMarketActivityLink link, CancellationToken ct = default);
    Task UpdateAsync(CarrierMarket carrierMarket, CancellationToken ct = default);
    Task UpdateContactAsync(CarrierMarketContact contact, CancellationToken ct = default);
    Task UpdateAppetiteNoteAsync(CarrierAppetiteNote note, CancellationToken ct = default);
    Task UpdateAppointmentAsync(CarrierAppointment appointment, CancellationToken ct = default);
}
