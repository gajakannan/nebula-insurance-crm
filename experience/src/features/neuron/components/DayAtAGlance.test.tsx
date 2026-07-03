import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils/render-app';
import { DayAtAGlance } from './DayAtAGlance';

vi.mock('@/services/api', () => {
  class ApiError extends Error {
    constructor(
      public status: number,
      public problem: unknown = null,
    ) {
      super(`HTTP ${status}`);
      this.name = 'ApiError';
    }
  }
  return { api: { get: vi.fn(), post: vi.fn() }, ApiError };
});

import { api, ApiError } from '@/services/api';

const mockGet = api.get as unknown as ReturnType<typeof vi.fn>;
const mockPost = api.post as unknown as ReturnType<typeof vi.fn>;

function glanceFixture(overrides: Record<string, unknown> = {}) {
  return {
    thread_id: 't1',
    zones: [
      { zone_id: 'broker_activity', zone_status: 'inactive', title: 'Broker activity', detail: 'Not yet active.' },
      { zone_id: 'pipeline', zone_status: 'inactive', title: 'Pipeline', detail: 'Not yet active.' },
      { zone_id: 'renewals', zone_status: 'empty', title: 'Renewals', detail: 'Live renewals data is delivered in F0038-S0003.' },
      { zone_id: 'tasks', zone_status: 'inactive', title: 'Tasks', detail: 'Not yet active.' },
    ],
    message: {
      envelope_version: 1,
      thread_id: 't1',
      message_id: 'm1',
      role: 'assistant',
      parts: [
        { part_type: 'status', state: 'completed' },
        { part_type: 'text', text: "Here's your day at a glance." },
      ],
    },
    ...overrides,
  };
}

function glanceWithRenewalsContent() {
  const fixture = glanceFixture();
  (fixture.zones as Array<Record<string, unknown>>)[2] = {
    zone_id: 'renewals',
    zone_status: 'content',
    title: 'Renewals',
    component: 'renewals.needs_attention_list',
    props: {
      items: [
        { renewalId: 'r1', accountName: 'Acme Mfg', expiresInDays: 12, workflowState: 'Identified', noBrokerContact30d: true },
      ],
    },
  };
  return fixture;
}

