namespace Nebula.Domain.Entities;

public class PolicyEndorsement : BaseEntity
{
    public Guid PolicyId { get; set; }
    public int EndorsementNumber { get; set; }
    public Guid PolicyVersionId { get; set; }
    public string EndorsementReasonCode { get; set; } = default!;
    public string? EndorsementReasonDetail { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string LineOfBusiness { get; set; } = default!;
    public Guid LobProductVersionId { get; set; }
    public string LobAttributesJson { get; set; } = "{}";
    public decimal PremiumDelta { get; set; }
    public string PremiumCurrency { get; set; } = "USD";

    public Policy Policy { get; set; } = default!;
    public PolicyVersion PolicyVersion { get; set; } = default!;
    public LobProductVersion LobProductVersion { get; set; } = default!;
}
