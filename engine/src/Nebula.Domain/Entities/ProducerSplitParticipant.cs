namespace Nebula.Domain.Entities;

public class ProducerSplitParticipant : BaseEntity
{
    public Guid ProducerSplitAssignmentId { get; set; }
    public Guid ProducerId { get; set; }
    public decimal SplitPercent { get; set; }
    public string? SourceOwnershipSnapshotJson { get; set; }
    public ProducerSplitAssignment? Assignment { get; set; }
}
