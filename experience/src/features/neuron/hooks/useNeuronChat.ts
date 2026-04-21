import { useState } from 'react';
import type { NeuronMessage } from '../types';

const INITIAL_MESSAGES: NeuronMessage[] = [
  {
    id: 1,
    role: 'assistant',
    content: 'Ask me about this page, data, or workflow and I can help.',
  },
];

export function useNeuronChat() {
  const [messages, setMessages] = useState<NeuronMessage[]>(INITIAL_MESSAGES);
  const [draft, setDraft] = useState('');

  function sendMessage() {
    const trimmed = draft.trim();
    if (!trimmed) return;

    setMessages((prev) => {
      const nextId = prev.length ? prev[prev.length - 1].id + 1 : 1;
      return [
        ...prev,
        { id: nextId, role: 'user', content: trimmed },
        {
          id: nextId + 1,
          role: 'assistant',
          content: 'Message received. Wire this panel to your chat backend when ready.',
        },
      ];
    });
    setDraft('');
  }

  return { messages, draft, setDraft, sendMessage };
}

