/**
 * Registered component `renewals.companion_context` (F0038-S0003 drill view). Renders
 * the per-renewal drill context returned by the engine via Neuron: account, broker,
 * workflow state, expiry, and recent timeline. Read-only; no model-generated markup.
 */
export const renewalCompanionContextSchema = {
  type: 'object',
  required: ['renewalId', 'accountName', 'workflowState'],
  additionalProperties: true,
  properties: {
    renewalId: { type: 'string' },
    accountName: { type: 'string' },
    brokerName: { type: 'string' },
    workflowState: { type: 'string' },
    expiryDate: { type: 'string' },
    canDraftOutreach: { type: 'boolean' },
    timeline: { type: 'array' },
  },
} as const;

interface TimelineItem {
  id?: string;
  eventType?: string;
  eventDescription?: string;
  actorDisplayName?: string;
  occurredAt?: string;
}

export function RenewalCompanionContext({ props }: { props: Record<string, unknown> }) {
  const accountName = String(props.accountName ?? '');
  const brokerName = props.brokerName ? String(props.brokerName) : undefined;
  const workflowState = String(props.workflowState ?? '');
  const expiryDate = props.expiryDate ? String(props.expiryDate) : undefined;
  const timeline = (props.timeline as TimelineItem[] | undefined) ?? [];

  return (
    <div data-testid="companion-context" className="rounded-md border border-surface-border bg-surface-card p-3">
      <div className="text-sm font-semibold text-text-primary">{accountName}</div>
      <dl className="mt-1 grid grid-cols-2 gap-x-3 gap-y-0.5 text-xs text-text-muted">
        {brokerName ? (
          <>
            <dt>Broker</dt>
            <dd className="text-text-secondary">{brokerName}</dd>
          </>
        ) : null}
        <dt>Status</dt>
        <dd className="text-text-secondary">{workflowState}</dd>
        {expiryDate ? (
          <>
            <dt>Expires</dt>
            <dd className="text-text-secondary">{expiryDate}</dd>
          </>
        ) : null}
      </dl>
      {timeline.length > 0 ? (
        <div className="mt-2">
          <div className="text-xs font-medium text-text-secondary">Recent activity</div>
          <ul className="mt-1 space-y-0.5">
            {timeline.map((event, index) => (
              <li key={event.id ?? index} className="text-xs text-text-muted">
                {event.eventDescription ?? event.eventType}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
