import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils/render-app';
import { NeuronConversation } from './NeuronConversation';

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
  return {
    api: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() },
    ApiError,
  };
});

import { api, ApiError } from '@/services/api';

const mockGet = api.get as unknown as ReturnType<typeof vi.fn>;
const mockPost = api.post as unknown as ReturnType<typeof vi.fn>;
const mockPatch = api.patch as unknown as ReturnType<typeof vi.fn>;
const mockDelete = api.delete as unknown as ReturnType<typeof vi.fn>;

function thread(overrides: Record<string, unknown> = {}) {
  return {
    thread_id: 't1',
    anchor_type: 'free_form',
    anchor_ref: null,
    title: 'Renewals check',
    created_at: '2026-07-25T10:00:00Z',
    updated_at: '2026-07-25T10:05:00Z',
    last_sequence: 2,
    ...overrides,
  };
}

function envelope(id: string, role: 'user' | 'assistant', text: string) {
  return {
    envelope_version: 1,
    thread_id: 't1',
    message_id: id,
    role,
    parts: [{ part_type: 'text', text }],
  };
}

/** Route GET by path so tests describe server state rather than call order. */
function routeGet(
  threads: unknown[],
  history: unknown[],
  { historyError }: { historyError?: unknown } = {},
) {
  mockGet.mockImplementation((path: string) => {
    if (path.includes('/messages')) {
      if (historyError) return Promise.reject(historyError);
      return Promise.resolve({ data: history, next_after: null });
    }
    if (path.includes('/v1/threads')) {
      return Promise.resolve({ data: threads, next_cursor: null });
    }
    return Promise.reject(new Error(`unexpected GET ${path}`));
  });
}

describe('NeuronConversation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('replays the transcript from server history, in server order', async () => {
    routeGet(
      [thread()],
      [
        envelope('m1', 'user', 'show me my renewals'),
        envelope('m2', 'assistant', 'Here are your renewals.'),
      ],
    );

    renderWithProviders(<NeuronConversation />);

    const transcript = await screen.findByTestId('neuron-transcript');
    await waitFor(() => {
      expect(within(transcript).getByText('show me my renewals')).toBeInTheDocument();
    });
    const rendered = within(transcript)
      .getAllByText(/renewals/i)
      .map((node) => node.textContent);
    // The user's turn precedes the assistant's, because the server said so.
    expect(rendered[0]).toContain('show me my renewals');
    expect(rendered[1]).toContain('Here are your renewals.');
  });

  it('selects the most recent thread so the panel reopens where the user left off', async () => {
    routeGet([thread({ thread_id: 'recent' }), thread({ thread_id: 'older' })], []);

    renderWithProviders(<NeuronConversation />);

    await waitFor(() => {
      expect(mockGet).toHaveBeenCalledWith(expect.stringContaining('/v1/threads/recent/messages'));
    });
  });

  it('opens a thread for a brand-new user who has none', async () => {
    routeGet([], []);
    mockPost.mockResolvedValue(thread({ thread_id: 'fresh' }));

    renderWithProviders(<NeuronConversation />);

    await waitFor(() => {
      expect(mockPost).toHaveBeenCalledWith(
        expect.stringContaining('/v1/threads'),
        expect.objectContaining({ anchor_type: 'free_form' }),
      );
    });
  });

  it('shows an empty state rather than an error when a thread has no messages', async () => {
    routeGet([thread()], []);

    renderWithProviders(<NeuronConversation />);

    expect(
      await screen.findByText(/ask about your renewals, tasks, or pipeline/i),
    ).toBeInTheDocument();
  });

  it('sends a message and refetches history instead of trusting local state', async () => {
    routeGet([thread()], [envelope('m1', 'assistant', 'Here is your day.')]);
    mockPost.mockResolvedValue(envelope('m2', 'assistant', 'Here are your renewals.'));

    renderWithProviders(<NeuronConversation />);
    await screen.findByText('Here is your day.');

    const box = screen.getByRole('textbox');
    await userEvent.type(box, 'show me my renewals');
    await userEvent.keyboard('{Enter}');

    await waitFor(() => {
      expect(mockPost).toHaveBeenCalledWith(
        expect.stringContaining('/v1/messages'),
        expect.objectContaining({ text: 'show me my renewals', thread_id: 't1' }),
      );
    });
    // The server transcript is refetched — the panel does not append and move on.
    await waitFor(() => {
      const historyCalls = mockGet.mock.calls.filter(([path]: [string]) =>
        String(path).includes('/messages'),
      );
      expect(historyCalls.length).toBeGreaterThan(1);
    });
  });

  it('shows the user turn immediately while the send is in flight', async () => {
    routeGet([thread()], []);
    let resolveSend: (value: unknown) => void = () => {};
    mockPost.mockImplementation(
      () => new Promise((resolve) => { resolveSend = resolve; }),
    );

    renderWithProviders(<NeuronConversation />);
    await screen.findByTestId('neuron-transcript');

    await userEvent.type(screen.getByRole('textbox'), 'my renewals');
    await userEvent.keyboard('{Enter}');

    expect(await screen.findByText('my renewals')).toBeInTheDocument();
    resolveSend(envelope('m9', 'assistant', 'ok'));
  });

  it('surfaces a bounded failure message when the send fails', async () => {
    routeGet([thread()], []);
    mockPost.mockRejectedValue(new ApiError(500));

    renderWithProviders(<NeuronConversation />);
    await screen.findByTestId('neuron-transcript');

    await userEvent.type(screen.getByRole('textbox'), 'hello');
    await userEvent.keyboard('{Enter}');

    expect(await screen.findByText(/couldn't send that/i)).toBeInTheDocument();
  });

  it('asks the user to sign in again when history returns 401', async () => {
    routeGet([thread()], [], { historyError: new ApiError(401) });

    renderWithProviders(<NeuronConversation />);

    expect(await screen.findByText(/sign in again/i)).toBeInTheDocument();
    // A retry button would be pointless for an auth failure.
    expect(screen.queryByRole('button', { name: /retry/i })).not.toBeInTheDocument();
  });

  it('offers a retry when history fails for a non-auth reason', async () => {
    routeGet([thread()], [], { historyError: new ApiError(500) });

    renderWithProviders(<NeuronConversation />);

    expect(await screen.findByRole('button', { name: /retry/i })).toBeInTheDocument();
  });
});

