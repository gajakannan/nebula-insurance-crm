import { useEffect, useMemo, useState } from 'react';
import { Activity, ArrowRightLeft, ListFilter, Plus, RefreshCw, Route, ShieldCheck } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Badge } from '@/components/ui/Badge';
import { Card } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { TextInput } from '@/components/ui/TextInput';
import { ApiError } from '@/services/api';
import {
  useAssignmentRules,
  useCoverageWindows,
  useQueueItems,
  useRoutingEvents,
  useWorkQueueMutations,
  useWorkQueues,
  type QueueStatus,
  type QueueWorkItem,
  type WorkQueue,
  type WorkType,
} from '@/features/work-queues';

const WORK_TYPES = [
  { value: '', label: 'All work types' },
  { value: 'Task', label: 'Task' },
  { value: 'Submission', label: 'Submission' },
  { value: 'Renewal', label: 'Renewal' },
  { value: 'Mixed', label: 'Mixed' },
];

const STATUSES = [
  { value: '', label: 'All statuses' },
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
];

const QUEUE_STATUSES = [
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
];

const RULE_TYPES = [
  { value: 'TerritoryOwnership', label: 'Territory ownership' },
  { value: 'WorkloadBalance', label: 'Workload balance' },
];

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

function toDateTime(value: string) {
  return value ? new Date(value).toISOString() : new Date().toISOString();
}

