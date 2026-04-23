namespace Nebula.Domain.Entities;

public class Policy : BaseEntity
{
    public string PolicyNumber { get; set; } = default!;
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public Guid CarrierId { get; set; }
    public string LineOfBusiness { get; set; } = default!;
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal TotalPremium { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public string CurrentStatus { get; set; } = "Pending";
    public Guid? CurrentVersionId { get; set; }
    public DateTime? BoundAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CancellationEffectiveDate { get; set; }
    public string? CancellationReasonCode { get; set; }
    public string? CancellationReasonDetail { get; set; }
    public DateTime? ReinstatementDeadline { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public Guid? PredecessorPolicyId { get; set; }
    public Guid? ProducerUserId { get; set; }
    public string ImportSource { get; set; } = "manual";
    public string? ExternalPolicyReference { get; set; }
    public string AccountDisplayNameAtLink { get; set; } = default!;
    public string AccountStatusAtRead { get; set; } = default!;
    public Guid? AccountSurvivorId { get; set; }

    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
    public CarrierRef Carrier { get; set; } = default!;
    public UserProfile? Producer { get; set; }
    public Policy? PredecessorPolicy { get; set; }
    public ICollection<PolicyVersion> Versions { get; set; } = [];
    public ICollection<PolicyEndorsement> Endorsements { get; set; } = [];
    public ICollection<PolicyCoverageLine> CoverageLines { get; set; } = [];
}
