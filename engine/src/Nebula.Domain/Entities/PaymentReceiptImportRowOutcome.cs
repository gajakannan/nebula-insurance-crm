namespace Nebula.Domain.Entities;

public class PaymentReceiptImportRowOutcome : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public int RowNumber { get; set; }
    public string? ExternalReference { get; set; }
    public string Outcome { get; set; } = "Rejected";
    public Guid? PaymentReceiptId { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonDetail { get; set; }

    public PaymentReceiptImportBatch ImportBatch { get; set; } = default!;
    public PaymentReceipt? PaymentReceipt { get; set; }
}
