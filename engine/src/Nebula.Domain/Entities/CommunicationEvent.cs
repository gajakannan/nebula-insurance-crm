namespace Nebula.Domain.Entities;

public class CommunicationEvent : BaseEntity
{
    public string Type { get; set; } = default!;
    public string? Direction { get; set; }
    public string Summary { get; set; } = default!;
    public string? Body { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Visibility { get; set; } = "InternalOnly";
    public string? EmailProvider { get; set; }
    public string? EmailMessageId { get; set; }
    public string? EmailSubject { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public DateTime? RedactedAt { get; set; }
    public Guid? RedactedByUserId { get; set; }
    public string? RedactionReason { get; set; }

    public ICollection<CommunicationLink> Links { get; set; } = [];
    public ICollection<CommunicationParticipant> Participants { get; set; } = [];
    public ICollection<CommunicationCorrection> Corrections { get; set; } = [];
    public ICollection<CommunicationFollowUpTaskLink> FollowUpTaskLinks { get; set; } = [];
}
