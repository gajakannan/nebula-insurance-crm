namespace Nebula.Domain.Entities;

public class LobBundleActivationEvent : BaseEntity
{
    public Guid LobSchemaBundleId { get; set; }
    public string FromStatus { get; set; } = default!;
    public string ToStatus { get; set; } = default!;
    public string? ChangeNote { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }

    public LobSchemaBundle LobSchemaBundle { get; set; } = default!;
}
