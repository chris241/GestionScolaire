import { useEffect, useState, type FormEvent } from 'react';
import { fetchTeachers, createTeacher } from '../api/teachers';
import { fetchTeacherLogs, createTeacherLog, deleteTeacherLog } from '../api/teacherLogs';
import type { Teacher, TeacherLog } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

const emptyForm = { firstName: '', lastName: '', email: '', password: '', specialty: '', hireDate: '' };
const emptyLogForm = { logType: '', description: '' };

export function Teachers() {
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  const [selectedTeacherId, setSelectedTeacherId] = useState<string | null>(null);
  const [logs, setLogs] = useState<TeacherLog[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [logForm, setLogForm] = useState(emptyLogForm);
  const [savingLog, setSavingLog] = useState(false);

  useEffect(() => {
    fetchTeachers()
      .then(setTeachers)
      .catch(() => setError('Impossible de charger les enseignants.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedTeacherId) return;
    setLogsLoading(true);
    fetchTeacherLogs(selectedTeacherId)
      .then(setLogs)
      .catch(() => setError('Impossible de charger le journal.'))
      .finally(() => setLogsLoading(false));
  }, [selectedTeacherId]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const created = await createTeacher(form);
      setTeachers((prev) => [...prev, created].sort((a, b) => a.fullName.localeCompare(b.fullName)));
      setForm(emptyForm);
    } catch {
      setError("Impossible de créer l'enseignant (l'email est peut-être déjà utilisé).");
    } finally {
      setSaving(false);
    }
  }

  async function handleCreateLog(event: FormEvent) {
    event.preventDefault();
    if (!selectedTeacherId) return;
    setSavingLog(true);
    setError(null);
    try {
      const created = await createTeacherLog({
        teacherId: selectedTeacherId,
        logDate: new Date().toISOString(),
        logType: logForm.logType,
        description: logForm.description,
      });
      setLogs((prev) => [created, ...prev]);
      setLogForm(emptyLogForm);
    } catch {
      setError("Impossible d'ajouter cette entrée au journal.");
    } finally {
      setSavingLog(false);
    }
  }

  async function handleDeleteLog(id: string) {
    setError(null);
    try {
      await deleteTeacherLog(id);
      setLogs((prev) => prev.filter((l) => l.id !== id));
    } catch {
      setError('Impossible de supprimer cette entrée.');
    }
  }

  const selectedTeacher = teachers.find((t) => t.id === selectedTeacherId);

  return (
    <div className="mx-auto max-w-5xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Enseignants</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : `${teachers.length} enseignant(s)`}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Liste des enseignants</h2>
          <div className="mt-4 flex flex-col gap-2">
            {teachers.length === 0 && <p className="text-sm text-slate-soft">Aucun enseignant enregistré.</p>}
            {teachers.map((t) => (
              <button
                key={t.id}
                type="button"
                onClick={() => setSelectedTeacherId(t.id)}
                className={`rounded-xl border px-3.5 py-2.5 text-left transition-colors ${
                  selectedTeacherId === t.id ? 'border-primary bg-primary-soft' : 'border-border hover:bg-bg'
                }`}
              >
                <p className="text-sm font-medium text-slate">{t.fullName}</p>
                <p className="text-xs text-slate-soft">{t.specialty} · {t.email} · embauché le {formatDate(t.hireDate)}</p>
              </button>
            ))}
          </div>

          {selectedTeacher && (
            <div className="mt-4 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-slate">Journal de « {selectedTeacher.fullName} »</h3>
              <div className="mt-3 flex flex-col gap-2">
                {logsLoading && <p className="text-sm text-slate-soft">Chargement...</p>}
                {!logsLoading && logs.length === 0 && <p className="text-sm text-slate-soft">Aucune entrée.</p>}
                {logs.map((log) => (
                  <div key={log.id} className="rounded-xl border border-border px-3.5 py-2.5">
                    <div className="flex items-center justify-between">
                      <p className="text-xs font-medium text-primary">{log.logType} · {formatDate(log.logDate)}</p>
                      <button
                        type="button"
                        onClick={() => handleDeleteLog(log.id)}
                        className="text-xs font-medium text-danger hover:text-danger"
                      >
                        Supprimer
                      </button>
                    </div>
                    <p className="mt-1 text-sm text-slate">{log.description}</p>
                  </div>
                ))}
              </div>

              <form onSubmit={handleCreateLog} className="mt-3 flex flex-col gap-2">
                <input
                  required
                  placeholder="Type (ex: Évaluation, Formation, Incident)"
                  value={logForm.logType}
                  onChange={(e) => setLogForm({ ...logForm, logType: e.target.value })}
                  className={inputClass}
                />
                <textarea
                  required
                  placeholder="Description"
                  value={logForm.description}
                  onChange={(e) => setLogForm({ ...logForm, description: e.target.value })}
                  className={`${inputClass} min-h-20 resize-none`}
                />
                <button
                  type="submit"
                  disabled={savingLog}
                  className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
                >
                  {savingLog ? 'Ajout...' : 'Ajouter au journal'}
                </button>
              </form>
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Ajouter un enseignant</h2>
          <form onSubmit={handleCreate} className="mt-4 flex flex-col gap-3">
            <div className="grid grid-cols-2 gap-3">
              <input required placeholder="Prénom" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} className={inputClass} />
              <input required placeholder="Nom" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} className={inputClass} />
            </div>
            <input required type="email" placeholder="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className={inputClass} />
            <input required type="password" minLength={8} placeholder="Mot de passe (8 caractères min.)" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} className={inputClass} />
            <input required placeholder="Spécialité (ex: Mathématiques)" value={form.specialty} onChange={(e) => setForm({ ...form, specialty: e.target.value })} className={inputClass} />
            <input required type="date" value={form.hireDate} onChange={(e) => setForm({ ...form, hireDate: e.target.value })} className={inputClass} />
            <button
              type="submit"
              disabled={saving}
              className="mt-1 w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {saving ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
