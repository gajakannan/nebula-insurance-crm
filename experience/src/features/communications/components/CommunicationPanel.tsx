import { useMemo, useState, type Dispatch, type SetStateAction } from 'react';
import { CalendarClock, MessageSquarePlus, ShieldAlert } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Skeleton } from '@/components/ui/Skeleton';
import { ApiError } from '@/services/api';
import { useCurrentUser } from '@/features/auth';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import {
  useCommunicationHistory,
  useCorrectCommunication,
  useCreateCommunication,
  useCreateCommunicationFollowUp,
} from '../hooks';
import type {
  CommunicationEventDto,
  CommunicationLinkEntityType,
  CommunicationType,
} from '../types';

interface CommunicationPanelProps {
  entityType: CommunicationLinkEntityType;
  entityId: string;
  entityLabel: string;
}

interface CommunicationFormState {
  type: CommunicationType;
  direction: 'Inbound' | 'Outbound' | 'Internal';
  summary: string;
  body: string;
  occurredAt: string;
  participantName: string;
  participantEmail: string;
  followUp: boolean;
  followUpTitle: string;
  followUpDueDate: string;
  followUpPriority: 'Low' | 'Normal' | 'High';
  followUpAssignee: UserSummaryDto | null;
}

const initialForm = (): CommunicationFormState => ({
  type: 'Note',
  direction: 'Internal',
  summary: '',
  body: '',
  occurredAt: toDatetimeLocal(new Date()),
  participantName: '',
  participantEmail: '',
  followUp: false,
  followUpTitle: '',
  followUpDueDate: '',
  followUpPriority: 'Normal',
  followUpAssignee: null,
});

