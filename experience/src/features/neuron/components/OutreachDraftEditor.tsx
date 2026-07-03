import { useState } from 'react';
import { cn } from '@/lib/utils';
import { useAction } from '../registry/actionContext';

/**
 * Registered component `outreach.draft_editor` (F0038-S0005/S0006). Renders the AI-generated
 * outreach draft in-chat: labelled "AI-generated draft" + InternalOnly, editable, with
 * `[Edit]` and `[Send (mock)]`. Send dispatches `mock_send` with the edited body; the engine
 * commits Identified→Outreach and fakes SMTP. Edited content is the body sent.
 */
export const outreachDraftEditorSchema = {
  type: 'object',
  required: ['renewalId', 'draftBody'],
  additionalProperties: true,
  properties: {
    renewalId: { type: 'string' },
    accountName: { type: 'string' },
    draftBody: { type: 'string' },
    timelineEventId: { type: 'string' },
    internalOnly: { type: 'boolean' },
    label: { type: 'string' },
  },
} as const;

export function OutreachDraftEditor({ props }: { props: Record<string, unknown> }) {
  const renewalId = String(props.renewalId ?? '');
  const accountName = props.accountName ? String(props.accountName) : undefined;
  const timelineEventId = props.timelineEventId ? String(props.timelineEventId) : undefined;
  const label = props.label ? String(props.label) : 'AI-generated draft';

  const [body, setBody] = useState(String(props.draftBody ?? ''));
  const [editing, setEditing] = useState(false);
  const { dispatch, pending } = useAction();

  return (
    <div data-testid="draft-editor" className="rounded-md border border-surface-border bg-surface-card p-3">
      <div className="mb-1 flex flex-wrap items-center gap-1 text-xs">
        <span className="rounded bg-nebula-violet/20 px-1.5 py-0.5 text-nebula-violet">{label}</span>
        <span className="rounded bg-surface-highlight px-1.5 py-0.5 text-text-muted">InternalOnly</span>
      </div>

      {editing ? (
        <textarea
          data-testid="draft-editor-textarea"
          value={body}
          onChange={(event) => setBody(event.target.value)}
          rows={4}
          className="w-full resize-none rounded-md border border-surface-border bg-surface-base px-2 py-1 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
        />
      ) : (
        <p className="whitespace-pre-wrap text-sm text-text-secondary">{body}</p>
      )}

      <div className="mt-2 flex items-center gap-2">
        <button
          type="button"
          onClick={() => setEditing((value) => !value)}
          className="inline-flex items-center rounded-md border border-surface-border px-2 py-1 text-xs text-text-secondary hover:bg-surface-highlight"
        >
          {editing ? 'Done' : 'Edit'}
        </button>
        <button
          type="button"
          onClick={() =>
            dispatch({
              actionType: 'mock_send',
              payload: { renewalId, editedBody: body, accountName, draftTimelineEventId: timelineEventId },
            })
          }
          disabled={pending || body.trim().length === 0}
          className={cn(
            'inline-flex items-center rounded-md border border-nebula-fuchsia/35 bg-nebula-fuchsia/10 px-2 py-1 text-xs text-nebula-fuchsia',
            'hover:bg-nebula-fuchsia/20 disabled:cursor-not-allowed disabled:opacity-65',
          )}
        >
          Send (mock)
        </button>
      </div>
    </div>
  );
}
