import { Menu, Sun, Moon, PanelLeftClose, PanelLeft, PanelRightClose, PanelRightOpen } from 'lucide-react';
import { useSidebar } from '@/hooks/useSidebar';
import { useTheme } from '@/hooks/useTheme';
import { NotificationDropdown } from '@/features/notifications';

interface TopBarProps {
  title?: string;
  chatCollapsed: boolean;
  onToggleChatCollapsed: () => void;
  onOpenMobileChat: () => void;
}

export function TopBar({ title, chatCollapsed, onToggleChatCollapsed, onOpenMobileChat }: TopBarProps) {
  const { collapsed, toggleCollapsed, openMobile } = useSidebar();
  const { theme, toggleTheme } = useTheme();

  return (
    <header className="sticky top-0 z-30 flex h-14 shrink-0 items-center gap-3 border-b border-topbar-border bg-topbar-bg px-4 lg:rounded-t-xl">
      <button
        onClick={openMobile}
        aria-label="Open navigation menu"
        className="flex h-9 w-9 items-center justify-center rounded-md text-text-secondary transition-colors lg:hidden"
      >
        <Menu size={20} />
      </button>
      <span className="fx-logo-glow text-lg font-bold tracking-tight text-nebula-violet lg:hidden">
        Nebula
      </span>
      <span className="text-sm text-text-muted lg:hidden">CRM</span>

      {/* Collapse toggle + separator + title (desktop only) */}
      <button
        onClick={toggleCollapsed}
        aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        aria-expanded={!collapsed}
        className="hidden lg:flex h-8 w-8 items-center justify-center rounded-md text-text-muted transition-colors hover:text-text-secondary"
      >
        {collapsed ? <PanelLeft size={18} /> : <PanelLeftClose size={18} />}
      </button>
      {title && (
        <>
          <div className="hidden lg:block h-5 w-px bg-topbar-border" />
          <h1 className="hidden text-xl font-semibold text-text-primary lg:block">
            {title}
          </h1>
        </>
      )}

      {/* Spacer */}
      <div className="flex-1" />

      {/* Right-side controls */}
      <div className="flex items-center gap-1">
        <button
          onClick={toggleTheme}
          aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          className="flex h-9 w-9 items-center justify-center rounded-md text-text-secondary transition-colors"
        >
          {theme === 'dark' ? <Sun size={20} /> : <Moon size={20} />}
        </button>
        <NotificationDropdown />
        <div className="hidden lg:block mx-1 h-5 w-px bg-topbar-border" />
        <button
          onClick={onOpenMobileChat}
          aria-label="Open chat"
          className="flex h-9 w-9 items-center justify-center rounded-md text-text-muted transition-colors hover:text-text-secondary lg:hidden"
        >
          <PanelRightOpen size={18} />
        </button>
        <button
          onClick={onToggleChatCollapsed}
          aria-label={chatCollapsed ? 'Expand chat panel' : 'Collapse chat panel'}
          aria-expanded={!chatCollapsed}
          className="hidden lg:flex h-9 w-9 items-center justify-center rounded-md text-text-muted transition-colors hover:text-text-secondary"
        >
          {chatCollapsed ? <PanelRightOpen size={18} /> : <PanelRightClose size={18} />}
        </button>
      </div>
    </header>
  );
}
