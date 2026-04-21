namespace Nebula.Domain.Entities;

public class AccountRelationshipHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string RelationshipType { get; set; } = default!;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime EffectiveAt { get; set; }
    public Guid ActorUserId { get; set; }
    public string? Notes { get; set; }

    public Account Account { get; set; } = default!;
}
