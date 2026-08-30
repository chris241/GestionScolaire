import { useEffect, useState, type FormEvent } from 'react';
import { fetchSchools, createSchool, updateSchool } from '../api/schools';
import { useAuth } from '../lib/AuthContext';
import type { School } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

const emptyForm = { name: '', address: '', currency: 'MGA', defaultMaxScore: '20' };

export function Schools() {
  const { user, switchSchool } = useAuth();
  const [schools, setSchools] = useState<School[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    fetchSchools()
      .then(setSchools)
      .catch(() => setError('Impossible de charger les écoles.'))
      .finally(() => setLoading(false));
  }, []);

  function flashMessage(text: string) {
    setMessage(text);
    setTimeout(() => setMessage(null), 3000);
  }

  function startEdit(school: School) {
    setEditingId(school.id);
    setForm({
      name: school.name,
      address: school.address ?? '',
      currency: school.currency,
      defaultMaxScore: String(school.defaultMaxScore),
    });
  }

  function cancelEdit() {
    setEditingId(null);
    setForm(emptyForm);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const payload = {
        name: form.name,
        address: form.address || null,
        currency: form.currency,
        defaultMaxScore: Number(form.defaultMaxScore),
      };

      if (editingId) {
        const updated = await updateSchool(editingId, payload);
        setSchools((prev) => prev.map((s) => (s.id === editingId ? updated : s)));
        flashMessage('École mise à jour.');
      } else {
        const created = await createSchool(payload);
        setSchools((prev) => [...prev, created]);
        flashMessage('École créée.');
      }
      cancelEdit();
    } catch {
      setError(editingId ? "Impossible de mettre à jour l'école." : "Impossible de créer l'école.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-4xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Écoles</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Gérez les établissements que vous dirigez.'}
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

      <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">Vos écoles</h2>

        <div className="mt-4 flex flex-col gap-2">
          {schools.length === 0 && !loading && (
            <p className="text-sm text-slate-soft">Aucune école pour l'instant. Créez-en une ci-dessous.</p>
          )}
          {schools.map((school) => (
            <div
              key={school.id}
              className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5"
            >
              <div>
                <p className="text-sm font-medium text-slate">
                  {school.name}
                  {user?.activeSchoolId === school.id && (
                    <span className="ml-2 rounded-full bg-primary-soft px-2.5 py-0.5 text-xs font-medium text-primary">
                      Active
                    </span>
                  )}
                </p>
                <p className="text-xs text-slate-soft">
                  {school.address ?? 'Adresse non renseignée'} · {school.currency} · Note max {school.defaultMaxScore}
                </p>
              </div>
              <div className="flex items-center gap-3">
                {user?.activeSchoolId !== school.id && (
                  <button
                    type="button"
                    onClick={() => switchSchool(school.id)}
                    className="text-xs font-medium text-primary hover:text-primary-hover"
                  >
                    Activer
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => startEdit(school)}
                  className="text-xs font-medium text-slate-soft hover:text-slate"
                >
                  Modifier
                </button>
              </div>
            </div>
          ))}
        </div>

        <form onSubmit={handleSubmit} className="mt-4 grid grid-cols-1 gap-3 border-t border-border pt-4 sm:grid-cols-2">
          <h3 className="text-sm font-semibold text-slate sm:col-span-2">
            {editingId ? "Modifier l'école" : 'Ajouter une école'}
          </h3>
          <input
            required
            placeholder="Nom de l'établissement"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            className={inputClass}
          />
          <input
            placeholder="Adresse"
            value={form.address}
            onChange={(e) => setForm({ ...form, address: e.target.value })}
            className={inputClass}
          />
          <input
            required
            placeholder="Devise"
            value={form.currency}
            onChange={(e) => setForm({ ...form, currency: e.target.value })}
            className={inputClass}
          />
          <input
            required
            type="number"
            step="0.5"
            min="1"
            placeholder="Note maximale par défaut"
            value={form.defaultMaxScore}
            onChange={(e) => setForm({ ...form, defaultMaxScore: e.target.value })}
            className={inputClass}
          />
          <div className="flex items-center gap-3 sm:col-span-2">
            <button
              type="submit"
              disabled={saving}
              className="mt-1 w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {saving ? 'Enregistrement...' : editingId ? 'Mettre à jour' : 'Créer'}
            </button>
            {editingId && (
              <button
                type="button"
                onClick={cancelEdit}
                className="mt-1 w-fit rounded-xl border border-border px-4 py-2.5 text-sm font-medium text-slate-soft transition-colors hover:bg-bg"
              >
                Annuler
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}
