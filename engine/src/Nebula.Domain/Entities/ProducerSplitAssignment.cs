namespace Nebula.Domain.Entities;

public class ProducerSplitAssignment : BaseEntity
{
    public Guid PolicyId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ICollection<ProducerSplitParticipant> Participants { get; set; } = new List<ProducerSplitParticipant>();
}
