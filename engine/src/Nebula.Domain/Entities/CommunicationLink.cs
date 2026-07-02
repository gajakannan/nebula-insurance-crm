namespace Nebula.Domain.Entities;

public class CommunicationLink : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; }
    public bool IsPrimary { get; set; }
}
