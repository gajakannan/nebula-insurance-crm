import { FormEvent, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Calculator, Check, Save, X } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Badge } from '@/components/ui/Badge'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Skeleton } from '@/components/ui/Skeleton'
import {
  useCalculateExpectedCommission,
  useCreateCommissionSchedule,
  useDecideCommissionAdjustment,
  useExpectedCommission,
  useRequestCommissionAdjustment,
  useUpsertProducerSplit,
} from '@/features/commissions'
import type {
  CommissionAdjustmentDto,
  CommissionScheduleUpsertDto,
  ExpectedCommissionDetailDto,
  ProducerSplitAssignmentUpsertDto,
} from '@/features/commissions'

const today = new Date().toISOString().slice(0, 10)

export default function CommissionDetailPage() {
  const { expectedCommissionId } = useParams()
  const detailQuery = useExpectedCommission(expectedCommissionId)

  return (
    <DashboardLayout title="Commission Detail">
      <div className="mb-4">
        <Link to="/commissions" className="inline-flex items-center gap-2 text-sm font-medium text-text-secondary hover:text-text-primary">
          <ArrowLeft size={16} /> Commissions
        </Link>
      </div>
      {detailQuery.isLoading && <Skeleton className="h-[520px] w-full rounded-xl" />}
      {detailQuery.isError && <ErrorFallback message="Unable to load commission detail." onRetry={() => detailQuery.refetch()} />}
      {detailQuery.data && <CommissionDetailWorkspace detail={detailQuery.data} expectedCommissionId={expectedCommissionId} />}
    </DashboardLayout>
  )
}

function CommissionDetailWorkspace({ detail, expectedCommissionId }: { detail: ExpectedCommissionDetailDto; expectedCommissionId: string | undefined }) {
  const commission = detail.commission
  const calculate = useCalculateExpectedCommission(expectedCommissionId)
  const mutationError = [calculate.error].find(Boolean)
  const policyLabel = commission.policyNumber ?? shortId(commission.policyId)

  return (
    <div className="space-y-5">
      <Card>
        <CardHeader>
          <CardTitle>Policy Context</CardTitle>
          <div className="flex flex-wrap gap-2">
            <Badge variant={commission.status === 'Exception' ? 'error' : 'info'}>{commission.status}</Badge>
            {commission.exceptionState !== 'None' && <Badge variant="warning">{commission.exceptionState}</Badge>}
          </div>
        </CardHeader>
        <div className="grid gap-4 lg:grid-cols-5">
          <Metric label="Policy" value={policyLabel} detail={commission.accountDisplayName ?? commission.policyId} />
          <Metric label="Carrier / Market" value={commission.carrierMarketName ?? shortId(commission.carrierMarketId)} detail={commission.brokerName ?? commission.carrierMarketId} />
          <Metric label="Producer" value={commission.producerDisplayName ?? 'Unassigned'} detail={commission.lineOfBusiness ?? undefined} />
          <Metric label="Premium Basis" value={money(commission.premiumBasisAmount)} />
          <Metric label="Expected Gross" value={money(commission.expectedGrossCommission)} />
        </div>
        <div className="mt-4 grid gap-4 border-t border-surface-border pt-4 sm:grid-cols-3">
          <Metric label="Adjusted Expected" value={money(commission.adjustedExpectedCommission)} />
          <Metric label="Approved Adjustments" value={money(commission.approvedAdjustmentTotal)} />
          <Metric label="Record ID" value={shortId(commission.id)} detail={commission.id} mono />
        </div>
        <div className="mt-4 flex flex-wrap items-center gap-3 border-t border-surface-border pt-4">
          <button
            type="button"
            onClick={() => calculate.mutate()}
            disabled={calculate.isPending}
            className="inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-nebula-violet px-4 text-sm font-medium text-white disabled:opacity-50"
          >
            <Calculator size={16} /> {calculate.isPending ? 'Calculating' : 'Recalculate'}
          </button>
          <p className="text-sm text-text-muted">Last calculated {commission.calculatedAt ? dateTime(commission.calculatedAt) : 'not yet'}</p>
        </div>
        {mutationError && <MutationError message="Unable to calculate expected commission." />}
      </Card>

      <div className="grid gap-5 xl:grid-cols-2">
        <SchedulePanel detail={detail} />
        <SplitPanel detail={detail} />
      </div>

      <AdjustmentPanel adjustments={detail.adjustments} expectedCommissionId={expectedCommissionId} />
    </div>
  )
}

