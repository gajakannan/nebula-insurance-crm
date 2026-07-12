namespace Nebula.Domain.Entities;

public class ConfigurationDraft : BaseEntity
{
    public string DomainKey { get; set; } = string.Empty;
    public int BasePublishedVersion { get; set; }
    public int DraftVersion { get; set; }
    public string Status { get; set; } = "Draft";
    public string PayloadJson { get; set; } = "{}";
    public string PayloadHash { get; set; } = string.Empty;

    public ConfigurationDomain? Domain { get; set; }
    public ICollection<ConfigurationValidationResult> ValidationResults { get; set; } = [];
}
