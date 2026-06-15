namespace Nebula.Domain.Entities;

public class SubmissionApprovalDecision : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public string Decision { get; set; } = default!;
    public Guid ApproverUserId { get; set; }
    public string Reason { get; set; } = default!;
    public string AuthorityContextJson { get; set; } = "{}";
    public DateTime DecidedAt { get; set; }
    public string BlockingConditionsJson { get; set; } = "[]";

    public Submission Submission { get; set; } = default!;
}
