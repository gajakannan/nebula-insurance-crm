import { useState, useCallback, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Plus, SlidersHorizontal } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Tabs } from '@/components/ui/Tabs';
import { useCurrentUser } from '@/features/auth';
import { cn } from '@/lib/utils';
import { TaskCenterList } from '@/features/tasks/components/TaskCenterList';
import { TaskFilterToolbar } from '@/features/tasks/components/TaskFilterToolbar';
import { TaskDetailPanel } from '@/features/tasks/components/TaskDetailPanel';
import { TaskDetailDrawer } from '@/features/tasks/components/TaskDetailPanel';
import { TaskCreateModal } from '@/features/tasks/components/TaskCreateModal';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  TaskListFilters,
  TaskView,
  TaskListItemDto,
  TaskDto,
  UserSummaryDto,
} from '@/features/tasks/types';

const MANAGER_ROLES = [
  'DistributionManager',
  'Admin',
];

function isManager(roles: string[]): boolean {
  return roles.some((r) => MANAGER_ROLES.includes(r));
}

const TAB_MY_WORK = 'My Work';
const TAB_ASSIGNED_BY_ME = 'Assigned By Me';

function makeDefaultFilters(view: TaskView): TaskListFilters {
  return {
    view,
    status: [],
    priority: [],
    sort: 'dueDate',
    sortDir: 'asc',
    page: 1,
    pageSize: 25,
  };
}

