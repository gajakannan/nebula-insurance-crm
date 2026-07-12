namespace Nebula.Domain.Entities;

public class ConfigurationDomain : BaseEntity
{
    public string DomainKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string OwningModule { get; set; } = string.Empty;
    public string Status { get; set; } = "Supported";
    public string EditableSchemaRef { get; set; } = string.Empty;
    public bool SupportsRollback { get; set; }

    public ICollection<ConfigurationDraft> Drafts { get; set; } = [];
    public ICollection<PublishedOperationalConfigurationSet> PublishedSets { get; set; } = [];
    public ICollection<ConfigurationAuditEvent> AuditEvents { get; set; } = [];
}