function SchedulePanel({ detail }: { detail: ExpectedCommissionDetailDto }) {
  const createSchedule = useCreateCommissionSchedule()
  const currentSchedule = detail.schedules.find((schedule) => schedule.id === detail.commission.commissionScheduleId) ?? detail.schedules[0]
  const [form, setForm] = useState<CommissionScheduleUpsertDto>(() => ({
    carrierMarketId: detail.commission.carrierMarketId,
    lineOfBusiness: detail.commission.lineOfBusiness ?? '',
    state: null,
    productCode: null,
    basis: 'PercentOfPremium',
    ratePercent: 10,
    flatAmount: null,
    effectiveFrom: today,
    effectiveTo: null,
    sourceNote: '',
  }))

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    createSchedule.mutate(cleanSchedule(form))
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Schedule Maintenance</CardTitle>
        <Badge variant="default">{detail.schedules.length} saved</Badge>
      </CardHeader>
      {currentSchedule && (
        <CurrentSummary
          title="Current schedule"
          items={[
            ['Line of business', currentSchedule.lineOfBusiness],
            ['Basis', currentSchedule.basis],
            ['Rate', currentSchedule.ratePercent !== null ? `${currentSchedule.ratePercent}%` : money(currentSchedule.flatAmount)],
            ['Effective', dateRange(currentSchedule.effectiveFrom, currentSchedule.effectiveTo)],
            ['Source', currentSchedule.sourceNote],
          ]}
        />
      )}
      <form className="space-y-3" onSubmit={onSubmit}>
        <div className="grid gap-3 sm:grid-cols-2">
          <TextInput label="Line of Business" value={form.lineOfBusiness} onChange={(value) => setForm((current) => ({ ...current, lineOfBusiness: value }))} required />
          <SelectInput label="Basis" value={form.basis} options={['PercentOfPremium', 'Flat']} onChange={(value) => setForm((current) => ({ ...current, basis: value as CommissionScheduleUpsertDto['basis'] }))} />
          <NumberInput label="Rate Percent" value={form.ratePercent} onChange={(value) => setForm((current) => ({ ...current, ratePercent: value }))} disabled={form.basis !== 'PercentOfPremium'} />
          <NumberInput label="Flat Amount" value={form.flatAmount} onChange={(value) => setForm((current) => ({ ...current, flatAmount: value }))} disabled={form.basis !== 'Flat'} />
          <TextInput label="State" value={form.state ?? ''} onChange={(value) => setForm((current) => ({ ...current, state: nullable(value) }))} />
          <TextInput label="Product Code" value={form.productCode ?? ''} onChange={(value) => setForm((current) => ({ ...current, productCode: nullable(value) }))} />
          <DateInput label="Effective From" value={form.effectiveFrom} onChange={(value) => setForm((current) => ({ ...current, effectiveFrom: value }))} required />
          <DateInput label="Effective To" value={form.effectiveTo ?? ''} onChange={(value) => setForm((current) => ({ ...current, effectiveTo: nullable(value) }))} />
        </div>
        <TextInput label="Source Note" value={form.sourceNote} onChange={(value) => setForm((current) => ({ ...current, sourceNote: value }))} required />
        <button
          type="submit"
          disabled={!form.lineOfBusiness || !form.sourceNote || createSchedule.isPending}
          className="inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-nebula-violet px-4 text-sm font-medium text-white disabled:opacity-50"
        >
          <Save size={16} /> {createSchedule.isPending ? 'Saving' : 'Save Schedule'}
        </button>
        {createSchedule.isError && <MutationError message="Unable to save commission schedule." />}
      </form>
      <HistoryList title="Recent schedules" items={detail.schedules.map((item) => `${item.lineOfBusiness} ${item.basis} from ${item.effectiveFrom}`)} />
    </Card>
  )
}

