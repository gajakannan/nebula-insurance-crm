import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import {
  isRegisteredComponent,
  renderRegisteredComponent,
} from './componentRegistry';

describe('componentRegistry (F0038-S0002 rendering contract)', () => {
  it('reports registration status', () => {
    expect(isRegisteredComponent('renewals.needs_attention_list')).toBe(true);
    expect(isRegisteredComponent('evil.script')).toBe(false);
  });

  it('renders a safe fallback for an unregistered component id', () => {
    render(renderRegisteredComponent('evil.script', { anything: true }));
    const fallback = screen.getByTestId('registry-fallback');
    expect(fallback).toHaveAttribute('data-reason', 'unregistered-component');
  });

  it('renders a safe fallback when props fail schema validation', () => {
    render(renderRegisteredComponent('renewals.needs_attention_list', { items: 'not-an-array' }));
    expect(screen.getByTestId('registry-fallback')).toHaveAttribute('data-reason', 'invalid-props');
  });

  it('renders the companion-context drill component', () => {
    render(
      renderRegisteredComponent('renewals.companion_context', {
        renewalId: 'r1', accountName: 'Acme Mfg', brokerName: 'Atlas Brokerage', workflowState: 'Identified',
      }),
    );
    expect(screen.getByTestId('companion-context')).toBeInTheDocument();
    expect(screen.getByText('Atlas Brokerage')).toBeInTheDocument();
  });

  it('renders the outreach draft editor component', () => {
    render(
      renderRegisteredComponent('outreach.draft_editor', {
        renewalId: 'r1', accountName: 'Acme Mfg', draftBody: 'Hi, can we connect?',
      }),
    );
    expect(screen.getByTestId('draft-editor')).toBeInTheDocument();
    expect(screen.getByText('Hi, can we connect?')).toBeInTheDocument();
  });

  it('renders the registered component when props are valid', () => {
    render(
      renderRegisteredComponent('renewals.needs_attention_list', {
        items: [
          { renewalId: 'r1', accountName: 'Acme Mfg', expiresInDays: 12, workflowState: 'Identified' },
        ],
      }),
    );
    expect(screen.getByText('Acme Mfg')).toBeInTheDocument();
    expect(screen.queryByTestId('registry-fallback')).not.toBeInTheDocument();
  });
});
