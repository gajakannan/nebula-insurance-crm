namespace Nebula.Domain.Entities;

public class CommunicationCorrection : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string? PreviousSummary { get; set; }
    public string? PreviousBody { get; set; }
    public string? NewSummary { get; set; }
    public string? NewBody { get; set; }
}