function SplitPanel({ detail }: { detail: ExpectedCommissionDetailDto }) {
  const saveSplit = useUpsertProducerSplit()
  const currentSplit = detail.splits.find((split) => split.id === detail.commission.producerSplitAssignmentId) ?? detail.splits[0]
  const [form, setForm] = useState<ProducerSplitAssignmentUpsertDto>(() => ({
    policyId: detail.commission.policyId,
    effectiveFrom: today,
    effectiveTo: null,
    reason: '',
    participants: [
      { producerId: '', splitPercent: 100 },
    ],
  }))
  const total = useMemo(() => form.participants.reduce((sum, participant) => sum + (Number(participant.splitPercent) || 0), 0), [form.participants])

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    saveSplit.mutate(form)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Producer Splits</CardTitle>
        <Badge variant={total === 100 ? 'success' : 'warning'}>{total}%</Badge>
      </CardHeader>
      {currentSplit && (
        <CurrentSummary
          title="Current split"
          items={[
            ['Effective', dateRange(currentSplit.effectiveFrom, currentSplit.effectiveTo)],
            ['Participants', currentSplit.participants.map((participant) => `${participant.producerDisplayName ?? shortId(participant.producerId)} ${participant.splitPercent}%`).join(', ')],
            ['Reason', currentSplit.reason],
          ]}
        />
      )}
      <form className="space-y-3" onSubmit={onSubmit}>
        <div className="grid gap-3 sm:grid-cols-2">
          <DateInput label="Effective From" value={form.effectiveFrom} onChange={(value) => setForm((current) => ({ ...current, effectiveFrom: value }))} required />
          <DateInput label="Effective To" value={form.effectiveTo ?? ''} onChange={(value) => setForm((current) => ({ ...current, effectiveTo: nullable(value) }))} />
        </div>
        {form.participants.map((participant, index) => (
          <div key={index} className="grid gap-3 sm:grid-cols-[1fr_140px]">
            <TextInput label={`Producer ${index + 1}`} value={participant.producerId} onChange={(value) => updateParticipant(index, 'producerId', value, setForm)} required />
            <NumberInput label="Split Percent" value={participant.splitPercent} onChange={(value) => updateParticipant(index, 'splitPercent', value ?? 0, setForm)} required />
          </div>
        ))}
        <button
          type="button"
          onClick={() => setForm((current) => ({ ...current, participants: [...current.participants, { producerId: '', splitPercent: 0 }] }))}
          className="inline-flex min-h-11 items-center justify-center rounded-lg border border-surface-border px-4 text-sm font-medium text-text-secondary hover:text-text-primary"
        >
          Add Participant
        </button>
        <TextInput label="Reason" value={form.reason} onChange={(value) => setForm((current) => ({ ...current, reason: value }))} required />
        <button
          type="submit"
          disabled={total !== 100 || form.participants.some((item) => !item.producerId) || !form.reason || saveSplit.isPending}
          className="inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-nebula-violet px-4 text-sm font-medium text-white disabled:opacity-50"
        >
          <Save size={16} /> {saveSplit.isPending ? 'Saving' : 'Save Split'}
        </button>
        {saveSplit.isError && <MutationError message="Unable to save producer split." />}
      </form>
      <HistoryList title="Recent splits" items={detail.splits.map((item) => `${item.participants.length} participants from ${item.effectiveFrom}`)} />
    </Card>
  )
}

