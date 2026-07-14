namespace Nebula.Domain.Entities;

public class CommissionAdjustment : BaseEntity
{
    public Guid ExpectedCommissionId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
    public ExpectedCommission? ExpectedCommission { get; set; }
}
