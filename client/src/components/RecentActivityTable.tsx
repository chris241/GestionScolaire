import type { RecentActivity } from '../types';
import { StatusBadge } from './StatusBadge';
import { formatAmount } from '../lib/format';

interface RecentActivityTableProps {
  items: RecentActivity[];
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function RecentActivityTable({ items }: RecentActivityTableProps) {
  return (
    <div className="rounded-2xl border border-border bg-surface shadow-sm">
      <div className="flex items-center justify-between border-b border-border px-6 py-4">
        <h2 className="text-base font-semibold text-slate">Derniers paiements & inscriptions</h2>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3 font-medium">Élève</th>
              <th className="px-6 py-3 font-medium">Description</th>
              <th className="px-6 py-3 font-medium">Montant</th>
              <th className="px-6 py-3 font-medium">Date</th>
              <th className="px-6 py-3 font-medium">Statut</th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-6 py-8 text-center text-slate-soft">
                  Aucune activité récente.
                </td>
              </tr>
            )}
            {items.map((item) => (
              <tr key={item.id} className="border-t border-border">
                <td className="px-6 py-4 font-medium text-slate">{item.studentFullName}</td>
                <td className="px-6 py-4 text-slate-soft">{item.description}</td>
                <td className="px-6 py-4 text-slate">{formatAmount(item.amount)}</td>
                <td className="px-6 py-4 text-slate-soft">{formatDate(item.date)}</td>
                <td className="px-6 py-4">
                  <StatusBadge status={item.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
