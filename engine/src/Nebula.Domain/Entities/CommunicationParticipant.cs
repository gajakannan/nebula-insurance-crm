namespace Nebula.Domain.Entities;

public class CommunicationParticipant : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Email { get; set; }
    public string ParticipantType { get; set; } = default!;
    public string? Role { get; set; }
    public string? LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
}
