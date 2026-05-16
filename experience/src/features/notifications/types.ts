export type NotificationTab = 'all' | 'unread';

export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  notificationType: string;
  isRead: boolean;
  readAt: string | null;
  linkedEntityType: string | null;
  linkedEntityId: string | null;
  createdAt: string;
}

export interface NotificationListResponseDto {
  notifications: NotificationDto[];
  totalCount: number;
  unreadCount: number;
}

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  timeLabel: string;
  actionLabel?: string;
  read: boolean;
  linkedEntityType?: string;
  linkedEntityId?: string;
}
