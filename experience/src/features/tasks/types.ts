export interface TaskSummaryDto {
  id: string;
  title: string;
  status: TaskStatus;
  dueDate: string | null;
  linkedEntityType: string | null;
  linkedEntityId: string | null;
  linkedEntityName: string | null;
  isOverdue: boolean;
}

export type TaskStatus = 'Open' | 'InProgress' | 'Done';

export interface MyTasksResponseDto {
  tasks: TaskSummaryDto[];
  totalCount: number;
}

// F0004: Task Center types
export interface TaskListItemDto {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;
  assignedToUserId: string;
  assignedToDisplayName: string | null;
  createdByUserId: string;
  createdByDisplayName: string | null;
  linkedEntityType: string | null;
  linkedEntityId: string | null;
  linkedEntityName: string | null;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
}

export type TaskPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface TaskListResponseDto {
  data: TaskListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TaskCreateRequest {
  title: string;
  description?: string;
  priority?: TaskPriority;
  dueDate?: string;
  assignedToUserId: string;
  linkedEntityType?: string;
  linkedEntityId?: string;
}

export interface TaskUpdateRequest {
  title?: string;
  description?: string | null;
  status?: TaskStatus;
  priority?: TaskPriority;
  dueDate?: string | null;
  assignedToUserId?: string;
}

export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;
  assignedToUserId: string;
  assignedToDisplayName: string | null;
  createdByUserId: string;
  createdByDisplayName: string | null;
  linkedEntityType: string | null;
  linkedEntityId: string | null;
  linkedEntityName: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  rowVersion: number;
}

export interface UserSummaryDto {
  userId: string;
  displayName: string;
  email: string;
  roles: string[];
  isActive: boolean;
}

export interface UserSearchResponseDto {
  users: UserSummaryDto[];
}

export type TaskView = 'myWork' | 'assignedByMe';

export interface TaskListFilters {
  view: TaskView;
  status?: string[];
  priority?: string[];
  dueDateFrom?: string;
  dueDateTo?: string;
  overdue?: boolean;
  assigneeId?: string;
  linkedEntityType?: string[];
  createdById?: string;
  sort: string;
  sortDir: 'asc' | 'desc';
  page: number;
  pageSize: number;
}
