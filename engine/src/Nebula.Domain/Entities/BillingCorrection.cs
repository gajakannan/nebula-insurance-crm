namespace Nebula.Domain.Entities;

public class BillingCorrection : BaseEntity
{
    public Guid ReconciliationExceptionId { get; set; }
    public Guid BillingInvoiceId { get; set; }
    public decimal BeforeOutstandingAmount { get; set; }
    public decimal CorrectionAmount { get; set; }
    public decimal ProposedOutstandingAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EvidenceNote { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? DecisionByUserId { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? DecisionNote { get; set; }

    public ReconciliationException ReconciliationException { get; set; } = default!;
    public BillingInvoice BillingInvoice { get; set; } = default!;
}