describe('DayAtAGlance (F0038-S0002/S0003 shell)', () => {
  beforeEach(() => {
    mockGet.mockReset();
    mockPost.mockReset();
  });

  it('renders four zones with Renewals live and stubs not-yet-active', async () => {
    mockGet.mockResolvedValue(glanceFixture());
    const { container } = renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('day-at-a-glance');

    const renewals = screen.getByLabelText('Renewals');
    expect(renewals).toHaveAttribute('data-status', 'empty');
    expect(within(renewals).getByText('LIVE')).toBeInTheDocument();
    expect(screen.getByLabelText('Tasks')).toHaveAttribute('data-status', 'inactive');
    expect(screen.getAllByText('not yet active').length).toBe(3);

    const zoneEls = Array.from(container.querySelectorAll('[data-zone]'));
    expect(zoneEls[0]).toHaveAttribute('data-zone', 'renewals');
  });

  it('renders the assistant message envelope', async () => {
    mockGet.mockResolvedValue(glanceFixture());
    renderWithProviders(<DayAtAGlance />);
    expect(await screen.findByText("Here's your day at a glance.")).toBeInTheDocument();
  });

  it('isolates a failing zone to its own slot', async () => {
    const fixture = glanceFixture();
    (fixture.zones as Array<Record<string, unknown>>)[0] = {
      zone_id: 'broker_activity', zone_status: 'error', title: 'Broker activity',
      detail: 'This zone is temporarily unavailable.',
    };
    mockGet.mockResolvedValue(fixture);
    renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('day-at-a-glance');
    expect(screen.getByLabelText('Broker activity')).toHaveAttribute('data-status', 'error');
    expect(screen.getByLabelText('Renewals')).toHaveAttribute('data-status', 'empty');
  });

  it('shows a loading state while fetching', () => {
    mockGet.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<DayAtAGlance />);
    expect(screen.getByTestId('glance-loading')).toBeInTheDocument();
  });

  it('shows a retryable error state on a non-auth failure', async () => {
    mockGet.mockRejectedValue(new ApiError(500));
    renderWithProviders(<DayAtAGlance />);
    const error = await screen.findByTestId('glance-error');
    expect(within(error).getByText('Retry')).toBeInTheDocument();
  });

  it('shows an auth-required state (no retry) on 403', async () => {
    mockGet.mockRejectedValue(new ApiError(403));
    renderWithProviders(<DayAtAGlance />);
    const error = await screen.findByTestId('glance-error');
    expect(within(error).queryByText('Retry')).not.toBeInTheDocument();
    expect(within(error).getByText(/sign in again/i)).toBeInTheDocument();
  });

  it('drills a renewal via the View action and renders the returned context', async () => {
    mockGet.mockResolvedValue(glanceWithRenewalsContent());
    mockPost.mockResolvedValue({
      envelope_version: 1, thread_id: 't1', message_id: 'm2', role: 'assistant',
      parts: [
        { part_type: 'text', text: "Here's the latest on Acme Mfg." },
        {
          part_type: 'app', component: 'renewals.companion_context',
          props: {
            renewalId: 'r1', accountName: 'Acme Mfg', brokerName: 'Atlas Brokerage',
            workflowState: 'Identified', expiryDate: '2026-07-13', canDraftOutreach: false,
            timeline: [{ id: 'e1', eventDescription: 'Renewal created', occurredAt: '2026-06-29T00:00:00Z' }],
          },
        },
      ],
    });

    renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('needs-attention-list');
    await userEvent.click(screen.getByRole('button', { name: 'View' }));

    const drill = await screen.findByTestId('thread-message');
    expect(within(drill).getByTestId('companion-context')).toBeInTheDocument();
    expect(within(drill).getByText('Atlas Brokerage')).toBeInTheDocument();
    expect(within(drill).getByText('Renewal created')).toBeInTheDocument();

    // The action posted the drill request with the renewal id + thread.
    expect(mockPost).toHaveBeenCalledWith(
      expect.stringContaining('/v1/actions'),
      expect.objectContaining({ action_type: 'drill_renewal', payload: { renewalId: 'r1' }, thread_id: 't1' }),
    );
  });

  it('drafts an outreach and mock-sends the edited draft', async () => {
    mockGet.mockResolvedValue(glanceWithRenewalsContent());
    mockPost
      .mockResolvedValueOnce({
        envelope_version: 1, thread_id: 't1', message_id: 'm2', role: 'assistant',
        parts: [
          { part_type: 'text', text: "Here's a draft outreach for Acme Mfg." },
          {
            part_type: 'app', component: 'outreach.draft_editor',
            props: {
              renewalId: 'r1', accountName: 'Acme Mfg', draftBody: 'Hi, can we connect about the renewal?',
              timelineEventId: 'evt-1', internalOnly: true, label: 'AI-generated draft',
            },
          },
        ],
      })
      .mockResolvedValueOnce({
        envelope_version: 1, thread_id: 't1', message_id: 'm3', role: 'assistant',
        parts: [
          { part_type: 'status', state: 'completed', detail: 'Sent (simulated)' },
          { part_type: 'text', text: 'Sent (simulated). Acme Mfg moved to Outreach — no email was dispatched.' },
        ],
      });

    renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('needs-attention-list');

    await userEvent.click(screen.getByRole('button', { name: 'Draft outreach' }));
    const editor = await screen.findByTestId('draft-editor');
    expect(within(editor).getByText('AI-generated draft')).toBeInTheDocument();
    expect(within(editor).getByText('InternalOnly')).toBeInTheDocument();

    await userEvent.click(within(editor).getByRole('button', { name: 'Send (mock)' }));

    await waitFor(() => expect(screen.getByText(/moved to Outreach/)).toBeInTheDocument());
    expect(mockPost).toHaveBeenNthCalledWith(
      1,
      expect.stringContaining('/v1/actions'),
      expect.objectContaining({ action_type: 'draft_outreach', payload: expect.objectContaining({ renewalId: 'r1' }) }),
    );
    expect(mockPost).toHaveBeenNthCalledWith(
      2,
      expect.stringContaining('/v1/actions'),
      expect.objectContaining({ action_type: 'mock_send', payload: expect.objectContaining({ renewalId: 'r1', editedBody: 'Hi, can we connect about the renewal?' }) }),
    );
  });

  // --- F0038-S0007 scope-guarded composer ---

  it('sends a CRM message and renders the scope-guarded reply', async () => {
    mockGet.mockResolvedValue(glanceFixture());
    mockPost.mockResolvedValue({
      envelope_version: 1, thread_id: 't1', message_id: 'm2', role: 'assistant',
      parts: [{ part_type: 'text', text: "Here's what needs your attention in Renewals." }],
    });

    renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('day-at-a-glance');

    await userEvent.type(screen.getByLabelText('Message the companion'), 'which renewals need attention?');
    await userEvent.click(screen.getByRole('button', { name: 'Send message' }));

    // The user's turn is echoed and the assistant reply is rendered.
    await waitFor(() => expect(screen.getByText('which renewals need attention?')).toBeInTheDocument());
    expect(await screen.findByText(/needs your attention in Renewals/)).toBeInTheDocument();
    expect(mockPost).toHaveBeenCalledWith(
      expect.stringContaining('/v1/messages'),
      expect.objectContaining({ text: 'which renewals need attention?', thread_id: 't1' }),
    );
  });

  it('renders a polite CRM redirect for an off-topic message (no general answer)', async () => {
    mockGet.mockResolvedValue(glanceFixture());
    mockPost.mockResolvedValue({
      envelope_version: 1, thread_id: 't1', message_id: 'm2', role: 'assistant',
      parts: [
        {
          part_type: 'text',
          text: "I'm your CRM companion, so I can help with your renewals, outreach, and broker follow-ups — but not with that.",
        },
      ],
    });

    renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('day-at-a-glance');

    await userEvent.type(screen.getByLabelText('Message the companion'), "what's the weather in Paris?");
    await userEvent.click(screen.getByRole('button', { name: 'Send message' }));

    expect(await screen.findByText(/CRM companion/)).toBeInTheDocument();
    // The redirect is plain text — never a rendered app component / general answer.
    const replies = screen.getAllByTestId('thread-message');
    replies.forEach((reply) => expect(reply.querySelector('[data-testid="companion-context"]')).toBeNull());
  });

  it('does not send an empty message', async () => {
    mockGet.mockResolvedValue(glanceFixture());
    renderWithProviders(<DayAtAGlance />);
    await screen.findByTestId('day-at-a-glance');

    const sendButton = screen.getByRole('button', { name: 'Send message' });
    expect(sendButton).toBeDisabled();
    await userEvent.type(screen.getByLabelText('Message the companion'), '   ');
    expect(sendButton).toBeDisabled();
    expect(mockPost).not.toHaveBeenCalled();
  });
});
