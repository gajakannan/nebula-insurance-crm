namespace Nebula.Domain.Entities;

public class PaymentReceipt : BaseEntity
{
    public string Source { get; set; } = "Manual";
    public string ExternalReference { get; set; } = string.Empty;
    public string NormalizedExternalReference { get; set; } = string.Empty;
    public DateOnly ReceivedDate { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
    public string? InvoiceReference { get; set; }
    public string? Memo { get; set; }
    public Guid? ImportBatchId { get; set; }
    public int? ImportRowNumber { get; set; }
    public string ApplicationStatus { get; set; } = "Unapplied";

    public PaymentReceiptImportBatch? ImportBatch { get; set; }
    public PaymentApplication? PaymentApplication { get; set; }
    public ICollection<ReconciliationException> ReconciliationExceptions { get; set; } = [];
}
