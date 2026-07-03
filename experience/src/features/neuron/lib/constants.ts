// Base path for the Neuron Companion service (neuron-api.yaml). A gateway/proxy routes
// /neuron/* to the stateless Neuron service (port 8200); that wiring is a DevOps concern
// finalized in the F0038 deployability slice. Overridable via VITE_NEURON_API_BASE.
export const NEURON_API_BASE =
  (import.meta.env.VITE_NEURON_API_BASE as string | undefined) ?? '/neuron';

export const GLANCE_PATH = `${NEURON_API_BASE}/v1/glance`;
export const ACTIONS_PATH = `${NEURON_API_BASE}/v1/actions`;
export const MESSAGES_PATH = `${NEURON_API_BASE}/v1/messages`;

// Client-side zone display order: Renewals first (live), stubs after (PRD ASCII layout).
export const ZONE_DISPLAY_ORDER = ['renewals', 'tasks', 'pipeline', 'broker_activity'];
