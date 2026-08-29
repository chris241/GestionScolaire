import { useEffect, useState, type FormEvent } from 'react';
import { Download } from 'lucide-react';
import { fetchStudents } from '../api/students';
import { fetchSubjects } from '../api/subjects';
import { fetchStudentGrades, fetchStudentAverage, createGrade, downloadBulletin } from '../api/grades';
import { fetchStudentLogs, createStudentLog } from '../api/studentLogs';
import type { Student, Subject, Grade, StudentGeneralAverage, StudentLog } from '../types';
import { useAuth } from '../lib/AuthContext';

const LOG_TYPES = ['Académique', 'Comportement', 'Médical', 'Général'];

const EVALUATION_TYPES = [
  { value: 1, label: 'Devoir' },
  { value: 2, label: 'Composition' },
  { value: 3, label: 'Examen' },
];

const TERM = 'Trimestre 1';

export function Grades() {
  const { user } = useAuth();
  const canEditGrades = user?.role === 'Teacher' || user?.role === 'Director';
  const [students, setStudents] = useState<Student[]>([]);
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [selectedStudentId, setSelectedStudentId] = useState('');

  const [grades, setGrades] = useState<Grade[]>([]);
  const [average, setAverage] = useState<StudentGeneralAverage | null>(null);
  const [loadingGrades, setLoadingGrades] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [downloading, setDownloading] = useState(false);

  const [form, setForm] = useState({ subjectId: '', score: '', maxScore: '20', coefficient: '1', type: '1' });
  const [submitting, setSubmitting] = useState(false);

  const [logs, setLogs] = useState<StudentLog[]>([]);
  const [logForm, setLogForm] = useState({ logType: LOG_TYPES[0], description: '' });
  const [submittingLog, setSubmittingLog] = useState(false);

  useEffect(() => {
    Promise.all([fetchStudents(), fetchSubjects()])
      .then(([studentsData, subjectsData]) => {
        setStudents(studentsData);
        setSubjects(subjectsData);
        if (studentsData.length > 0) setSelectedStudentId(studentsData[0].id);
      })
      .catch(() => setError("Impossible de charger les élèves ou les matières."));
  }, []);

  useEffect(() => {
    if (!selectedStudentId) return;
    let cancelled = false;

    setLoadingGrades(true);
    Promise.all([fetchStudentGrades(selectedStudentId), fetchStudentAverage(selectedStudentId), fetchStudentLogs(selectedStudentId)])
      .then(([gradesData, averageData, logsData]) => {
        if (cancelled) return;
        setGrades(gradesData);
        setAverage(averageData);
        setLogs(logsData);
      })
      .catch(() => !cancelled && setError('Impossible de charger les notes de cet élève.'))
      .finally(() => !cancelled && setLoadingGrades(false));

    return () => {
      cancelled = true;
    };
  }, [selectedStudentId]);

  const selectedStudent = students.find((s) => s.id === selectedStudentId);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!selectedStudent || !form.subjectId) return;

    setSubmitting(true);
    setError(null);
    try {
      await createGrade({
        studentId: selectedStudent.id,
        subjectId: form.subjectId,
        classId: selectedStudent.classId,
        score: Number(form.score),
        maxScore: Number(form.maxScore),
        coefficient: Number(form.coefficient),
        type: Number(form.type),
        term: TERM,
      });

      const [gradesData, averageData] = await Promise.all([
        fetchStudentGrades(selectedStudent.id),
        fetchStudentAverage(selectedStudent.id),
      ]);
      setGrades(gradesData);
      setAverage(averageData);
      setForm({ subjectId: '', score: '', maxScore: '20', coefficient: '1', type: '1' });
    } catch {
      setError("Impossible d'enregistrer la note. Vérifiez vos droits d'accès.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleAddLog(event: FormEvent) {
    event.preventDefault();
    if (!selectedStudent || !logForm.description.trim()) return;

    setSubmittingLog(true);
    setError(null);
    try {
      const created = await createStudentLog({
        studentId: selectedStudent.id,
        logDate: new Date().toISOString(),
        logType: logForm.logType,
        description: logForm.description,
      });
      setLogs((prev) => [created, ...prev]);
      setLogForm({ logType: LOG_TYPES[0], description: '' });
    } catch {
      setError("Impossible d'ajouter cette entrée au journal.");
    } finally {
      setSubmittingLog(false);
    }
  }

  async function handleDownloadBulletin() {
    if (!selectedStudent) return;
    setDownloading(true);
    try {
      await downloadBulletin(selectedStudent.id, TERM);
    } catch {
      setError('Impossible de générer le bulletin.');
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="mx-auto max-w-7xl px-6 py-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-slate">Notes</h1>

        <div className="flex items-center gap-3">
          <select
            value={selectedStudentId}
            onChange={(e) => setSelectedStudentId(e.target.value)}
            className="rounded-xl border border-border bg-surface px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
          >
            {students.map((s) => (
              <option key={s.id} value={s.id}>
                {s.firstName} {s.lastName} — {s.className}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={handleDownloadBulletin}
            disabled={!selectedStudent || downloading}
            className="flex items-center gap-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            <Download size={16} strokeWidth={2} />
            {downloading ? 'Génération...' : 'Bulletin PDF'}
          </button>
        </div>
      </div>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm lg:col-span-1">
          <h2 className="text-base font-semibold text-slate">Moyenne générale</h2>
          <p className="mt-4 text-4xl font-semibold text-primary">
            {average ? `${average.generalAverage.toFixed(2)}/20` : '—'}
          </p>

          <div className="mt-6 flex flex-col gap-3">
            {average?.subjectAverages.map((s) => (
              <div key={s.subjectName} className="flex items-center justify-between text-sm">
                <span className="text-slate-soft">{s.subjectName}</span>
                <span className="font-medium text-slate">{s.average.toFixed(2)}/20</span>
              </div>
            ))}
          </div>

          {canEditGrades && (
            <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-3 border-t border-border pt-6">
              <h3 className="text-sm font-semibold text-slate">Ajouter une note</h3>

              <select
                required
                value={form.subjectId}
                onChange={(e) => setForm({ ...form, subjectId: e.target.value })}
                className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              >
                <option value="" disabled>Matière</option>
                {subjects.map((s) => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>

              <div className="grid grid-cols-2 gap-3">
                <input
                  required
                  type="number"
                  step="0.1"
                  min="0"
                  placeholder="Note"
                  value={form.score}
                  onChange={(e) => setForm({ ...form, score: e.target.value })}
                  className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
                <input
                  required
                  type="number"
                  step="0.1"
                  min="1"
                  placeholder="Sur"
                  value={form.maxScore}
                  onChange={(e) => setForm({ ...form, maxScore: e.target.value })}
                  className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <input
                  required
                  type="number"
                  step="0.5"
                  min="0.5"
                  placeholder="Coefficient"
                  value={form.coefficient}
                  onChange={(e) => setForm({ ...form, coefficient: e.target.value })}
                  className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
                <select
                  value={form.type}
                  onChange={(e) => setForm({ ...form, type: e.target.value })}
                  className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                >
                  {EVALUATION_TYPES.map((t) => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>

              <button
                type="submit"
                disabled={submitting}
                className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
              >
                {submitting ? 'Enregistrement...' : 'Enregistrer la note'}
              </button>
            </form>
          )}
        </div>

        <div className="rounded-2xl border border-border bg-surface shadow-sm lg:col-span-2">
          <div className="border-b border-border px-6 py-4">
            <h2 className="text-base font-semibold text-slate">Historique des notes</h2>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="text-xs uppercase tracking-wide text-slate-soft">
                  <th className="px-6 py-3 font-medium">Matière</th>
                  <th className="px-6 py-3 font-medium">Type</th>
                  <th className="px-6 py-3 font-medium">Note</th>
                  <th className="px-6 py-3 font-medium">Coeff.</th>
                  <th className="px-6 py-3 font-medium">Date</th>
                </tr>
              </thead>
              <tbody>
                {!loadingGrades && grades.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-6 py-8 text-center text-slate-soft">
                      Aucune note enregistrée.
                    </td>
                  </tr>
                )}
                {grades.map((grade) => (
                  <tr key={grade.id} className="border-t border-border">
                    <td className="px-6 py-4 font-medium text-slate">{grade.subjectName}</td>
                    <td className="px-6 py-4 text-slate-soft">{grade.type}</td>
                    <td className="px-6 py-4 text-slate">{grade.score}/{grade.maxScore}</td>
                    <td className="px-6 py-4 text-slate-soft">{grade.coefficient}</td>
                    <td className="px-6 py-4 text-slate-soft">
                      {new Date(grade.evaluatedAt).toLocaleDateString('fr-FR')}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm lg:col-span-3">
          <h2 className="text-base font-semibold text-slate">Journal de l'élève</h2>

          <div className="mt-4 flex flex-col gap-2">
            {logs.length === 0 && <p className="text-sm text-slate-soft">Aucune entrée pour le moment.</p>}
            {logs.map((log) => (
              <div key={log.id} className="rounded-xl border border-border px-4 py-3">
                <div className="flex items-center justify-between">
                  <span className="rounded-full bg-primary-soft px-2.5 py-0.5 text-xs font-medium text-primary">
                    {log.logType}
                  </span>
                  <span className="text-xs text-slate-soft">
                    {new Date(log.logDate).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' })}
                  </span>
                </div>
                <p className="mt-2 text-sm text-slate">{log.description}</p>
              </div>
            ))}
          </div>

          {canEditGrades && (
            <form onSubmit={handleAddLog} className="mt-4 flex flex-col gap-3 border-t border-border pt-4 sm:flex-row sm:items-start">
              <select
                value={logForm.logType}
                onChange={(e) => setLogForm({ ...logForm, logType: e.target.value })}
                className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              >
                {LOG_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
              <input
                required
                placeholder="Description de l'événement..."
                value={logForm.description}
                onChange={(e) => setLogForm({ ...logForm, description: e.target.value })}
                className="flex-1 rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
              <button
                type="submit"
                disabled={submittingLog}
                className="rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
              >
                {submittingLog ? 'Ajout...' : 'Ajouter'}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
