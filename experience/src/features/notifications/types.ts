export type NotificationTab = 'all' | 'unread' | 'assigned';

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  timeLabel: string;
  actionLabel?: string;
  read: boolean;
  assigned: boolean;
}

