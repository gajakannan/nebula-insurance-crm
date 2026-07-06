import { useEffect, useMemo, useState } from 'react'
import {
  AlertCircle,
  CheckCircle2,
  FileClock,
  History,
  RotateCcw,
  Save,
  Search,
  ShieldCheck,
  X,
} from 'lucide-react'
import { Badge } from '@/components/ui/Badge'
import { Card } from '@/components/ui/Card'
import { Select } from '@/components/ui/Select'
import { TextInput } from '@/components/ui/TextInput'
import { ApiError } from '@/services/api'
import {
  useAdminConfigurationAudit,
  useAdminConfigurationDomain,
  useAdminConfigurationDomains,
  useAdminConfigurationMutations,
} from '../hooks'
import type {
  AdminConfigurationAuditEvent,
  AdminConfigurationChangeSummary,
  AdminConfigurationDomain,
  AdminConfigurationValidationIssue,
  AdminConfigurationValidationResult,
} from '../types'

function fmt(value: string | null | undefined) {
  if (!value) return 'Not published'
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

function pretty(value: unknown) {
  return JSON.stringify(value ?? {}, null, 2)
}

function describeError(error: unknown) {
  if (error instanceof ApiError) return error.problem?.detail ?? error.problem?.title ?? error.message
  return error instanceof Error ? error.message : 'The operation could not be completed.'
}

function statusVariant(status: string | null | undefined): 'success' | 'warning' | 'default' | 'info' | 'error' {
  if (status === 'Passed' || status === 'Published' || status === 'Supported' || status === 'Refreshed' || status === 'Succeeded') return 'success'
  if (status === 'Failed' || status === 'ValidationFailed') return 'warning'
  if (status === 'Draft' || status === 'ValidationPassed' || status === 'Pending') return 'info'
  if (status === 'Denied') return 'error'
  return 'default'
}

const actionOptions = [
  { value: 'DraftCreated', label: 'Draft created' },
  { value: 'DraftUpdated', label: 'Draft updated' },
  { value: 'ValidationPassed', label: 'Validation passed' },
  { value: 'ValidationFailed', label: 'Validation failed' },
  { value: 'Published', label: 'Published' },
  { value: 'RollbackPublished', label: 'Rollback' },
]

export function AdminConfigurationWorkspace() {
  const domainsQuery = useAdminConfigurationDomains()
  const domains = domainsQuery.data ?? []
  const [selectedDomainKey, setSelectedDomainKey] = useState('')
  const [draftText, setDraftText] = useState('{}')
  const [reason, setReason] = useState('Routine configuration update')
  const [notice, setNotice] = useState<{ tone: 'success' | 'error'; text: string } | null>(null)
  const [compareResult, setCompareResult] = useState<AdminConfigurationValidationResult | null>(null)
  const [publishOpen, setPublishOpen] = useState(false)
  const [rollbackOpen, setRollbackOpen] = useState(false)
  const [rollbackVersion, setRollbackVersion] = useState('')
  const [selectedAuditEvent, setSelectedAuditEvent] = useState<AdminConfigurationAuditEvent | null>(null)
  const [auditFilters, setAuditFilters] = useState({ action: '', outcome: '', actorUserId: '', from: '', to: '' })

  useEffect(() => {
    if (!selectedDomainKey && domains[0]) setSelectedDomainKey(domains[0].domainKey)
  }, [domains, selectedDomainKey])

  const detailQuery = useAdminConfigurationDomain(selectedDomainKey)
  const mutations = useAdminConfigurationMutations(selectedDomainKey)
  const auditQuery = useAdminConfigurationAudit({
    domainKey: selectedDomainKey,
    action: auditFilters.action,
    outcome: auditFilters.outcome,
    actorUserId: auditFilters.actorUserId,
    from: auditFilters.from,
    to: auditFilters.to,
  })
  const detail = detailQuery.data
  const selectedDomain = detail?.domain ?? domains.find((domain) => domain.domainKey === selectedDomainKey)
  const latestValidation = detail?.activeDraft?.latestValidation
  const canPublish = latestValidation?.status === 'Passed' && latestValidation.draftPayloadHash === detail?.activeDraft?.payloadHash
  const rollbackOptions = (detail?.publishedSets ?? []).filter((set) => set.publishedVersion !== detail?.currentPublishedSet?.publishedVersion)

  useEffect(() => {
    if (detail?.activeDraft) setDraftText(pretty(detail.activeDraft.payload))
    else if (detail?.currentPublishedSet) setDraftText(pretty(detail.currentPublishedSet.payloadSnapshot))
    else setDraftText('{}')
    setCompareResult(null)
  }, [detail?.activeDraft, detail?.currentPublishedSet])

  useEffect(() => {
    if (!rollbackVersion && rollbackOptions[0]) setRollbackVersion(String(rollbackOptions[0].publishedVersion))
  }, [rollbackOptions, rollbackVersion])

  const parsedDraft = useMemo(() => {
    try {
      return { ok: true as const, value: JSON.parse(draftText || '{}') }
    } catch (error) {
      return { ok: false as const, error }
    }
  }, [draftText])

  async function runMutation(action: () => Promise<unknown>, success: string) {
    setNotice(null)
    try {
      await action()
      setNotice({ tone: 'success', text: success })
    } catch (error) {
      setNotice({ tone: 'error', text: describeError(error) })
    }
  }

  function createDraft() {
    if (!selectedDomainKey) return
    void runMutation(() => mutations.createDraft.mutateAsync({ key: selectedDomainKey, reason }), 'Draft created.')
  }

  function validateDraft() {
    const draft = detail?.activeDraft
    if (!draft) return
    void runMutation(() => mutations.validateDraft.mutateAsync(draft.id), 'Validation completed.')
  }

  function compareDraft() {
    const draft = detail?.activeDraft
    if (!draft) return
    void runMutation(async () => {
      setCompareResult(await mutations.compareDraft.mutateAsync(draft.id))
    }, 'Comparison loaded.')
  }

  function confirmPublish() {
    const draft = detail?.activeDraft
    if (!draft) return
    void runMutation(async () => {
      await mutations.publishDraft.mutateAsync({ draftId: draft.id, reason })
      setPublishOpen(false)
    }, 'Configuration published.')
  }

  function saveDraft() {
    const draft = detail?.activeDraft
    if (!draft || !parsedDraft.ok) return
    void runMutation(
      () => mutations.updateDraft.mutateAsync({ draftId: draft.id, payload: parsedDraft.value, rowVersion: draft.rowVersion, reason }),
      'Draft saved.',
    )
  }

  function confirmRollback() {
    const target = Number(rollbackVersion)
    if (!target) return
    void runMutation(async () => {
      await mutations.rollback.mutateAsync({ targetPublishedVersion: target, reason })
      setRollbackOpen(false)
    }, 'Rollback published.')
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 border-b border-surface-border pb-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-text-primary">Admin Configuration</h1>
          <p className="text-sm text-text-secondary">Governed draft, validation, publish, rollback, and audit lifecycle.</p>
        </div>
        <Select
          label="Domain"
          value={selectedDomainKey}
          onChange={(event) => setSelectedDomainKey(event.target.value)}
          options={domains.map((domain) => ({ value: domain.domainKey, label: domain.displayName }))}
          placeholder={domainsQuery.isLoading ? 'Loading domains' : 'Select domain'}
        />
      </div>

      {notice && (
        <div className={`rounded-md border px-3 py-2 text-sm ${notice.tone === 'error' ? 'border-status-error/40 bg-status-error/10 text-status-error' : 'border-status-success/40 bg-status-success/10 text-text-primary'}`}>
          {notice.text}
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-[280px_minmax(0,1fr)_390px]">
        <Card className="p-3">
          <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-text-primary">
            <ShieldCheck size={16} />
            Domains
          </div>
          {domainsQuery.isLoading && <StateMessage text="Loading governed domains..." />}
          {domainsQuery.isError && (
            <RetryState text={describeError(domainsQuery.error)} onRetry={() => void domainsQuery.refetch()} />
          )}
          {!domainsQuery.isLoading && !domainsQuery.isError && domains.length === 0 && (
            <StateMessage text="No governed domains are available for your role." />
          )}
          <div className="space-y-2">
            {domains.map((domain) => (
              <DomainButton
                key={domain.domainKey}
                domain={domain}
                active={domain.domainKey === selectedDomainKey}
                onClick={() => setSelectedDomainKey(domain.domainKey)}
              />
            ))}
          </div>
        </Card>

        <Card className="p-4">
          {detailQuery.isError && <RetryState text={describeError(detailQuery.error)} onRetry={() => void detailQuery.refetch()} />}
          {!detailQuery.isError && (
            <>
              <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-text-primary">{selectedDomain?.displayName ?? 'Configuration domain'}</h2>
                  <p className="mt-1 text-xs text-text-muted">{selectedDomain?.owningModule ?? 'Loading'} owned runtime configuration</p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Badge variant={statusVariant(selectedDomain?.status)}>{selectedDomain?.status ?? 'Loading'}</Badge>
                    <Badge variant={statusVariant(detail?.activeDraft?.status)}>{detail?.activeDraft?.status ?? 'No active draft'}</Badge>
                    <Badge variant={statusVariant(latestValidation?.status)}>{latestValidation?.status ?? 'Not validated'}</Badge>
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  <button className="btn btn-secondary" onClick={createDraft} disabled={!selectedDomainKey || Boolean(detail?.activeDraft) || mutations.createDraft.isPending || !reason.trim()}>
                    <FileClock size={16} /> Draft
                  </button>
                  <button className="btn btn-secondary" onClick={validateDraft} disabled={!detail?.activeDraft || mutations.validateDraft.isPending}>
                    <CheckCircle2 size={16} /> Validate
                  </button>
                  <button className="btn btn-secondary" onClick={compareDraft} disabled={!detail?.activeDraft || mutations.compareDraft.isPending}>
                    <Search size={16} /> Compare
                  </button>
                  <button className="btn btn-primary" onClick={() => setPublishOpen(true)} disabled={!canPublish || mutations.publishDraft.isPending}>
                    <Save size={16} /> Publish
                  </button>
                </div>
              </div>

              {detailQuery.isLoading && <StateMessage text="Loading configuration detail..." />}

              <div className="grid gap-3 md:grid-cols-3">
                <SummaryMetric label="Published" value={detail?.currentPublishedSet ? `v${detail.currentPublishedSet.publishedVersion}` : 'None'} />
                <SummaryMetric label="Last publish" value={fmt(detail?.currentPublishedSet?.publishedAt)} />
                <SummaryMetric label="Refresh" value={detail?.refreshStatuses[0]?.status ?? 'Not applicable'} />
              </div>

              <ValidationPanel validation={latestValidation} compare={compareResult} stale={!canPublish && latestValidation?.status === 'Passed'} />

              <div className="mt-4">
                <label className="mb-2 block text-sm font-medium text-text-primary">Draft payload</label>
                <textarea
                  className="min-h-[320px] w-full rounded-md border border-surface-border bg-surface-card px-3 py-2 font-mono text-sm text-text-primary outline-none focus:border-nebula-violet"
                  value={draftText}
                  onChange={(event) => setDraftText(event.target.value)}
                  spellCheck={false}
                  aria-label="Draft payload JSON"
                />
                {!parsedDraft.ok && <p className="mt-2 text-sm text-status-error">Payload must be valid JSON before saving.</p>}
              </div>

              <div className="mt-3 flex flex-wrap items-end gap-2">
                <TextInput label="Reason" value={reason} onChange={(event) => setReason(event.target.value)} required />
                <button className="btn btn-secondary" onClick={saveDraft} disabled={!detail?.activeDraft || !parsedDraft.ok || mutations.updateDraft.isPending || !reason.trim()}>
                  <Save size={16} /> Save draft
                </button>
                <button className="btn btn-secondary" onClick={() => setRollbackOpen(true)} disabled={rollbackOptions.length === 0 || mutations.rollback.isPending || !reason.trim()}>
                  <RotateCcw size={16} /> Rollback
                </button>
              </div>
            </>
          )}
        </Card>

        <Card className="p-4">
          <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-text-primary">
            <History size={16} />
            Audit
          </div>
          <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-1">
            <Select label="Action" value={auditFilters.action} onChange={(event) => setAuditFilters((current) => ({ ...current, action: event.target.value }))} options={actionOptions} placeholder="All actions" />
            <Select label="Status" value={auditFilters.outcome} onChange={(event) => setAuditFilters((current) => ({ ...current, outcome: event.target.value }))} options={[{ value: 'Succeeded', label: 'Succeeded' }, { value: 'Passed', label: 'Passed' }, { value: 'Failed', label: 'Failed' }]} placeholder="All statuses" />
            <TextInput label="Actor ID" value={auditFilters.actorUserId} onChange={(event) => setAuditFilters((current) => ({ ...current, actorUserId: event.target.value }))} placeholder="User UUID" />
            <TextInput label="From" type="date" value={auditFilters.from} onChange={(event) => setAuditFilters((current) => ({ ...current, from: event.target.value }))} />
            <TextInput label="To" type="date" value={auditFilters.to} onChange={(event) => setAuditFilters((current) => ({ ...current, to: event.target.value }))} />
          </div>
          {auditQuery.isLoading && <StateMessage text="Loading audit history..." />}
          {auditQuery.isError && <RetryState text={describeError(auditQuery.error)} onRetry={() => void auditQuery.refetch()} />}
          <div className="mt-4 space-y-3">
            {(auditQuery.data?.items ?? []).map((event) => (
              <button key={event.id} type="button" onClick={() => setSelectedAuditEvent(event)} className="w-full rounded-md border border-surface-border p-3 text-left transition-colors hover:border-nebula-violet">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-sm font-medium text-text-primary">{event.action}</span>
                  <Badge variant={statusVariant(event.outcome)}>{event.outcome}</Badge>
                </div>
                <p className="mt-1 text-xs text-text-muted">{fmt(event.createdAt)}</p>
                <p className="mt-1 text-xs text-text-secondary">{event.domainKey}</p>
              </button>
            ))}
            {auditQuery.data?.items?.length === 0 && <StateMessage text="No audit events match the current filters." />}
          </div>
        </Card>
      </div>

      {publishOpen && detail?.activeDraft && (
        <ConfirmDialog
          title="Publish configuration"
          confirmLabel="Publish"
          onCancel={() => setPublishOpen(false)}
          onConfirm={confirmPublish}
          disabled={!reason.trim()}
        >
          <p>Publish draft v{detail.activeDraft.draftVersion} for {selectedDomain?.displayName}. The prior published version remains available in history.</p>
          <p className="mt-2">Reason: {reason}</p>
          <ValidationSummary validation={latestValidation} />
        </ConfirmDialog>
      )}

      {rollbackOpen && (
        <ConfirmDialog
          title="Rollback configuration"
          confirmLabel="Publish rollback"
          onCancel={() => setRollbackOpen(false)}
          onConfirm={confirmRollback}
          disabled={!rollbackVersion || !reason.trim()}
        >
          <Select
            label="Rollback target"
            value={rollbackVersion}
            onChange={(event) => setRollbackVersion(event.target.value)}
            options={rollbackOptions.map((set) => ({ value: String(set.publishedVersion), label: `v${set.publishedVersion} - ${fmt(set.publishedAt)}` }))}
          />
          <p className="mt-3">Rollback creates a new published version and preserves audit history.</p>
          <p className="mt-2">Reason: {reason}</p>
        </ConfirmDialog>
      )}

      {selectedAuditEvent && (
        <ConfirmDialog title="Audit details" confirmLabel="Close" onCancel={() => setSelectedAuditEvent(null)} onConfirm={() => setSelectedAuditEvent(null)}>
          <dl className="grid gap-2 text-sm">
            <DetailRow label="Action" value={selectedAuditEvent.action} />
            <DetailRow label="Outcome" value={selectedAuditEvent.outcome} />
            <DetailRow label="Actor" value={selectedAuditEvent.actorUserId} />
            <DetailRow label="Timestamp" value={fmt(selectedAuditEvent.createdAt)} />
          </dl>
          <pre className="mt-3 max-h-64 overflow-auto rounded-md border border-surface-border bg-surface-muted p-3 text-xs text-text-primary">{pretty(selectedAuditEvent.summary)}</pre>
        </ConfirmDialog>
      )}
    </div>
  )
}

function DomainButton({ domain, active, onClick }: { domain: AdminConfigurationDomain; active: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`w-full rounded-md border px-3 py-2 text-left text-sm ${active ? 'border-nebula-violet bg-nebula-violet/10' : 'border-surface-border bg-surface-card'}`}
    >
      <span className="block font-medium text-text-primary">{domain.displayName}</span>
      <span className="mt-1 block text-xs text-text-muted">{domain.owningModule} - {domain.currentPublishedVersion ? `v${domain.currentPublishedVersion}` : 'unpublished'} - {domain.draftStatus ?? 'No active draft'}</span>
    </button>
  )
}

function SummaryMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md border border-surface-border bg-surface-muted px-3 py-2">
      <div className="text-xs uppercase text-text-muted">{label}</div>
      <div className="mt-1 text-sm font-semibold text-text-primary">{value}</div>
    </div>
  )
}

