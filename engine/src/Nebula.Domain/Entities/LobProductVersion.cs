namespace Nebula.Domain.Entities;

public class LobProductVersion : BaseEntity
{
    public Guid LobProductId { get; set; }
    public string Version { get; set; } = default!;
    public string Status { get; set; } = "Active";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? DeprecatedAt { get; set; }

    public LobProduct LobProduct { get; set; } = default!;
    public ICollection<LobSchemaBundle> SchemaBundles { get; set; } = [];
}
