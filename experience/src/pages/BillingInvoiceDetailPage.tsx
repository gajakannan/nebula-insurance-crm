import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, CheckCircle2, RefreshCw } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Badge } from '@/components/ui/Badge'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Skeleton } from '@/components/ui/Skeleton'
import {
  useApplyExactPayment,
  useBillingInvoice,
  usePaymentReceipts,
  usePolicyBillingSummary,
} from '@/features/billing'
import type { PaymentReceipt } from '@/features/billing'
import {
  MutationFeedback,
  TextField,
} from '@/features/billing/components/BillingUi'
import { dateTime, money, primaryButtonClass } from '@/features/billing/presentation'
import { useExpectedCommissions } from '@/features/commissions'

export default function BillingInvoiceDetailPage() {
  const { invoiceId } = useParams()
  const invoiceQuery = useBillingInvoice(invoiceId)

  return (
    <DashboardLayout title="Billing Invoice">
      <Link to="/billing" className="mb-4 inline-flex min-h-11 items-center gap-2 text-sm font-medium text-text-secondary hover:text-text-primary">
        <ArrowLeft size={16} /> Billing workspace
      </Link>
      {invoiceQuery.isLoading && <Skeleton className="h-[480px] w-full rounded-xl" />}
      {invoiceQuery.isError && <ErrorFallback message="Unable to load this source-authorized invoice." onRetry={() => invoiceQuery.refetch()} />}
      {invoiceQuery.data && <InvoiceWorkspace invoice={invoiceQuery.data} />}
    </DashboardLayout>
  )
}