function fmt(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function statusVariant(status: string): 'success' | 'warning' | 'default' | 'info' {
  if (status === 'Active' || status === 'Assigned') return 'success';
  if (status === 'Open' || status === 'Draft' || status === 'Scheduled') return 'warning';
  if (status === 'InProgress') return 'info';
  return 'default';
}

type NoticeState = { tone: 'success' | 'error' | 'info'; message: string } | null;

function describeWorkQueueError(error: unknown): string {
  if (error instanceof ApiError) {
    const firstFieldError = error.problem?.errors ? Object.values(error.problem.errors)[0]?.[0] : undefined;
    return firstFieldError ?? error.problem?.detail ?? error.problem?.title ?? error.message;
  }

  return error instanceof Error ? error.message : 'The operation could not be completed.';
}

function Notice({ notice }: { notice: NoticeState }) {
  if (!notice) return null;

  const className = notice.tone === 'success'
    ? 'border-status-success/35 bg-status-success/10 text-text-primary'
    : notice.tone === 'error'
      ? 'border-status-error/35 bg-status-error/10 text-status-error'
      : 'border-surface-border bg-surface-highlight text-text-secondary';

  return (
    <div className={`rounded-lg border px-3 py-2 text-sm ${className}`} role={notice.tone === 'error' ? 'alert' : 'status'}>
      {notice.message}
    </div>
  );
}

export default function WorkQueuesPage() {
  const [workType, setWorkType] = useState<WorkType | ''>('');
  const [status, setStatus] = useState('');
  const queuesQuery = useWorkQueues({ workType, status });
  const queues = queuesQuery.data?.items ?? [];
  const [selectedQueueId, setSelectedQueueId] = useState<string>('');
  const selectedQueue = useMemo(
    () => queues.find((queue) => queue.id === selectedQueueId) ?? queues[0],
    [queues, selectedQueueId],
  );

  useEffect(() => {
    if (!selectedQueueId && queues[0]) setSelectedQueueId(queues[0].id);
  }, [queues, selectedQueueId]);

  const rulesQuery = useAssignmentRules(selectedQueue?.id);
  const coverageQuery = useCoverageWindows(selectedQueue?.id);
  const itemsQuery = useQueueItems(selectedQueue?.id);
  const eventsQuery = useRoutingEvents(workType || undefined);
  const mutations = useWorkQueueMutations();
  const [notice, setNotice] = useState<NoticeState>(null);

  const [queueForm, setQueueForm] = useState<{
    name: string;
    workType: WorkType;
    status: QueueStatus;
    description: string;
    memberUserId: string;
    memberRole: 'Manager' | 'Member' | 'Backup';
  }>({
    name: '',
    workType: 'Task' as WorkType,
    status: 'Active',
    description: '',
    memberUserId: '',
    memberRole: 'Member',
  });
  const [editingQueueId, setEditingQueueId] = useState<string | null>(null);
  const [ruleForm, setRuleForm] = useState({
    ruleType: 'TerritoryOwnership',
    precedence: '20',
    conditionsJson: '{"match":"ownership"}',
    outcomeJson: '{"assign":"owner"}',
  });
  const [coverageForm, setCoverageForm] = useState({
    coveredUserId: '',
    backupUserId: '',
    startsAt: todayIso(),
    endsAt: todayIso(),
    reason: '',
  });
  const [reassignForm, setReassignForm] = useState({ itemId: '', assignedToUserId: '', reason: '' });

  function editQueue(queue: WorkQueue) {
    setSelectedQueueId(queue.id);
    setEditingQueueId(queue.id);
    setQueueForm({
      name: queue.name,
      workType: queue.workType,
      status: queue.status,
      description: queue.description ?? '',
      memberUserId: '',
      memberRole: 'Member',
    });
  }

  function resetQueueForm() {
    setEditingQueueId(null);
    setQueueForm({ name: '', workType: 'Task', status: 'Active', description: '', memberUserId: '', memberRole: 'Member' });
  }

  const queueMutationPending = mutations.createQueue.isPending || mutations.updateQueue.isPending;
  const coverageMutationPending = mutations.createCoverage.isPending;
  const reassignMutationPending = mutations.reassign.isPending;
  const rebalanceMutationPending = mutations.rebalance.isPending;

  async function submitQueue() {
    setNotice(null);
    if (!queueForm.name.trim()) {
      setNotice({ tone: 'error', message: 'Queue name is required.' });
      return;
    }
    if (queueForm.status === 'Active' && !queueForm.memberUserId.trim()) {
      setNotice({ tone: 'error', message: 'Active queues require at least one member user ID.' });
      return;
    }

    const members = queueForm.memberUserId.trim()
      ? [{
          userProfileId: queueForm.memberUserId.trim(),
          role: queueForm.memberRole,
          effectiveFrom: new Date().toISOString(),
          effectiveTo: null,
        }]
      : [];
    const body = {
      name: queueForm.name.trim(),
      workType: queueForm.workType,
      status: queueForm.status,
      description: queueForm.description.trim() || null,
      members,
    };
    const queueToUpdate = queues.find((queue) => queue.id === editingQueueId);
    try {
      const saved = queueToUpdate
        ? await mutations.updateQueue.mutateAsync({ queue: queueToUpdate, body })
        : await mutations.createQueue.mutateAsync(body);

      setWorkType(saved.workType);
      setStatus(saved.status);
      setSelectedQueueId(saved.id);
      setEditingQueueId(saved.id);
      setQueueForm((form) => ({
        ...form,
        name: saved.name,
        workType: saved.workType,
        status: saved.status,
        description: saved.description ?? '',
      }));
      await Promise.all([queuesQuery.refetch(), rulesQuery.refetch(), coverageQuery.refetch(), itemsQuery.refetch(), eventsQuery.refetch()]);
      setNotice({ tone: 'success', message: queueToUpdate ? 'Queue changes saved.' : 'Queue created and selected.' });
    } catch (error) {
      setNotice({ tone: 'error', message: describeWorkQueueError(error) });
    }
  }

  function submitRule() {
    if (!selectedQueue) return;
    mutations.createRule.mutate({
      workQueueId: selectedQueue.id,
      ruleType: ruleForm.ruleType,
      precedence: Number(ruleForm.precedence),
      status: 'Active',
      conditionsJson: ruleForm.conditionsJson,
      outcomeJson: ruleForm.outcomeJson,
    });
  }

  async function submitCoverage() {
    setNotice(null);
    if (!selectedQueue) {
      setNotice({ tone: 'error', message: 'Select a queue before adding coverage.' });
      return;
    }
    if (!coverageForm.coveredUserId.trim() || !coverageForm.backupUserId.trim()) {
      setNotice({ tone: 'error', message: 'Covered user ID and backup user ID are required.' });
      return;
    }
    if (new Date(coverageForm.endsAt) < new Date(coverageForm.startsAt)) {
      setNotice({ tone: 'error', message: 'Coverage end date must be on or after the start date.' });
      return;
    }

    try {
      await mutations.createCoverage.mutateAsync({
        coveredUserId: coverageForm.coveredUserId.trim(),
        backupUserId: coverageForm.backupUserId.trim(),
        workQueueId: selectedQueue.id,
        startsAt: toDateTime(coverageForm.startsAt),
        endsAt: toDateTime(coverageForm.endsAt),
        status: 'Active',
        reason: coverageForm.reason.trim() || null,
      });
      setCoverageForm({ coveredUserId: '', backupUserId: '', startsAt: todayIso(), endsAt: todayIso(), reason: '' });
      await Promise.all([coverageQuery.refetch(), eventsQuery.refetch()]);
      setNotice({ tone: 'success', message: 'Coverage window added.' });
    } catch (error) {
      setNotice({ tone: 'error', message: describeWorkQueueError(error) });
    }
  }

  async function submitReassign(item?: QueueWorkItem) {
    setNotice(null);
    const target = item ?? itemsQuery.data?.items.find((candidate) => candidate.id === reassignForm.itemId);
    if (!target) {
      setNotice({ tone: 'error', message: 'Select a queue item to reassign.' });
      return;
    }
    if (!reassignForm.assignedToUserId.trim()) {
      setNotice({ tone: 'error', message: 'Assigned user ID is required.' });
      return;
    }

    try {
      await mutations.reassign.mutateAsync({
        item: target,
        body: { assignedToUserId: reassignForm.assignedToUserId.trim(), reason: reassignForm.reason.trim() || 'Manual queue assignment' },
      });
      setReassignForm({ itemId: '', assignedToUserId: '', reason: '' });
      await Promise.all([itemsQuery.refetch(), queuesQuery.refetch(), eventsQuery.refetch()]);
      setNotice({ tone: 'success', message: 'Queue item reassigned.' });
    } catch (error) {
      setNotice({ tone: 'error', message: describeWorkQueueError(error) });
    }
  }

  async function rebalanceQueue() {
    setNotice(null);
    if (!selectedQueue) {
      setNotice({ tone: 'error', message: 'Select a queue before rebalancing.' });
      return;
    }

    try {
      await mutations.rebalance.mutateAsync({
        queueId: selectedQueue.id,
        body: { strategy: 'oldest-open-first', maxItems: 25, reason: 'Manager rebalance from queue console' },
      });
      await Promise.all([itemsQuery.refetch(), queuesQuery.refetch(), eventsQuery.refetch()]);
      setNotice({ tone: 'success', message: 'Queue rebalance completed.' });
    } catch (error) {
      setNotice({ tone: 'error', message: describeWorkQueueError(error) });
    }
  }

  return (
    <DashboardLayout title="Work queues">
      <div className="space-y-4">
        <Notice notice={notice} />
        <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_360px]">
          <Card className="p-4">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div>
                <h1 className="text-base font-semibold text-text-primary">Queue routing console</h1>
                <p className="text-xs text-text-muted">Operational queues, routing rules, coverage, and reassignments.</p>
              </div>
              <div className="flex items-end gap-2">
                <Select id="queue-filter-work-type" label="Work type" value={workType} options={WORK_TYPES} onChange={(e) => setWorkType(e.target.value as WorkType | '')} />
                <Select id="queue-filter-status" label="Status" value={status} options={STATUSES} onChange={(e) => setStatus(e.target.value)} />
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px] text-left text-sm">
                <thead className="border-b border-surface-border text-xs uppercase text-text-muted">
                  <tr>
                    <th className="py-2 pr-3 font-medium">Queue</th>
                    <th className="py-2 pr-3 font-medium">Type</th>
                    <th className="py-2 pr-3 font-medium">Members</th>
                    <th className="py-2 pr-3 font-medium">Open</th>
                    <th className="py-2 pr-3 font-medium">State</th>
                    <th className="py-2 pr-3 font-medium"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-border">
                  {queues.map((queue) => (
                    <tr key={queue.id} className={queue.id === selectedQueue?.id ? 'bg-surface-highlight/60' : undefined}>
                      <td className="py-3 pr-3">
                        <button type="button" className="text-left font-medium text-text-primary hover:text-nebula-violet" onClick={() => editQueue(queue)}>
                          {queue.name}
                        </button>
                        {queue.isFallback && <Badge className="ml-2">Fallback</Badge>}
                      </td>
                      <td className="py-3 pr-3 text-text-secondary">{queue.workType}</td>
                      <td className="py-3 pr-3 text-text-secondary">{queue.activeMemberCount}</td>
                      <td className="py-3 pr-3 text-text-secondary">{queue.openItemCount}</td>
                      <td className="py-3 pr-3"><Badge variant={statusVariant(queue.status)}>{queue.status}</Badge></td>
                      <td className="py-3 pr-3 text-right">
                        <button type="button" className="rounded-lg border border-surface-border px-2 py-1 text-xs text-text-secondary hover:text-text-primary" onClick={() => editQueue(queue)}>
                          Select
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {queuesQuery.isLoading && <p className="py-6 text-sm text-text-muted">Loading queues...</p>}
            </div>
          </Card>

          <Card className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <Plus size={16} className="text-nebula-violet" />
              <h2 className="text-sm font-semibold text-text-primary">Queue setup</h2>
            </div>
            <div className="space-y-3">
              <TextInput id="queue-form-name" label="Name" value={queueForm.name} onChange={(e) => setQueueForm((f) => ({ ...f, name: e.target.value }))} />
              <Select id="queue-form-work-type" label="Work type" value={queueForm.workType} options={WORK_TYPES.slice(1)} onChange={(e) => setQueueForm((f) => ({ ...f, workType: e.target.value as WorkType }))} />
              <Select id="queue-form-status" label="Status" value={queueForm.status} options={QUEUE_STATUSES} onChange={(e) => setQueueForm((f) => ({ ...f, status: e.target.value as 'Active' | 'Inactive' }))} />
              <TextInput id="queue-form-description" label="Description" value={queueForm.description} onChange={(e) => setQueueForm((f) => ({ ...f, description: e.target.value }))} />
              <TextInput id="queue-form-member-user-id" label="Member user ID" value={queueForm.memberUserId} onChange={(e) => setQueueForm((f) => ({ ...f, memberUserId: e.target.value }))} />
              <Select id="queue-form-member-role" label="Member role" value={queueForm.memberRole} options={[
                { value: 'Member', label: 'Member' },
                { value: 'Manager', label: 'Manager' },
                { value: 'Backup', label: 'Backup' },
              ]} onChange={(e) => setQueueForm((f) => ({ ...f, memberRole: e.target.value as 'Manager' | 'Member' | 'Backup' }))} />
              <div className="flex gap-2">
                <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-60" onClick={submitQueue} disabled={queueMutationPending}>
                  {queueMutationPending ? <RefreshCw size={15} className="animate-spin" /> : <Plus size={15} />} {queueMutationPending ? 'Saving...' : 'Save'}
                </button>
                <button type="button" className="rounded-lg border border-surface-border px-3 py-2 text-sm text-text-secondary" onClick={resetQueueForm}>
                  New
                </button>
              </div>
            </div>
          </Card>
        </div>

        <div className="grid gap-3 xl:grid-cols-3">
          <Card className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <Route size={16} className="text-nebula-violet" />
              <h2 className="text-sm font-semibold text-text-primary">Assignment rules</h2>
            </div>
            <div className="space-y-3">
              <Select id="queue-rule-type" label="Rule type" value={ruleForm.ruleType} options={RULE_TYPES} onChange={(e) => setRuleForm((f) => ({ ...f, ruleType: e.target.value }))} />
              <TextInput id="queue-rule-precedence" label="Precedence" type="number" value={ruleForm.precedence} onChange={(e) => setRuleForm((f) => ({ ...f, precedence: e.target.value }))} />
              <TextInput id="queue-rule-conditions-json" label="Conditions JSON" value={ruleForm.conditionsJson} onChange={(e) => setRuleForm((f) => ({ ...f, conditionsJson: e.target.value }))} />
              <TextInput id="queue-rule-outcome-json" label="Outcome JSON" value={ruleForm.outcomeJson} onChange={(e) => setRuleForm((f) => ({ ...f, outcomeJson: e.target.value }))} />
              <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white" onClick={submitRule}>
                <Plus size={15} /> Add rule
              </button>
            </div>
            <div className="mt-4 space-y-2">
              {(rulesQuery.data?.items ?? []).map((rule) => (
                <div key={rule.id} className="rounded-lg border border-surface-border p-2 text-xs">
                  <div className="flex justify-between gap-2 text-text-primary">
                    <span>{rule.ruleType}</span>
                    <Badge variant={statusVariant(rule.status)}>v{rule.version}</Badge>
                  </div>
                  <p className="mt-1 text-text-muted">Precedence {rule.precedence}</p>
                </div>
              ))}
            </div>
          </Card>

          <Card className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <ShieldCheck size={16} className="text-nebula-violet" />
              <h2 className="text-sm font-semibold text-text-primary">Coverage</h2>
            </div>
            <div className="space-y-3">
              <TextInput id="queue-coverage-covered-user-id" label="Covered user ID" value={coverageForm.coveredUserId} onChange={(e) => setCoverageForm((f) => ({ ...f, coveredUserId: e.target.value }))} />
              <TextInput id="queue-coverage-backup-user-id" label="Backup user ID" value={coverageForm.backupUserId} onChange={(e) => setCoverageForm((f) => ({ ...f, backupUserId: e.target.value }))} />
              <div className="grid grid-cols-2 gap-2">
                <TextInput id="queue-coverage-starts-at" label="Starts" type="date" value={coverageForm.startsAt} onChange={(e) => setCoverageForm((f) => ({ ...f, startsAt: e.target.value }))} />
                <TextInput id="queue-coverage-ends-at" label="Ends" type="date" value={coverageForm.endsAt} onChange={(e) => setCoverageForm((f) => ({ ...f, endsAt: e.target.value }))} />
              </div>
              <TextInput id="queue-coverage-reason" label="Reason" value={coverageForm.reason} onChange={(e) => setCoverageForm((f) => ({ ...f, reason: e.target.value }))} />
              <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-60" onClick={submitCoverage} disabled={coverageMutationPending}>
                {coverageMutationPending ? <RefreshCw size={15} className="animate-spin" /> : <Plus size={15} />} {coverageMutationPending ? 'Adding...' : 'Add coverage'}
              </button>
            </div>
            <div className="mt-4 space-y-2">
              {(coverageQuery.data?.items ?? []).slice(0, 4).map((window) => (
                <div key={window.id} className="rounded-lg border border-surface-border p-2 text-xs text-text-secondary">
                  <div className="flex justify-between gap-2">
                    <span>{window.coveredUserId.slice(0, 8)} {'->'} {window.backupUserId.slice(0, 8)}</span>
                    <Badge variant={statusVariant(window.status)}>{window.status}</Badge>
                  </div>
                  <p className="mt-1 text-text-muted">{fmt(window.startsAt)} to {fmt(window.endsAt)}</p>
                </div>
              ))}
              {coverageQuery.isFetching && <p className="text-xs text-text-muted">Refreshing coverage...</p>}
              {!coverageQuery.isLoading && !(coverageQuery.data?.items.length) && (
                <p className="text-xs text-text-muted">No coverage windows are configured for this queue.</p>
              )}
            </div>
          </Card>

          <Card className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <ArrowRightLeft size={16} className="text-nebula-violet" />
              <h2 className="text-sm font-semibold text-text-primary">Reassign and rebalance</h2>
            </div>
            <div className="space-y-3">
              <Select
                label="Queue item"
                id="queue-reassign-item"
                value={reassignForm.itemId}
                placeholder="Select open item"
                options={(itemsQuery.data?.items ?? []).map((item) => ({ value: item.id, label: `${item.sourceType} ${item.sourceId.slice(0, 8)}` }))}
                onChange={(e) => setReassignForm((f) => ({ ...f, itemId: e.target.value }))}
              />
              <TextInput id="queue-reassign-assigned-user-id" label="Assigned user ID" value={reassignForm.assignedToUserId} onChange={(e) => setReassignForm((f) => ({ ...f, assignedToUserId: e.target.value }))} />
              <TextInput id="queue-reassign-reason" label="Reason" value={reassignForm.reason} onChange={(e) => setReassignForm((f) => ({ ...f, reason: e.target.value }))} />
              <div className="flex flex-wrap gap-2">
                <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-60" onClick={() => submitReassign()} disabled={reassignMutationPending}>
                  {reassignMutationPending ? <RefreshCw size={15} className="animate-spin" /> : <ArrowRightLeft size={15} />} {reassignMutationPending ? 'Reassigning...' : 'Reassign'}
                </button>
                <button type="button" className="inline-flex items-center gap-2 rounded-lg border border-surface-border px-3 py-2 text-sm text-text-secondary disabled:cursor-not-allowed disabled:opacity-60" onClick={rebalanceQueue} disabled={rebalanceMutationPending}>
                  <RefreshCw size={15} className={rebalanceMutationPending ? 'animate-spin' : undefined} /> {rebalanceMutationPending ? 'Rebalancing...' : 'Rebalance'}
                </button>
              </div>
            </div>
          </Card>
        </div>

        <div className="grid gap-3 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)]">
          <Card className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <ListFilter size={16} className="text-nebula-violet" />
              <h2 className="text-sm font-semibold text-text-primary">Queue worklist</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[760px] text-left text-sm">
                <thead className="border-b border-surface-border text-xs uppercase text-text-muted">
                  <tr>
                    <th className="py-2 pr-3 font-medium">Source</th>
                    <th className="py-2 pr-3 font-medium">Assigned</th>
                    <th className="py-2 pr-3 font-medium">Status</th>
                    <th className="py-2 pr-3 font-medium">Match</th>
                    <th className="py-2 pr-3 font-medium">Routed</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-border">
                  {(itemsQuery.data?.items ?? []).map((item) => (
                    <tr key={item.id}>
                      <td className="py-3 pr-3 font-medium text-text-primary">{item.sourceType} <span className="text-text-muted">{item.sourceId.slice(0, 8)}</span></td>
                      <td className="py-3 pr-3 text-text-secondary">{item.assignedToUserId?.slice(0, 8) ?? 'Unassigned'}</td>
                      <td className="py-3 pr-3"><Badge variant={statusVariant(item.queueStatus)}>{item.queueStatus}</Badge></td>
                      <td className="py-3 pr-3 text-text-secondary">{item.matchReason ?? 'n/a'}</td>
                      <td className="py-3 pr-3 text-text-secondary">{fmt(item.routedAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {!itemsQuery.isLoading && !(itemsQuery.data?.items.length) && (
                <p className="py-6 text-sm text-text-muted">No routed work is currently in this queue.</p>
              )}
            </div>
          </Card>

          <Card className="p-4">
            <div className="mb-3 flex items-center gap-2">
              <Activity size={16} className="text-nebula-violet" />
              <h2 className="text-sm font-semibold text-text-primary">Routing audit</h2>
            </div>
            <div className="max-h-[420px] space-y-2 overflow-auto pr-1">
              {(eventsQuery.data?.items ?? []).map((event) => (
                <div key={event.id} className="rounded-lg border border-surface-border p-3 text-xs">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="font-medium text-text-primary">{event.outcome}</p>
                      <p className="text-text-muted">{event.sourceType} {event.sourceId.slice(0, 8)}</p>
                    </div>
                    <Badge>{event.reasonCode}</Badge>
                  </div>
                  <p className="mt-2 text-text-muted">{fmt(event.occurredAt)}</p>
                </div>
              ))}
              {!eventsQuery.isLoading && !(eventsQuery.data?.items.length) && (
                <p className="py-6 text-sm text-text-muted">No routing events match the current filter.</p>
              )}
            </div>
          </Card>
        </div>
      </div>
    </DashboardLayout>
  );
}
