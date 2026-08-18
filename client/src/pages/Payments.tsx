import { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { fetchPayments, fetchPaymentsByStudent } from '../api/payments';
import { fetchStudents } from '../api/students';
import type { Payment } from '../types';
import { StatusBadge } from '../components/StatusBadge';
import { formatAmount } from '../lib/format';
import { useAuth } from '../lib/AuthContext';

function formatDate(date: string | null) {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

async function loadPaymentsForParent(): Promise<Payment[]> {
  const children = await fetchStudents();
  const perChild = await Promise.all(children.map((c) => fetchPaymentsByStudent(c.id)));
  return perChild.flat().sort((a, b) => new Date(b.dueDate).getTime() - new Date(a.dueDate).getTime());
}

export function Payments() {
  const { user } = useAuth();
  const isParent = user?.role === 'Parent';

  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (user?.role === 'Teacher') return;

    let cancelled = false;

    const load = isParent ? loadPaymentsForParent() : fetchPayments();

    load
      .then((data) => !cancelled && setPayments(data))
      .catch(() => !cancelled && setError('Impossible de charger les paiements.'))
      .finally(() => !cancelled && setLoading(false));

    return () => {
      cancelled = true;
    };
  }, [isParent, user?.role]);

  if (user?.role === 'Teacher') {
    return <Navigate to="/notes" replace />;
  }

  const totalDue = payments.reduce((sum, p) => sum + p.amount, 0);
  const totalPaid = payments.filter((p) => p.status === 'Paye').reduce((sum, p) => sum + p.amount, 0);

  return (
    <div className="mx-auto max-w-7xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Paiements</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : `${formatAmount(totalPaid)} encaissés sur ${formatAmount(totalDue)}`}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 overflow-x-auto rounded-2xl border border-border bg-surface shadow-sm">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3 font-medium">Élève</th>
              <th className="px-6 py-3 font-medium">Description</th>
              <th className="px-6 py-3 font-medium">Montant</th>
              <th className="px-6 py-3 font-medium">Échéance</th>
              <th className="px-6 py-3 font-medium">Payé le</th>
              <th className="px-6 py-3 font-medium">Statut</th>
            </tr>
          </thead>
          <tbody>
            {!loading && payments.length === 0 && (
              <tr>
                <td colSpan={6} className="px-6 py-8 text-center text-slate-soft">
                  Aucun paiement enregistré.
                </td>
              </tr>
            )}
            {payments.map((payment) => (
              <tr key={payment.id} className="border-t border-border">
                <td className="px-6 py-4 font-medium text-slate">{payment.studentFullName}</td>
                <td className="px-6 py-4 text-slate-soft">{payment.description}</td>
                <td className="px-6 py-4 text-slate">{formatAmount(payment.amount)}</td>
                <td className="px-6 py-4 text-slate-soft">{formatDate(payment.dueDate)}</td>
                <td className="px-6 py-4 text-slate-soft">{formatDate(payment.paidAt)}</td>
                <td className="px-6 py-4">
                  <StatusBadge status={payment.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
