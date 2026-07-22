namespace Nebula.Domain.Entities;

public class PaymentApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BillingInvoiceId { get; set; }
    public Guid PaymentReceiptId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal AppliedAmount { get; set; }
    public decimal InvoiceOutstandingBefore { get; set; }
    public decimal InvoiceOutstandingAfter { get; set; }
    public DateTime AppliedAt { get; set; }
    public Guid AppliedByUserId { get; set; }

    public BillingInvoice BillingInvoice { get; set; } = default!;
    public PaymentReceipt PaymentReceipt { get; set; } = default!;
}
