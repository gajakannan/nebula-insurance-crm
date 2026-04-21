import { Link } from 'react-router-dom';
import { useMyTasks } from '../hooks/useMyTasks';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { TaskRow } from './TaskRow';

export function MyTasksWidget() {
  const { data, isLoading, isError, refetch } = useMyTasks();

  return (
    <section
      className="glass-card operational-panel rounded-xl p-4 md:p-5"
      aria-label="My tasks section"
    >
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-text-primary">My Tasks</h2>
        <Link to="/tasks" className="text-xs text-text-muted hover:text-nebula-violet transition-colors">
          View all
        </Link>
      </div>

      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      )}

      {isError && (
        <ErrorFallback
          message="Unable to load tasks"
          onRetry={() => refetch()}
        />
      )}

      {data && (
        <>
          {data.tasks.length === 0 ? (
            <p className="py-6 text-center text-sm text-text-muted">
              No tasks assigned. You're all caught up.
            </p>
          ) : (
            <div className="space-y-1">
              {data.tasks.map((task) => (
                <TaskRow key={task.id} task={task} />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}
