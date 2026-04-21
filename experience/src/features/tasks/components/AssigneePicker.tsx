import { useState, useRef, useEffect, useId } from 'react';
import { X } from 'lucide-react';
import { useDebounce } from '@/hooks/useDebounce';
import { Badge } from '@/components/ui/Badge';
import { cn } from '@/lib/utils';
import { useUserSearch } from '../hooks/useUserSearch';
import type { UserSummaryDto } from '../types';

interface AssigneePickerProps {
  label?: string;
  selectedUser: UserSummaryDto | null;
  onSelect: (user: UserSummaryDto | null) => void;
  /** When true, renders as read-only showing current user chip */
  readOnly?: boolean;
  required?: boolean;
  error?: string;
  allowedRoles?: string[];
}

function roleBadgeVariant(role: string): 'default' | 'info' | 'warning' {
  if (role.includes('Manager') || role.includes('Admin')) return 'warning';
  if (role.includes('Underwriter') || role.includes('Relationship')) return 'info';
  return 'default';
}

export function AssigneePicker({
  label = 'Assignee',
  selectedUser,
  onSelect,
  readOnly = false,
  required,
  error,
  allowedRoles,
}: AssigneePickerProps) {
  const [inputValue, setInputValue] = useState('');
  const [open, setOpen] = useState(false);
  const debouncedQuery = useDebounce(inputValue, 300);
  const { data, isFetching } = useUserSearch(debouncedQuery, !readOnly);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listboxId = useId();
  const labelId = useId();

  // Close dropdown on outside click
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  function handleSelect(user: UserSummaryDto) {
    onSelect(user);
    setInputValue('');
    setOpen(false);
  }

  const filteredUsers = data?.users.filter((user) => (
    !allowedRoles || allowedRoles.some((allowedRole) => user.roles.includes(allowedRole))
  ));

  function handleClear() {
    onSelect(null);
    setInputValue('');
    setTimeout(() => inputRef.current?.focus(), 0);
  }

  if (readOnly && selectedUser) {
    return (
      <div className="space-y-1.5">
        <span id={labelId} className="block text-xs font-medium text-text-secondary">
          {label}
        </span>
        <div
          aria-labelledby={labelId}
          className="flex items-center gap-1.5 rounded-lg border border-surface-border bg-surface-card px-3 py-2"
        >
          <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-[10px] font-bold text-nebula-violet">
            {selectedUser.displayName.charAt(0).toUpperCase()}
          </span>
          <span className="text-sm text-text-primary">{selectedUser.displayName}</span>
        </div>
      </div>
    );
  }

  return (
    <div ref={containerRef} className="space-y-1.5">
      <label htmlFor={`${labelId}-input`} id={labelId} className="block text-xs font-medium text-text-secondary">
        {label}
        {required && <span className="ml-0.5 text-status-error">*</span>}
      </label>

      {selectedUser ? (
        <div
          className="flex items-center justify-between rounded-lg border border-surface-border bg-surface-card px-3 py-2"
          aria-labelledby={labelId}
        >
          <div className="flex items-center gap-2 min-w-0">
            <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-[10px] font-bold text-nebula-violet">
              {selectedUser.displayName.charAt(0).toUpperCase()}
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm text-text-primary">{selectedUser.displayName}</p>
              <p className="truncate text-xs text-text-muted">{selectedUser.email}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleClear}
            aria-label={`Remove assignee ${selectedUser.displayName}`}
            className="ml-2 shrink-0 rounded-md p-0.5 text-text-muted transition-colors hover:bg-surface-card-hover hover:text-text-primary"
          >
            <X size={14} />
          </button>
        </div>
      ) : (
        <div className="relative">
          <input
            ref={inputRef}
            id={`${labelId}-input`}
            type="text"
            role="combobox"
            aria-expanded={open}
            aria-autocomplete="list"
            aria-controls={open ? listboxId : undefined}
            aria-haspopup="listbox"
            value={inputValue}
            placeholder="Search by name or email..."
            onChange={(e) => {
              setInputValue(e.target.value);
              setOpen(true);
            }}
            onFocus={() => {
              if (inputValue.length >= 2) setOpen(true);
            }}
            onKeyDown={(e) => {
              if (e.key === 'Escape') {
                setOpen(false);
                setInputValue('');
              }
            }}
            className={cn(
              'w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors',
              'focus:outline-none focus:ring-1',
              error
                ? 'border-status-error focus:ring-status-error'
                : 'focus:ring-nebula-violet',
            )}
          />

          {open && (
            <ul
              id={listboxId}
              role="listbox"
              aria-label="User suggestions"
              className="absolute z-50 mt-1 w-full rounded-lg border border-surface-border bg-surface-card shadow-xl overflow-hidden"
            >
              {isFetching && (
                <li className="px-3 py-2 text-xs text-text-muted">Searching...</li>
              )}
              {!isFetching && filteredUsers && filteredUsers.length === 0 && debouncedQuery.length >= 2 && (
                <li className="px-3 py-2 text-xs text-text-muted">No users found.</li>
              )}
              {!isFetching && debouncedQuery.length < 2 && (
                <li className="px-3 py-2 text-xs text-text-muted">Type at least 2 characters to search.</li>
              )}
              {!isFetching && filteredUsers?.map((user) => (
                <li
                  key={user.userId}
                  role="option"
                  aria-selected={false}
                  onClick={() => handleSelect(user)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') handleSelect(user);
                  }}
                  tabIndex={0}
                  className="flex cursor-pointer items-center gap-2.5 px-3 py-2.5 transition-colors hover:bg-surface-highlight focus:bg-surface-highlight focus:outline-none"
                >
                  <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-[10px] font-bold text-nebula-violet">
                    {user.displayName.charAt(0).toUpperCase()}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-text-primary">{user.displayName}</p>
                    <p className="truncate text-xs text-text-muted">{user.email}</p>
                  </div>
                  <div className="flex shrink-0 flex-wrap gap-1">
                    {user.roles.slice(0, 2).map((role) => (
                      <Badge key={role} variant={roleBadgeVariant(role)} className="text-[10px]">
                        {role}
                      </Badge>
                    ))}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {error && <p className="text-xs text-status-error">{error}</p>}
    </div>
  );
}
