namespace Nebula.Domain.Entities;

public class SubmissionQuotePacket : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public string Status { get; set; } = "Draft";
    public string LinkedDocumentRefsJson { get; set; } = "[]";
    public decimal? RecordedPremiumAmount { get; set; }
    public string? RecordedLimits { get; set; }
    public string? RecordedDeductibles { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? CarrierMarket { get; set; }
    public string ReadinessState { get; set; } = "Draft";
    public DateTime? ReadyAt { get; set; }
    public Guid? ReadyByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public Submission Submission { get; set; } = default!;
}
