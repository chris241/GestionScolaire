import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { LayoutDashboard, GraduationCap, NotebookPen, Wallet, Settings, LogOut } from 'lucide-react';
import { useAuth } from '../lib/AuthContext';

const NAV_ITEMS: {
  to: string;
  label: string;
  roleLabels: Record<string, string>;
  icon: typeof LayoutDashboard;
  end: boolean;
  hideFor: string[];
}[] = [
  { to: '/', label: 'Tableau de bord', roleLabels: {}, icon: LayoutDashboard, end: true, hideFor: ['Parent', 'Teacher'] },
  { to: '/eleves', label: 'Élèves', roleLabels: { Parent: 'Mes enfants', Teacher: 'Mes élèves' }, icon: GraduationCap, end: false, hideFor: [] },
  { to: '/notes', label: 'Notes', roleLabels: {}, icon: NotebookPen, end: false, hideFor: [] },
  { to: '/paiements', label: 'Paiements', roleLabels: {}, icon: Wallet, end: false, hideFor: ['Teacher'] },
  { to: '/parametres', label: 'Paramètres', roleLabels: {}, icon: Settings, end: false, hideFor: ['Teacher', 'Parent'] },
];

export function AppLayout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();
  const navItems = NAV_ITEMS.filter((item) => !user || !item.hideFor.includes(user.role));

  return (
    <div className="flex min-h-svh bg-bg">
      <aside className="flex w-64 shrink-0 flex-col border-r border-border bg-surface px-4 py-6">
        <div className="flex items-center gap-2 px-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary-soft text-primary">
            <GraduationCap size={18} strokeWidth={2} />
          </span>
          <span className="text-base font-semibold text-slate">GestionScolaire</span>
        </div>

        <nav className="mt-8 flex flex-1 flex-col gap-1">
          {navItems.map(({ to, label, roleLabels, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-primary-soft text-primary'
                    : 'text-slate-soft hover:bg-bg hover:text-slate'
                }`
              }
            >
              <Icon size={18} strokeWidth={2} />
              {(user && roleLabels[user.role]) || label}
            </NavLink>
          ))}
        </nav>

        <div className="flex items-center gap-3 rounded-xl border border-border px-3 py-2.5">
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary-soft text-sm font-semibold text-primary">
            {user?.firstName?.[0]}
            {user?.lastName?.[0]}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-slate">{user?.firstName} {user?.lastName}</p>
            <p className="truncate text-xs text-slate-soft">{user?.role}</p>
          </div>
          <button
            type="button"
            onClick={logout}
            aria-label="Déconnexion"
            className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-soft transition-colors hover:bg-danger-soft hover:text-danger"
          >
            <LogOut size={16} strokeWidth={2} />
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto">{children}</main>
    </div>
  );
}
