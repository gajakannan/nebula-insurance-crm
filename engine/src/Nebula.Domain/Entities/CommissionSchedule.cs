namespace Nebula.Domain.Entities;

public class CommissionSchedule : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public string LineOfBusiness { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? ProductCode { get; set; }
    public string Basis { get; set; } = "premium_percent";
    public decimal? RatePercent { get; set; }
    public decimal? FlatAmount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string SourceNote { get; set; } = string.Empty;
}
