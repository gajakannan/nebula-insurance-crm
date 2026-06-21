import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search } from 'lucide-react';

/**
 * Global search trigger for the authenticated shell (TopBar). Submitting navigates
 * to the deep-linkable /search workspace. Keyboard-accessible (form submit on Enter).
 */
export function SearchOverlay() {
  const navigate = useNavigate();
  const [value, setValue] = useState('');

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const q = value.trim();
    if (q.length >= 2) {
      navigate(`/search?q=${encodeURIComponent(q)}`);
    }
  }

  return (
    <form role="search" onSubmit={submit} className="hidden md:flex items-center">
      <label htmlFor="global-search-input" className="sr-only">Search CRM records</label>
      <div className="relative">
        <Search size={16} className="pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-text-muted" />
        <input
          id="global-search-input"
          type="search"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder="Search accounts, policies, tasks…"
          className="h-9 w-56 rounded-md border border-surface-border bg-surface-card pl-8 pr-3 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-2 focus:ring-nebula-violet lg:w-72"
        />
      </div>
    </form>
  );
}