export default function TaskCenterPage() {
  const { taskId } = useParams<{ taskId?: string }>();
  const navigate = useNavigate();
  const currentUser = useCurrentUser();

  const canSeeAssignedByMe = currentUser ? isManager(currentUser.roles) : false;

  const [activeTab, setActiveTab] = useState<string>(TAB_MY_WORK);
  const [myWorkFilters, setMyWorkFilters] = useState<TaskListFilters>(
    makeDefaultFilters('myWork'),
  );
  const [assignedFilters, setAssignedFilters] = useState<TaskListFilters>(
    makeDefaultFilters('assignedByMe'),
  );
  const [showFilters, setShowFilters] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<TaskListItemDto | null>(null);
  const [assigneeFilterUser, setAssigneeFilterUser] = useState<UserSummaryDto | null>(null);

  const currentView: TaskView = activeTab === TAB_ASSIGNED_BY_ME ? 'assignedByMe' : 'myWork';
  const currentFilters = currentView === 'assignedByMe' ? assignedFilters : myWorkFilters;

  function setCurrentFilters(patch: Partial<TaskListFilters>) {
    if (currentView === 'assignedByMe') {
      setAssignedFilters((prev) => ({ ...prev, ...patch }));
    } else {
      setMyWorkFilters((prev) => ({ ...prev, ...patch }));
    }
  }

  function clearCurrentFilters() {
    const cleared = makeDefaultFilters(currentView);
    if (currentView === 'assignedByMe') {
      setAssignedFilters(cleared);
      setAssigneeFilterUser(null);
    } else {
      setMyWorkFilters(cleared);
    }
  }

  const hasActiveFilters = Boolean(
    currentFilters.status?.length ||
    currentFilters.priority?.length ||
    currentFilters.dueDateFrom ||
    currentFilters.dueDateTo ||
    currentFilters.overdue ||
    currentFilters.assigneeId ||
    currentFilters.linkedEntityType?.length,
  );

  // The active detail ID: either from user click (selectedTask) or URL param (deep link)
  const activeTaskId = selectedTask?.id ?? taskId ?? null;

  // Fetch full task detail when a task is active (needed for rowVersion + edit)
  const { data: taskDetail } = useQuery({
    queryKey: ['task', activeTaskId],
    queryFn: () => api.get<TaskDto>(`/tasks/${activeTaskId!}`),
    enabled: !!activeTaskId,
  });

  // Deep-link: if taskId is in the URL on mount but no selectedTask, auto-open the panel
  useEffect(() => {
    if (taskId && !selectedTask && taskDetail) {
      setSelectedTask({
        id: taskDetail.id,
        title: taskDetail.title,
        description: taskDetail.description ?? null,
        status: taskDetail.status,
        priority: taskDetail.priority,
        dueDate: taskDetail.dueDate ?? null,
        assignedToUserId: taskDetail.assignedToUserId,
        assignedToDisplayName: taskDetail.assignedToDisplayName ?? null,
        createdByUserId: taskDetail.createdByUserId,
        createdByDisplayName: taskDetail.createdByDisplayName ?? null,
        linkedEntityType: taskDetail.linkedEntityType ?? null,
        linkedEntityId: taskDetail.linkedEntityId ?? null,
        linkedEntityName: taskDetail.linkedEntityName ?? null,
        isOverdue: false,
        createdAt: taskDetail.createdAt,
        updatedAt: taskDetail.updatedAt,
        completedAt: taskDetail.completedAt ?? null,
      });
    }
  }, [taskId, selectedTask, taskDetail]);

  function handleSelectTask(task: TaskListItemDto) {
    setSelectedTask(task);
    navigate(`/tasks/${task.id}`, { replace: true });
  }

  const handleCloseDetail = useCallback(() => {
    setSelectedTask(null);
    navigate('/tasks', { replace: true });
  }, [navigate]);

  const tabs = canSeeAssignedByMe
    ? [TAB_MY_WORK, TAB_ASSIGNED_BY_ME]
    : [TAB_MY_WORK];

  return (
    <DashboardLayout title="Tasks">
      <div className="flex h-full gap-4">
        {/* Main content */}
        <div
          className={cn(
            'flex-1 min-w-0 space-y-4',
            // When detail panel is open on desktop, constrain list width
            activeTaskId && taskDetail ? 'lg:max-w-[calc(100%-22rem)]' : '',
          )}
        >
          {/* Page header */}
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-xl font-semibold text-text-primary">Task Center</h1>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setShowFilters((v) => !v)}
                aria-pressed={showFilters}
                aria-label="Toggle filters"
                className={cn(
                  'flex items-center gap-1.5 rounded-lg border px-3 py-2 text-sm font-medium transition-colors',
                  showFilters
                    ? 'border-nebula-violet bg-nebula-violet/10 text-nebula-violet'
                    : 'border-surface-border bg-surface-card text-text-secondary hover:bg-surface-highlight hover:text-text-primary',
                  hasActiveFilters && !showFilters && 'border-nebula-violet/50',
                )}
              >
                <SlidersHorizontal size={14} />
                <span className="hidden sm:inline">Filters</span>
                {hasActiveFilters && (
                  <span className="flex h-4 w-4 items-center justify-center rounded-full bg-nebula-violet text-[10px] font-bold text-white">
                    !
                  </span>
                )}
              </button>
              <button
                type="button"
                onClick={() => setCreateOpen(true)}
                className="flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
              >
                <Plus size={14} />
                <span className="hidden sm:inline">New Task</span>
              </button>
            </div>
          </div>

          {/* Tabs */}
          <Tabs tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab}>
            {/* Filter toolbar (rendered inside tab panel so it respects view context) */}
            {showFilters && (
              <div className="mb-4">
                <TaskFilterToolbar
                  filters={currentFilters}
                  view={currentView}
                  assigneeUser={assigneeFilterUser}
                  onAssigneeChange={setAssigneeFilterUser}
                  onChange={setCurrentFilters}
                  onClear={clearCurrentFilters}
                  hasActiveFilters={hasActiveFilters}
                />
              </div>
            )}

            <div className="glass-card operational-panel rounded-xl p-4">
              <TaskCenterList
                filters={currentFilters}
                view={currentView}
                selectedTaskId={selectedTask?.id ?? taskId ?? null}
                onSelectTask={handleSelectTask}
                onFiltersChange={setCurrentFilters}
                onCreateClick={() => setCreateOpen(true)}
              />
            </div>
          </Tabs>
        </div>

        {/* Desktop detail panel (inline, ≥1024px) */}
        {activeTaskId && taskDetail && (
          <aside
            className="hidden lg:flex lg:w-[22rem] shrink-0 rounded-xl overflow-hidden border border-surface-border"
            aria-label="Task detail"
          >
            <div className="w-full">
              <TaskDetailPanel task={taskDetail} onClose={handleCloseDetail} />
            </div>
          </aside>
        )}
      </div>

      {/* Mobile/tablet detail drawer (< 1024px) */}
      {activeTaskId && taskDetail && (
        <TaskDetailDrawer task={taskDetail} onClose={handleCloseDetail} />
      )}

      {/* Create modal */}
      <TaskCreateModal open={createOpen} onClose={() => setCreateOpen(false)} />
    </DashboardLayout>
  );
}
