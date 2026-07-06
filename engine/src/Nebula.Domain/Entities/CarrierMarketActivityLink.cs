namespace Nebula.Domain.Entities;

public class CarrierMarketActivityLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CarrierMarketId { get; set; }
    public string RelatedEntityType { get; set; } = default!;
    public Guid RelatedEntityId { get; set; }
    public string RelationshipKind { get; set; } = default!;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public CarrierMarket CarrierMarket { get; set; } = default!;
}
