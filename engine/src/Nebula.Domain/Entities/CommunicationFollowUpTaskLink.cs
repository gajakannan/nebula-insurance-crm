namespace Nebula.Domain.Entities;

public class CommunicationFollowUpTaskLink : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public Guid TaskId { get; set; }
}
