import { useEffect, useState, type FormEvent } from 'react';
import { fetchPrograms, createProgram, deleteProgram } from '../api/programs';
import { fetchProgramEnrollments, bulkEnrollStudents } from '../api/programEnrollments';
import { fetchAcademicYears } from '../api/academicYears';
import { fetchStudents } from '../api/students';
import type { Program, ProgramEnrollment, AcademicYear, Student } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

export function Programs() {
  const [programs, setPrograms] = useState<Program[]>([]);
  const [years, setYears] = useState<AcademicYear[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedProgramId, setSelectedProgramId] = useState<string | null>(null);
  const [enrollments, setEnrollments] = useState<ProgramEnrollment[]>([]);
  const [studentsToEnroll, setStudentsToEnroll] = useState<string[]>([]);

  const [programForm, setProgramForm] = useState({ name: '', code: '', description: '' });
  const [savingProgram, setSavingProgram] = useState(false);
  const [enrolling, setEnrolling] = useState(false);

  useEffect(() => {
    Promise.all([fetchPrograms(), fetchAcademicYears(), fetchStudents()])
      .then(([programsData, yearsData, studentsData]) => {
        setPrograms(programsData);
        setYears(yearsData);
        setStudents(studentsData);
      })
      .catch(() => setError('Impossible de charger les données.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedProgramId) return;
    fetchProgramEnrollments(selectedProgramId)
      .then(setEnrollments)
      .catch(() => setError('Impossible de charger les inscriptions.'));
  }, [selectedProgramId]);

  async function handleCreateProgram(event: FormEvent) {
    event.preventDefault();
    setSavingProgram(true);
    setError(null);
    try {
      const created = await createProgram({
        name: programForm.name,
        code: programForm.code,
        description: programForm.description || null,
      });
      setPrograms((prev) => [...prev, created]);
      setProgramForm({ name: '', code: '', description: '' });
    } catch {
      setError('Impossible de créer le programme.');
    } finally {
      setSavingProgram(false);
    }
  }

  async function handleDeleteProgram(id: string) {
    setError(null);
    try {
      await deleteProgram(id);
      setPrograms((prev) => prev.filter((p) => p.id !== id));
      if (selectedProgramId === id) setSelectedProgramId(null);
    } catch {
      setError('Impossible de supprimer ce programme (il est peut-être encore utilisé).');
    }
  }

  async function handleBulkEnroll(event: FormEvent) {
    event.preventDefault();
    const currentYear = years.find((y) => y.isCurrent) ?? years[0];
    if (!selectedProgramId || !currentYear || studentsToEnroll.length === 0) return;

    setEnrolling(true);
    setError(null);
    try {
      const updated = await bulkEnrollStudents({
        studentIds: studentsToEnroll,
        programId: selectedProgramId,
        academicYearId: currentYear.id,
      });
      setEnrollments(updated);
      setStudentsToEnroll([]);
      setPrograms((prev) =>
        prev.map((p) => (p.id === selectedProgramId ? p : p))
      );
    } catch {
      setError("Impossible d'inscrire les élèves sélectionnés.");
    } finally {
      setEnrolling(false);
    }
  }

  const selectedProgram = programs.find((p) => p.id === selectedProgramId);
  const availableStudents = students.filter((s) => !enrollments.some((e) => e.studentId === s.id));

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Programmes</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Programmes académiques et inscription des élèves.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Liste des programmes</h2>

          <div className="mt-4 flex flex-col gap-2">
            {programs.length === 0 && <p className="text-sm text-slate-soft">Aucun programme créé.</p>}
            {programs.map((program) => (
              <button
                key={program.id}
                type="button"
                onClick={() => setSelectedProgramId(program.id)}
                className={`flex items-center justify-between rounded-xl border px-3.5 py-2.5 text-left transition-colors ${
                  selectedProgramId === program.id ? 'border-primary bg-primary-soft' : 'border-border hover:bg-bg'
                }`}
              >
                <div>
                  <p className="text-sm font-medium text-slate">{program.name} <span className="text-xs text-slate-soft">({program.code})</span></p>
                  <p className="text-xs text-slate-soft">
                    {program.classCount} classe(s) · {program.courseCount} cours
                  </p>
                </div>
                <span
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDeleteProgram(program.id);
                  }}
                  className="text-xs font-medium text-danger hover:text-danger"
                >
                  Supprimer
                </span>
              </button>
            ))}
          </div>

          <form onSubmit={handleCreateProgram} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Créer un programme</h3>
            <input
              required
              placeholder="Nom (ex: Collège Général)"
              value={programForm.name}
              onChange={(e) => setProgramForm({ ...programForm, name: e.target.value })}
              className={inputClass}
            />
            <input
              required
              placeholder="Code (ex: COL-GEN)"
              value={programForm.code}
              onChange={(e) => setProgramForm({ ...programForm, code: e.target.value })}
              className={inputClass}
            />
            <input
              placeholder="Description"
              value={programForm.description}
              onChange={(e) => setProgramForm({ ...programForm, description: e.target.value })}
              className={inputClass}
            />
            <button
              type="submit"
              disabled={savingProgram}
              className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {savingProgram ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">
            {selectedProgram ? `Inscriptions — ${selectedProgram.name}` : 'Inscriptions'}
          </h2>

          {!selectedProgram && <p className="mt-4 text-sm text-slate-soft">Sélectionnez un programme.</p>}

          {selectedProgram && (
            <>
              <div className="mt-4 flex flex-col gap-2">
                {enrollments.length === 0 && <p className="text-sm text-slate-soft">Aucun élève inscrit.</p>}
                {enrollments.map((enrollment) => (
                  <div
                    key={enrollment.id}
                    className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5"
                  >
                    <div>
                      <p className="text-sm font-medium text-slate">{enrollment.studentFullName}</p>
                      <p className="text-xs text-slate-soft">{enrollment.academicYearName} · {enrollment.status}</p>
                    </div>
                  </div>
                ))}
              </div>

              <form onSubmit={handleBulkEnroll} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
                <h3 className="text-sm font-semibold text-slate">Inscrire des élèves</h3>
                <select
                  multiple
                  value={studentsToEnroll}
                  onChange={(e) =>
                    setStudentsToEnroll(Array.from(e.target.selectedOptions, (o) => o.value))
                  }
                  className={`${inputClass} h-32`}
                >
                  {availableStudents.map((s) => (
                    <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>
                  ))}
                </select>
                <button
                  type="submit"
                  disabled={enrolling || studentsToEnroll.length === 0}
                  className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
                >
                  {enrolling ? 'Inscription...' : `Inscrire (${studentsToEnroll.length})`}
                </button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
