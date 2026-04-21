import { createContext, useContext, useState, useCallback, useEffect } from 'react';

interface SidebarContextValue {
  collapsed: boolean;
  toggleCollapsed: () => void;
  mobileOpen: boolean;
  openMobile: () => void;
  closeMobile: () => void;
}

export const SidebarContext = createContext<SidebarContextValue>({
  collapsed: false,
  toggleCollapsed: () => {},
  mobileOpen: false,
  openMobile: () => {},
  closeMobile: () => {},
});

export function useSidebar() {
  return useContext(SidebarContext);
}

const STORAGE_KEY = 'nebula-sidebar-collapsed';
const DESKTOP_BREAKPOINT = 1024; // lg

export function useSidebarProvider(): SidebarContextValue {
  const [collapsed, setCollapsed] = useState(() => {
    return localStorage.getItem(STORAGE_KEY) === 'true';
  });
  const [mobileOpen, setMobileOpen] = useState(false);

  const toggleCollapsed = useCallback(() => {
    setCollapsed((prev) => {
      localStorage.setItem(STORAGE_KEY, String(!prev));
      return !prev;
    });
  }, []);

  const openMobile = useCallback(() => setMobileOpen(true), []);
  const closeMobile = useCallback(() => setMobileOpen(false), []);

  // Close mobile sidebar on resize to desktop
  useEffect(() => {
    function handleResize() {
      if (window.innerWidth >= DESKTOP_BREAKPOINT) {
        setMobileOpen(false);
      }
    }
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  return { collapsed, toggleCollapsed, mobileOpen, openMobile, closeMobile };
}
