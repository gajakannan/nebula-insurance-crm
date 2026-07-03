namespace Nebula.Domain.Entities;

public class ServiceCaseTransition : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = default!;
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }

    public ServiceCase ServiceCase { get; set; } = default!;
}
