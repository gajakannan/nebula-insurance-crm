namespace Nebula.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdempotencyKey { get; set; } = default!;
    public string Operation { get; set; } = default!;
    public Guid? ResourceId { get; set; }
    public Guid ActorUserId { get; set; }
    public int ResponseStatusCode { get; set; }
    public string? ResponsePayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
