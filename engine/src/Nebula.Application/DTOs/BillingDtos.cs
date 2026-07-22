using Nebula.Application.Common;

namespace Nebula.Application.DTOs;

public record BillingInvoiceSearchQuery(
    string? Q,
    Guid? PolicyId,
    Guid? AccountId,
    string? Status,
    bool? HasOpenException,
    int Page = 1,
    int PageSize = 25);

public record BillingInvoiceCreateRequestDto(
    string InvoiceNumber,
    Guid PolicyId,
    Guid PolicyVersionId,
    Guid AccountId,
    string Currency,
    decimal OriginalAmount,
    DateOnly InvoiceDate,
    DateOnly DueDate);

public record BillingInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid PolicyId,
    Guid PolicyVersionId,
    Guid AccountId,
    string Currency,
    decimal OriginalAmount,
    decimal OutstandingAmount,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Status,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    string RowVersion);

public record PaymentReceiptSearchQuery(
    string? ApplicationStatus,
    string? ExternalReference,
    string? Currency,
    int Page = 1,
    int PageSize = 25);

public record PaymentReceiptCreateRequestDto(
    string ExternalReference,
    DateOnly ReceivedDate,
    string Currency,
    decimal Amount,
    string? InvoiceReference,
    string? Memo);

public record PaymentReceiptDto(
    Guid Id,
    string Source,
    string ExternalReference,
    DateOnly ReceivedDate,
    string Currency,
    decimal Amount,
    string? InvoiceReference,
    string? Memo,
    Guid? ImportBatchId,
    int? ImportRowNumber,
    string ApplicationStatus,
    string RowVersion);

public record PaymentReceiptImportRowOutcomeDto(
    int RowNumber,
    string? ExternalReference,
    string Outcome,
    Guid? PaymentReceiptId,
    string? ReasonCode,
    string? ReasonDetail);

public record PaymentReceiptImportResultDto(
    Guid ImportBatchId,
    string ContractVersion,
    string FileName,
    string FileSha256,
    string Status,
    int SubmittedCount,
    int CreatedCount,
    int DuplicateCount,
    int RejectedCount,
    IReadOnlyList<PaymentReceiptImportRowOutcomeDto> Outcomes);

public record PaymentApplicationRequestDto(
    Guid BillingInvoiceId,
    Guid PaymentReceiptId,
    string PaymentReceiptRowVersion);

public record PaymentApplicationDto(
    Guid Id,
    Guid BillingInvoiceId,
    Guid PaymentReceiptId,
    string Currency,
    decimal AppliedAmount,
    decimal InvoiceOutstandingBefore,
    decimal InvoiceOutstandingAfter,
    DateTime AppliedAt,
    Guid AppliedByUserId);

public record ReconciliationExceptionSearchQuery(
    string? Status,
    string? Type,
    int Page = 1,
    int PageSize = 25);

public record ReconciliationExceptionDto(
    Guid Id,
    string Type,
    Guid? BillingInvoiceId,
    Guid? PaymentReceiptId,
    Guid? ImportBatchId,
    Guid? ImportRowOutcomeId,
    string Status,
    DateTime OpenedAt,
    Guid OpenedByUserId,
    DateTime? ResolvedAt,
    Guid? ResolvedByUserId,
    string? ResolutionCode,
    string? ResolutionNote,
    BillingCorrectionDto? PendingCorrection,
    string RowVersion);

public record ReconciliationReferenceCorrectionRequestDto(
    Guid BillingInvoiceId,
    string ResolutionCode,
    string ResolutionNote);

public record BillingCorrectionRequestDto(
    decimal CorrectionAmount,
    decimal ProposedOutstandingAmount,
    string Reason,
    string EvidenceNote);

public record BillingCorrectionDecisionRequestDto(string Decision, string DecisionNote);

public record BillingCorrectionDto(
    Guid Id,
    Guid ReconciliationExceptionId,
    Guid BillingInvoiceId,
    decimal BeforeOutstandingAmount,
    decimal CorrectionAmount,
    decimal ProposedOutstandingAmount,
    string Reason,
    string EvidenceNote,
    string Status,
    Guid RequestedByUserId,
    DateTime RequestedAt,
    Guid? DecisionByUserId,
    DateTime? DecisionAt,
    string? DecisionNote,
    string RowVersion);

public record BillingInvoiceDetailDto(
    BillingInvoiceDto Invoice,
    IReadOnlyList<PaymentApplicationDto> Applications,
    IReadOnlyList<PaymentReceiptDto> Receipts,
    IReadOnlyList<ReconciliationExceptionDto> Exceptions,
    IReadOnlyList<TimelineEventDto> AuditEvents);

public record ReconciliationBacklogRowDto(string Type, int Count);

public record ReconciliationBacklogResponseDto(
    int OpenCount,
    int ExactApplicationCount,
    int PendingCorrectionCount,
    int RejectedImportRowCount,
    int DuplicateImportRowCount,
    int? OldestOpenDays,
    IReadOnlyList<ReconciliationBacklogRowDto> ByType);

public record PolicyBillingSummaryDto(
    Guid PolicyId,
    string Currency,
    int InvoiceCount,
    int OutstandingInvoiceCount,
    decimal OutstandingAmount,
    DateOnly? NextDueDate,
    DateTime AsOf);

public record BillingInvoiceSearchResult(PaginatedResult<BillingInvoiceDto> Page);
public record PaymentReceiptSearchResult(PaginatedResult<PaymentReceiptDto> Page);
public record ReconciliationExceptionSearchResult(PaginatedResult<ReconciliationExceptionDto> Page);
