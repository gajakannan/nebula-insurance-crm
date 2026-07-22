export interface PaginatedResponse<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface BillingInvoiceSearchParams {
  q?: string
  status?: 'Outstanding' | 'Reconciled' | 'All'
  hasOpenException?: boolean
  page?: number
  pageSize?: number
}

export interface BillingInvoiceCreateRequest {
  invoiceNumber: string
  policyId: string
  policyVersionId: string
  accountId: string
  currency: string
  originalAmount: number
  invoiceDate: string
  dueDate: string
}

export interface BillingInvoice extends BillingInvoiceCreateRequest {
  id: string
  outstandingAmount: number
  status: 'Outstanding' | 'Reconciled'
  createdAt: string
  createdByUserId: string
  rowVersion: string
}

export interface PaymentReceiptSearchParams {
  applicationStatus?: 'Unapplied' | 'Applied' | 'All'
  externalReference?: string
  currency?: string
  page?: number
  pageSize?: number
}

export interface PaymentReceiptCreateRequest {
  externalReference: string
  receivedDate: string
  currency: string
  amount: number
  invoiceReference: string | null
  memo: string | null
}

export interface PaymentReceipt extends PaymentReceiptCreateRequest {
  id: string
  source: 'Manual' | 'MockVendorCsv'
  importBatchId: string | null
  importRowNumber: number | null
  applicationStatus: 'Unapplied' | 'Applied'
  rowVersion: string
}

export interface PaymentReceiptImportOutcome {
  rowNumber: number
  externalReference: string | null
  outcome: 'Created' | 'Duplicate' | 'Rejected'
  paymentReceiptId: string | null
  reasonCode: string | null
  reasonDetail: string | null
}

export interface PaymentReceiptImportResult {
  importBatchId: string
  contractVersion: 'mock-payment-receipt-row-v1'
  fileName: string
  fileSha256: string
  status: 'Completed' | 'Rejected'
  submittedCount: number
  createdCount: number
  duplicateCount: number
  rejectedCount: number
  outcomes: PaymentReceiptImportOutcome[]
}

export interface PaymentApplication {
  id: string
  billingInvoiceId: string
  paymentReceiptId: string
  currency: string
  appliedAmount: number
  invoiceOutstandingBefore: number
  invoiceOutstandingAfter: 0
  appliedAt: string
  appliedByUserId: string
}

export type ReconciliationExceptionType =
  | 'MissingInvoiceReference'
  | 'InvoiceReferenceConflict'
  | 'AmountMismatch'
  | 'CurrencyMismatch'
  | 'DuplicateReceipt'
  | 'InvalidSourceData'

export interface ReconciliationException {
  id: string
  type: ReconciliationExceptionType
  billingInvoiceId: string | null
  paymentReceiptId: string | null
  importBatchId: string | null
  importRowOutcomeId: string | null
  status: 'Open' | 'Resolved'
  openedAt: string
  openedByUserId: string
  resolvedAt: string | null
  resolvedByUserId: string | null
  resolutionCode: string | null
  resolutionNote: string | null
  pendingCorrection: BillingCorrection | null
  rowVersion: string
}

export interface BillingCorrection {
  id: string
  reconciliationExceptionId: string
  billingInvoiceId: string
  beforeOutstandingAmount: number
  correctionAmount: number
  proposedOutstandingAmount: number
  reason: string
  evidenceNote: string
  status: 'Pending' | 'Approved' | 'Rejected'
  requestedByUserId: string
  requestedAt: string
  decisionByUserId: string | null
  decisionAt: string | null
  decisionNote: string | null
  rowVersion: string
}

export interface TimelineEvent {
  id: string
  entityType: string
  entityId: string
  eventType: string
  eventDescription: string | null
  entityName: string | null
  actorDisplayName: string | null
  occurredAt: string
}

export interface BillingInvoiceDetail {
  invoice: BillingInvoice
  applications: PaymentApplication[]
  receipts: PaymentReceipt[]
  exceptions: ReconciliationException[]
  auditEvents: TimelineEvent[]
}

export interface ReconciliationBacklog {
  openCount: number
  exactApplicationCount: number
  pendingCorrectionCount: number
  rejectedImportRowCount: number
  duplicateImportRowCount: number
  oldestOpenDays: number | null
  byType: Array<{ type: ReconciliationExceptionType; count: number }>
}

export interface PolicyBillingSummary {
  policyId: string
  currency: string
  invoiceCount: number
  outstandingInvoiceCount: number
  outstandingAmount: number
  nextDueDate: string | null
  asOf: string
}