export function CommunicationPanel({ entityType, entityId, entityLabel }: CommunicationPanelProps) {
  const currentUser = useCurrentUser();
  const historyQuery = useCommunicationHistory(entityType, entityId);
  const createCommunication = useCreateCommunication(entityType, entityId);
  const createFollowUp = useCreateCommunicationFollowUp(entityType, entityId);
  const correctCommunication = useCorrectCommunication(entityType, entityId);

  const [addOpen, setAddOpen] = useState(false);
  const [detail, setDetail] = useState<CommunicationEventDto | null>(null);
  const [form, setForm] = useState(initialForm);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState('');
  const [correctionReason, setCorrectionReason] = useState('');
  const [correctionSummary, setCorrectionSummary] = useState('');
  const [redactionReason, setRedactionReason] = useState('');

  const communications = historyQuery.data?.data ?? [];
  const canRedact = currentUser?.roles?.includes('Admin') ?? false;

  const groupedCount = useMemo(() => {
    return communications.reduce((count, item) => count + item.followUpTaskIds.length, 0);
  }, [communications]);

  const openAdd = () => {
    setForm(initialForm());
    setFormErrors({});
    setServerError('');
    setAddOpen(true);
  };

  const submitCommunication = async (event: React.FormEvent) => {
    event.preventDefault();
    const errors: Record<string, string> = {};
    if (!form.summary.trim()) errors.summary = 'Summary is required.';
    if (!form.occurredAt) errors.occurredAt = 'Time is required.';
    if (form.followUp && !form.followUpTitle.trim()) errors.followUpTitle = 'Follow-up title is required.';
    if (form.followUp && !form.followUpAssignee) errors.followUpAssignee = 'Assignee is required.';
    setFormErrors(errors);
    if (Object.keys(errors).length > 0) return;

    try {
      await createCommunication.mutateAsync({
        type: form.type,
        direction: form.direction,
        summary: form.summary.trim(),
        body: form.body.trim() || null,
        occurredAt: new Date(form.occurredAt).toISOString(),
        emailReference: null,
        participants: form.participantName.trim()
          ? [{
              displayName: form.participantName.trim(),
              email: form.participantEmail.trim() || null,
              participantType: 'Other',
              role: null,
              linkedEntityType: null,
              linkedEntityId: null,
            }]
          : [],
        links: [{ entityType, entityId, isPrimary: true, label: entityLabel }],
        followUp: form.followUp && form.followUpAssignee
          ? {
              title: form.followUpTitle.trim(),
              description: form.summary.trim(),
              priority: form.followUpPriority,
              dueDate: form.followUpDueDate || null,
              assignedToUserId: form.followUpAssignee.userId,
              linkedEntityType: entityType,
              linkedEntityId: entityId,
            }
          : null,
      });
      setAddOpen(false);
    } catch (error) {
      setServerError(error instanceof ApiError ? error.message : 'Unable to save communication.');
    }
  };

  const submitFollowUp = async (communication: CommunicationEventDto) => {
    if (!form.followUpTitle.trim() || !form.followUpAssignee) {
      setFormErrors({
        followUpTitle: !form.followUpTitle.trim() ? 'Follow-up title is required.' : '',
        followUpAssignee: !form.followUpAssignee ? 'Assignee is required.' : '',
      });
      return;
    }

    await createFollowUp.mutateAsync({
      communicationId: communication.id,
      body: {
        title: form.followUpTitle.trim(),
        description: communication.summary,
        priority: form.followUpPriority,
        dueDate: form.followUpDueDate || null,
        assignedToUserId: form.followUpAssignee.userId,
        linkedEntityType: entityType,
        linkedEntityId: entityId,
      },
    });
    setDetail(null);
  };

  const submitCorrection = async (communication: CommunicationEventDto) => {
    if (!correctionReason.trim() || !correctionSummary.trim()) return;
    await correctCommunication.mutateAsync({
      communicationId: communication.id,
      body: {
        action: 'Correct',
        reason: correctionReason.trim(),
        summary: correctionSummary.trim(),
        body: communication.body ?? null,
      },
    });
    setDetail(null);
  };

  const submitRedaction = async (communication: CommunicationEventDto) => {
    if (!redactionReason.trim()) return;
    await correctCommunication.mutateAsync({
      communicationId: communication.id,
      body: { action: 'Redact', reason: redactionReason.trim() },
    });
    setDetail(null);
  };

  if (historyQuery.isLoading) {
    return <Skeleton className="h-44 w-full" />;
  }

  if (historyQuery.error) {
    return (
      <div className="rounded-lg border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-100">
        Unable to load communications.
      </div>
    );
  }

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-text-primary">Communications</h2>
          <p className="mt-1 text-sm text-text-secondary">
            {communications.length} captured · {groupedCount} follow-up links
          </p>
        </div>
        <button
          type="button"
          onClick={openAdd}
          className="inline-flex items-center gap-2 rounded-md bg-nebula-violet px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
        >
          <MessageSquarePlus className="h-4 w-4" aria-hidden />
          Add
        </button>
      </div>

      {communications.length === 0 ? (
        <div className="rounded-lg border border-dashed border-surface-border p-6 text-sm text-text-secondary">
          No communication activity captured for this record yet.
        </div>
      ) : (
        <div className="divide-y divide-surface-border rounded-lg border border-surface-border">
          {communications.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => {
                setDetail(item);
                setCorrectionReason('');
                setCorrectionSummary(item.isRedacted ? '' : item.summary);
                setRedactionReason('');
                setForm((current) => ({
                  ...current,
                  followUpTitle: `Follow up: ${item.summary}`,
                  followUpAssignee: null,
                  followUpDueDate: '',
                }));
              }}
              className="flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-surface-card-hover"
            >
              <CalendarClock className="mt-0.5 h-4 w-4 shrink-0 text-text-muted" aria-hidden />
              <span className="min-w-0 flex-1">
                <span className="flex flex-wrap items-center gap-2">
                  <span className="text-sm font-medium text-text-primary">{item.summary}</span>
                  <span className="rounded bg-surface-card px-2 py-0.5 text-xs text-text-secondary">{item.type}</span>
                  {item.isRedacted && <ShieldAlert className="h-4 w-4 text-red-300" aria-label="Redacted" />}
                </span>
                <span className="mt-1 block text-xs text-text-muted">
                  {formatDateTime(item.occurredAt)}
                  {item.followUpTaskIds.length > 0 ? ` · ${item.followUpTaskIds.length} follow-up` : ''}
                </span>
              </span>
            </button>
          ))}
        </div>
      )}

      <Modal open={addOpen} onClose={() => setAddOpen(false)} title="Add Communication" className="max-w-2xl">
        <form className="space-y-4" onSubmit={submitCommunication}>
          <CommunicationFields form={form} setForm={setForm} errors={formErrors} />
          {serverError && <p className="text-sm text-red-300">{serverError}</p>}
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setAddOpen(false)} className="rounded-md px-3 py-2 text-sm text-text-secondary hover:bg-surface-card-hover">Cancel</button>
            <button type="submit" className="rounded-md bg-nebula-violet px-3 py-2 text-sm font-medium text-white">Save</button>
          </div>
        </form>
      </Modal>

      <Modal open={!!detail} onClose={() => setDetail(null)} title="Communication Detail" className="max-w-2xl">
        {detail && (
          <div className="space-y-5">
            <div className="rounded-lg border border-surface-border p-4">
              <div className="text-sm font-medium text-text-primary">{detail.summary}</div>
              {detail.body && <p className="mt-2 whitespace-pre-wrap text-sm text-text-secondary">{detail.body}</p>}
              <p className="mt-3 text-xs text-text-muted">{formatDateTime(detail.occurredAt)}</p>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-3">
                <h3 className="text-xs font-semibold uppercase tracking-wide text-text-muted">Follow-up</h3>
                <input className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-sm text-text-primary" value={form.followUpTitle} onChange={(e) => setForm((current) => ({ ...current, followUpTitle: e.target.value }))} />
                <AssigneePicker selectedUser={form.followUpAssignee} onSelect={(assignee) => setForm((current) => ({ ...current, followUpAssignee: assignee }))} />
                {formErrors.followUpAssignee && <p className="text-xs text-red-300">{formErrors.followUpAssignee}</p>}
                <button type="button" onClick={() => submitFollowUp(detail)} className="rounded-md bg-nebula-violet px-3 py-2 text-sm font-medium text-white">Create Follow-up</button>
              </div>
              <div className="space-y-3">
                <h3 className="text-xs font-semibold uppercase tracking-wide text-text-muted">Correction</h3>
                <input className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-sm text-text-primary" value={correctionSummary} onChange={(e) => setCorrectionSummary(e.target.value)} />
                <textarea className="min-h-20 w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-sm text-text-primary" value={correctionReason} onChange={(e) => setCorrectionReason(e.target.value)} placeholder="Reason" />
                <button type="button" onClick={() => submitCorrection(detail)} className="rounded-md border border-surface-border px-3 py-2 text-sm text-text-primary hover:bg-surface-card-hover">Save Correction</button>
              </div>
            </div>
            {canRedact && !detail.isRedacted && (
              <div className="space-y-3 border-t border-surface-border pt-4">
                <h3 className="text-xs font-semibold uppercase tracking-wide text-red-200">Redaction</h3>
                <textarea className="min-h-20 w-full rounded-md border border-red-500/30 bg-surface-base px-3 py-2 text-sm text-text-primary" value={redactionReason} onChange={(e) => setRedactionReason(e.target.value)} placeholder="Reason" />
                <button type="button" onClick={() => submitRedaction(detail)} className="rounded-md bg-red-600 px-3 py-2 text-sm font-medium text-white">Redact</button>
              </div>
            )}
          </div>
        )}
      </Modal>
    </section>
  );
}

