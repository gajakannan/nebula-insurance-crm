import { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Bell } from 'lucide-react';
import { useNotifications } from '../hooks/useNotifications';

export function NotificationDropdown() {
  const triggerRef = useRef<HTMLButtonElement>(null!);
  const contentRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const {
    items,
    tab,
    unreadCount,
    visibleItems,
    setTab,
    markAllRead,
    clearAll,
    toggleRead,
    dismiss,
    openNotification,
  } = useNotifications();

  const handleClose = useCallback(() => {
    setOpen(false);
    triggerRef.current?.focus();
  }, []);

  useEffect(() => {
    if (!open) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') handleClose();
    }

    function handleClickOutside(event: MouseEvent) {
      if (
        contentRef.current &&
        !contentRef.current.contains(event.target as Node) &&
        triggerRef.current &&
        !triggerRef.current.contains(event.target as Node)
      ) {
        handleClose();
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [open, handleClose]);

  return (
    <>
      <button
        ref={triggerRef}
        onClick={() => setOpen((prev) => !prev)}
        aria-label="Notifications"
        aria-expanded={open}
        className="relative flex h-9 w-9 items-center justify-center rounded-md text-text-secondary transition-colors"
      >
        <Bell size={20} />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-nebula-violet px-1 text-[10px] font-bold text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>
      {open &&
        createPortal(
          <div
            ref={contentRef}
            className="fixed z-50 glass-card popover-glass rounded-xl p-4 shadow-2xl"
            style={{
              top: triggerRef.current
                ? triggerRef.current.getBoundingClientRect().bottom + 8
                : 0,
              right: triggerRef.current
                ? window.innerWidth - triggerRef.current.getBoundingClientRect().right
                : 0,
              minWidth: 360,
            }}
          >
            <div className="flex items-center justify-between gap-3">
              <h3 className="text-sm font-semibold text-text-primary">Notifications</h3>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={markAllRead}
                  disabled={unreadCount === 0}
                  className="text-xs font-medium text-nebula-violet disabled:cursor-not-allowed disabled:text-text-muted"
                >
                  Mark all read
                </button>
                <button
                  type="button"
                  onClick={clearAll}
                  disabled={items.length === 0}
                  className="text-xs font-medium text-text-secondary disabled:cursor-not-allowed disabled:text-text-muted"
                >
                  Clear all
                </button>
              </div>
            </div>

            <div className="mt-3 inline-flex items-center gap-1 rounded-lg border border-border-muted bg-surface-panel p-1">
              {(['all', 'unread', 'assigned'] as const).map((key) => {
                const active = tab === key;
                return (
                  <button
                    key={key}
                    type="button"
                    onClick={() => setTab(key)}
                    className={
                      active
                        ? 'rounded-md bg-nebula-violet/15 px-2 py-1 text-xs font-semibold text-nebula-violet'
                        : 'rounded-md px-2 py-1 text-xs text-text-muted hover:text-text-secondary'
                    }
                  >
                    {key === 'all' ? 'All' : key === 'unread' ? 'Unread' : 'Assigned'}
                  </button>
                );
              })}
            </div>

            <div className="timeline-scrollbar mt-3 max-h-80 space-y-2 overflow-y-auto pr-1">
              {visibleItems.length === 0 ? (
                <p className="rounded-lg border border-surface-border bg-surface-card/70 px-3 py-3 text-xs text-text-muted">
                  {items.length === 0
                    ? "You're all caught up."
                    : tab === 'unread'
                      ? 'No unread notifications.'
                      : 'No assigned notifications.'}
                </p>
              ) : (
                visibleItems.map((item) => (
                  <div
                    key={item.id}
                    className="rounded-lg border border-surface-border bg-surface-card/70 px-3 py-2.5"
                  >
                    <div className="flex items-start gap-2">
                      <span
                        className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${
                          item.read ? 'bg-text-muted/45' : 'bg-nebula-violet'
                        }`}
                        aria-hidden="true"
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-2">
                          <p
                            className={`text-xs font-semibold ${
                              item.read ? 'text-text-secondary' : 'text-text-primary'
                            }`}
                          >
                            {item.title}
                          </p>
                          <span className="shrink-0 text-[11px] text-text-muted">
                            {item.timeLabel}
                          </span>
                        </div>
                        <p className="mt-0.5 text-xs text-text-muted">{item.message}</p>
                        <div className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1">
                          {item.actionLabel && (
                            <button
                              type="button"
                              onClick={() => openNotification(item.id)}
                              className="text-[11px] font-medium text-nebula-violet hover:text-nebula-fuchsia"
                            >
                              {item.actionLabel}
                            </button>
                          )}
                          <button
                            type="button"
                            onClick={() => toggleRead(item.id)}
                            className="text-[11px] font-medium text-text-secondary hover:text-text-primary"
                          >
                            {item.read ? 'Mark unread' : 'Mark read'}
                          </button>
                          <button
                            type="button"
                            onClick={() => dismiss(item.id)}
                            className="text-[11px] font-medium text-text-secondary hover:text-text-primary"
                          >
                            Dismiss
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>,
          document.body,
        )}
    </>
  );
}

