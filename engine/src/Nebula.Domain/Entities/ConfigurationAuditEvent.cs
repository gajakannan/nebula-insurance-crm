namespace Nebula.Domain.Entities;

public class ConfigurationAuditEvent : BaseEntity
{
    public string DomainKey { get; set; } = string.Empty;
    public Guid? DraftId { get; set; }
    public Guid? PublishedSetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string SummaryJson { get; set; } = "{}";

    public ConfigurationDomain? Domain { get; set; }
}