function ValidationPanel({ validation, compare, stale }: { validation: AdminConfigurationValidationResult | null | undefined; compare: AdminConfigurationValidationResult | null; stale: boolean }) {
  const summary = compare ?? validation
  return (
    <div className="mt-4 rounded-md border border-surface-border bg-surface-muted p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-text-primary">Validation and compare</h3>
        <Badge variant={statusVariant(validation?.status)}>{validation?.status ?? 'Not validated'}</Badge>
      </div>
      {stale && <p className="mt-2 text-sm text-status-warning">Validation is stale for the current draft payload. Re-run validation before publish.</p>}
      <IssueList title="Blocking errors" issues={validation?.blockingErrors ?? []} empty="No blocking errors." />
      <IssueList title="Warnings" issues={validation?.warnings ?? []} empty="No warnings." />
      <CompareList changes={summary?.compareSummary ?? []} />
    </div>
  )
}

function ValidationSummary({ validation }: { validation: AdminConfigurationValidationResult | null | undefined }) {
  return (
    <div className="mt-3 rounded-md border border-surface-border bg-surface-muted p-3">
      <p>Status: {validation?.status ?? 'Not validated'}</p>
      <p>Blocking errors: {validation?.blockingErrors.length ?? 0}</p>
      <p>Warnings: {validation?.warnings.length ?? 0}</p>
      <p>Changed fields: {validation?.compareSummary.length ?? 0}</p>
    </div>
  )
}

