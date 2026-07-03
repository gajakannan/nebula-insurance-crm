import Ajv, { type ValidateFunction } from 'ajv';
import addFormats from 'ajv-formats';
import type { ReactElement } from 'react';
import {
  RenewalsNeedsAttentionList,
  renewalsNeedsAttentionSchema,
} from '../components/RenewalsNeedsAttentionList';
import {
  RenewalCompanionContext,
  renewalCompanionContextSchema,
} from '../components/RenewalCompanionContext';
import {
  OutreachDraftEditor,
  outreachDraftEditorSchema,
} from '../components/OutreachDraftEditor';

/**
 * F0038-S0002 component registry (intake L1). The model/Neuron picks a component id +
 * props; React renders only *registered* components whose props pass schema validation.
 * An unregistered id or invalid props renders a safe, non-executable fallback — never
 * model-generated markup.
 */

const ajv = new Ajv({ allErrors: true, strict: false });
addFormats(ajv);

interface RegistryEntry {
  Component: (args: { props: Record<string, unknown> }) => ReactElement;
  validate: ValidateFunction;
}

const REGISTRY: Record<string, RegistryEntry> = {
  'renewals.needs_attention_list': {
    Component: RenewalsNeedsAttentionList,
    validate: ajv.compile(renewalsNeedsAttentionSchema),
  },
  'renewals.companion_context': {
    Component: RenewalCompanionContext,
    validate: ajv.compile(renewalCompanionContextSchema),
  },
  'outreach.draft_editor': {
    Component: OutreachDraftEditor,
    validate: ajv.compile(outreachDraftEditorSchema),
  },
};

export function isRegisteredComponent(id: string): boolean {
  return Object.prototype.hasOwnProperty.call(REGISTRY, id);
}

export function SafeFallback({ reason }: { reason: string }): ReactElement {
  // WHY: renders a static note only — never echoes the untrusted component id/props into
  // the DOM, and nothing executable. This is the "refuse to render" branch (F0038-S0002).
  return (
    <div
      role="note"
      data-testid="registry-fallback"
      data-reason={reason}
      className="rounded-md border border-surface-border bg-surface-card px-3 py-2 text-xs text-text-muted"
    >
      Unsupported content.
    </div>
  );
}

export function renderRegisteredComponent(
  id: string,
  props: Record<string, unknown>,
): ReactElement {
  const entry = REGISTRY[id];
  if (!entry) {
    return <SafeFallback reason="unregistered-component" />;
  }
  if (!entry.validate(props)) {
    return <SafeFallback reason="invalid-props" />;
  }
  const Component = entry.Component;
  return <Component props={props} />;
}