function AdjustmentPanel({ adjustments, expectedCommissionId }: { adjustments: CommissionAdjustmentDto[]; expectedCommissionId: string | undefined }) {
  const requestAdjustment = useRequestCommissionAdjustment(expectedCommissionId)
  const decideAdjustment = useDecideCommissionAdjustment(expectedCommissionId)
  const [form, setForm] = useState({ amount: 0, effectiveDate: today, reason: '' })
  const [decisionNotes, setDecisionNotes] = useState<Record<string, string>>({})

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    requestAdjustment.mutate(form, {
      onSuccess: () => setForm({ amount: 0, effectiveDate: today, reason: '' }),
    })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Adjustment Review</CardTitle>
        <Badge variant="default">{adjustments.length} adjustments</Badge>
      </CardHeader>
      <form className="grid gap-3 lg:grid-cols-[160px_180px_1fr_auto]" onSubmit={onSubmit}>
        <NumberInput label="Amount" value={form.amount} onChange={(value) => setForm((current) => ({ ...current, amount: value ?? 0 }))} required />
        <DateInput label="Effective Date" value={form.effectiveDate} onChange={(value) => setForm((current) => ({ ...current, effectiveDate: value }))} required />
        <TextInput label="Reason" value={form.reason} onChange={(value) => setForm((current) => ({ ...current, reason: value }))} required />
        <button
          type="submit"
          disabled={!form.reason || requestAdjustment.isPending}
          className="mt-5 inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-nebula-violet px-4 text-sm font-medium text-white disabled:opacity-50 lg:mt-auto"
        >
          <Save size={16} /> Request
        </button>
      </form>
      {requestAdjustment.isError && <MutationError message="Unable to request commission adjustment." />}
      <div className="mt-4 space-y-3">
        {adjustments.length === 0 && <p className="py-6 text-center text-sm text-text-muted">No adjustments recorded.</p>}
        {adjustments.map((adjustment) => (
          <div key={adjustment.id} className="rounded-lg border border-surface-border bg-surface-card p-3">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="text-sm font-medium text-text-primary">{money(adjustment.amount)}</p>
                <p className="mt-1 text-xs text-text-muted">{adjustment.reason}</p>
              </div>
              <Badge variant={adjustment.status === 'Approved' ? 'success' : adjustment.status === 'Rejected' ? 'error' : 'warning'}>{adjustment.status}</Badge>
            </div>
            {adjustment.status === 'Pending' && (
              <div className="mt-3 grid gap-3 md:grid-cols-[1fr_auto_auto]">
                <TextInput
                  label="Decision Note"
                  value={decisionNotes[adjustment.id] ?? ''}
                  onChange={(value) => setDecisionNotes((current) => ({ ...current, [adjustment.id]: value }))}
                  required
                />
                <DecisionButton
                  label="Approve"
                  icon={<Check size={16} />}
                  disabled={!decisionNotes[adjustment.id] || decideAdjustment.isPending}
                  onClick={() => decideAdjustment.mutate({ adjustmentId: adjustment.id, dto: { decision: 'Approved', decisionNote: decisionNotes[adjustment.id] } })}
                />
                <DecisionButton
                  label="Reject"
                  icon={<X size={16} />}
                  disabled={!decisionNotes[adjustment.id] || decideAdjustment.isPending}
                  onClick={() => decideAdjustment.mutate({ adjustmentId: adjustment.id, dto: { decision: 'Rejected', decisionNote: decisionNotes[adjustment.id] } })}
                />
              </div>
            )}
          </div>
        ))}
      </div>
      {decideAdjustment.isError && <MutationError message="Unable to record adjustment decision." />}
    </Card>
  )
}

function TextInput({ label, value, onChange, required = false }: { label: string; value: string; onChange: (value: string) => void; required?: boolean }) {
  return (
    <label className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        className="h-11 rounded-lg border border-surface-border bg-surface-card px-3 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
      />
    </label>
  )
}

function NumberInput({ label, value, onChange, disabled = false, required = false }: { label: string; value: number | null; onChange: (value: number | null) => void; disabled?: boolean; required?: boolean }) {
  return (
    <label className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <input
        type="number"
        step="0.0001"
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value ? Number(event.target.value) : null)}
        disabled={disabled}
        required={required}
        className="h-11 rounded-lg border border-surface-border bg-surface-card px-3 text-sm text-text-primary disabled:opacity-50 focus:outline-none focus:ring-1 focus:ring-nebula-violet"
      />
    </label>
  )
}

