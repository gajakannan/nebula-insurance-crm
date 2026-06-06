namespace Nebula.Domain.Entities;

public class SubmissionBindHandoff : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public string IdempotencyKey { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string PayloadSnapshotJson { get; set; } = "{}";
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Submission Submission { get; set; } = default!;
}
