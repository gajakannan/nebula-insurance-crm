namespace Nebula.Domain.Entities;

public class ServiceCase : BaseEntity
{
    public string CaseNumber { get; set; } = default!;
    public Guid AccountId { get; set; }
    public Guid? PolicyId { get; set; }
    public string Summary { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string Status { get; set; } = "Intake";
    public string Priority { get; set; } = "Medium";
    public Guid OwnerUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? FollowUpSummary { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ResolutionSummary { get; set; }

    public Account Account { get; set; } = default!;
    public Policy? Policy { get; set; }
    public ServiceCaseClaimReference? ClaimReference { get; set; }
    public ICollection<ServiceCaseCommunicationLink> CommunicationLinks { get; set; } = [];
    public ICollection<ServiceCaseTaskLink> TaskLinks { get; set; } = [];
    public ICollection<ServiceCaseTransition> Transitions { get; set; } = [];
}
