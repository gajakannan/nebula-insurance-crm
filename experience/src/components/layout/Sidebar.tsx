import { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  BriefcaseBusiness,
  RotateCwSquare,
  Building2,
  ShieldCheck,
  Users,
  ClipboardList,
  PanelLeftClose,
  LogOut,
  ChevronRight,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useSidebar } from '@/hooks/useSidebar';

const NAV_ITEMS = [
  { label: 'Dashboard', href: '/', icon: LayoutDashboard },
  { label: 'Accounts', href: '/accounts', icon: Building2 },
  { label: 'Submissions', href: '/submissions', icon: BriefcaseBusiness },
  { label: 'Renewals', href: '/renewals', icon: RotateCwSquare },
  { label: 'Policies', href: '/policies', icon: ShieldCheck },
  { label: 'Brokers', href: '/brokers', icon: Users },
  { label: 'Tasks', href: '/tasks', icon: ClipboardList },
];

function isActive(href: string, pathname: string) {
  return href === '/' ? pathname === '/' : pathname.startsWith(href);
}

export function Sidebar() {
  const { collapsed, mobileOpen, closeMobile } = useSidebar();
  const { pathname } = useLocation();


  // Close mobile sidebar on route change
  useEffect(() => {
    closeMobile();
  }, [pathname, closeMobile]);

  const sidebarWidth = collapsed ? 'w-16' : 'w-64';

  const sidebarContent = (
    <div className={cn('sidebar h-full flex flex-col', sidebarWidth)}>
      {/* Header: logo */}
      <div className="flex h-14 items-center px-3">
        <Link to="/" className="flex items-center gap-2 overflow-hidden">
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-nebula-violet/20 text-nebula-violet font-bold text-sm">
            N
          </span>
          <span
            className="overflow-hidden whitespace-nowrap transition-all duration-200"
            style={{ width: collapsed ? 0 : 'auto', opacity: collapsed ? 0 : 1 }}
          >
            <span className="fx-logo-glow text-lg font-bold tracking-tight text-nebula-violet">
              Nebula
            </span>
            <span className="ml-1 text-sm text-text-muted">CRM</span>
          </span>
        </Link>
      </div>

      {/* Navigation */}
      <div className="px-2 pt-4">
        {!collapsed && (
          <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-widest text-text-muted">
            Navigation
          </p>
        )}
        <nav aria-label="Main navigation" className="space-y-1">
          {NAV_ITEMS.map((item) => {
            const active = isActive(item.href, pathname);
            return (
              <Link
                key={item.href}
                to={item.href}
                aria-current={active ? 'page' : undefined}
                className={cn('sidebar-item', active && 'sidebar-item-active')}
              >
                <item.icon size={20} className="shrink-0" />
                <span
                  className="overflow-hidden whitespace-nowrap transition-all duration-200"
                  style={{ width: collapsed ? 0 : 'auto', opacity: collapsed ? 0 : 1 }}
                >
                  {item.label}
                </span>
                {active && !collapsed && (
                  <ChevronRight size={14} className="ml-auto shrink-0 opacity-50" />
                )}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Spacer */}
      <div className="flex-1" />

      {/* User footer */}
      <div className="border-t border-sidebar-border px-2 py-3">
        <div className="sidebar-item">
          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-xs font-bold text-nebula-violet">
            AD
          </span>
          <span
            className="overflow-hidden whitespace-nowrap transition-all duration-200"
            style={{ width: collapsed ? 0 : 'auto', opacity: collapsed ? 0 : 1 }}
          >
            <span className="text-sm font-medium text-text-primary">Admin</span>
          </span>
          {!collapsed && (
            <LogOut size={14} className="ml-auto shrink-0 text-text-muted" />
          )}
        </div>
      </div>
    </div>
  );

  return (
    <>
      {/* Desktop sidebar */}
      <div className="hidden lg:block">
        {sidebarContent}
      </div>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          {/* Backdrop */}
          <div
            className="absolute inset-0 bg-black/60 backdrop-blur-sm"
            onClick={closeMobile}
            aria-hidden="true"
          />
          {/* Sidebar panel */}
          <div className="relative w-64 h-full animate-slide-in-left">
            {/* Force expanded width in mobile */}
            <div className="sidebar h-full w-64 flex flex-col">
              {/* Header */}
              <div className="flex h-14 items-center justify-between px-3">
                <Link to="/" className="flex items-center gap-2">
                  <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-nebula-violet/20 text-nebula-violet font-bold text-sm">
                    N
                  </span>
                  <span className="fx-logo-glow text-lg font-bold tracking-tight text-nebula-violet">
                    Nebula
                  </span>
                  <span className="text-sm text-text-muted">CRM</span>
                </Link>
                <button
                  onClick={closeMobile}
                  aria-label="Close navigation menu"
                  className="flex h-7 w-7 items-center justify-center rounded-md text-text-muted"
                >
                  <PanelLeftClose size={16} />
                </button>
              </div>

              {/* Navigation */}
              <div className="px-2 pt-4">
                <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-widest text-text-muted">
                  Navigation
                </p>
                <nav aria-label="Main navigation" className="space-y-1">
                  {NAV_ITEMS.map((item) => {
                    const active = isActive(item.href, pathname);
                    return (
                      <Link
                        key={item.href}
                        to={item.href}
                        aria-current={active ? 'page' : undefined}
                        className={cn('sidebar-item', active && 'sidebar-item-active')}
                      >
                        <item.icon size={20} className="shrink-0" />
                        <span>{item.label}</span>
                        {active && (
                          <ChevronRight size={14} className="ml-auto shrink-0 opacity-50" />
                        )}
                      </Link>
                    );
                  })}
                </nav>
              </div>

              <div className="flex-1" />

              {/* User footer */}
              <div className="border-t border-sidebar-border px-2 py-3">
                <div className="sidebar-item">
                  <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-xs font-bold text-nebula-violet">
                    AD
                  </span>
                  <span className="text-sm font-medium text-text-primary">Admin</span>
                  <LogOut size={14} className="ml-auto shrink-0 text-text-muted" />
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
