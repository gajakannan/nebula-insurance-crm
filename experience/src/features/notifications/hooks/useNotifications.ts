import { useCallback, useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { NotificationItem, NotificationListResponseDto, NotificationTab } from '../types';

function formatTimeLabel(createdAt: string): string {
  const now = Date.now();
  const created = new Date(createdAt).getTime();
  const diffMs = now - created;
  const diffMin = Math.floor(diffMs / 60_000);

  if (diffMin < 1) return 'Just now';
  if (diffMin < 60) return `${diffMin}m ago`;

  const diffHours = Math.floor(diffMin / 60);
  if (diffHours < 24) return `${diffHours}h ago`;

  const diffDays = Math.floor(diffHours / 24);
  return `${diffDays}d ago`;
}

function mapActionLabel(linkedEntityType: string | null): string | undefined {
  if (!linkedEntityType) return undefined;
  return `Open ${linkedEntityType.toLowerCase()}`;
}

export function useNotifications() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<NotificationTab>('all');

  const { data, isLoading } = useQuery({
    queryKey: ['my', 'notifications', tab],
    queryFn: () =>
      api.get<NotificationListResponseDto>(
        `/my/notifications?limit=20${tab === 'unread' ? '&tab=unread' : ''}`,
      ),
    refetchInterval: 30_000,
  });

  const items: NotificationItem[] = useMemo(() => {
    if (!data?.notifications) return [];
    return data.notifications.map((n) => ({
      id: n.id,
      title: n.title,
      message: n.message,
      timeLabel: formatTimeLabel(n.createdAt),
      actionLabel: mapActionLabel(n.linkedEntityType),
      read: n.isRead,
      linkedEntityType: n.linkedEntityType ?? undefined,
      linkedEntityId: n.linkedEntityId ?? undefined,
    }));
  }, [data]);

  const unreadCount = data?.unreadCount ?? 0;

  const visibleItems = items;

  const markReadMutation = useMutation({
    mutationFn: (id: string) =>
      api.patch<void>(`/my/notifications/${id}/read`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my', 'notifications'] });
    },
  });

  const markAllReadMutation = useMutation({
    mutationFn: () => api.post<void>('/my/notifications/mark-all-read', {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my', 'notifications'] });
    },
  });

  const dismissMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/my/notifications/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my', 'notifications'] });
    },
  });

  const markAllRead = useCallback(() => {
    markAllReadMutation.mutate();
  }, [markAllReadMutation]);

  const clearAll = useCallback(() => {
    // Dismiss all visible notifications one by one
    items.forEach((item) => dismissMutation.mutate(item.id));
  }, [items, dismissMutation]);

  const toggleRead = useCallback(
    (id: string) => {
      markReadMutation.mutate(id);
    },
    [markReadMutation],
  );

  const dismiss = useCallback(
    (id: string) => {
      dismissMutation.mutate(id);
    },
    [dismissMutation],
  );

  const openNotification = useCallback(
    (id: string) => {
      markReadMutation.mutate(id);
    },
    [markReadMutation],
  );

  return {
    items,
    tab,
    unreadCount,
    visibleItems,
    isLoading,
    setTab,
    markAllRead,
    clearAll,
    toggleRead,
    dismiss,
    openNotification,
  };
}
