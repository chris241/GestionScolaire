import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { fetchCourseSchedules, createCourseSchedule, deleteCourseSchedule, autoPlanSchedule, commitAutoPlan } from '../api/courseSchedules';
import { fetchStudents } from '../api/students';
import { fetchCourses } from '../api/courses';
import { fetchRooms } from '../api/rooms';
import { fetchTeachers } from '../api/teachers';
import { fetchAcademicTerms } from '../api/academicTerms';
import { fetchAcademicYears } from '../api/academicYears';
import { useAuth } from '../lib/AuthContext';
import type { CourseSchedule, Course, Room, Teacher, AcademicTerm, ScheduleRequirement, AutoPlanScheduleResult, ProposedScheduleSlot } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

const DAY_LABELS: Record<number, string> = {
  1: 'Lundi',
  2: 'Mardi',
  3: 'Mercredi',
  4: 'Jeudi',
  5: 'Vendredi',
  6: 'Samedi',
  0: 'Dimanche',
};

const DAY_ORDER = [1, 2, 3, 4, 5, 6, 0];

export function Schedule() {
  const { user } = useAuth();
  const isDirector = user?.role === 'Director';

  const [classOptions, setClassOptions] = useState<[string, string][]>([]);
  const [selectedClassId, setSelectedClassId] = useState('');
  const [schedules, setSchedules] = useState<CourseSchedule[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [rooms, setRooms] = useState<Room[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [terms, setTerms] = useState<AcademicTerm[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const [form, setForm] = useState({
    courseId: '',
    roomId: '',
    teacherId: '',
    academicTermId: '',
    dayOfWeek: '1',
    startTime: '08:00',
    endTime: '09:00',
  });

  const [showWizard, setShowWizard] = useState(false);
  const [requirements, setRequirements] = useState<ScheduleRequirement[]>([]);
  const [wizardDays, setWizardDays] = useState<number[]>([1, 2, 3, 4, 5]);
  const [wizardSettings, setWizardSettings] = useState({ dailyStartTime: '08:00', periodsPerDay: '6', periodDurationMinutes: '60' });
  const [planning, setPlanning] = useState(false);
  const [committing, setCommitting] = useState(false);
  const [planResult, setPlanResult] = useState<AutoPlanScheduleResult | null>(null);
  const [commitSummary, setCommitSummary] = useState<{ created: number; skipped: string[] } | null>(null);

  useEffect(() => {
    fetchStudents()
      .then((students) => {
        const options = Array.from(new Map(students.map((s) => [s.classId, s.className])).entries());
        setClassOptions(options);
        if (options.length > 0) setSelectedClassId(options[0][0]);
      })
      .catch(() => setError('Impossible de charger les classes.'))
      .finally(() => setLoading(false));

    if (isDirector) {
      Promise.all([fetchCourses(), fetchRooms(), fetchTeachers(), fetchAcademicYears()])
        .then(([coursesData, roomsData, teachersData, yearsData]) => {
          setCourses(coursesData);
          setRooms(roomsData);
          setTeachers(teachersData);
          const currentYear = yearsData.find((y) => y.isCurrent) ?? yearsData[0];
          if (currentYear) {
            fetchAcademicTerms(currentYear.id).then((termsData) => {
              setTerms(termsData);
              if (termsData.length > 0) setForm((f) => ({ ...f, academicTermId: termsData[0].id }));
            });
          }
        })
        .catch(() => setError('Impossible de charger les données du planning.'));
    }
  }, [isDirector]);

  useEffect(() => {
    if (!selectedClassId) return;
    fetchCourseSchedules({ classId: selectedClassId })
      .then(setSchedules)
      .catch(() => setError("Impossible de charger l'emploi du temps."));
  }, [selectedClassId]);

  const schedulesByDay = useMemo(() => {
    const map = new Map<number, CourseSchedule[]>();
    for (const s of schedules) {
      const list = map.get(s.dayOfWeek) ?? [];
      list.push(s);
      map.set(s.dayOfWeek, list);
    }
    for (const list of map.values()) list.sort((a, b) => a.startTime.localeCompare(b.startTime));
    return map;
  }, [schedules]);

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    if (!form.courseId || !form.roomId || !form.teacherId || !form.academicTermId || !selectedClassId) return;

    setSaving(true);
    setError(null);
    try {
      const created = await createCourseSchedule({
        courseId: form.courseId,
        roomId: form.roomId,
        teacherId: form.teacherId,
        classId: selectedClassId,
        academicTermId: form.academicTermId,
        dayOfWeek: Number(form.dayOfWeek),
        startTime: form.startTime,
        endTime: form.endTime,
      });
      setSchedules((prev) => [...prev, created]);
    } catch {
      setError('Impossible de créer ce créneau (la salle est peut-être déjà réservée).');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string) {
    setError(null);
    try {
      await deleteCourseSchedule(id);
      setSchedules((prev) => prev.filter((s) => s.id !== id));
    } catch {
      setError('Impossible de supprimer ce créneau.');
    }
  }

  function addRequirement() {
    if (courses.length === 0 || teachers.length === 0) return;
    setRequirements((prev) => [...prev, { courseId: courses[0].id, teacherId: teachers[0].id, sessionsPerWeek: 2 }]);
  }

  function updateRequirement(index: number, patch: Partial<ScheduleRequirement>) {
    setRequirements((prev) => prev.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  }

  function removeRequirement(index: number) {
    setRequirements((prev) => prev.filter((_, i) => i !== index));
  }

  function toggleWizardDay(day: number) {
    setWizardDays((prev) => (prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day].sort()));
  }

  async function handlePropose() {
    if (!selectedClassId || !form.academicTermId || requirements.length === 0 || wizardDays.length === 0) return;

    setPlanning(true);
    setError(null);
    setPlanResult(null);
    setCommitSummary(null);
    try {
      const result = await autoPlanSchedule({
        classId: selectedClassId,
        academicTermId: form.academicTermId,
        days: wizardDays,
        dailyStartTime: wizardSettings.dailyStartTime,
        periodsPerDay: Number(wizardSettings.periodsPerDay) || 1,
        periodDurationMinutes: Number(wizardSettings.periodDurationMinutes) || 60,
        requirements,
      });
      setPlanResult(result);
    } catch {
      setError('Impossible de générer une proposition de planning.');
    } finally {
      setPlanning(false);
    }
  }

  function removeProposedSlot(index: number) {
    if (!planResult) return;
    setPlanResult({ ...planResult, proposed: planResult.proposed.filter((_, i) => i !== index) });
  }

  async function handleCommitPlan() {
    if (!planResult || planResult.proposed.length === 0) return;

    setCommitting(true);
    setError(null);
    try {
      const result = await commitAutoPlan(planResult.proposed);
      setCommitSummary({ created: result.created.length, skipped: result.skipped });
      setSchedules((prev) => [...prev, ...result.created.filter((c) => c.classId === selectedClassId)]);
      setPlanResult(null);
      setRequirements([]);
    } catch {
      setError("Impossible d'enregistrer ce planning.");
    } finally {
      setCommitting(false);
    }
  }

  const proposedByDay = useMemo(() => {
    const map = new Map<number, ProposedScheduleSlot[]>();
    for (const s of planResult?.proposed ?? []) {
      const list = map.get(s.dayOfWeek) ?? [];
      list.push(s);
      map.set(s.dayOfWeek, list);
    }
    for (const list of map.values()) list.sort((a, b) => a.startTime.localeCompare(b.startTime));
    return map;
  }, [planResult]);

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Emploi du temps</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Planning hebdomadaire des cours par classe.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 flex items-center gap-3">
        <label className="text-sm font-medium text-slate">Classe</label>
        <select
          value={selectedClassId}
          onChange={(e) => setSelectedClassId(e.target.value)}
          className={inputClass}
        >
          {classOptions.map(([id, name]) => (
            <option key={id} value={id}>{name}</option>
          ))}
        </select>
      </div>

      <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-2 xl:grid-cols-3">
        {DAY_ORDER.map((day) => (
          <div key={day} className="rounded-2xl border border-border bg-surface p-5 shadow-sm">
            <h2 className="text-sm font-semibold text-slate">{DAY_LABELS[day]}</h2>
            <div className="mt-3 flex flex-col gap-2">
              {(schedulesByDay.get(day) ?? []).length === 0 && (
                <p className="text-xs text-slate-soft">Aucun cours.</p>
              )}
              {(schedulesByDay.get(day) ?? []).map((s) => (
                <div key={s.id} className="rounded-xl border border-border px-3 py-2">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium text-slate">{s.courseName}</p>
                    {isDirector && (
                      <button
                        type="button"
                        onClick={() => handleDelete(s.id)}
                        className="text-xs font-medium text-danger hover:text-danger"
                      >
                        Supprimer
                      </button>
                    )}
                  </div>
                  <p className="text-xs text-slate-soft">
                    {s.startTime}–{s.endTime} · {s.roomName} · {s.teacherName}
                  </p>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {isDirector && (
        <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base font-semibold text-slate">Assistant de planification</h2>
              <p className="mt-1 text-sm text-slate-soft">
                Propose un planning complet pour la classe et le trimestre sélectionnés, à partir d'une liste de cours à placer.
              </p>
            </div>
            <button
              type="button"
              onClick={() => setShowWizard((v) => !v)}
              className="rounded-xl border border-primary px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary-soft"
            >
              {showWizard ? 'Fermer' : 'Ouvrir l’assistant'}
            </button>
          </div>

          {showWizard && (
            <div className="mt-4 flex flex-col gap-4 border-t border-border pt-4">
              {!form.academicTermId && (
                <p className="text-sm text-slate-soft">Sélectionnez d'abord un trimestre ci-dessous.</p>
              )}

              <div className="flex flex-wrap items-center gap-4">
                <div className="flex flex-wrap items-center gap-2">
                  {DAY_ORDER.filter((d) => d !== 0).map((day) => (
                    <label key={day} className="flex items-center gap-1.5 text-sm text-slate-soft">
                      <input type="checkbox" checked={wizardDays.includes(day)} onChange={() => toggleWizardDay(day)} />
                      {DAY_LABELS[day]}
                    </label>
                  ))}
                </div>
                <div className="flex items-center gap-2">
                  <label className="text-xs text-slate-soft">Début</label>
                  <input
                    type="time"
                    value={wizardSettings.dailyStartTime}
                    onChange={(e) => setWizardSettings({ ...wizardSettings, dailyStartTime: e.target.value })}
                    className={`${inputClass} py-1.5`}
                  />
                  <label className="text-xs text-slate-soft">Périodes/jour</label>
                  <input
                    type="number" min="1" max="12"
                    value={wizardSettings.periodsPerDay}
                    onChange={(e) => setWizardSettings({ ...wizardSettings, periodsPerDay: e.target.value })}
                    className={`${inputClass} w-16 py-1.5`}
                  />
                  <label className="text-xs text-slate-soft">Durée (min)</label>
                  <input
                    type="number" min="15" max="180" step="5"
                    value={wizardSettings.periodDurationMinutes}
                    onChange={(e) => setWizardSettings({ ...wizardSettings, periodDurationMinutes: e.target.value })}
                    className={`${inputClass} w-20 py-1.5`}
                  />
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <h3 className="text-sm font-semibold text-slate">Cours à placer</h3>
                {requirements.length === 0 && <p className="text-sm text-slate-soft">Aucun cours ajouté.</p>}
                {requirements.map((req, i) => (
                  <div key={i} className="flex flex-wrap items-center gap-2">
                    <select
                      value={req.courseId}
                      onChange={(e) => updateRequirement(i, { courseId: e.target.value })}
                      className={`${inputClass} flex-1`}
                    >
                      {courses.map((c) => (
                        <option key={c.id} value={c.id}>{c.name}</option>
                      ))}
                    </select>
                    <select
                      value={req.teacherId}
                      onChange={(e) => updateRequirement(i, { teacherId: e.target.value })}
                      className={`${inputClass} flex-1`}
                    >
                      {teachers.map((t) => (
                        <option key={t.id} value={t.id}>{t.fullName}</option>
                      ))}
                    </select>
                    <input
                      type="number" min="1" max="10"
                      value={req.sessionsPerWeek}
                      onChange={(e) => updateRequirement(i, { sessionsPerWeek: Number(e.target.value) || 1 })}
                      className={`${inputClass} w-20`}
                      title="Séances par semaine"
                    />
                    <span className="text-xs text-slate-soft">séance(s)/sem.</span>
                    <button type="button" onClick={() => removeRequirement(i)} className="text-xs font-medium text-danger hover:text-danger">
                      Retirer
                    </button>
                  </div>
                ))}
                <button type="button" onClick={addRequirement} className="w-fit text-xs font-medium text-primary hover:text-primary-hover">
                  + Ajouter un cours
                </button>
              </div>

              <button
                type="button"
                onClick={handlePropose}
                disabled={planning || requirements.length === 0 || !form.academicTermId}
                className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
              >
                {planning ? 'Calcul...' : 'Proposer un planning'}
              </button>

              {commitSummary && (
                <div className="rounded-xl border border-success/20 bg-success-soft px-4 py-3 text-sm text-success">
                  {commitSummary.created} créneau(x) enregistré(s).
                  {commitSummary.skipped.length > 0 && ` ${commitSummary.skipped.length} ignoré(s) : ${commitSummary.skipped.join(' ')}`}
                </div>
              )}

              {planResult && (
                <div className="rounded-xl border border-border bg-bg p-4">
                  <p className={`text-sm font-medium ${planResult.fullyPlaced ? 'text-success' : 'text-warning'}`}>
                    {planResult.proposed.length} séance(s) proposée(s)
                    {planResult.fullyPlaced ? ', planning complet.' : `, ${planResult.unplaced.length} non placée(s).`}
                  </p>
                  {planResult.unplaced.length > 0 && (
                    <ul className="mt-2 flex flex-col gap-1">
                      {planResult.unplaced.map((msg, i) => (
                        <li key={i} className="text-xs text-warning">{msg}</li>
                      ))}
                    </ul>
                  )}

                  <div className="mt-3 flex flex-col gap-2">
                    {DAY_ORDER.filter((d) => proposedByDay.has(d)).map((day) => (
                      <div key={day}>
                        <p className="text-xs font-semibold uppercase tracking-wide text-slate-soft">{DAY_LABELS[day]}</p>
                        {proposedByDay.get(day)!.map((slot) => {
                          const globalIndex = planResult.proposed.indexOf(slot);
                          return (
                            <div key={globalIndex} className="mt-1 flex items-center justify-between rounded-lg border border-border bg-surface px-3 py-1.5">
                              <span className="text-sm text-slate">
                                {slot.startTime}–{slot.endTime} · {slot.courseName} · {slot.teacherName} · {slot.roomName}
                              </span>
                              <button
                                type="button"
                                onClick={() => removeProposedSlot(globalIndex)}
                                className="text-xs font-medium text-danger hover:text-danger"
                              >
                                Retirer
                              </button>
                            </div>
                          );
                        })}
                      </div>
                    ))}
                  </div>

                  {planResult.proposed.length > 0 && (
                    <button
                      type="button"
                      onClick={handleCommitPlan}
                      disabled={committing}
                      className="mt-3 w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
                    >
                      {committing ? 'Enregistrement...' : `Confirmer et enregistrer (${planResult.proposed.length})`}
                    </button>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {isDirector && (
        <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Ajouter un créneau</h2>
          <form onSubmit={handleCreate} className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-3">
            <select
              required
              value={form.courseId}
              onChange={(e) => setForm({ ...form, courseId: e.target.value })}
              className={inputClass}
            >
              <option value="" disabled>Cours...</option>
              {courses.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            <select
              required
              value={form.roomId}
              onChange={(e) => setForm({ ...form, roomId: e.target.value })}
              className={inputClass}
            >
              <option value="" disabled>Salle...</option>
              {rooms.map((r) => (
                <option key={r.id} value={r.id}>{r.name}</option>
              ))}
            </select>
            <select
              required
              value={form.teacherId}
              onChange={(e) => setForm({ ...form, teacherId: e.target.value })}
              className={inputClass}
            >
              <option value="" disabled>Enseignant...</option>
              {teachers.map((t) => (
                <option key={t.id} value={t.id}>{t.fullName}</option>
              ))}
            </select>
            <select
              required
              value={form.academicTermId}
              onChange={(e) => setForm({ ...form, academicTermId: e.target.value })}
              className={inputClass}
            >
              <option value="" disabled>Trimestre...</option>
              {terms.map((t) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
            <select
              required
              value={form.dayOfWeek}
              onChange={(e) => setForm({ ...form, dayOfWeek: e.target.value })}
              className={inputClass}
            >
              {DAY_ORDER.map((day) => (
                <option key={day} value={day}>{DAY_LABELS[day]}</option>
              ))}
            </select>
            <div className="grid grid-cols-2 gap-2">
              <input
                type="time"
                required
                value={form.startTime}
                onChange={(e) => setForm({ ...form, startTime: e.target.value })}
                className={inputClass}
              />
              <input
                type="time"
                required
                value={form.endTime}
                onChange={(e) => setForm({ ...form, endTime: e.target.value })}
                className={inputClass}
              />
            </div>
            <button
              type="submit"
              disabled={saving}
              className="col-span-2 mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60 md:col-span-3"
            >
              {saving ? 'Création...' : 'Ajouter le créneau'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
}
