namespace Nebula.Domain.Entities;

public class ExpectedCommission : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid? PolicyVersionId { get; set; }
    public Guid CarrierMarketId { get; set; }
    public Guid? CommissionScheduleId { get; set; }
    public Guid? ProducerSplitAssignmentId { get; set; }
    public decimal? PremiumBasisAmount { get; set; }
    public decimal? ExpectedGrossCommission { get; set; }
    public decimal ApprovedAdjustmentTotal { get; set; }
    public decimal? AdjustedExpectedCommission { get; set; }
    public string Status { get; set; } = "MissingInputs";
    public string ExceptionState { get; set; } = "missing_schedule";
    public string SourceSnapshotJson { get; set; } = "{}";
    public DateTime? CalculatedAt { get; set; }
    public ICollection<CommissionAdjustment> Adjustments { get; set; } = new List<CommissionAdjustment>();
}
