import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import {
  fetchPayments,
  fetchPaymentsByStudent,
  fetchPendingPayments,
  createPayment,
  declarePayment,
  validatePayment,
  rejectPayment,
} from '../api/payments';
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

function formatMonthLabel(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' });
}

interface FeeMonthGroup {
  feeScheduleId: string;
  dueDate: string;
  termName: string;
  lines: Invoice[];
  status: 'Paye' | 'EnRetard' | 'EnAttente';
}

function groupInvoicesByMonth(invoices: Invoice[]): FeeMonthGroup[] {
  const byMonth = invoices.reduce<Record<string, FeeMonthGroup>>((acc, invoice) => {
    const group = (acc[invoice.feeScheduleId] ??= {
      feeScheduleId: invoice.feeScheduleId,
      dueDate: invoice.dueDate,
      termName: invoice.academicTermName,
      lines: [],
      status: 'Paye',
    });
    group.lines.push(invoice);
    return acc;
  }, {});

  return Object.values(byMonth)
    .map((group) => {
      const allPaid = group.lines.every((l) => l.status === 'Paye');
      const isOverdue = new Date(group.dueDate) < new Date();
      return { ...group, status: allPaid ? 'Paye' as const : isOverdue ? 'EnRetard' as const : 'EnAttente' as const };
    })
    .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime());
}

// Un Parent voit les paiements de tous ses enfants ; un Élève ne voit que les siens (un seul "enfant" : lui-même).
async function loadPaymentsForSelfView(): Promise<Payment[]> {
  const children = await fetchStudents();
  const perChild = await Promise.all(children.map((c) => fetchPaymentsByStudent(c.id)));
  return perChild.flat().sort((a, b) => new Date(b.dueDate).getTime() - new Date(a.dueDate).getTime());
}

