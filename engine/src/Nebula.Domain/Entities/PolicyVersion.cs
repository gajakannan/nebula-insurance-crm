namespace Nebula.Domain.Entities;

public class PolicyVersion : BaseEntity
{
    public Guid PolicyId { get; set; }
    public int VersionNumber { get; set; }
    public string VersionReason { get; set; } = default!;
    public Guid? EndorsementId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal TotalPremium { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public string ProfileSnapshotJson { get; set; } = "{}";
    public string CoverageSnapshotJson { get; set; } = "[]";
    public string PremiumSnapshotJson { get; set; } = "{}";

    public Policy Policy { get; set; } = default!;
    public ICollection<PolicyCoverageLine> CoverageLines { get; set; } = [];
}
