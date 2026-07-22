namespace Nebula.Domain.Entities;

public class ReconciliationException : BaseEntity
{
    public string Type { get; set; } = "InvalidSourceData";
    public Guid? BillingInvoiceId { get; set; }
    public Guid? PaymentReceiptId { get; set; }
    public Guid? ImportBatchId { get; set; }
    public Guid? ImportRowOutcomeId { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime OpenedAt { get; set; }
    public Guid OpenedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionCode { get; set; }
    public string? ResolutionNote { get; set; }

    public BillingInvoice? BillingInvoice { get; set; }
    public PaymentReceipt? PaymentReceipt { get; set; }
    public PaymentReceiptImportBatch? ImportBatch { get; set; }
    public PaymentReceiptImportRowOutcome? ImportRowOutcome { get; set; }
    public ICollection<BillingCorrection> Corrections { get; set; } = [];
}
