import type { LucideIcon } from 'lucide-react';

interface StatCardProps {
  label: string;
  value: string;
  icon: LucideIcon;
  accent?: 'primary' | 'success' | 'warning' | 'danger';
}

const ACCENT_STYLES: Record<NonNullable<StatCardProps['accent']>, string> = {
  primary: 'bg-primary-soft text-primary',
  success: 'bg-success-soft text-success',
  warning: 'bg-warning-soft text-warning',
  danger: 'bg-danger-soft text-danger',
};

export function StatCard({ label, value, icon: Icon, accent = 'primary' }: StatCardProps) {
  return (
    <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-slate-soft">{label}</p>
        <span className={`flex h-10 w-10 items-center justify-center rounded-xl ${ACCENT_STYLES[accent]}`}>
          <Icon size={20} strokeWidth={2} />
        </span>
      </div>
      <p className="mt-4 text-3xl font-semibold text-slate">{value}</p>
    </div>
  );
}