export function Payments() {
  const { user } = useAuth();
  const isParent = user?.role === 'Parent';
  const isSelfView = isParent || user?.role === 'Student';
  const isDirector = user?.role === 'Director';

  const [payments, setPayments] = useState<Payment[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [studentInvoices, setStudentInvoices] = useState<Invoice[]>([]);
  const [pending, setPending] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [academicYear, setAcademicYear] = useState('');

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

  const [declareForm, setDeclareForm] = useState({
    studentId: '',
    description: '',
    amount: '',
    academicYear: '2025-2026',
    term: 'Trimestre 1',
    method: 'Mobile Money',
    invoiceId: '',
  });
  const [declaring, setDeclaring] = useState(false);
  const [decidingId, setDecidingId] = useState<string | null>(null);
  const [rejectNotes, setRejectNotes] = useState<Record<string, string>>({});
  const declareFormRef = useRef<HTMLFormElement | null>(null);

  function loadDirectorPayments(year?: string) {
    return Promise.all([fetchPayments(50, year || undefined), fetchPendingPayments()]).then(([all, pendingList]) => {
      setPayments(all);
      setPending(pendingList);
    });
  }

  useEffect(() => {
    if (user?.role === 'Teacher') return;

    let cancelled = false;
    setLoading(true);

    const loadPromise = isDirector
      ? loadDirectorPayments()
      : (isSelfView ? loadPaymentsForSelfView() : fetchPayments()).then((data) => {
          if (!cancelled) setPayments(data);
        });

    loadPromise
      .catch(() => !cancelled && setError('Impossible de charger les paiements.'))
      .finally(() => !cancelled && setLoading(false));

    if (isDirector || isParent) {
      fetchStudents().then((data) => !cancelled && setStudents(data));
    }

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isSelfView, isDirector, isParent, user?.role]);

  useEffect(() => {
    if (!isDirector || !form.studentId) {
      setStudentInvoices([]);
      return;
    }
    fetchStudentInvoices(form.studentId)
      .then((data) => setStudentInvoices(data.filter((i) => i.status !== 'Paye')))
      .catch(() => setStudentInvoices([]));
  }, [isDirector, form.studentId]);

  const [childInvoices, setChildInvoices] = useState<Invoice[]>([]);
  useEffect(() => {
    if (!isParent || !declareForm.studentId) {
      setChildInvoices([]);
      return;
    }
    fetchStudentInvoices(declareForm.studentId)
      .then(setChildInvoices)
      .catch(() => setChildInvoices([]));
  }, [isParent, declareForm.studentId]);

  const declareInvoices = childInvoices.filter((i) => i.status !== 'Paye');

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

  function handleDeclareInvoiceChange(invoiceId: string) {
    const invoice = childInvoices.find((i) => i.id === invoiceId);
    setDeclareForm((prev) => ({
      ...prev,
      invoiceId,
      amount: invoice ? String(invoice.totalAmount) : prev.amount,
      description: invoice ? `${invoice.feeCategoryName} — ${invoice.academicTermName}` : prev.description,
    }));
  }

  function handlePickInvoiceToPay(invoiceId: string) {
    handleDeclareInvoiceChange(invoiceId);
    declareFormRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  async function handleDeclarePayment(event: FormEvent) {
    event.preventDefault();
    setDeclaring(true);
    setError(null);
    try {
      await declarePayment({
        studentId: declareForm.studentId,
        description: declareForm.description,
        amount: Number(declareForm.amount),
        academicYear: declareForm.academicYear,
        term: declareForm.term,
        method: declareForm.method,
        invoiceId: declareForm.invoiceId || null,
      });
      const refreshed = await fetchPaymentsByStudent(declareForm.studentId);
      setPayments((prev) => [...refreshed, ...prev.filter((p) => p.studentId !== declareForm.studentId)]);
      setDeclareForm({ ...declareForm, description: '', amount: '', invoiceId: '' });
    } catch {
      setError('Impossible de déclarer ce paiement. Vérifiez les champs et réessayez.');
    } finally {
      setDeclaring(false);
    }
  }

  async function handleValidate(id: string) {
    setDecidingId(id);
    setError(null);
    try {
      await validatePayment(id);
      await loadDirectorPayments(academicYear);
    } catch {
      setError('Impossible de valider ce paiement.');
    } finally {
      setDecidingId(null);
    }
  }

  async function handleReject(id: string) {
    setDecidingId(id);
    setError(null);
    try {
      await rejectPayment(id, rejectNotes[id]?.trim() || null);
      setRejectNotes((prev) => ({ ...prev, [id]: '' }));
      await loadDirectorPayments(academicYear);
    } catch {
      setError('Impossible de rejeter ce paiement.');
    } finally {
      setDecidingId(null);
    }
  }

  function handleAcademicYearFilterChange(value: string) {
    setAcademicYear(value);
    loadDirectorPayments(value).catch(() => setError('Impossible de charger les paiements.'));
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

      {isParent && (
        <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Calendrier des mois</h2>
          <p className="mt-1 text-xs text-slate-soft">
            Choisissez un enfant pour voir les mois payés et non payés de l'année scolaire.
          </p>
          <select
            value={declareForm.studentId}
            onChange={(e) => setDeclareForm({ ...declareForm, studentId: e.target.value, invoiceId: '', amount: '', description: '' })}
            className={`${inputClass} mt-3`}
          >
            <option value="">Enfant...</option>
            {students.map((s) => (
              <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>
            ))}
          </select>

          {declareForm.studentId && (
            <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
              {groupInvoicesByMonth(childInvoices).length === 0 && (
                <p className="col-span-full text-sm text-slate-soft">Aucune facture générée pour cet enfant pour l'instant.</p>
              )}
              {groupInvoicesByMonth(childInvoices).map((group) => {
                const unpaidLines = group.lines.filter((l) => l.status !== 'Paye');
                return (
                  <div key={group.feeScheduleId} className="rounded-xl border border-border p-3">
                    <div className="flex items-center justify-between gap-2">
                      <p className="text-sm font-medium capitalize text-slate">{formatMonthLabel(group.dueDate)}</p>
                      <StatusBadge status={group.status} />
                    </div>
                    <p className="mt-0.5 text-xs text-slate-soft">{group.termName}</p>
                    {unpaidLines.length > 0 ? (
                      <div className="mt-2 flex flex-col gap-1">
                        {unpaidLines.map((line) => (
                          <button
                            key={line.id}
                            type="button"
                            onClick={() => handlePickInvoiceToPay(line.id)}
                            className="rounded-lg border border-primary/40 px-2 py-1 text-left text-xs font-medium text-primary transition-colors hover:bg-primary-soft"
                          >
                            Payer {line.feeCategoryName} — {formatAmount(line.totalAmount)}
                          </button>
                        ))}
                      </div>
                    ) : (
                      <p className="mt-2 text-xs text-success">Tout est réglé pour ce mois.</p>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {isParent && (
        <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Déclarer un paiement</h2>
          <p className="mt-1 text-xs text-slate-soft">
            Réglé hors application (espèces remis à l'école, Mobile Money, virement) : le Directeur vérifiera et validera votre déclaration.
          </p>
          <form ref={declareFormRef} onSubmit={handleDeclarePayment} className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-3">
            <select
              required
              value={declareForm.studentId}
              onChange={(e) => setDeclareForm({ ...declareForm, studentId: e.target.value, invoiceId: '', amount: '', description: '' })}
              className={inputClass}
            >
              <option value="" disabled>Enfant...</option>
              {students.map((s) => (
                <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>
              ))}
            </select>
            <select value={declareForm.invoiceId} onChange={(e) => handleDeclareInvoiceChange(e.target.value)} className={inputClass}>
              <option value="">Sans facture</option>
              {declareInvoices.map((i) => (
                <option key={i.id} value={i.id}>{i.invoiceNumber} — {formatAmount(i.totalAmount)}</option>
              ))}
            </select>
            <input required type="number" min="0" placeholder="Montant" value={declareForm.amount} onChange={(e) => setDeclareForm({ ...declareForm, amount: e.target.value })} className={inputClass} />
            <input required placeholder="Description" value={declareForm.description} onChange={(e) => setDeclareForm({ ...declareForm, description: e.target.value })} className={inputClass} />
            <input required placeholder="Trimestre" value={declareForm.term} onChange={(e) => setDeclareForm({ ...declareForm, term: e.target.value })} className={inputClass} />
            <input required placeholder="Méthode (ex: Mobile Money)" value={declareForm.method} onChange={(e) => setDeclareForm({ ...declareForm, method: e.target.value })} className={inputClass} />
            <button
              type="submit"
              disabled={declaring}
              className="col-span-2 mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60 md:col-span-3"
            >
              {declaring ? 'Envoi...' : 'Déclarer le paiement'}
            </button>
          </form>
        </div>
      )}

      {isDirector && pending.length > 0 && (
        <div className="mt-6 rounded-2xl border border-warning/30 bg-warning-soft/40 p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Paiements à valider ({pending.length})</h2>
          <p className="mt-1 text-xs text-slate-soft">Déclarations envoyées par des parents, en attente de votre confirmation.</p>
          <div className="mt-4 flex flex-col gap-3">
            {pending.map((p) => (
              <div key={p.id} className="rounded-xl border border-border bg-surface px-4 py-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <p className="text-sm font-medium text-slate">{p.studentFullName} — {formatAmount(p.amount)}</p>
                    <p className="text-xs text-slate-soft">{p.description} · {p.method ?? '—'} · {formatDate(p.dueDate)}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => handleValidate(p.id)}
                      disabled={decidingId === p.id}
                      className="rounded-xl bg-success px-3 py-2 text-xs font-medium text-white shadow-sm transition-colors hover:opacity-90 disabled:opacity-60"
                    >
                      Valider
                    </button>
                    <button
                      type="button"
                      onClick={() => handleReject(p.id)}
                      disabled={decidingId === p.id}
                      className="rounded-xl border border-danger/40 px-3 py-2 text-xs font-medium text-danger transition-colors hover:bg-danger-soft disabled:opacity-60"
                    >
                      Rejeter
                    </button>
                  </div>
                </div>
                <input
                  placeholder="Motif de rejet (optionnel)"
                  value={rejectNotes[p.id] ?? ''}
                  onChange={(e) => setRejectNotes((prev) => ({ ...prev, [p.id]: e.target.value }))}
                  className={`${inputClass} mt-2 w-full text-xs`}
                />
              </div>
            ))}
          </div>
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
        {isDirector && (
          <div className="flex items-center justify-between gap-3 border-b border-border px-6 py-3">
            <h2 className="text-sm font-semibold text-slate">Historique des paiements</h2>
            <select
              value={academicYear}
              onChange={(e) => handleAcademicYearFilterChange(e.target.value)}
              className="rounded-xl border border-border bg-bg px-3 py-2 text-xs text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
            >
              <option value="">Toutes les années</option>
              <option value="2025-2026">2025-2026</option>
              <option value="2026-2027">2026-2027</option>
            </select>
          </div>
        )}
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