function CommunicationFields({
  form,
  setForm,
  errors,
}: {
  form: CommunicationFormState;
  setForm: Dispatch<SetStateAction<CommunicationFormState>>;
  errors: Record<string, string>;
}) {
  const updateForm = (patch: Partial<CommunicationFormState>) => {
    setForm((current) => ({ ...current, ...patch }));
  };

  return (
    <div className="grid gap-4">
      <div className="grid gap-3 md:grid-cols-3">
        <label className="space-y-1 text-sm">
          <span className="text-text-secondary">Type</span>
          <select className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.type} onChange={(e) => updateForm({ type: e.target.value as CommunicationType })}>
            <option>Note</option>
            <option>Call</option>
            <option>Meeting</option>
            <option>EmailReference</option>
          </select>
        </label>
        <label className="space-y-1 text-sm">
          <span className="text-text-secondary">Direction</span>
          <select className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.direction} onChange={(e) => updateForm({ direction: e.target.value as CommunicationFormState['direction'] })}>
            <option>Internal</option>
            <option>Inbound</option>
            <option>Outbound</option>
          </select>
        </label>
        <label className="space-y-1 text-sm">
          <span className="text-text-secondary">Occurred</span>
          <input type="datetime-local" className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.occurredAt} onChange={(e) => updateForm({ occurredAt: e.target.value })} />
          {errors.occurredAt && <span className="text-xs text-red-300">{errors.occurredAt}</span>}
        </label>
      </div>
      <label className="space-y-1 text-sm">
        <span className="text-text-secondary">Summary</span>
        <input className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.summary} onChange={(e) => updateForm({ summary: e.target.value })} />
        {errors.summary && <span className="text-xs text-red-300">{errors.summary}</span>}
      </label>
      <label className="space-y-1 text-sm">
        <span className="text-text-secondary">Notes</span>
        <textarea className="min-h-24 w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.body} onChange={(e) => updateForm({ body: e.target.value })} />
      </label>
      <div className="grid gap-3 md:grid-cols-2">
        <label className="space-y-1 text-sm">
          <span className="text-text-secondary">Participant</span>
          <input className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.participantName} onChange={(e) => updateForm({ participantName: e.target.value })} />
        </label>
        <label className="space-y-1 text-sm">
          <span className="text-text-secondary">Participant email</span>
          <input className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.participantEmail} onChange={(e) => updateForm({ participantEmail: e.target.value })} />
        </label>
      </div>
      <label className="flex items-center gap-2 text-sm text-text-secondary">
        <input type="checkbox" checked={form.followUp} onChange={(e) => updateForm({ followUp: e.target.checked })} />
        Create follow-up task
      </label>
      {form.followUp && (
        <div className="grid gap-3 rounded-lg border border-surface-border p-3 md:grid-cols-2">
          <label className="space-y-1 text-sm">
            <span className="text-text-secondary">Task title</span>
            <input className="w-full rounded-md border border-surface-border bg-surface-base px-3 py-2 text-text-primary" value={form.followUpTitle} onChange={(e) => updateForm({ followUpTitle: e.target.value })} />
            {errors.followUpTitle && <span className="text-xs text-red-300">{errors.followUpTitle}</span>}
          </label>
          <AssigneePicker selectedUser={form.followUpAssignee} onSelect={(assignee) => updateForm({ followUpAssignee: assignee })} />
        </div>
      )}
    </div>
  );
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function toDatetimeLocal(value: Date): string {
  const offset = value.getTimezoneOffset();
  const local = new Date(value.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}
