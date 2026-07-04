namespace Nebula.Domain.Entities;

public class BrokerInsightProjection : BaseEntity
{
    public Guid BrokerId { get; set; }
    public string BrokerName { get; set; } = string.Empty;
    public string MetricKey { get; set; } = string.Empty;
    public string MetricLabel { get; set; } = string.Empty;
    public string MetricFamily { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string? Bucket { get; set; }
    public decimal? Value { get; set; }
    public int Denominator { get; set; }
    public string Unit { get; set; } = "count";
    public decimal? ComparisonValue { get; set; }
    public DateOnly? ComparisonPeriodStart { get; set; }
    public DateOnly? ComparisonPeriodEnd { get; set; }
    public string SourceObjectTypesJson { get; set; } = "[]";
    public int SourceRecordCount { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? ProducerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public DateTimeOffset LastSourceUpdatedAt { get; set; }
    public DateTimeOffset ProjectedAt { get; set; }
    public string ProjectionStatus { get; set; } = "Available";
}
