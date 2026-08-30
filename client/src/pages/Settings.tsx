import { useEffect, useState, type FormEvent } from 'react';
import {
  fetchAcademicYears,
  createAcademicYear,
  setCurrentAcademicYear,
} from '../api/academicYears';
import { fetchAcademicTerms, createAcademicTerm, deleteAcademicTerm } from '../api/academicTerms';
import type { AcademicYear, AcademicTerm } from '../types';

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function Settings() {
  const [years, setYears] = useState<AcademicYear[]>([]);
  const [terms, setTerms] = useState<AcademicTerm[]>([]);
  const [selectedYearId, setSelectedYearId] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [yearForm, setYearForm] = useState({ name: '', startDate: '', endDate: '' });
  const [termForm, setTermForm] = useState({ name: '', order: '1', startDate: '', endDate: '' });
  const [savingYear, setSavingYear] = useState(false);
  const [savingTerm, setSavingTerm] = useState(false);

  useEffect(() => {
    fetchAcademicYears()
      .then((yearsData) => {
        setYears(yearsData);
        const current = yearsData.find((y) => y.isCurrent) ?? yearsData[0];
        if (current) setSelectedYearId(current.id);
      })
      .catch(() => setError('Impossible de charger les paramètres.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedYearId) return;
    fetchAcademicTerms(selectedYearId)
      .then(setTerms)
      .catch(() => setError('Impossible de charger les trimestres.'));
  }, [selectedYearId]);

  function flashMessage(text: string) {
    setMessage(text);
    setTimeout(() => setMessage(null), 3000);
  }

  async function handleCreateYear(event: FormEvent) {
    event.preventDefault();
    setSavingYear(true);
    setError(null);
    try {
      const created = await createAcademicYear(yearForm);
      setYears((prev) => [created, ...prev]);
      setYearForm({ name: '', startDate: '', endDate: '' });
      flashMessage('Année académique créée.');
    } catch {
      setError("Impossible de créer l'année académique.");
    } finally {
      setSavingYear(false);
    }
  }

  async function handleSetCurrent(id: string) {
    setError(null);
    try {
      await setCurrentAcademicYear(id);
      setYears((prev) => prev.map((y) => ({ ...y, isCurrent: y.id === id })));
      flashMessage('Année courante mise à jour.');
    } catch {
      setError("Impossible de définir l'année courante.");
    }
  }

  async function handleCreateTerm(event: FormEvent) {
    event.preventDefault();
    if (!selectedYearId) return;
    setSavingTerm(true);
    setError(null);
    try {
      const created = await createAcademicTerm({
        name: termForm.name,
        order: Number(termForm.order),
        startDate: termForm.startDate,
        endDate: termForm.endDate,
        academicYearId: selectedYearId,
      });
      setTerms((prev) => [...prev, created].sort((a, b) => a.order - b.order));
      setTermForm({ name: '', order: '1', startDate: '', endDate: '' });
      flashMessage('Trimestre créé.');
    } catch {
      setError('Impossible de créer le trimestre.');
    } finally {
      setSavingTerm(false);
    }
  }

  async function handleDeleteTerm(id: string) {
    setError(null);
    try {
      await deleteAcademicTerm(id);
      setTerms((prev) => prev.filter((t) => t.id !== id));
    } catch {
      setError('Impossible de supprimer ce trimestre.');
    }
  }

  const inputClass =
    'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

  return (
    <div className="mx-auto max-w-5xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Paramètres</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Années et trimestres académiques.'}
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
          <h2 className="text-base font-semibold text-slate">Années académiques</h2>

          <div className="mt-4 flex flex-col gap-2">
            {years.map((year) => (
              <div
                key={year.id}
                className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5"
              >
                <div>
                  <p className="text-sm font-medium text-slate">{year.name}</p>
                  <p className="text-xs text-slate-soft">
                    {formatDate(year.startDate)} — {formatDate(year.endDate)}
                  </p>
                </div>
                {year.isCurrent ? (
                  <span className="rounded-full bg-success-soft px-3 py-1 text-xs font-medium text-success">
                    Courante
                  </span>
                ) : (
                  <button
                    type="button"
                    onClick={() => handleSetCurrent(year.id)}
                    className="text-xs font-medium text-primary hover:text-primary-hover"
                  >
                    Définir courante
                  </button>
                )}
              </div>
            ))}
          </div>

          <form onSubmit={handleCreateYear} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Ajouter une année</h3>
            <input
              required
              placeholder="Ex: 2026-2027"
              value={yearForm.name}
              onChange={(e) => setYearForm({ ...yearForm, name: e.target.value })}
              className={inputClass}
            />
            <div className="grid grid-cols-2 gap-3">
              <input
                required
                type="date"
                value={yearForm.startDate}
                onChange={(e) => setYearForm({ ...yearForm, startDate: e.target.value })}
                className={inputClass}
              />
              <input
                required
                type="date"
                value={yearForm.endDate}
                onChange={(e) => setYearForm({ ...yearForm, endDate: e.target.value })}
                className={inputClass}
              />
            </div>
            <button
              type="submit"
              disabled={savingYear}
              className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {savingYear ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <div className="flex items-center justify-between">
            <h2 className="text-base font-semibold text-slate">Trimestres</h2>
            <select
              value={selectedYearId}
              onChange={(e) => setSelectedYearId(e.target.value)}
              className="rounded-xl border border-border bg-bg px-3 py-2 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
            >
              {years.map((y) => (
                <option key={y.id} value={y.id}>{y.name}</option>
              ))}
            </select>
          </div>

          <div className="mt-4 flex flex-col gap-2">
            {terms.length === 0 && (
              <p className="text-sm text-slate-soft">Aucun trimestre pour cette année.</p>
            )}
            {terms.map((term) => (
              <div
                key={term.id}
                className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5"
              >
                <div>
                  <p className="text-sm font-medium text-slate">{term.name}</p>
                  <p className="text-xs text-slate-soft">
                    {formatDate(term.startDate)} — {formatDate(term.endDate)}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => handleDeleteTerm(term.id)}
                  className="text-xs font-medium text-danger hover:text-danger"
                >
                  Supprimer
                </button>
              </div>
            ))}
          </div>

          <form onSubmit={handleCreateTerm} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Ajouter un trimestre</h3>
            <div className="grid grid-cols-2 gap-3">
              <input
                required
                placeholder="Nom (ex: Trimestre 1)"
                value={termForm.name}
                onChange={(e) => setTermForm({ ...termForm, name: e.target.value })}
                className={inputClass}
              />
              <input
                required
                type="number"
                min="1"
                placeholder="Ordre"
                value={termForm.order}
                onChange={(e) => setTermForm({ ...termForm, order: e.target.value })}
                className={inputClass}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <input
                required
                type="date"
                value={termForm.startDate}
                onChange={(e) => setTermForm({ ...termForm, startDate: e.target.value })}
                className={inputClass}
              />
              <input
                required
                type="date"
                value={termForm.endDate}
                onChange={(e) => setTermForm({ ...termForm, endDate: e.target.value })}
                className={inputClass}
              />
            </div>
            <button
              type="submit"
              disabled={savingTerm || !selectedYearId}
              className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {savingTerm ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
