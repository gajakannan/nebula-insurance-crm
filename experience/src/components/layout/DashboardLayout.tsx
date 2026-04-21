import { useCallback, useState } from 'react';
import { SidebarContext, useSidebarProvider } from '@/hooks/useSidebar';
import { useSidebar } from '@/hooks/useSidebar';
import { RightChatPanel } from './RightChatPanel';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';

interface DashboardLayoutProps {
  title?: string;
  flatCanvas?: boolean;
  children: React.ReactNode;
}

export function DashboardLayout({ title, flatCanvas = false, children }: DashboardLayoutProps) {
  const sidebarValue = useSidebarProvider();
  const [chatCollapsed, setChatCollapsed] = useState(() => {
    return localStorage.getItem('nebula-chat-panel-collapsed') === 'true';
  });
  const [chatFullscreen, setChatFullscreen] = useState(false);

  const toggleChatCollapsed = useCallback(() => {
    setChatCollapsed((prev) => {
      localStorage.setItem('nebula-chat-panel-collapsed', String(!prev));
      return !prev;
    });
  }, []);

  const toggleChatFullscreen = useCallback(() => {
    setChatFullscreen((prev) => !prev);
  }, []);

  return (
    <SidebarContext.Provider value={sidebarValue}>
      {!chatFullscreen && <Sidebar />}
      <RightChatPanel
        collapsed={chatCollapsed}
        fullscreen={chatFullscreen}
        onToggleFullscreen={toggleChatFullscreen}
      />
      {!chatFullscreen && (
        <ContentArea
          title={title}
          flatCanvas={flatCanvas}
          chatCollapsed={chatCollapsed}
          onToggleChatCollapsed={toggleChatCollapsed}
          onOpenMobileChat={toggleChatFullscreen}
        >
          {children}
        </ContentArea>
      )}
    </SidebarContext.Provider>
  );
}

function ContentArea({
  title,
  flatCanvas,
  children,
  chatCollapsed,
  onToggleChatCollapsed,
  onOpenMobileChat,
}: {
  title?: string;
  flatCanvas: boolean;
  children: React.ReactNode;
  chatCollapsed: boolean;
  onToggleChatCollapsed: () => void;
  onOpenMobileChat: () => void;
}) {
  const { collapsed } = useSidebar();

  return (
    <div
      className="lg-sidebar-offset"
      style={
        {
          '--sidebar-width': collapsed ? '4rem' : '16rem',
          '--chat-panel-width': chatCollapsed ? '4rem' : '22rem',
        } as React.CSSProperties
      }
    >
      <div className={flatCanvas ? 'content-shell-flat' : 'content-inset'}>
        <TopBar
          title={title}
          chatCollapsed={chatCollapsed}
          onToggleChatCollapsed={onToggleChatCollapsed}
          onOpenMobileChat={onOpenMobileChat}
        />
        <main className="px-4 py-6 sm:px-6 lg:pl-6 lg:pr-8">
          {title && (
            <h1 className="mb-4 text-xl font-semibold text-text-primary lg:hidden">
              {title}
            </h1>
          )}
          {children}
        </main>
      </div>
    </div>
  );
}
