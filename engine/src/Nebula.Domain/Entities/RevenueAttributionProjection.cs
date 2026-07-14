namespace Nebula.Domain.Entities;

public class RevenueAttributionProjection : BaseEntity
{
    public Guid ExpectedCommissionId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? ProducerId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid CarrierMarketId { get; set; }
    public DateOnly PolicyPeriodStart { get; set; }
    public DateOnly PolicyPeriodEnd { get; set; }
    public string LineOfBusiness { get; set; } = string.Empty;
    public decimal ExpectedGrossCommission { get; set; }
    public decimal ApprovedAdjustmentTotal { get; set; }
    public decimal AdjustedExpectedCommission { get; set; }
    public decimal ProducerAllocationAmount { get; set; }
    public int UnresolvedExceptionCount { get; set; }
    public DateTime SourceRefreshedAt { get; set; }
}
