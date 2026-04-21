import { cn } from '@/lib/utils';

type BadgeVariant = 'default' | 'success' | 'warning' | 'error' | 'info' | 'gradient';

const variantClasses: Record<BadgeVariant, string> = {
  default: 'border border-surface-border bg-surface-highlight text-text-secondary',
  success: 'border border-status-success/35 bg-status-success/20 text-text-primary',
  warning: 'border border-status-warning/35 bg-status-warning/20 text-text-primary',
  error: 'border border-status-error/35 bg-status-error/20 text-text-primary',
  info: 'border border-status-info/35 bg-status-info/20 text-text-primary',
  gradient: 'bg-gradient-to-r from-nebula-violet/20 to-nebula-fuchsia/20 text-nebula-fuchsia border border-nebula-fuchsia/20',
};

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  className?: string;
}

export function Badge({ children, variant = 'default', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
        variantClasses[variant],
        className,
      )}
    >
      {children}
    </span>
  );
}
