namespace Nebula.Domain.Entities;

public class CarrierAppointment : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public string AppointmentStatus { get; set; } = default!;
    public string[] States { get; set; } = [];
    public string? LineOfBusiness { get; set; }
    public string? AppointmentNumber { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string? Notes { get; set; }

    public CarrierMarket CarrierMarket { get; set; } = default!;
}
