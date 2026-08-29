import { useEffect, useState, type FormEvent } from 'react';
import { fetchFeeCategories, createFeeCategory, deleteFeeCategory } from '../api/feeCategories';
import {
  fetchFeeStructures,
  createFeeStructure,
  addFeeStructureItem,
  addFeeSchedule,
  generateInvoices,
} from '../api/feeStructures';
import { fetchInvoices } from '../api/invoices';
import { fetchAcademicYears } from '../api/academicYears';
import { fetchAcademicTerms } from '../api/academicTerms';
import { fetchPrograms } from '../api/programs';
import { StatusBadge } from '../components/StatusBadge';
import { formatAmount } from '../lib/format';
import type { AcademicTerm, AcademicYear, FeeCategory, FeeStructure, Invoice, Program } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function Fees() {
  const [categories, setCategories] = useState<FeeCategory[]>([]);
  const [structures, setStructures] = useState<FeeStructure[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [years, setYears] = useState<AcademicYear[]>([]);
  const [terms, setTerms] = useState<AcademicTerm[]>([]);
  const [programs, setPrograms] = useState<Program[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [categoryForm, setCategoryForm] = useState({ name: '', description: '' });
  const [structureForm, setStructureForm] = useState({ name: '', programId: '' });
  const [itemForm, setItemForm] = useState({ structureId: '', feeCategoryId: '', amount: '' });
  const [scheduleForm, setScheduleForm] = useState({ structureId: '', academicTermId: '', dueDate: '' });
  const [savingCategory, setSavingCategory] = useState(false);
  const [savingStructure, setSavingStructure] = useState(false);
  const [savingItem, setSavingItem] = useState(false);
  const [savingSchedule, setSavingSchedule] = useState(false);
  const [generatingId, setGeneratingId] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([fetchFeeCategories(), fetchFeeStructures(), fetchInvoices(), fetchAcademicYears(), fetchPrograms()])
      .then(([categoriesData, structuresData, invoicesData, yearsData, programsData]) => {
        setCategories(categoriesData);
        setStructures(structuresData);
        setInvoices(invoicesData);
        setYears(yearsData);
        setPrograms(programsData);
        const current = yearsData.find((y) => y.isCurrent) ?? yearsData[0];
        if (current) fetchAcademicTerms(current.id).then(setTerms);
      })
      .catch(() => setError('Impossible de charger les données de facturation.'))
      .finally(() => setLoading(false));
  }, []);

  function flashMessage(text: string) {
    setMessage(text);
    setTimeout(() => setMessage(null), 4000);
  }

  async function refreshStructures() {
    const data = await fetchFeeStructures();
    setStructures(data);
  }

  async function handleCreateCategory(event: FormEvent) {
    event.preventDefault();
    setSavingCategory(true);
    setError(null);
    try {
      const created = await createFeeCategory({ name: categoryForm.name, description: categoryForm.description || null });
      setCategories((prev) => [...prev, created]);
      setCategoryForm({ name: '', description: '' });
    } catch {
      setError('Impossible de créer la catégorie de frais.');
    } finally {
      setSavingCategory(false);
    }
  }

  async function handleDeleteCategory(id: string) {
    setError(null);
    try {
      await deleteFeeCategory(id);
      setCategories((prev) => prev.filter((c) => c.id !== id));
    } catch {
      setError('Impossible de supprimer cette catégorie.');
    }
  }

  async function handleCreateStructure(event: FormEvent) {
    event.preventDefault();
    const currentYear = years.find((y) => y.isCurrent) ?? years[0];
    if (!currentYear) return;

    setSavingStructure(true);
    setError(null);
    try {
      await createFeeStructure({
        name: structureForm.name,
        academicYearId: currentYear.id,
        programId: structureForm.programId || null,
      });
      await refreshStructures();
      setStructureForm({ name: '', programId: '' });
    } catch {
      setError('Impossible de créer la structure de frais.');
    } finally {
      setSavingStructure(false);
    }
  }

  async function handleAddItem(event: FormEvent) {
    event.preventDefault();
    if (!itemForm.structureId || !itemForm.feeCategoryId) return;

    setSavingItem(true);
    setError(null);
    try {
      await addFeeStructureItem(itemForm.structureId, { feeCategoryId: itemForm.feeCategoryId, amount: Number(itemForm.amount) });
      await refreshStructures();
      setItemForm({ structureId: itemForm.structureId, feeCategoryId: '', amount: '' });
    } catch {
      setError("Impossible d'ajouter cet élément.");
    } finally {
      setSavingItem(false);
    }
  }

  async function handleAddSchedule(event: FormEvent) {
    event.preventDefault();
    if (!scheduleForm.structureId || !scheduleForm.academicTermId) return;

    setSavingSchedule(true);
    setError(null);
    try {
      await addFeeSchedule(scheduleForm.structureId, { academicTermId: scheduleForm.academicTermId, dueDate: scheduleForm.dueDate });
      await refreshStructures();
      setScheduleForm({ structureId: scheduleForm.structureId, academicTermId: '', dueDate: '' });
    } catch {
      setError("Impossible d'ajouter cette échéance.");
    } finally {
      setSavingSchedule(false);
    }
  }

  async function handleGenerateInvoices(scheduleId: string) {
    setGeneratingId(scheduleId);
    setError(null);
    try {
      const result = await generateInvoices(scheduleId);
      flashMessage(`${result.created} facture(s) générée(s), ${result.alreadyExisted} déjà existante(s).`);
      await refreshStructures();
      setInvoices(await fetchInvoices());
    } catch {
      setError('Impossible de générer les factures.');
    } finally {
      setGeneratingId(null);
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Frais et facturation</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Catégories, structures de frais et génération des factures.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}
      {message && (
        <div className="mt-6 rounded-xl border border-success/20 bg-success-soft px-4 py-3 text-sm text-success">
          {message}
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Catégories de frais</h2>
          <div className="mt-4 flex flex-col gap-2">
            {categories.length === 0 && <p className="text-sm text-slate-soft">Aucune catégorie créée.</p>}
            {categories.map((c) => (
              <div key={c.id} className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5">
                <div>
                  <p className="text-sm font-medium text-slate">{c.name}</p>
                  {c.description && <p className="text-xs text-slate-soft">{c.description}</p>}
                </div>
                <button type="button" onClick={() => handleDeleteCategory(c.id)} className="text-xs font-medium text-danger hover:text-danger">
                  Supprimer
                </button>
              </div>
            ))}
          </div>
          <form onSubmit={handleCreateCategory} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Créer une catégorie</h3>
            <input required placeholder="Nom (ex: Cantine)" value={categoryForm.name} onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })} className={inputClass} />
            <input placeholder="Description" value={categoryForm.description} onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })} className={inputClass} />
            <button type="submit" disabled={savingCategory} className="mt-1 w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
              {savingCategory ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Créer une structure de frais</h2>
          <form onSubmit={handleCreateStructure} className="mt-4 flex flex-col gap-3">
            <input required placeholder="Nom (ex: Frais standard 2025-2026)" value={structureForm.name} onChange={(e) => setStructureForm({ ...structureForm, name: e.target.value })} className={inputClass} />
            <select value={structureForm.programId} onChange={(e) => setStructureForm({ ...structureForm, programId: e.target.value })} className={inputClass}>
              <option value="">Tous les programmes</option>
              {programs.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
            <button type="submit" disabled={savingStructure} className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
              {savingStructure ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>
      </div>

      <div className="mt-6 flex flex-col gap-4">
        {structures.map((structure) => (
          <div key={structure.id} className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-base font-semibold text-slate">{structure.name}</h2>
                <p className="text-xs text-slate-soft">
                  {structure.academicYearName} · {structure.programName ?? 'Tous les programmes'} · Total : {formatAmount(structure.totalAmount)}
                </p>
              </div>
            </div>

            <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
              <div>
                <h3 className="text-sm font-semibold text-slate">Éléments</h3>
                <div className="mt-2 flex flex-col gap-2">
                  {structure.items.length === 0 && <p className="text-sm text-slate-soft">Aucun élément.</p>}
                  {structure.items.map((i) => (
                    <div key={i.id} className="flex items-center justify-between rounded-xl border border-border px-3 py-2 text-sm">
                      <span className="text-slate">{i.feeCategoryName}</span>
                      <span className="text-slate-soft">{formatAmount(i.amount)}</span>
                    </div>
                  ))}
                </div>
                <form onSubmit={handleAddItem} className="mt-3 flex gap-2">
                  <select value={itemForm.structureId === structure.id ? itemForm.feeCategoryId : ''} onChange={(e) => setItemForm({ structureId: structure.id, feeCategoryId: e.target.value, amount: itemForm.amount })} className={`${inputClass} flex-1`}>
                    <option value="" disabled>Catégorie...</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                  <input type="number" min="0" placeholder="Montant" value={itemForm.structureId === structure.id ? itemForm.amount : ''} onChange={(e) => setItemForm({ structureId: structure.id, feeCategoryId: itemForm.feeCategoryId, amount: e.target.value })} className={`${inputClass} w-28`} />
                  <button type="submit" disabled={savingItem} className="rounded-xl bg-primary px-3 py-2.5 text-xs font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                    Ajouter
                  </button>
                </form>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-slate">Échéances</h3>
                <div className="mt-2 flex flex-col gap-2">
                  {structure.schedules.length === 0 && <p className="text-sm text-slate-soft">Aucune échéance.</p>}
                  {structure.schedules.map((s) => (
                    <div key={s.id} className="flex items-center justify-between rounded-xl border border-border px-3 py-2 text-sm">
                      <span className="text-slate">{s.academicTermName} · {formatDate(s.dueDate)} · {s.invoiceCount} facture(s)</span>
                      <button
                        type="button"
                        onClick={() => handleGenerateInvoices(s.id)}
                        disabled={generatingId === s.id}
                        className="text-xs font-medium text-primary hover:text-primary-hover disabled:opacity-60"
                      >
                        {generatingId === s.id ? 'Génération...' : 'Générer les factures'}
                      </button>
                    </div>
                  ))}
                </div>
                <form onSubmit={handleAddSchedule} className="mt-3 flex gap-2">
                  <select value={scheduleForm.structureId === structure.id ? scheduleForm.academicTermId : ''} onChange={(e) => setScheduleForm({ structureId: structure.id, academicTermId: e.target.value, dueDate: scheduleForm.dueDate })} className={`${inputClass} flex-1`}>
                    <option value="" disabled>Trimestre...</option>
                    {terms.map((t) => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                  <input type="date" value={scheduleForm.structureId === structure.id ? scheduleForm.dueDate : ''} onChange={(e) => setScheduleForm({ structureId: structure.id, academicTermId: scheduleForm.academicTermId, dueDate: e.target.value })} className={inputClass} />
                  <button type="submit" disabled={savingSchedule} className="rounded-xl bg-primary px-3 py-2.5 text-xs font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                    Ajouter
                  </button>
                </form>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-6 overflow-x-auto rounded-2xl border border-border bg-surface shadow-sm">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3 font-medium">Facture</th>
              <th className="px-6 py-3 font-medium">Élève</th>
              <th className="px-6 py-3 font-medium">Structure</th>
              <th className="px-6 py-3 font-medium">Montant</th>
              <th className="px-6 py-3 font-medium">Échéance</th>
              <th className="px-6 py-3 font-medium">Statut</th>
            </tr>
          </thead>
          <tbody>
            {invoices.length === 0 && (
              <tr><td colSpan={6} className="px-6 py-8 text-center text-slate-soft">Aucune facture générée.</td></tr>
            )}
            {invoices.map((invoice) => (
              <tr key={invoice.id} className="border-t border-border">
                <td className="px-6 py-4 text-slate-soft">{invoice.invoiceNumber}</td>
                <td className="px-6 py-4 font-medium text-slate">{invoice.studentFullName}</td>
                <td className="px-6 py-4 text-slate-soft">{invoice.feeStructureName} · {invoice.academicTermName}</td>
                <td className="px-6 py-4 text-slate">{formatAmount(invoice.totalAmount)}</td>
                <td className="px-6 py-4 text-slate-soft">{formatDate(invoice.dueDate)}</td>
                <td className="px-6 py-4"><StatusBadge status={invoice.status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
