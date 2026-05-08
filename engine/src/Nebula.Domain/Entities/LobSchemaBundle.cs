namespace Nebula.Domain.Entities;

public class LobSchemaBundle : BaseEntity
{
    public Guid LobProductVersionId { get; set; }
    public string SchemaVersion { get; set; } = default!;
    public string Status { get; set; } = "Draft";
    public string DataSchemaJson { get; set; } = "{}";
    public string UiSchemaJson { get; set; } = "{}";
    public string RulesJson { get; set; } = "{}";
    public string ProjectionMapJson { get; set; } = "{}";
    public string ContentHash { get; set; } = default!;
    public DateTime? ActivatedAt { get; set; }
    public Guid? ActivatedByUserId { get; set; }
    public DateTime? RetiredAt { get; set; }
    public Guid? RetiredByUserId { get; set; }

    public LobProductVersion LobProductVersion { get; set; } = default!;
    public ICollection<LobBundleActivationEvent> ActivationEvents { get; set; } = [];
}