describe('ThreadList (within the conversation)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('lists the user’s own conversations', async () => {
    routeGet([thread({ title: 'Renewals check' }), thread({ thread_id: 't2', title: 'Tasks' })], []);

    renderWithProviders(<NeuronConversation />);

    const list = await screen.findByTestId('neuron-thread-list');
    expect(await within(list).findByText('Renewals check')).toBeInTheDocument();
    expect(await within(list).findByText('Tasks')).toBeInTheDocument();
  });

  it('shows an empty state when there are no conversations', async () => {
    routeGet([], []);
    mockPost.mockResolvedValue(thread({ thread_id: 'fresh' }));

    renderWithProviders(<NeuronConversation />);

    expect(await screen.findByText(/no conversations yet/i)).toBeInTheDocument();
  });

  it('renames a thread through the API', async () => {
    routeGet([thread()], []);
    mockPatch.mockResolvedValue(thread({ title: 'Q3 renewals' }));

    renderWithProviders(<NeuronConversation />);
    await screen.findByTestId('neuron-thread-list');
    await screen.findByRole('button', { name: /rename renewals check/i });

    await userEvent.click(screen.getByRole('button', { name: /rename renewals check/i }));
    const input = screen.getByRole('textbox', { name: /rename renewals check/i });
    await userEvent.clear(input);
    await userEvent.type(input, 'Q3 renewals');
    await userEvent.keyboard('{Enter}');

    await waitFor(() => {
      expect(mockPatch).toHaveBeenCalledWith(
        expect.stringContaining('/v1/threads/t1'),
        { title: 'Q3 renewals' },
      );
    });
  });

  it('does not send an empty rename', async () => {
    routeGet([thread()], []);

    renderWithProviders(<NeuronConversation />);
    await screen.findByTestId('neuron-thread-list');
    await screen.findByRole('button', { name: /rename renewals check/i });

    await userEvent.click(screen.getByRole('button', { name: /rename renewals check/i }));
    const input = screen.getByRole('textbox', { name: /rename renewals check/i });
    await userEvent.clear(input);
    await userEvent.keyboard('{Enter}');

    expect(mockPatch).not.toHaveBeenCalled();
  });

  it('confirms before deleting', async () => {
    routeGet([thread()], []);
    mockDelete.mockResolvedValue(undefined);

    renderWithProviders(<NeuronConversation />);
    await screen.findByTestId('neuron-thread-list');
    await screen.findByRole('button', { name: /delete renewals check/i });

    await userEvent.click(screen.getByRole('button', { name: /delete renewals check/i }));
    // Nothing is deleted on the first click — the destructive step needs confirmation.
    expect(mockDelete).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: /confirm delete renewals check/i }));
    await waitFor(() => {
      expect(mockDelete).toHaveBeenCalledWith(expect.stringContaining('/v1/threads/t1'));
    });
  });

  it('creates a new conversation on demand', async () => {
    routeGet([thread()], []);
    mockPost.mockResolvedValue(thread({ thread_id: 'new-one' }));

    renderWithProviders(<NeuronConversation />);
    await screen.findByTestId('neuron-thread-list');

    await userEvent.click(screen.getByRole('button', { name: /new conversation/i }));

    await waitFor(() => {
      expect(mockPost).toHaveBeenCalledWith(
        expect.stringContaining('/v1/threads'),
        expect.objectContaining({ anchor_type: 'free_form' }),
      );
    });
  });
});
