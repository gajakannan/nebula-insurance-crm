namespace Nebula.Domain.Entities;

public class CarrierAppetiteNote : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public string AppetiteLevel { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public string? Detail { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Source { get; set; }

    public CarrierMarket CarrierMarket { get; set; } = default!;
}