function DateInput({ label, value, onChange, required = false }: { label: string; value: string; onChange: (value: string) => void; required?: boolean }) {
  return (
    <label className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <input
        type="date"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        className="h-11 rounded-lg border border-surface-border bg-surface-card px-3 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
      />
    </label>
  )
}

function SelectInput<T extends string>({ label, value, options, onChange }: { label: string; value: T; options: readonly T[]; onChange: (value: string) => void }) {
  return (
    <label className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-11 rounded-lg border border-surface-border bg-surface-card px-3 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
      >
        {options.map((option) => <option key={option} value={option}>{option}</option>)}
      </select>
    </label>
  )
}

function DecisionButton({ label, icon, disabled, onClick }: { label: string; icon: React.ReactNode; disabled: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="mt-5 inline-flex min-h-11 items-center justify-center gap-2 rounded-lg border border-surface-border px-4 text-sm font-medium text-text-secondary hover:text-text-primary disabled:opacity-50 md:mt-auto"
    >
      {icon} {label}
    </button>
  )
}

function Metric({ label, value, detail, mono = false }: { label: string; value: string; detail?: string; mono?: boolean }) {
  return (
    <div className="min-w-0">
      <p className="text-xs text-text-muted">{label}</p>
      <p className={`mt-1 truncate text-sm font-medium text-text-primary ${mono ? 'font-mono' : ''}`}>{value}</p>
      {detail && <p className="mt-1 truncate text-xs text-text-muted">{detail}</p>}
    </div>
  )
}

function CurrentSummary({ title, items }: { title: string; items: Array<[string, string]> }) {
  return (
    <div className="mb-4 rounded-lg border border-surface-border bg-surface-card p-3">
      <p className="text-xs font-medium uppercase text-text-muted">{title}</p>
      <dl className="mt-3 grid gap-3 sm:grid-cols-2">
        {items.map(([label, value]) => (
          <div key={label} className="min-w-0">
            <dt className="text-xs text-text-muted">{label}</dt>
            <dd className="mt-1 truncate text-sm font-medium text-text-primary">{value}</dd>
          </div>
        ))}
      </dl>
    </div>
  )
}

function HistoryList({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="mt-4 border-t border-surface-border pt-4">
      <p className="text-xs font-medium uppercase text-text-muted">{title}</p>
      {items.length === 0 ? (
        <p className="mt-3 text-sm text-text-muted">No history recorded.</p>
      ) : (
        <ul className="mt-3 space-y-2">
          {items.slice(0, 4).map((item) => <li key={item} className="text-sm text-text-secondary">{item}</li>)}
        </ul>
      )}
    </div>
  )
}

function MutationError({ message }: { message: string }) {
  return <p role="alert" className="mt-3 rounded-lg border border-status-error/35 bg-status-error/15 px-3 py-2 text-sm text-text-primary">{message}</p>
}

function cleanSchedule(form: CommissionScheduleUpsertDto): CommissionScheduleUpsertDto {
  return {
    ...form,
    ratePercent: form.basis === 'PercentOfPremium' ? form.ratePercent : null,
    flatAmount: form.basis === 'Flat' ? form.flatAmount : null,
  }
}

function updateParticipant(
  index: number,
  field: 'producerId' | 'splitPercent',
  value: string | number,
  setForm: React.Dispatch<React.SetStateAction<ProducerSplitAssignmentUpsertDto>>,
) {
  setForm((current) => ({
    ...current,
    participants: current.participants.map((participant, participantIndex) =>
      participantIndex === index ? { ...participant, [field]: value } : participant,
    ),
  }))
}

function nullable(value: string) {
  return value.trim() ? value : null
}

function money(value: number | null | undefined) {
  if (value === null || value === undefined) return 'Not calculated'
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function dateTime(value: string) {
  return new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

function dateRange(start: string, end: string | null) {
  return `${start} to ${end ?? 'open'}`
}

function shortId(value: string) {
  return value.slice(0, 8)
}
