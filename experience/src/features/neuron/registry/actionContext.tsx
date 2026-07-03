import { createContext, useContext } from 'react';

/**
 * F0038-S0003 — action dispatch context. Registry-rendered components (e.g. the
 * Renewals list) call `dispatch` to fire an allow-listed envelope action (drill /
 * draft / mock-send); the shell posts it to Neuron and renders the returned envelope.
 * The component never carries authorization — the engine re-authorizes server-side.
 */
export interface CompanionAction {
  actionType: string;
  payload?: Record<string, unknown>;
}

export interface ActionContextValue {
  dispatch: (action: CompanionAction) => void;
  pending: boolean;
}

const ActionContext = createContext<ActionContextValue>({
  dispatch: () => {},
  pending: false,
});

export const ActionProvider = ActionContext.Provider;

export function useAction(): ActionContextValue {
  return useContext(ActionContext);
}
