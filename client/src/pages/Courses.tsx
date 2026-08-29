import { useEffect, useState, type FormEvent } from 'react';
import { fetchCourses, createCourse, deleteCourse, addTopic, deleteTopic } from '../api/courses';
import { fetchPrograms } from '../api/programs';
import { fetchSubjects } from '../api/subjects';
import { useAuth } from '../lib/AuthContext';
import type { Course, Program, Subject } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

export function Courses() {
  const { user } = useAuth();
  const isDirector = user?.role === 'Director';
  const [courses, setCourses] = useState<Course[]>([]);
  const [programs, setPrograms] = useState<Program[]>([]);
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedCourseId, setSelectedCourseId] = useState<string | null>(null);
  const [courseForm, setCourseForm] = useState({ name: '', code: '', subjectId: '', programId: '' });
  const [topicForm, setTopicForm] = useState({ name: '', order: '1' });
  const [savingCourse, setSavingCourse] = useState(false);
  const [savingTopic, setSavingTopic] = useState(false);

  useEffect(() => {
    Promise.all([fetchCourses(), fetchPrograms(), fetchSubjects()])
      .then(([coursesData, programsData, subjectsData]) => {
        setCourses(coursesData);
        setPrograms(programsData);
        setSubjects(subjectsData);
      })
      .catch(() => setError('Impossible de charger les données.'))
      .finally(() => setLoading(false));
  }, []);

  async function handleCreateCourse(event: FormEvent) {
    event.preventDefault();
    if (!courseForm.subjectId || !courseForm.programId) return;

    setSavingCourse(true);
    setError(null);
    try {
      const created = await createCourse({
        name: courseForm.name,
        code: courseForm.code || null,
        description: null,
        subjectId: courseForm.subjectId,
        programId: courseForm.programId,
      });
      setCourses((prev) => [...prev, created]);
      setCourseForm({ name: '', code: '', subjectId: '', programId: '' });
    } catch {
      setError('Impossible de créer le cours.');
    } finally {
      setSavingCourse(false);
    }
  }

  async function handleDeleteCourse(id: string) {
    setError(null);
    try {
      await deleteCourse(id);
      setCourses((prev) => prev.filter((c) => c.id !== id));
      if (selectedCourseId === id) setSelectedCourseId(null);
    } catch {
      setError('Impossible de supprimer ce cours.');
    }
  }

  async function handleAddTopic(event: FormEvent) {
    event.preventDefault();
    if (!selectedCourseId) return;

    setSavingTopic(true);
    setError(null);
    try {
      const topic = await addTopic(selectedCourseId, {
        name: topicForm.name,
        description: null,
        order: Number(topicForm.order) || 1,
      });
      setCourses((prev) =>
        prev.map((c) => (c.id === selectedCourseId ? { ...c, topics: [...c.topics, topic] } : c))
      );
      setTopicForm({ name: '', order: '1' });
    } catch {
      setError("Impossible d'ajouter ce chapitre.");
    } finally {
      setSavingTopic(false);
    }
  }

  async function handleDeleteTopic(topicId: string) {
    if (!selectedCourseId) return;
    setError(null);
    try {
      await deleteTopic(topicId);
      setCourses((prev) =>
        prev.map((c) =>
          c.id === selectedCourseId ? { ...c, topics: c.topics.filter((t) => t.id !== topicId) } : c
        )
      );
    } catch {
      setError('Impossible de supprimer ce chapitre.');
    }
  }

  const selectedCourse = courses.find((c) => c.id === selectedCourseId);

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Cours</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Cours rattachés aux matières et programmes, avec leurs chapitres.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Liste des cours</h2>

          <div className="mt-4 flex flex-col gap-2">
            {courses.length === 0 && <p className="text-sm text-slate-soft">Aucun cours créé.</p>}
            {courses.map((course) => (
              <button
                key={course.id}
                type="button"
                onClick={() => setSelectedCourseId(course.id)}
                className={`flex items-center justify-between rounded-xl border px-3.5 py-2.5 text-left transition-colors ${
                  selectedCourseId === course.id ? 'border-primary bg-primary-soft' : 'border-border hover:bg-bg'
                }`}
              >
                <div>
                  <p className="text-sm font-medium text-slate">{course.name}</p>
                  <p className="text-xs text-slate-soft">
                    {course.programName} · {course.subjectName} · {course.topics.length} chapitre(s)
                  </p>
                </div>
                {isDirector && (
                  <span
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDeleteCourse(course.id);
                    }}
                    className="text-xs font-medium text-danger hover:text-danger"
                  >
                    Supprimer
                  </span>
                )}
              </button>
            ))}
          </div>

          {isDirector && (
          <form onSubmit={handleCreateCourse} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Créer un cours</h3>
            <input
              required
              placeholder="Nom du cours"
              value={courseForm.name}
              onChange={(e) => setCourseForm({ ...courseForm, name: e.target.value })}
              className={inputClass}
            />
            <input
              placeholder="Code (optionnel)"
              value={courseForm.code}
              onChange={(e) => setCourseForm({ ...courseForm, code: e.target.value })}
              className={inputClass}
            />
            <select
              required
              value={courseForm.subjectId}
              onChange={(e) => setCourseForm({ ...courseForm, subjectId: e.target.value })}
              className={inputClass}
            >
              <option value="" disabled>Matière...</option>
              {subjects.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <select
              required
              value={courseForm.programId}
              onChange={(e) => setCourseForm({ ...courseForm, programId: e.target.value })}
              className={inputClass}
            >
              <option value="" disabled>Programme...</option>
              {programs.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
            <button
              type="submit"
              disabled={savingCourse}
              className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {savingCourse ? 'Création...' : 'Créer'}
            </button>
          </form>
          )}
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">
            {selectedCourse ? `Chapitres — ${selectedCourse.name}` : 'Chapitres'}
          </h2>

          {!selectedCourse && <p className="mt-4 text-sm text-slate-soft">Sélectionnez un cours.</p>}

          {selectedCourse && (
            <>
              <div className="mt-4 flex flex-col gap-2">
                {selectedCourse.topics.length === 0 && (
                  <p className="text-sm text-slate-soft">Aucun chapitre.</p>
                )}
                {[...selectedCourse.topics]
                  .sort((a, b) => a.order - b.order)
                  .map((topic) => (
                    <div
                      key={topic.id}
                      className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5"
                    >
                      <span className="text-sm text-slate">{topic.order}. {topic.name}</span>
                      <button
                        type="button"
                        onClick={() => handleDeleteTopic(topic.id)}
                        className="text-xs font-medium text-danger hover:text-danger"
                      >
                        Supprimer
                      </button>
                    </div>
                  ))}
              </div>

              <form onSubmit={handleAddTopic} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
                <h3 className="text-sm font-semibold text-slate">Ajouter un chapitre</h3>
                <div className="grid grid-cols-3 gap-3">
                  <input
                    required
                    placeholder="Titre"
                    value={topicForm.name}
                    onChange={(e) => setTopicForm({ ...topicForm, name: e.target.value })}
                    className={`${inputClass} col-span-2`}
                  />
                  <input
                    type="number"
                    min="1"
                    placeholder="Ordre"
                    value={topicForm.order}
                    onChange={(e) => setTopicForm({ ...topicForm, order: e.target.value })}
                    className={inputClass}
                  />
                </div>
                <button
                  type="submit"
                  disabled={savingTopic}
                  className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
                >
                  {savingTopic ? 'Ajout...' : 'Ajouter'}
                </button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
