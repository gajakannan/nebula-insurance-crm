import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Save } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Modal } from '@/components/ui/Modal';
import { Skeleton } from '@/components/ui/Skeleton';
import { TextInput } from '@/components/ui/TextInput';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import {
  ServiceCasePriorityBadge,
  ServiceCaseStatusBadge,
  useCreateServiceCaseFollowUpTask,
  useLinkServiceCaseCommunication,
  useServiceCase,
  useTransitionServiceCase,
  useUpdateServiceCase,
  useUpdateServiceCaseClaimReference,
  type ServiceCasePriority,
  type ServiceCaseStatus,
} from '@/features/service-cases';
import { ApiError } from '@/services/api';

const NEXT_STATUS: Record<ServiceCaseStatus, ServiceCaseStatus[]> = {
  Intake: ['InProgress', 'Waiting'],
  InProgress: ['Waiting', 'Resolved'],
  Waiting: ['InProgress', 'Resolved'],
  Resolved: ['Closed'],
  Closed: [],
};
const PRIORITIES: ServiceCasePriority[] = ['Low', 'Medium', 'High', 'Urgent'];

export default function ServiceCaseDetailPage() {
  const { serviceCaseId = '' } = useParams<{ serviceCaseId: string }>();
  const query = useServiceCase(serviceCaseId);
  const transition = useTransitionServiceCase(serviceCaseId);
  const updateClaim = useUpdateServiceCaseClaimReference(serviceCaseId);
  const updateCase = useUpdateServiceCase(serviceCaseId);
  const linkCommunication = useLinkServiceCaseCommunication(serviceCaseId);
  const createTask = useCreateServiceCaseFollowUpTask(serviceCaseId);
  const serviceCase = query.data;

  const [targetStatus, setTargetStatus] = useState<ServiceCaseStatus | ''>('');
  const [transitionNote, setTransitionNote] = useState('');
  const [resolutionSummary, setResolutionSummary] = useState('');
  const [claimOpen, setClaimOpen] = useState(false);
  const [taskOpen, setTaskOpen] = useState(false);
  const [communicationOpen, setCommunicationOpen] = useState(false);
  const [summary, setSummary] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<ServiceCasePriority>('Medium');
  const [dueDate, setDueDate] = useState('');
  const [followUpSummary, setFollowUpSummary] = useState('');
  const [owner, setOwner] = useState<UserSummaryDto | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!serviceCase) return;
    setSummary(serviceCase.summary);
    setDescription(serviceCase.description ?? '');
    setPriority(serviceCase.priority);
    setDueDate(serviceCase.dueDate ?? '');
    setFollowUpSummary(serviceCase.followUpSummary ?? '');
    setOwner({
      userId: serviceCase.ownerUserId,
      displayName: serviceCase.ownerDisplayName ?? serviceCase.ownerUserId,
      email: '',
      roles: [],
      isActive: true,
    });
  }, [serviceCase]);

  if (query.isLoading) {
    return (
      <DashboardLayout title="Service Case">
        <div className="space-y-4">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-80 w-full" />
        </div>
      </DashboardLayout>
    );
  }

  if (query.error || !serviceCase) {
    const apiError = query.error instanceof ApiError ? query.error : null;
    return (
      <DashboardLayout title="Service Case">
        {apiError?.status === 404
          ? <p className="py-12 text-center text-sm text-text-secondary">Service case not found.</p>
          : <ErrorFallback message="Unable to load service case." onRetry={() => query.refetch()} />}
      </DashboardLayout>
    );
  }

  const nextStatuses = NEXT_STATUS[serviceCase.status];

  async function runTransition() {
    if (!targetStatus || !serviceCase) return;
    try {
      await transition.mutateAsync({
        toStatus: targetStatus,
        note: normalize(transitionNote),
        resolutionSummary: targetStatus === 'Resolved' || targetStatus === 'Closed' ? resolutionSummary : null,
        rowVersion: serviceCase.rowVersion,
      });
      setTargetStatus('');
      setTransitionNote('');
      setResolutionSummary('');
      setError('');
    } catch (transitionError) {
      setError(describeError(transitionError));
    }
  }

  async function saveWorkManagement() {
    if (!serviceCase || !owner) return;
    try {
      await updateCase.mutateAsync({
        summary: summary.trim(),
        description: normalize(description),
        priority,
        ownerUserId: owner.userId,
        dueDate: dueDate || null,
        followUpSummary: normalize(followUpSummary),
        rowVersion: serviceCase.rowVersion,
      });
      setError('');
    } catch (updateError) {
      setError(describeError(updateError));
    }
  }

  return (
    <DashboardLayout title="Service Case">
      <div className="space-y-6">
        <Link to="/service-cases" className="text-xs text-text-muted hover:text-text-secondary">Service Cases</Link>

        <Card>
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <ServiceCaseStatusBadge status={serviceCase.status} />
                <ServiceCasePriorityBadge priority={serviceCase.priority} />
                <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-[11px] font-medium text-text-muted">
                  {serviceCase.type}
                </span>
              </div>
              <h1 className="mt-3 text-2xl font-semibold text-text-primary">{serviceCase.caseNumber}</h1>
              <p className="mt-2 text-sm text-text-secondary">{serviceCase.summary}</p>
            </div>
            <div className="flex flex-wrap gap-2">
              <button type="button" onClick={() => setClaimOpen(true)} className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card-hover hover:text-text-primary">
                Claim Reference
              </button>
              <button type="button" onClick={() => setCommunicationOpen(true)} className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card-hover hover:text-text-primary">
                Link Communication
              </button>
              <button type="button" onClick={() => setTaskOpen(true)} className="rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90">
                Follow-Up Task
              </button>
            </div>
          </div>
        </Card>

        <div className="grid gap-4 xl:grid-cols-[1.2fr_0.8fr]">
          <Card>
            <CardHeader><CardTitle>Case Details</CardTitle></CardHeader>
            <div className="grid gap-3 md:grid-cols-2">
              <Detail label="Account" value={<Link to={`/accounts/${serviceCase.accountId}`} className="text-nebula-violet hover:underline">{serviceCase.accountDisplayName ?? 'Open account'}</Link>} />
              <Detail label="Policy" value={serviceCase.policyId ? <Link to={`/policies/${serviceCase.policyId}`} className="text-nebula-violet hover:underline">{serviceCase.policyNumber ?? 'Open policy'}</Link> : 'Not linked'} />
              <Detail label="Owner" value={serviceCase.ownerDisplayName ?? serviceCase.ownerUserId} />
              <Detail label="Due date" value={serviceCase.dueDate ? formatDate(serviceCase.dueDate) : 'Unscheduled'} />
              <Detail label="Created" value={formatDateTime(serviceCase.createdAt)} />
              <Detail label="Updated" value={serviceCase.updatedAt ? formatDateTime(serviceCase.updatedAt) : 'Not updated'} />
            </div>
            {serviceCase.description && <p className="mt-4 text-sm text-text-secondary">{serviceCase.description}</p>}
          </Card>

          <Card>
            <CardHeader><CardTitle>Status</CardTitle></CardHeader>
            {nextStatuses.length === 0 ? (
              <p className="text-sm text-text-muted">This case is closed.</p>
            ) : (
              <div className="space-y-3">
                <select value={targetStatus} onChange={(event) => setTargetStatus(event.target.value as ServiceCaseStatus | '')} className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet">
                  <option value="">Select next status</option>
                  {nextStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
                </select>
                {targetStatus === 'Resolved' && (
                  <TextInput label="Resolution summary" value={resolutionSummary} onChange={(event) => setResolutionSummary(event.target.value)} />
                )}
                {targetStatus === 'Closed' && (
                  <TextInput label="Closure summary" value={resolutionSummary} onChange={(event) => setResolutionSummary(event.target.value)} />
                )}
                {targetStatus === 'Waiting' && (
                  <TextInput label="Waiting reason" value={transitionNote} onChange={(event) => setTransitionNote(event.target.value)} />
                )}
                {error && <p className="text-sm text-status-error">{error}</p>}
                <button type="button" onClick={runTransition} disabled={!targetStatus || transition.isPending} className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60">
                  <Save size={14} />
                  Save Status
                </button>
              </div>
            )}
          </Card>
        </div>

        <Card>
          <CardHeader><CardTitle>Work Management</CardTitle></CardHeader>
          <div className="grid gap-3 md:grid-cols-2">
            <TextInput label="Summary" value={summary} onChange={(event) => setSummary(event.target.value)} />
            <label className="block space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Priority</span>
              <select value={priority} onChange={(event) => setPriority(event.target.value as ServiceCasePriority)} className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet">
                {PRIORITIES.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
            </label>
            <TextInput label="Due date" type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
            <AssigneePicker label="Owner" selectedUser={owner} onSelect={setOwner} required />
          </div>
          <div className="mt-3 grid gap-3 md:grid-cols-2">
            <label className="block space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Description</span>
              <textarea value={description} onChange={(event) => setDescription(event.target.value)} className="min-h-[90px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet" />
            </label>
            <label className="block space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Follow-up summary</span>
              <textarea value={followUpSummary} onChange={(event) => setFollowUpSummary(event.target.value)} className="min-h-[90px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet" />
            </label>
          </div>
          {error && <p className="mt-3 text-sm text-status-error">{error}</p>}
          <button type="button" onClick={saveWorkManagement} disabled={updateCase.isPending || serviceCase.status === 'Closed'} className="mt-4 inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60">
            <Save size={14} />
            Save Work
          </button>
        </Card>

        <div className="grid gap-4 xl:grid-cols-3">
          <Rail title="Claim Reference">
            <Detail label="Carrier Claim" value={serviceCase.claimReference?.carrierClaimNumber ?? 'Not recorded'} />
            <Detail label="Date Of Loss" value={serviceCase.claimReference?.dateOfLoss ? formatDate(serviceCase.claimReference.dateOfLoss) : 'Not recorded'} />
            <Detail label="Claimant" value={serviceCase.claimReference?.claimantDisplayName ?? 'Not recorded'} />
          </Rail>
          <Rail title="Linked Communications">
            <p className="text-2xl font-semibold text-text-primary">{serviceCase.communicationLinks.length}</p>
            {serviceCase.communicationLinks.map((link) => (
              <p key={link.communicationEventId} className="text-xs text-text-muted">{link.linkType} · {formatDateTime(link.createdAt)}</p>
            ))}
          </Rail>
          <Rail title="Follow-Up Tasks">
            <p className="text-2xl font-semibold text-text-primary">{serviceCase.taskLinks.length}</p>
            {serviceCase.taskLinks.map((link) => (
              <p key={link.taskId} className="text-xs text-text-muted">{link.relationship} · {formatDateTime(link.createdAt)}</p>
            ))}
          </Rail>
        </div>

        <Card>
          <CardHeader><CardTitle>History</CardTitle></CardHeader>
          <div className="space-y-2">
            {serviceCase.transitions.map((item) => (
              <div key={`${item.occurredAt}-${item.toStatus}`} className="rounded-lg border border-surface-border px-3 py-2">
                <p className="text-sm text-text-primary">{item.fromStatus ?? 'Created'} → {item.toStatus}</p>
                <p className="mt-1 text-xs text-text-muted">{formatDateTime(item.occurredAt)}</p>
              </div>
            ))}
            {serviceCase.communicationLinks.map((item) => (
              <div key={`communication-${item.communicationEventId}`} className="rounded-lg border border-surface-border px-3 py-2">
                <p className="text-sm text-text-primary">Communication linked · {item.linkType}</p>
                <p className="mt-1 text-xs text-text-muted">{formatDateTime(item.createdAt)}</p>
              </div>
            ))}
            {serviceCase.taskLinks.map((item) => (
              <div key={`task-${item.taskId}`} className="rounded-lg border border-surface-border px-3 py-2">
                <p className="text-sm text-text-primary">Follow-up task linked · {item.relationship}</p>
                <p className="mt-1 text-xs text-text-muted">{formatDateTime(item.createdAt)}</p>
              </div>
            ))}
          </div>
        </Card>

        <ClaimModal
          open={claimOpen}
          onClose={() => setClaimOpen(false)}
          rowVersion={serviceCase.rowVersion}
          initial={serviceCase.claimReference}
          busy={updateClaim.isPending}
          onSave={(body) => updateClaim.mutateAsync(body).then(() => setClaimOpen(false))}
        />
        <FollowUpModal
          open={taskOpen}
          onClose={() => setTaskOpen(false)}
          rowVersion={serviceCase.rowVersion}
          busy={createTask.isPending}
          onSave={(body) => createTask.mutateAsync(body).then(() => setTaskOpen(false))}
        />
        <CommunicationModal
          open={communicationOpen}
          onClose={() => setCommunicationOpen(false)}
          rowVersion={serviceCase.rowVersion}
          busy={linkCommunication.isPending}
          onSave={(body) => linkCommunication.mutateAsync(body).then(() => setCommunicationOpen(false))}
        />
      </div>
    </DashboardLayout>
  );
}

function ClaimModal({ open, onClose, initial, rowVersion, busy, onSave }: {
  open: boolean;
  onClose: () => void;
  initial: { carrierClaimNumber: string | null; dateOfLoss: string | null; claimantDisplayName: string | null; lossSummary: string | null; carrierContactReference: string | null } | null;
  rowVersion: number;
  busy: boolean;
  onSave: (body: { carrierClaimNumber: string | null; dateOfLoss: string | null; claimantDisplayName: string | null; lossSummary: string | null; carrierContactReference: string | null; rowVersion: number }) => Promise<unknown>;
}) {
  const [carrierClaimNumber, setCarrierClaimNumber] = useState(initial?.carrierClaimNumber ?? '');
  const [dateOfLoss, setDateOfLoss] = useState(initial?.dateOfLoss ?? '');
  const [claimantDisplayName, setClaimantDisplayName] = useState(initial?.claimantDisplayName ?? '');
  const [lossSummary, setLossSummary] = useState(initial?.lossSummary ?? '');
  const [carrierContactReference, setCarrierContactReference] = useState(initial?.carrierContactReference ?? '');
  const [error, setError] = useState('');

  async function save() {
    try {
      await onSave({
        carrierClaimNumber: normalize(carrierClaimNumber),
        dateOfLoss: dateOfLoss || null,
        claimantDisplayName: normalize(claimantDisplayName),
        lossSummary: normalize(lossSummary),
        carrierContactReference: normalize(carrierContactReference),
        rowVersion,
      });
      setError('');
    } catch (saveError) {
      setError(describeError(saveError));
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Claim reference">
      <div className="space-y-3">
        <TextInput label="Carrier claim number" value={carrierClaimNumber} onChange={(event) => setCarrierClaimNumber(event.target.value)} />
        <TextInput label="Date of loss" type="date" value={dateOfLoss} onChange={(event) => setDateOfLoss(event.target.value)} />
        <TextInput label="Claimant" value={claimantDisplayName} onChange={(event) => setClaimantDisplayName(event.target.value)} />
        <TextInput label="Carrier contact" value={carrierContactReference} onChange={(event) => setCarrierContactReference(event.target.value)} />
        <textarea value={lossSummary} onChange={(event) => setLossSummary(event.target.value)} className="min-h-[86px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet" />
        {error && <p className="text-sm text-status-error">{error}</p>}
        <ActionRow onClose={onClose} onSave={save} busy={busy} />
      </div>
    </Modal>
  );
}

function FollowUpModal({ open, onClose, rowVersion, busy, onSave }: {
  open: boolean;
  onClose: () => void;
  rowVersion: number;
  busy: boolean;
  onSave: (body: { title: string; description: string | null; assignedToUserId: string; dueDate: string | null; priority: 'Low' | 'Normal' | 'High'; rowVersion: number }) => Promise<unknown>;
}) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [priority, setPriority] = useState<'Low' | 'Normal' | 'High'>('Normal');
  const [assignee, setAssignee] = useState<UserSummaryDto | null>(null);
  const [error, setError] = useState('');

  async function save() {
    if (!title.trim() || !assignee) {
      setError('Title and assignee are required.');
      return;
    }
    try {
      await onSave({
        title: title.trim(),
        description: normalize(description),
        assignedToUserId: assignee.userId,
        dueDate: dueDate || null,
        priority,
        rowVersion,
      });
      setError('');
    } catch (saveError) {
      setError(describeError(saveError));
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Follow-up task">
      <div className="space-y-3">
        <TextInput label="Title" value={title} onChange={(event) => setTitle(event.target.value)} />
        <textarea value={description} onChange={(event) => setDescription(event.target.value)} className="min-h-[80px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet" />
        <div className="grid gap-3 md:grid-cols-2">
          <TextInput label="Due date" type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
          <label className="block space-y-1.5">
            <span className="block text-xs font-medium text-text-secondary">Priority</span>
            <select value={priority} onChange={(event) => setPriority(event.target.value as 'Low' | 'Normal' | 'High')} className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet">
              <option value="Low">Low</option>
              <option value="Normal">Normal</option>
              <option value="High">High</option>
            </select>
          </label>
        </div>
        <AssigneePicker selectedUser={assignee} onSelect={setAssignee} required />
        {error && <p className="text-sm text-status-error">{error}</p>}
        <ActionRow onClose={onClose} onSave={save} busy={busy} />
      </div>
    </Modal>
  );
}

function CommunicationModal({ open, onClose, rowVersion, busy, onSave }: {
  open: boolean;
  onClose: () => void;
  rowVersion: number;
  busy: boolean;
  onSave: (body: { communicationEventId: string; linkType: 'Context' | 'Evidence' | 'FollowUp'; rowVersion: number }) => Promise<unknown>;
}) {
  const [communicationEventId, setCommunicationEventId] = useState('');
  const [linkType, setLinkType] = useState<'Context' | 'Evidence' | 'FollowUp'>('Context');
  const [error, setError] = useState('');

  async function save() {
    if (!communicationEventId.trim()) {
      setError('Communication event ID is required.');
      return;
    }
    try {
      await onSave({ communicationEventId: communicationEventId.trim(), linkType, rowVersion });
      setCommunicationEventId('');
      setLinkType('Context');
      setError('');
    } catch (saveError) {
      setError(describeError(saveError));
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Link communication">
      <div className="space-y-3">
        <TextInput label="Communication event ID" value={communicationEventId} onChange={(event) => setCommunicationEventId(event.target.value)} />
        <label className="block space-y-1.5">
          <span className="block text-xs font-medium text-text-secondary">Link type</span>
          <select value={linkType} onChange={(event) => setLinkType(event.target.value as 'Context' | 'Evidence' | 'FollowUp')} className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet">
            <option value="Context">Context</option>
            <option value="Evidence">Evidence</option>
            <option value="FollowUp">Follow-up</option>
          </select>
        </label>
        {error && <p className="text-sm text-status-error">{error}</p>}
        <ActionRow onClose={onClose} onSave={save} busy={busy} />
      </div>
    </Modal>
  );
}

function Detail({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase text-text-muted">{label}</p>
      <div className="mt-1 text-sm text-text-primary">{value}</div>
    </div>
  );
}

function Rail({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Card className="border border-surface-border bg-surface-card/35">
      <CardHeader><CardTitle>{title}</CardTitle></CardHeader>
      <div className="space-y-3">{children}</div>
    </Card>
  );
}

function ActionRow({ onClose, onSave, busy }: { onClose: () => void; onSave: () => void; busy: boolean }) {
  return (
    <div className="flex justify-end gap-2">
      <button type="button" onClick={onClose} className="rounded-lg border border-surface-border px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card">Cancel</button>
      <button type="button" onClick={onSave} disabled={busy} className="rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60">Save</button>
    </div>
  );
}

function normalize(value: string): string | null {
  return value.trim() ? value.trim() : null;
}

function describeError(error: unknown): string {
  return error instanceof ApiError ? error.problem?.detail ?? error.message : 'The change could not be saved.';
}

function formatDate(value: string): string {
  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString();
}
