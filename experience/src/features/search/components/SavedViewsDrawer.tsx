import { useState } from 'react';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';
import { Select } from '@/components/ui/Select';
import { TextInput } from '@/components/ui/TextInput';
import { ApiError } from '@/services/api';
import {
  useArchiveSavedView,
  useCreateSavedView,
  useSavedViews,
  useSetDefaultSavedView,
} from '../hooks';
import type { SavedView, SavedViewType, SavedViewVisibility } from '../types';

interface SavedViewsDrawerProps {
  viewType: SavedViewType;
  currentCriteria: unknown;
  onApply: (view: SavedView) => void;
}

export function SavedViewsDrawer({ viewType, currentCriteria, onApply }: SavedViewsDrawerProps) {
  const { data, isLoading } = useSavedViews({ viewType });
  const [saveOpen, setSaveOpen] = useState(false);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-semibold text-text-primary">Saved views</h2>
        <button
          type="button"
          onClick={() => setSaveOpen(true)}
          className="rounded-md bg-nebula-violet px-3 py-1 text-xs font-medium text-white hover:bg-nebula-violet/90"
        >
          Save current
        </button>
      </div>

      {isLoading && <p className="text-xs text-text-muted">Loading saved views…</p>}
      {data && data.data.length === 0 && <p className="text-xs text-text-muted">No saved views yet.</p>}

      <ul className="space-y-1">
        {data?.data.map((v) => (
          <li key={v.id} className="rounded-md border border-surface-border bg-surface-card p-2">
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => onApply(v)}
                className="flex-1 text-left text-sm text-text-primary hover:underline"
              >
                {v.name}
              </button>
              {v.isDefault && <Badge variant="success">Default</Badge>}
              <Badge variant={v.visibility === 'Team' ? 'info' : 'default'}>{v.visibility}</Badge>
            </div>
            <SavedViewActions view={v} />
          </li>
        ))}
      </ul>

      <SaveViewModal
        open={saveOpen}
        viewType={viewType}
        criteria={currentCriteria}
        onClose={() => setSaveOpen(false)}
      />
    </div>
  );
}

function SavedViewActions({ view }: { view: SavedView }) {
  const setDefault = useSetDefaultSavedView(view.id);
  const archive = useArchiveSavedView(view.id);

  return (
    <div className="mt-1 flex gap-3 text-xs">
      {!view.isDefault && (
        <button
          type="button"
          onClick={() => setDefault.mutate({ rowVersion: view.rowVersion })}
          className="text-text-secondary hover:text-text-primary"
        >
          Make default
        </button>
      )}
      <button
        type="button"
        onClick={() => {
          if (window.confirm(`Delete saved view “${view.name}”?`)) {
            archive.mutate({ rowVersion: view.rowVersion });
          }
        }}
        className="text-text-secondary hover:text-text-primary"
      >
        Delete
      </button>
    </div>
  );
}

function SaveViewModal({
  open,
  viewType,
  criteria,
  onClose,
}: {
  open: boolean;
  viewType: SavedViewType;
  criteria: unknown;
  onClose: () => void;
}) {
  const create = useCreateSavedView();
  const [name, setName] = useState('');
  const [visibility, setVisibility] = useState<SavedViewVisibility>('Personal');
  const [scopeType, setScopeType] = useState('Region');
  const [scopeKey, setScopeKey] = useState('');
  const [error, setError] = useState<string | null>(null);

  function submit() {
    setError(null);
    create.mutate(
      {
        name,
        viewType,
        visibility,
        teamScopeType: visibility === 'Team' ? scopeType : null,
        teamScopeKey: visibility === 'Team' ? scopeKey : null,
        criteria,
        isDefault: false,
      },
      {
        onSuccess: () => {
          setName('');
          setScopeKey('');
          onClose();
        },
        onError: (e) => setError(e instanceof ApiError ? (e.problem?.title ?? 'Save failed') : 'Save failed'),
      },
    );
  }

  return (
    <Modal open={open} onClose={onClose} title="Save view" description="Save the current criteria for quick reuse.">
      <div className="space-y-3">
        <TextInput label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <Select
          label="Visibility"
          value={visibility}
          onChange={(e) => setVisibility(e.target.value as SavedViewVisibility)}
          options={[
            { value: 'Personal', label: 'Personal' },
            { value: 'Team', label: 'Team' },
          ]}
        />
        {visibility === 'Team' && (
          <div className="grid grid-cols-2 gap-2">
            <Select
              label="Scope type"
              value={scopeType}
              onChange={(e) => setScopeType(e.target.value)}
              options={[
                { value: 'Region', label: 'Region' },
                { value: 'Role', label: 'Role' },
                { value: 'Program', label: 'Program' },
                { value: 'Territory', label: 'Territory' },
              ]}
            />
            <TextInput label="Scope key" value={scopeKey} onChange={(e) => setScopeKey(e.target.value)} />
          </div>
        )}
        {error && <p className="text-xs text-status-error" role="alert">{error}</p>}
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-secondary">
            Cancel
          </button>
          <button
            type="button"
            onClick={submit}
            disabled={!name.trim() || create.isPending}
            className="rounded-md bg-nebula-violet px-3 py-1 text-sm font-medium text-white disabled:opacity-40"
          >
            Save
          </button>
        </div>
      </div>
    </Modal>
  );
}
