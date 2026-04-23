namespace Nebula.Domain.Entities;

public class PolicyEndorsement : BaseEntity
{
    public Guid PolicyId { get; set; }
    public int EndorsementNumber { get; set; }
    public Guid PolicyVersionId { get; set; }
    public string EndorsementReasonCode { get; set; } = default!;
    public string? EndorsementReasonDetail { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal PremiumDelta { get; set; }
    public string PremiumCurrency { get; set; } = "USD";

    public Policy Policy { get; set; } = default!;
    public PolicyVersion PolicyVersion { get; set; } = default!;
}
