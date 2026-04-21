import { useCallback, useMemo, useState } from 'react';
import type { NotificationItem, NotificationTab } from '../types';

const INITIAL_NOTIFICATIONS: NotificationItem[] = [
  {
    id: 'n-1',
    title: 'Broker created',
    message: 'Harbor Agency 012 was added by Priya Patel.',
    timeLabel: '2m ago',
    actionLabel: 'Open broker',
    read: false,
    assigned: true,
  },
  {
    id: 'n-2',
    title: 'Opportunity moved stage',
    message: 'Compass Markets 138 moved to Quoted.',
    timeLabel: '8m ago',
    actionLabel: 'Open opportunity',
    read: false,
    assigned: false,
  },
  {
    id: 'n-3',
    title: 'Task overdue',
    message: 'Follow up with Blue Horizon Risk Partners is overdue.',
    timeLabel: '1h ago',
    actionLabel: 'Open task',
    read: false,
    assigned: true,
  },
  {
    id: 'n-4',
    title: 'Data sync warning',
    message: 'CRM import completed with 3 warnings.',
    timeLabel: '2h ago',
    actionLabel: 'View details',
    read: true,
    assigned: false,
  },
  {
    id: 'n-5',
    title: 'Neuron analysis ready',
    message: 'Renewal risk summary completed for this account.',
    timeLabel: '3h ago',
    actionLabel: 'Open Neuron',
    read: true,
    assigned: false,
  },
];

export function useNotifications() {
  const [items, setItems] = useState<NotificationItem[]>(INITIAL_NOTIFICATIONS);
  const [tab, setTab] = useState<NotificationTab>('all');

  const unreadCount = useMemo(
    () => items.reduce((count, item) => count + (item.read ? 0 : 1), 0),
    [items],
  );

  const visibleItems = useMemo(() => {
    if (tab === 'unread') return items.filter((item) => !item.read);
    if (tab === 'assigned') return items.filter((item) => item.assigned);
    return items;
  }, [items, tab]);

  const markAllRead = useCallback(() => {
    setItems((prev) => prev.map((item) => ({ ...item, read: true })));
  }, []);

  const clearAll = useCallback(() => {
    setItems([]);
  }, []);

  const toggleRead = useCallback((id: string) => {
    setItems((prev) =>
      prev.map((item) => (item.id === id ? { ...item, read: !item.read } : item)),
    );
  }, []);

  const dismiss = useCallback((id: string) => {
    setItems((prev) => prev.filter((item) => item.id !== id));
  }, []);

  const openNotification = useCallback((id: string) => {
    setItems((prev) =>
      prev.map((item) => (item.id === id ? { ...item, read: true } : item)),
    );
  }, []);

  return {
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
  };
}

