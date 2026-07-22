namespace Nebula.Domain.Entities;

public class PaymentReceiptImportBatch : BaseEntity
{
    public string ContractVersion { get; set; } = "mock-payment-receipt-row-v1";
    public string FileName { get; set; } = string.Empty;
    public string FileSha256 { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public int SubmittedCount { get; set; }
    public int CreatedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int RejectedCount { get; set; }
    public DateTime ImportedAt { get; set; }
    public Guid ImportedByUserId { get; set; }

    public ICollection<PaymentReceiptImportRowOutcome> Outcomes { get; set; } = [];
    public ICollection<PaymentReceipt> Receipts { get; set; } = [];
}
