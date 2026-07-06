namespace Nebula.Domain.Entities;

public class ServiceCaseCommunicationLink : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public Guid CommunicationEventId { get; set; }
    public string LinkType { get; set; } = "Context";

    public ServiceCase ServiceCase { get; set; } = default!;
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
}
