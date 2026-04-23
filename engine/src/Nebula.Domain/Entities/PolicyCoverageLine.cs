namespace Nebula.Domain.Entities;

public class PolicyCoverageLine : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid PolicyVersionId { get; set; }
    public int VersionNumber { get; set; }
    public string CoverageCode { get; set; } = default!;
    public string? CoverageName { get; set; }
    public decimal Limit { get; set; }
    public decimal? Deductible { get; set; }
    public decimal Premium { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public string? ExposureBasis { get; set; }
    public decimal? ExposureQuantity { get; set; }
    public bool IsCurrent { get; set; } = true;

    public Policy Policy { get; set; } = default!;
    public PolicyVersion PolicyVersion { get; set; } = default!;
}
