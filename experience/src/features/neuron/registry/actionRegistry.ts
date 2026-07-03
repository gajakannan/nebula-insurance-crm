import type { RegisteredActionType } from '../types';

/**
 * Allow-listed envelope action types (F0038-S0002 rendering contract). The renderer
 * only surfaces registered actions; handlers for draft/mock-send land in
 * F0038-S0005/S0006 and drill in F0038-S0003, so S0002 renders them inert.
 */
export const REGISTERED_ACTION_TYPES: readonly RegisteredActionType[] = [
  'draft_outreach',
  'mock_send',
  'drill_renewal',
  'scope_redirect_ack',
];

export function isRegisteredAction(actionType: string): actionType is RegisteredActionType {
  return (REGISTERED_ACTION_TYPES as readonly string[]).includes(actionType);
}
