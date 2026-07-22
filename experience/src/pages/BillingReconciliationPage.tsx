import { FormEvent, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Check, Save, X } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Badge } from '@/components/ui/Badge'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Skeleton } from '@/components/ui/Skeleton'
import {
  useCorrectReconciliationReference,
  useDecideBillingCorrection,
  useReconciliationBacklog,
  useReconciliationExceptions,
  useRequestBillingCorrection,
} from '@/features/billing'
import type { ReconciliationException, ReconciliationExceptionType } from '@/features/billing'
import {
  MutationFeedback,
  SelectField,
  TextField,
} from '@/features/billing/components/BillingUi'
import { dateTime, primaryButtonClass, secondaryButtonClass } from '@/features/billing/presentation'

const EXCEPTION_TYPES: Array<ReconciliationExceptionType | 'All'> = [
  'All', 'MissingInvoiceReference', 'InvoiceReferenceConflict', 'AmountMismatch',
  'CurrencyMismatch', 'DuplicateReceipt', 'InvalidSourceData',
]

export default function BillingReconciliationPage() {
  const [status, setStatus] = useState('Open')
  const [type, setType] = useState<ReconciliationExceptionType | 'All'>('All')
  const backlog = useReconciliationBacklog()
  const exceptions = useReconciliationExceptions({ status: status as 'Open' | 'Resolved' | 'All', type, pageSize: 40 })

  return (
    <DashboardLayout title="Billing Reconciliation">
      <Link to="/billing" className="mb-4 inline-flex min-h-11 items-center gap-2 text-sm font-medium text-text-secondary hover:text-text-primary">
        <ArrowLeft size={16} /> Billing workspace
      </Link>
      <div className="space-y-5">
        <Card>
          <CardHeader><CardTitle>Source-Filtered Backlog</CardTitle><Badge variant="info">Post-authorization totals</Badge></CardHeader>
          {backlog.isLoading && <Skeleton className="h-24 w-full rounded-lg" />}
          {backlog.isError && <ErrorFallback message="Unable to load reconciliation backlog totals." onRetry={() => backlog.refetch()} />}
          {backlog.data && (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <Summary label="Open exceptions" value={String(backlog.data.openCount)} />
              <Summary label="Exact applications" value={String(backlog.data.exactApplicationCount)} />
              <Summary label="Pending corrections" value={String(backlog.data.pendingCorrectionCount)} />
              <Summary label="Rejected import rows" value={String(backlog.data.rejectedImportRowCount)} />
              <Summary label="Duplicate import rows" value={String(backlog.data.duplicateImportRowCount)} />
              <Summary label="Oldest open" value={backlog.data.oldestOpenDays === null ? 'None' : `${backlog.data.oldestOpenDays} days`} />
              {backlog.data.byType.slice(0, 2).map((row) => <Summary key={row.type} label={humanize(row.type)} value={String(row.count)} />)}
            </div>
          )}
        </Card>

        <Card>
          <CardHeader><CardTitle>Exception Review</CardTitle>{exceptions.data && <span className="text-sm text-text-muted">{exceptions.data.totalCount} authorized records</span>}</CardHeader>
          <div className="mb-4 grid gap-3 sm:grid-cols-2">
            <SelectField label="Status" value={status} options={['Open', 'Resolved', 'All']} onChange={setStatus} />
            <SelectField label="Exception type" value={type} options={EXCEPTION_TYPES} onChange={(value) => setType(value as ReconciliationExceptionType | 'All')} />
          </div>
          {exceptions.isLoading && <div className="space-y-3">{Array.from({ length: 4 }).map((_, index) => <Skeleton key={index} className="h-36 w-full rounded-lg" />)}</div>}
          {exceptions.isError && <ErrorFallback message="Unable to load source-authorized exceptions." onRetry={() => exceptions.refetch()} />}
          {exceptions.data?.data.length === 0 && <p className="py-10 text-center text-sm text-text-muted">No exceptions match the selected filters. Change the filters to inspect resolved work.</p>}
          {exceptions.data && exceptions.data.data.length > 0 && (
            <div className="space-y-3">{exceptions.data.data.map((exception) => <ExceptionCard key={exception.id} exception={exception} />)}</div>
          )}
        </Card>
      </div>
    </DashboardLayout>
  )
}