function InvoiceWorkspace({ invoice: detail }: { invoice: NonNullable<ReturnType<typeof useBillingInvoice>['data']> }) {
  const { invoice } = detail
  const summary = usePolicyBillingSummary(invoice.policyId)
  const commissions = useExpectedCommissions({ policyId: invoice.policyId, pageSize: 20 })
  const [receiptSearch, setReceiptSearch] = useState('')
  const receipts = usePaymentReceipts({ applicationStatus: 'Unapplied', externalReference: receiptSearch, currency: invoice.currency, pageSize: 25 })
  const apply = useApplyExactPayment()

  function applyReceipt(receipt: PaymentReceipt) {
    apply.mutate({ invoiceId: invoice.id, invoiceRowVersion: invoice.rowVersion, receipt })
  }

  return (
    <div className="space-y-5">
      <Card>
        <CardHeader>
          <CardTitle>{invoice.invoiceNumber}</CardTitle>
          <Badge variant={invoice.status === 'Reconciled' ? 'success' : 'warning'}>{invoice.status}</Badge>
        </CardHeader>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <Metric label="Original amount" value={money(invoice.originalAmount, invoice.currency)} />
          <Metric label="Outstanding" value={money(invoice.outstandingAmount, invoice.currency)} />
          <Metric label="Invoice date" value={invoice.invoiceDate} />
          <Metric label="Due date" value={invoice.dueDate} />
          <Metric label="Policy ID" value={invoice.policyId} mono />
          <Metric label="Policy version ID" value={invoice.policyVersionId} mono />
          <Metric label="Account ID" value={invoice.accountId} mono />
          <Metric label="Created" value={dateTime(invoice.createdAt)} />
        </div>
      </Card>

      <Card>
        <CardHeader><CardTitle>Expected Commission Context</CardTitle><Badge variant="info">Read-only</Badge></CardHeader>
        {commissions.isLoading && <Skeleton className="h-24 w-full rounded-lg" />}
        {commissions.isError && <ErrorFallback message="Unable to load source-authorized expected commission context." onRetry={() => commissions.refetch()} />}
        {commissions.data?.data.length === 0 && <p className="text-sm text-text-muted">No expected commission record is available for this policy.</p>}
        {commissions.data && commissions.data.data.length > 0 && (
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            {commissions.data.data.map((commission) => (
              <div key={commission.id} className="rounded-lg border border-surface-border bg-surface-card p-3">
                <p className="text-sm font-medium text-text-primary">{commission.producerDisplayName ?? 'Unassigned producer'}</p>
                <p className="mt-1 text-xs text-text-muted">{commission.status} · {commission.exceptionState}</p>
                <p className="mt-3 text-lg font-semibold text-text-primary">
                  {commission.adjustedExpectedCommission === null ? 'Unavailable' : money(commission.adjustedExpectedCommission, invoice.currency)}
                </p>
              </div>
            ))}
          </div>
        )}
      </Card>

      <Card>
        <CardHeader><CardTitle>Applications and Receipt Provenance</CardTitle><Badge variant="info">Immutable evidence</Badge></CardHeader>
        {detail.applications.length === 0 && <p className="text-sm text-text-muted">No exact payment has been applied.</p>}
        {detail.applications.map((application) => {
          const receipt = detail.receipts.find((candidate) => candidate.id === application.paymentReceiptId)
          return (
            <article key={application.id} className="grid gap-3 rounded-lg border border-surface-border bg-surface-card p-3 sm:grid-cols-2 xl:grid-cols-4">
              <Metric label="Applied amount" value={money(application.appliedAmount, application.currency)} />
              <Metric label="Applied at" value={dateTime(application.appliedAt)} />
              <Metric label="Receipt reference" value={receipt?.externalReference ?? application.paymentReceiptId} mono />
              <Metric label="Balance transition" value={`${money(application.invoiceOutstandingBefore, application.currency)} → ${money(application.invoiceOutstandingAfter, application.currency)}`} />
            </article>
          )
        })}
      </Card>

      <Card>
        <CardHeader><CardTitle>Reconciliation Exceptions</CardTitle><Link to="/billing/reconciliation" className="text-sm font-medium text-nebula-violet">Open reconciliation workspace</Link></CardHeader>
        {detail.exceptions.length === 0 && <p className="text-sm text-text-muted">No reconciliation exceptions are linked to this invoice.</p>}
        <div className="space-y-3">
          {detail.exceptions.map((exception) => (
            <article key={exception.id} className="rounded-lg border border-surface-border bg-surface-card p-3">
              <div className="flex flex-wrap items-center justify-between gap-3"><p className="text-sm font-medium text-text-primary">{exception.type}</p><Badge variant={exception.status === 'Open' ? 'warning' : 'success'}>{exception.status}</Badge></div>
              <p className="mt-2 text-xs text-text-muted">Opened {dateTime(exception.openedAt)}{exception.pendingCorrection ? ` · correction ${exception.pendingCorrection.status}` : ''}</p>
            </article>
          ))}
        </div>
      </Card>

      <Card>
        <CardHeader><CardTitle>Permitted Audit History</CardTitle><Badge variant="info">Newest first</Badge></CardHeader>
        {detail.auditEvents.length === 0 && <p className="text-sm text-text-muted">No permitted audit events are available.</p>}
        <ol className="space-y-3">
          {detail.auditEvents.map((event) => (
            <li key={event.id} className="rounded-lg border border-surface-border bg-surface-card p-3">
              <p className="text-sm font-medium text-text-primary">{event.eventType}</p>
              <p className="mt-1 text-sm text-text-secondary">{event.eventDescription ?? 'Billing activity recorded.'}</p>
              <p className="mt-2 text-xs text-text-muted">{dateTime(event.occurredAt)} · {event.actorDisplayName ?? 'Unknown user'} · {event.entityType}</p>
            </li>
          ))}
        </ol>
      </Card>

      <Card>
        <CardHeader><CardTitle>Bounded Policy Billing Summary</CardTitle></CardHeader>
        {summary.isLoading && <Skeleton className="h-24 w-full rounded-lg" />}
        {summary.isError && <ErrorFallback message="Unable to load the bounded policy billing summary." onRetry={() => summary.refetch()} />}
        {summary.data && (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Metric label="Invoice count" value={String(summary.data.invoiceCount)} />
            <Metric label="Outstanding invoices" value={String(summary.data.outstandingInvoiceCount)} />
            <Metric label="Outstanding total" value={money(summary.data.outstandingAmount, summary.data.currency)} />
            <Metric label="Next due date" value={summary.data.nextDueDate ?? 'None'} />
          </div>
        )}
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Explicit Exact Application</CardTitle>
          <Link to="/billing/reconciliation" className="text-sm font-medium text-nebula-violet">Review exceptions</Link>
        </CardHeader>
        <p className="mb-4 text-sm text-text-muted">Select one unapplied receipt. Application succeeds only when currency matches, amount equals the full outstanding balance, and any source invoice reference agrees.</p>
        <div className="mb-4 max-w-md">
          <TextField label="Find receipt by external reference" type="search" value={receiptSearch} onChange={setReceiptSearch} placeholder="Receipt reference" />
        </div>
        {receipts.isLoading && <Skeleton className="h-32 w-full rounded-lg" />}
        {receipts.isError && <ErrorFallback message="Unable to load source-authorized unapplied receipts." onRetry={() => receipts.refetch()} />}
        {receipts.data?.data.length === 0 && (
          <div className="py-8 text-center">
            <p className="text-sm text-text-muted">No matching unapplied receipts are available.</p>
            <Link to="/billing" className="mt-3 inline-flex min-h-11 items-center text-sm font-medium text-nebula-violet">Record or import a receipt</Link>
          </div>
        )}
        {receipts.data && receipts.data.data.length > 0 && (
          <div className="space-y-3">
            {receipts.data.data.map((receipt) => (
              <article key={receipt.id} className="grid gap-3 rounded-lg border border-surface-border bg-surface-card p-3 lg:grid-cols-[1fr_1fr_1fr_auto] lg:items-center">
                <div><p className="text-sm font-medium text-text-primary">{receipt.externalReference}</p><p className="mt-1 text-xs text-text-muted">{receipt.source} · received {receipt.receivedDate}</p></div>
                <Metric label="Receipt amount" value={money(receipt.amount, receipt.currency)} />
                <Metric label="Source invoice reference" value={receipt.invoiceReference ?? 'Not supplied'} />
                <button
                  type="button"
                  onClick={() => applyReceipt(receipt)}
                  disabled={apply.isPending || invoice.status !== 'Outstanding'}
                  className={primaryButtonClass}
                >
                  {apply.isPending ? <RefreshCw size={16} className="animate-spin" /> : <CheckCircle2 size={16} />}
                  Apply exact payment
                </button>
              </article>
            ))}
          </div>
        )}
        <div className="mt-4">
          <MutationFeedback isPending={apply.isPending} isSuccess={apply.isSuccess} error={apply.error} pending="Applying the exact receipt…" success="Payment applied; refresh confirms the reconciled invoice state." failure="The receipt was not applied." />
        </div>
      </Card>
    </div>
  )
}

function Metric({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return <div className="min-w-0"><p className="text-xs text-text-muted">{label}</p><p className={`mt-1 truncate text-sm font-medium text-text-primary ${mono ? 'font-mono' : ''}`}>{value}</p></div>
}
