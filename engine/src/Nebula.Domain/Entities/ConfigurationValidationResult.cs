namespace Nebula.Domain.Entities;

public class ConfigurationValidationResult : BaseEntity
{
    public Guid DraftId { get; set; }
    public string Status { get; set; } = "Pending";
    public string DraftPayloadHash { get; set; } = string.Empty;
    public string BlockingErrorsJson { get; set; } = "[]";
    public string WarningsJson { get; set; } = "[]";
    public string CompareSummaryJson { get; set; } = "[]";

    public ConfigurationDraft? Draft { get; set; }
}
