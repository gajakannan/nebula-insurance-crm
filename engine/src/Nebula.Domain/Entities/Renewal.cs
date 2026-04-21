namespace Nebula.Domain.Entities;

public class Renewal : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public Guid PolicyId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string CurrentStatus { get; set; } = "Identified";
    public DateTime PolicyExpirationDate { get; set; }
    public DateTime TargetOutreachDate { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string? LostReasonCode { get; set; }
    public string? LostReasonDetail { get; set; }
    public Guid? BoundPolicyId { get; set; }
    public Guid? RenewalSubmissionId { get; set; }
    public string AccountDisplayNameAtLink { get; set; } = default!;
    public string AccountStatusAtRead { get; set; } = default!;
    public Guid? AccountSurvivorId { get; set; }

    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
    public Policy Policy { get; set; } = default!;
    public Policy? BoundPolicy { get; set; }
    public UserProfile AssignedToUser { get; set; } = default!;
    public Submission? RenewalSubmission { get; set; }
}
