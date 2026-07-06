namespace Nebula.Domain.Entities;

public class CarrierMarketContact : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public string FullName { get; set; } = default!;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string[] Roles { get; set; } = [];
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }

    public CarrierMarket CarrierMarket { get; set; } = default!;
}