function IssueList({ title, issues, empty }: { title: string; issues: AdminConfigurationValidationIssue[]; empty: string }) {
  return (
    <div className="mt-3">
      <h4 className="text-xs font-semibold uppercase text-text-muted">{title}</h4>
      {issues.length === 0 ? <p className="mt-1 text-xs text-text-secondary">{empty}</p> : (
        <ul className="mt-1 space-y-1">
          {issues.map((issue) => (
            <li key={`${issue.code}-${issue.path ?? ''}`} className="text-xs text-text-primary">{issue.path ?? '$'}: {issue.message}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

function CompareList({ changes }: { changes: AdminConfigurationChangeSummary[] }) {
  return (
    <div className="mt-3">
      <h4 className="text-xs font-semibold uppercase text-text-muted">Changed fields</h4>
      {changes.length === 0 ? <p className="mt-1 text-xs text-text-secondary">Run compare to review changes against the published version.</p> : (
        <div className="mt-2 max-h-40 overflow-auto rounded-md border border-surface-border">
          {changes.map((change) => (
            <div key={`${change.path}-${change.changeType}`} className="grid gap-1 border-b border-surface-border p-2 text-xs last:border-b-0">
              <span className="font-medium text-text-primary">{change.path} - {change.changeType}</span>
              <span className="text-text-secondary">Before: {change.before ?? 'n/a'}</span>
              <span className="text-text-secondary">After: {change.after ?? 'n/a'}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function StateMessage({ text }: { text: string }) {
  return <p className="rounded-md border border-surface-border bg-surface-muted p-3 text-sm text-text-secondary">{text}</p>
}

function RetryState({ text, onRetry }: { text: string; onRetry: () => void }) {
  return (
    <div className="rounded-md border border-status-error/40 bg-status-error/10 p-3 text-sm text-status-error">
      <div className="flex items-start gap-2">
        <AlertCircle size={16} />
        <p>{text}</p>
      </div>
      <button type="button" className="btn btn-secondary mt-3" onClick={onRetry}>Retry</button>
    </div>
  )
}

function ConfirmDialog({ title, confirmLabel, children, onCancel, onConfirm, disabled = false }: { title: string; confirmLabel: string; children: React.ReactNode; onCancel: () => void; onConfirm: () => void; disabled?: boolean }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div role="dialog" aria-modal="true" aria-label={title} className="w-full max-w-xl rounded-lg border border-surface-border bg-surface-panel p-4 shadow-xl">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-lg font-semibold text-text-primary">{title}</h2>
          <button type="button" className="btn btn-ghost" onClick={onCancel} aria-label="Close dialog"><X size={16} /></button>
        </div>
        <div className="mt-3 text-sm text-text-secondary">{children}</div>
        <div className="mt-4 flex justify-end gap-2">
          <button type="button" className="btn btn-secondary" onClick={onCancel}>Cancel</button>
          <button type="button" className="btn btn-primary" onClick={onConfirm} disabled={disabled}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[110px_minmax(0,1fr)] gap-2">
      <dt className="text-text-muted">{label}</dt>
      <dd className="break-all text-text-primary">{value}</dd>
    </div>
  )
}
