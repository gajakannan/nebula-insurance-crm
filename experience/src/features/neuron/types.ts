// --- F0038-S0002 Day-at-a-Glance contracts (mirror the neuron JSON schemas) ---

export type ZoneStatus = 'content' | 'inactive' | 'empty' | 'error';

export interface ZonePayload {
  zone_id: string;
  zone_status: ZoneStatus;
  title?: string;
  component?: string;
  props?: Record<string, unknown>;
  detail?: string;
}

export type StatusState =
  | 'working'
  | 'input_required'
  | 'completed'
  | 'failed'
  | 'inactive';

export type RegisteredActionType =
  | 'draft_outreach'
  | 'mock_send'
  | 'drill_renewal'
  | 'scope_redirect_ack';

export interface TextPart {
  part_type: 'text';
  text: string;
}
export interface StatusPart {
  part_type: 'status';
  state: StatusState;
  detail?: string;
}
export interface AppPart {
  part_type: 'app';
  component: string;
  props: Record<string, unknown>;
}
export interface SourceRef {
  label: string;
  ref?: string;
}
export interface SourcesPart {
  part_type: 'sources';
  sources: SourceRef[];
}
export interface EnvelopeAction {
  action_id: string;
  label: string;
  action_type: RegisteredActionType;
  payload?: Record<string, unknown>;
}
export interface ActionsPart {
  part_type: 'actions';
  actions: EnvelopeAction[];
}

export type MessagePart =
  | TextPart
  | StatusPart
  | AppPart
  | SourcesPart
  | ActionsPart;

export interface MessageEnvelope {
  envelope_version: number;
  thread_id: string;
  message_id: string;
  role: 'user' | 'assistant';
  in_reply_to_message_id?: string;
  created_at?: string;
  parts: MessagePart[];
}

export interface GlanceResponse {
  thread_id: string;
  zones: ZonePayload[];
  message: MessageEnvelope;
}

// --- F0039-S0002/S0003 durable conversation contracts (mirror neuron-api.yaml) ---

export type ThreadAnchorType = 'free_form' | 'domain' | 'record';

/** An owner-scoped thread. `owner_user_id` is server-derived and never sent to clients. */
export interface NeuronThread {
  thread_id: string;
  anchor_type: ThreadAnchorType;
  anchor_ref?: string | null;
  title: string;
  created_at: string;
  updated_at: string;
  /** Highest server-assigned message sequence in the thread; 0 when empty. */
  last_sequence?: number;
}

export interface ThreadPage {
  data: NeuronThread[];
  /** Null on the last page. */
  next_cursor?: string | null;
}

export interface ThreadHistoryPage {
  data: MessageEnvelope[];
  /** Next sequence cursor, or null when the thread is exhausted. */
  next_after?: number | null;
}

export interface CreateThreadRequest {
  anchor_type: ThreadAnchorType;
  anchor_ref?: string;
  title?: string;
  thread_idempotency_key?: string;
}