function ExceptionCard({ exception }: { exception: ReconciliationException }) {
  const correct = useCorrectReconciliationReference()
  const request = useRequestBillingCorrection()
  const decide = useDecideBillingCorrection()
  const [reference, setReference] = useState({ billingInvoiceId: exception.billingInvoiceId ?? '', resolutionCode: 'ReferenceCorrected', resolutionNote: '' })
  const [correction, setCorrection] = useState({ correctionAmount: 0, proposedOutstandingAmount: 0, reason: '', evidenceNote: '' })
  const [decision, setDecision] = useState({ decision: 'Approve' as 'Approve' | 'Reject', decisionNote: '' })
  const referenceCorrectable = exception.type === 'MissingInvoiceReference' || exception.type === 'InvoiceReferenceConflict'

  function submitReference(event: FormEvent) {
    event.preventDefault()
    correct.mutate({ exceptionId: exception.id, rowVersion: exception.rowVersion, ...reference })
  }
  function submitCorrection(event: FormEvent) {
    event.preventDefault()
    request.mutate({ exceptionId: exception.id, rowVersion: exception.rowVersion, ...correction })
  }
  function submitDecision(event: FormEvent) {
    event.preventDefault()
    if (!exception.pendingCorrection) return
    decide.mutate({
      correctionId: exception.pendingCorrection.id,
      rowVersion: exception.pendingCorrection.rowVersion,
      ...decision,
    })
  }

  return (
    <article className="rounded-lg border border-surface-border bg-surface-card p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-text-primary">{humanize(exception.type)}</h3>
          <p className="mt-1 text-xs text-text-muted">Opened {dateTime(exception.openedAt)} · ID {exception.id}</p>
          <p className="mt-1 text-xs text-text-muted">Invoice {exception.billingInvoiceId ?? 'unlinked'} · Receipt {exception.paymentReceiptId ?? 'unlinked'}</p>
        </div>
        <Badge variant={exception.status === 'Open' ? 'warning' : 'success'}>{exception.status}</Badge>
      </div>
      {exception.status === 'Resolved' && (
        <p className="mt-3 rounded-lg border border-surface-border bg-surface-highlight p-3 text-sm text-text-secondary">
          {exception.resolutionCode}: {exception.resolutionNote ?? 'No resolution note recorded.'}
        </p>
      )}
      {exception.status === 'Open' && referenceCorrectable && (
        <form className="mt-4 grid gap-3 lg:grid-cols-[1fr_180px_1.4fr_auto]" onSubmit={submitReference}>
          <TextField label="Correct billing invoice ID" value={reference.billingInvoiceId} onChange={(value) => setReference({ ...reference, billingInvoiceId: value })} required />
          <TextField label="Resolution code" value={reference.resolutionCode} onChange={(value) => setReference({ ...reference, resolutionCode: value })} required />
          <TextField label="Resolution note" value={reference.resolutionNote} onChange={(value) => setReference({ ...reference, resolutionNote: value })} required />
          <button type="submit" disabled={correct.isPending} className={`${primaryButtonClass} lg:mt-5`}><Save size={16} />Correct reference</button>
          <div className="lg:col-span-4"><MutationFeedback isPending={correct.isPending} isSuccess={correct.isSuccess} error={correct.error} pending="Correcting the reference…" success="Reference corrected without changing invoice balance." failure="Unable to correct the reference." /></div>
        </form>
      )}
      {exception.status === 'Open' && exception.pendingCorrection && (
        <section className="mt-4 rounded-lg border border-surface-border bg-surface-highlight p-3">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-sm font-semibold text-text-primary">Pending manager correction</p>
              <p className="mt-1 text-xs text-text-muted">Requested {dateTime(exception.pendingCorrection.requestedAt)} · ID {exception.pendingCorrection.id}</p>
            </div>
            <Badge variant="warning">Different principal required</Badge>
          </div>
          <div className="mt-3 grid gap-3 sm:grid-cols-3">
            <Summary label="Before" value={String(exception.pendingCorrection.beforeOutstandingAmount)} />
            <Summary label="Correction" value={String(exception.pendingCorrection.correctionAmount)} />
            <Summary label="Proposed" value={String(exception.pendingCorrection.proposedOutstandingAmount)} />
          </div>
          <p className="mt-3 text-sm text-text-secondary">{exception.pendingCorrection.reason} · {exception.pendingCorrection.evidenceNote}</p>
          <form className="mt-3 grid gap-3 lg:grid-cols-[180px_1fr_auto]" onSubmit={submitDecision}>
            <SelectField label="Decision" value={decision.decision} options={['Approve', 'Reject']} onChange={(value) => setDecision({ ...decision, decision: value as 'Approve' | 'Reject' })} />
            <TextField label="Decision note" value={decision.decisionNote} onChange={(value) => setDecision({ ...decision, decisionNote: value })} required />
            <button type="submit" disabled={decide.isPending} className={`${decision.decision === 'Approve' ? primaryButtonClass : secondaryButtonClass} lg:mt-5`}>
              {decision.decision === 'Approve' ? <Check size={16} /> : <X size={16} />}{decision.decision} correction
            </button>
            <div className="lg:col-span-3"><MutationFeedback isPending={decide.isPending} isSuccess={decide.isSuccess} error={decide.error} pending="Recording the terminal decision…" success={`Correction ${decision.decision.toLowerCase()} decision recorded.`} failure="Unable to record the correction decision." /></div>
          </form>
        </section>
      )}
      {exception.status === 'Open' && exception.billingInvoiceId && !exception.pendingCorrection && (
        <details className="mt-4 rounded-lg border border-surface-border p-3">
          <summary className="cursor-pointer text-sm font-medium text-text-primary">Request a manager-decided balance correction</summary>
          <form className="mt-3 grid gap-3 sm:grid-cols-2" onSubmit={submitCorrection}>
            <TextField label="Correction amount" type="number" step="0.01" value={correction.correctionAmount || ''} onChange={(value) => setCorrection({ ...correction, correctionAmount: Number(value) })} required />
            <TextField label="Proposed outstanding amount" type="number" min={0} step="0.01" value={correction.proposedOutstandingAmount || ''} onChange={(value) => setCorrection({ ...correction, proposedOutstandingAmount: Number(value) })} required />
            <TextField label="Reason" value={correction.reason} onChange={(value) => setCorrection({ ...correction, reason: value })} required />
            <TextField label="Evidence note" value={correction.evidenceNote} onChange={(value) => setCorrection({ ...correction, evidenceNote: value })} required />
            <button type="submit" disabled={request.isPending} className={primaryButtonClass}><Save size={16} />Request correction</button>
            <div className="sm:col-span-2"><MutationFeedback isPending={request.isPending} isSuccess={request.isSuccess} error={request.error} pending="Requesting the correction…" success="Pending correction created. It remains available on this exception after reload for an authorized manager." failure="Unable to request the correction." /></div>
          </form>
        </details>
      )}
    </article>
  )
}

function Summary({ label, value }: { label: string; value: string }) {
  return <div className="rounded-lg border border-surface-border bg-surface-card p-3"><p className="text-xs text-text-muted">{label}</p><p className="mt-1 text-xl font-semibold text-text-primary">{value}</p></div>
}

function humanize(value: string) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}
