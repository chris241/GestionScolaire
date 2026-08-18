import { useEffect, useState } from 'react';
import { GraduationCap, Users, Wallet, UserX } from 'lucide-react';
import { fetchDashboardStats, fetchRecentActivity } from '../api/dashboard';
import type { DashboardStats, RecentActivity } from '../types';
import { DashboardHeader } from '../components/DashboardHeader';
import { StatCard } from '../components/StatCard';
import { RecentActivityTable } from '../components/RecentActivityTable';
import { useAuth } from '../lib/AuthContext';

export function Dashboard() {
  const { user } = useAuth();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [activity, setActivity] = useState<RecentActivity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        setLoading(true);
        const [statsData, activityData] = await Promise.all([
          fetchDashboardStats(),
          fetchRecentActivity(),
        ]);
        if (!cancelled) {
          setStats(statsData);
          setActivity(activityData);
          setError(null);
        }
      } catch {
        if (!cancelled) setError('Impossible de charger les données du tableau de bord.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="mx-auto max-w-7xl px-6 py-8">
      <DashboardHeader userName={user ? `${user.firstName} ${user.lastName}` : ''} />

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-8 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label="Élèves inscrits"
          value={loading ? '—' : String(stats?.enrolledStudents ?? 0)}
          icon={GraduationCap}
          accent="primary"
        />
        <StatCard
          label="Professeurs"
          value={loading ? '—' : String(stats?.teachers ?? 0)}
          icon={Users}
          accent="primary"
        />
        <StatCard
          label="Taux de recouvrement"
          value={loading ? '—' : `${stats?.recoveryRate ?? 0}%`}
          icon={Wallet}
          accent="success"
        />
        <StatCard
          label="Absences du jour"
          value={loading ? '—' : String(stats?.todayAbsences ?? 0)}
          icon={UserX}
          accent="warning"
        />
      </div>

      <div className="mt-8">
        <RecentActivityTable items={activity} />
      </div>
    </div>
  );
}
