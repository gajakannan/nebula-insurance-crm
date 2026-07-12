namespace Nebula.Domain.Entities;

public class ConfigurationRefreshStatus : BaseEntity
{
    public Guid PublishedSetId { get; set; }
    public string ConsumerKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? RefreshedAt { get; set; }
    public string? ErrorSummary { get; set; }

    public PublishedOperationalConfigurationSet? PublishedSet { get; set; }
}
