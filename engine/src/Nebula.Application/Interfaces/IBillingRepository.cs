using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public record BillingPolicyContext(
    Guid PolicyId,
    Guid PolicyVersionId,
    Guid AccountId,
    string Currency);

public interface IBillingRepository
{
    Task<PaginatedResult<BillingInvoice>> SearchInvoicesAsync(BillingInvoiceSearchQuery query, ICurrentUserService user, CancellationToken ct = default);
    Task<BillingInvoice?> GetInvoiceDetailAsync(Guid invoiceId, ICurrentUserService user, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityTimelineEvent>> GetInvoiceAuditEventsAsync(Guid invoiceId, ICurrentUserService user, CancellationToken ct = default);
    Task<BillingInvoice?> GetInvoiceForMutationAsync(Guid invoiceId, ICurrentUserService user, CancellationToken ct = default);
    Task<BillingPolicyContext?> GetPolicyContextAsync(Guid policyId, Guid policyVersionId, ICurrentUserService user, CancellationToken ct = default);
    Task<bool> InvoiceNumberExistsAsync(string normalizedInvoiceNumber, CancellationToken ct = default);
    Task AddInvoiceAsync(BillingInvoice invoice, CancellationToken ct = default);

    Task<PaginatedResult<PaymentReceipt>> SearchReceiptsAsync(PaymentReceiptSearchQuery query, ICurrentUserService user, CancellationToken ct = default);
    Task<PaymentReceipt?> GetReceiptForMutationAsync(Guid receiptId, ICurrentUserService user, CancellationToken ct = default);
    Task<bool> ReceiptReferenceExistsAsync(string source, string normalizedExternalReference, CancellationToken ct = default);
    Task AddReceiptAsync(PaymentReceipt receipt, CancellationToken ct = default);
    Task AddImportBatchAsync(PaymentReceiptImportBatch batch, CancellationToken ct = default);
    Task<PaymentReceiptImportBatch?> GetImportBatchAsync(Guid batchId, ICurrentUserService user, CancellationToken ct = default);

    Task AddApplicationAsync(PaymentApplication application, CancellationToken ct = default);
    Task<ReconciliationException?> FindOpenExceptionAsync(Guid? invoiceId, Guid? receiptId, string type, CancellationToken ct = default);
    Task AddExceptionAsync(ReconciliationException exception, CancellationToken ct = default);
    Task<PaginatedResult<ReconciliationException>> SearchExceptionsAsync(ReconciliationExceptionSearchQuery query, ICurrentUserService user, CancellationToken ct = default);
    Task<ReconciliationException?> GetExceptionForMutationAsync(Guid exceptionId, ICurrentUserService user, CancellationToken ct = default);

    Task<bool> PendingCorrectionExistsAsync(Guid exceptionId, CancellationToken ct = default);
    Task AddCorrectionAsync(BillingCorrection correction, CancellationToken ct = default);
    Task<BillingCorrection?> GetCorrectionForMutationAsync(Guid correctionId, ICurrentUserService user, CancellationToken ct = default);

    Task<ReconciliationBacklogResponseDto> GetBacklogAsync(ICurrentUserService user, CancellationToken ct = default);
    Task<PolicyBillingSummaryDto?> GetPolicyBillingSummaryAsync(Guid policyId, ICurrentUserService user, CancellationToken ct = default);
}
