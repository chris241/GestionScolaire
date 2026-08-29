import { useEffect, useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import { fetchPayments, fetchPaymentsByStudent, createPayment } from '../api/payments';
import { fetchStudents } from '../api/students';
import { fetchStudentInvoices } from '../api/invoices';
import type { Payment, Student, Invoice } from '../types';
import { StatusBadge } from '../components/StatusBadge';
import { formatAmount } from '../lib/format';
import { useAuth } from '../lib/AuthContext';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

function formatDate(date: string | null) {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

// Un Parent voit les paiements de tous ses enfants ; un Élève ne voit que les siens (un seul "enfant" : lui-même).
async function loadPaymentsForSelfView(): Promise<Payment[]> {
  const children = await fetchStudents();
  const perChild = await Promise.all(children.map((c) => fetchPaymentsByStudent(c.id)));
  return perChild.flat().sort((a, b) => new Date(b.dueDate).getTime() - new Date(a.dueDate).getTime());
}

export function Payments() {
  const { user } = useAuth();
  const isSelfView = user?.role === 'Parent' || user?.role === 'Student';
  const isDirector = user?.role === 'Director';

  const [payments, setPayments] = useState<Payment[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [studentInvoices, setStudentInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
    studentId: '',
    description: '',
    amount: '',
    academicYear: '2025-2026',
    term: 'Trimestre 1',
    method: 'Espèces',
    invoiceId: '',
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (user?.role === 'Teacher') return;

    let cancelled = false;

    const load = isSelfView ? loadPaymentsForSelfView() : fetchPayments();

    load
      .then((data) => !cancelled && setPayments(data))
      .catch(() => !cancelled && setError('Impossible de charger les paiements.'))
      .finally(() => !cancelled && setLoading(false));

    if (isDirector) {
      fetchStudents().then((data) => !cancelled && setStudents(data));
    }

    return () => {
      cancelled = true;
    };
  }, [isSelfView, isDirector, user?.role]);

  useEffect(() => {
    if (!isDirector || !form.studentId) {
      setStudentInvoices([]);
      return;
    }
    fetchStudentInvoices(form.studentId)
      .then((data) => setStudentInvoices(data.filter((i) => i.status !== 'Paye')))
      .catch(() => setStudentInvoices([]));
  }, [isDirector, form.studentId]);

  if (user?.role === 'Teacher') {
    return <Navigate to="/notes" replace />;
  }

  async function handleCreatePayment(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const created = await createPayment({
        studentId: form.studentId,
        description: form.description,
        amount: Number(form.amount),
        academicYear: form.academicYear,
        term: form.term,
        method: form.method,
        invoiceId: form.invoiceId || null,
      });
      setPayments((prev) => [created, ...prev]);
      setForm({ ...form, description: '', amount: '', invoiceId: '' });
    } catch {
      setError("Impossible d'enregistrer ce paiement.");
    } finally {
      setSaving(false);
    }
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

      {isDirector && (
        <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Enregistrer un paiement</h2>
          <p className="mt-1 text-xs text-slate-soft">Pour un paiement déjà reçu (espèces, Mobile Money...).</p>
          <form onSubmit={handleCreatePayment} className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-3">
            <select required value={form.studentId} onChange={(e) => setForm({ ...form, studentId: e.target.value, invoiceId: '' })} className={inputClass}>
              <option value="" disabled>Élève...</option>
              {students.map((s) => (
                <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>
              ))}
            </select>
            <select value={form.invoiceId} onChange={(e) => setForm({ ...form, invoiceId: e.target.value })} className={inputClass}>
              <option value="">Sans facture</option>
              {studentInvoices.map((i) => (
                <option key={i.id} value={i.id}>{i.invoiceNumber} — {formatAmount(i.totalAmount)}</option>
              ))}
            </select>
            <input required type="number" min="0" placeholder="Montant" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} className={inputClass} />
            <input required placeholder="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} className={inputClass} />
            <input required placeholder="Trimestre" value={form.term} onChange={(e) => setForm({ ...form, term: e.target.value })} className={inputClass} />
            <input required placeholder="Méthode (ex: Mobile Money)" value={form.method} onChange={(e) => setForm({ ...form, method: e.target.value })} className={inputClass} />
            <button
              type="submit"
              disabled={saving}
              className="col-span-2 mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60 md:col-span-3"
            >
              {saving ? 'Enregistrement...' : 'Enregistrer le paiement'}
            </button>
          </form>
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
