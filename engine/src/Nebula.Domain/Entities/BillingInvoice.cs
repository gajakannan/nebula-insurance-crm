namespace Nebula.Domain.Entities;

public class BillingInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string NormalizedInvoiceNumber { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public Guid PolicyVersionId { get; set; }
    public Guid AccountId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal OriginalAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "Outstanding";

    public Policy Policy { get; set; } = default!;
    public PolicyVersion PolicyVersion { get; set; } = default!;
    public Account Account { get; set; } = default!;
    public PaymentApplication? PaymentApplication { get; set; }
    public ICollection<ReconciliationException> ReconciliationExceptions { get; set; } = [];
    public ICollection<BillingCorrection> Corrections { get; set; } = [];
}
