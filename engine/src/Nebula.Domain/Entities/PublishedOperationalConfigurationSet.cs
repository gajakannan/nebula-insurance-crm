namespace Nebula.Domain.Entities;

public class PublishedOperationalConfigurationSet : BaseEntity
{
    public string DomainKey { get; set; } = string.Empty;
    public int PublishedVersion { get; set; }
    public string PayloadSnapshotJson { get; set; } = "{}";
    public string PayloadHash { get; set; } = string.Empty;
    public Guid PublishedByUserId { get; set; }
    public string PublishReason { get; set; } = string.Empty;

    public ConfigurationDomain? Domain { get; set; }
    public ICollection<ConfigurationRefreshStatus> RefreshStatuses { get; set; } = [];
}
