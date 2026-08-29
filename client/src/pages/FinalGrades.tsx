import { useEffect, useState, type FormEvent } from 'react';
import { fetchFinalGradesByClass, fetchCourseWiseAssessment } from '../api/finalGrades';
import { fetchStudents } from '../api/students';
import { fetchAcademicYears } from '../api/academicYears';
import { fetchAcademicTerms } from '../api/academicTerms';
import { fetchAssessmentGroups, createAssessmentGroup, deleteAssessmentGroup } from '../api/assessmentGroups';
import { fetchGradingScales, createGradingScale, addGradingScaleInterval, deleteGradingScale } from '../api/gradingScales';
import { fetchCourses } from '../api/courses';
import { fetchAssessmentPlans, createAssessmentPlan, updateAssessmentPlanStatus } from '../api/assessmentPlans';
import { downloadClassBulletins } from '../api/grades';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth } from '../lib/AuthContext';
import type { AcademicTerm, AssessmentGroup, AssessmentPlan, AssessmentPlanStatus, Course, CourseWiseAssessment, FinalGrade, GradingScale } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

export function FinalGrades() {
  const { user } = useAuth();
  const isDirector = user?.role === 'Director';

  const [classOptions, setClassOptions] = useState<[string, string][]>([]);
  const [selectedClassId, setSelectedClassId] = useState('');
  const [terms, setTerms] = useState<AcademicTerm[]>([]);
  const [selectedTermName, setSelectedTermName] = useState('');
  const [finalGrades, setFinalGrades] = useState<FinalGrade[]>([]);
  const [courseWiseReport, setCourseWiseReport] = useState<CourseWiseAssessment[]>([]);
  const [downloadingBulletins, setDownloadingBulletins] = useState(false);
  const [groups, setGroups] = useState<AssessmentGroup[]>([]);
  const [scales, setScales] = useState<GradingScale[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [plans, setPlans] = useState<AssessmentPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [groupForm, setGroupForm] = useState({ name: '', weightage: '50' });
  const [scaleForm, setScaleForm] = useState({ name: '', isDefault: false });
  const [intervalForm, setIntervalForm] = useState({ scaleId: '', grade: '', minScore: '', maxScore: '' });
  const [planForm, setPlanForm] = useState({ name: '', courseId: '', assessmentGroupId: '', maxScore: '20', plannedDate: '' });
  const [savingGroup, setSavingGroup] = useState(false);
  const [savingScale, setSavingScale] = useState(false);
  const [savingInterval, setSavingInterval] = useState(false);
  const [savingPlan, setSavingPlan] = useState(false);

  useEffect(() => {
    Promise.all([fetchStudents(), fetchAcademicYears()])
      .then(([students, yearsData]) => {
        const options = Array.from(new Map(students.map((s) => [s.classId, s.className])).entries());
        setClassOptions(options);
        if (options.length > 0) setSelectedClassId(options[0][0]);
        const current = yearsData.find((y) => y.isCurrent) ?? yearsData[0];
        if (current) {
          fetchAcademicTerms(current.id).then((termsData) => {
            setTerms(termsData);
            if (termsData.length > 0) setSelectedTermName(termsData[0].name);
          });
        }
      })
      .catch(() => setError('Impossible de charger les classes.'))
      .finally(() => setLoading(false));

    if (isDirector) {
      Promise.all([fetchAssessmentGroups(), fetchGradingScales(), fetchCourses()])
        .then(([groupsData, scalesData, coursesData]) => {
          setGroups(groupsData);
          setScales(scalesData);
          setCourses(coursesData);
        })
        .catch(() => setError("Impossible de charger les données d'évaluation."));
    }
  }, [isDirector]);

  useEffect(() => {
    if (!selectedClassId || !selectedTermName) return;
    fetchFinalGradesByClass(selectedClassId, selectedTermName)
      .then(setFinalGrades)
      .catch(() => setError('Impossible de charger les résultats finaux.'));
    fetchCourseWiseAssessment(selectedClassId, selectedTermName)
      .then(setCourseWiseReport)
      .catch(() => setError("Impossible de charger le rapport d'évaluation par cours."));
  }, [selectedClassId, selectedTermName]);

  async function handleDownloadClassBulletins() {
    if (!selectedClassId || !selectedTermName) return;
    setDownloadingBulletins(true);
    setError(null);
    try {
      await downloadClassBulletins(selectedClassId, selectedTermName);
    } catch {
      setError('Impossible de générer les bulletins de la classe.');
    } finally {
      setDownloadingBulletins(false);
    }
  }

  useEffect(() => {
    if (!isDirector || !selectedClassId) return;
    fetchAssessmentPlans({ classId: selectedClassId })
      .then(setPlans)
      .catch(() => setError("Impossible de charger les plans d'évaluation."));
  }, [isDirector, selectedClassId]);

  async function handleCreateGroup(event: FormEvent) {
    event.preventDefault();
    const term = terms.find((t) => t.name === selectedTermName);
    if (!term) return;

    setSavingGroup(true);
    setError(null);
    try {
      const created = await createAssessmentGroup({
        name: groupForm.name,
        weightage: Number(groupForm.weightage),
        academicTermId: term.id,
      });
      setGroups((prev) => [...prev, created]);
      setGroupForm({ name: '', weightage: '50' });
    } catch {
      setError("Impossible de créer le groupe d'évaluation.");
    } finally {
      setSavingGroup(false);
    }
  }

  async function handleDeleteGroup(id: string) {
    setError(null);
    try {
      await deleteAssessmentGroup(id);
      setGroups((prev) => prev.filter((g) => g.id !== id));
    } catch {
      setError('Impossible de supprimer ce groupe.');
    }
  }

  async function handleCreateScale(event: FormEvent) {
    event.preventDefault();
    setSavingScale(true);
    setError(null);
    try {
      const created = await createGradingScale({ name: scaleForm.name, isDefault: scaleForm.isDefault });
      setScales((prev) => [...prev, created]);
      setScaleForm({ name: '', isDefault: false });
    } catch {
      setError('Impossible de créer le barème.');
    } finally {
      setSavingScale(false);
    }
  }

  async function handleDeleteScale(id: string) {
    setError(null);
    try {
      await deleteGradingScale(id);
      setScales((prev) => prev.filter((s) => s.id !== id));
    } catch {
      setError('Impossible de supprimer ce barème.');
    }
  }

  async function handleAddInterval(event: FormEvent) {
    event.preventDefault();
    if (!intervalForm.scaleId) return;

    setSavingInterval(true);
    setError(null);
    try {
      await addGradingScaleInterval(intervalForm.scaleId, {
        grade: intervalForm.grade,
        minScore: Number(intervalForm.minScore),
        maxScore: Number(intervalForm.maxScore),
      });
      const refreshed = await fetchGradingScales();
      setScales(refreshed);
      setIntervalForm({ scaleId: intervalForm.scaleId, grade: '', minScore: '', maxScore: '' });
    } catch {
      setError("Impossible d'ajouter cette tranche.");
    } finally {
      setSavingInterval(false);
    }
  }

  async function handleCreatePlan(event: FormEvent) {
    event.preventDefault();
    const term = terms.find((t) => t.name === selectedTermName);
    if (!term || !planForm.courseId || !planForm.assessmentGroupId || !selectedClassId) return;

    setSavingPlan(true);
    setError(null);
    try {
      const created = await createAssessmentPlan({
        name: planForm.name,
        maxScore: Number(planForm.maxScore),
        plannedDate: planForm.plannedDate,
        courseId: planForm.courseId,
        classId: selectedClassId,
        academicTermId: term.id,
        assessmentGroupId: planForm.assessmentGroupId,
        gradingScaleId: null,
      });
      setPlans((prev) => [created, ...prev]);
      setPlanForm({ name: '', courseId: '', assessmentGroupId: '', maxScore: '20', plannedDate: '' });
    } catch {
      setError("Impossible de créer le plan d'évaluation.");
    } finally {
      setSavingPlan(false);
    }
  }

  async function handleUpdatePlanStatus(planId: string, status: AssessmentPlanStatus) {
    setError(null);
    try {
      const updated = await updateAssessmentPlanStatus(planId, status);
      setPlans((prev) => prev.map((p) => (p.id === planId ? updated : p)));
    } catch {
      setError('Impossible de mettre à jour le statut de ce plan.');
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Résultats finaux</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Classement et moyennes générales par classe et par trimestre.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <select value={selectedClassId} onChange={(e) => setSelectedClassId(e.target.value)} className={inputClass}>
          {classOptions.map(([id, name]) => (
            <option key={id} value={id}>{name}</option>
          ))}
        </select>
        <select value={selectedTermName} onChange={(e) => setSelectedTermName(e.target.value)} className={inputClass}>
          {terms.map((t) => (
            <option key={t.id} value={t.name}>{t.name}</option>
          ))}
        </select>
        <button
          type="button"
          onClick={handleDownloadClassBulletins}
          disabled={downloadingBulletins || !selectedClassId}
          className="rounded-xl border border-primary px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary-soft disabled:opacity-60"
        >
          {downloadingBulletins ? 'Génération...' : 'Tous les bulletins (ZIP)'}
        </button>
      </div>

      <div className="mt-6 overflow-x-auto rounded-2xl border border-border bg-surface shadow-sm">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3">Rang</th>
              <th className="px-6 py-3">Élève</th>
              <th className="px-6 py-3">Moyenne générale</th>
              <th className="px-6 py-3">Mention</th>
              <th className="px-6 py-3">Note</th>
            </tr>
          </thead>
          <tbody>
            {finalGrades.length === 0 && (
              <tr><td colSpan={5} className="px-6 py-6 text-center text-slate-soft">Aucun résultat pour cette sélection.</td></tr>
            )}
            {finalGrades.map((fg) => (
              <tr key={fg.studentId} className="border-b border-border last:border-0">
                <td className="px-6 py-3 font-medium text-slate">{fg.classRank}/{fg.classSize}</td>
                <td className="px-6 py-3 text-slate">{fg.studentFullName}</td>
                <td className="px-6 py-3 text-slate">{fg.generalAverage.toFixed(2)}/20</td>
                <td className="px-6 py-3 text-slate-soft">{fg.mention}</td>
                <td className="px-6 py-3">
                  {fg.letterGrade && (
                    <span className="rounded-full bg-primary-soft px-2.5 py-1 text-xs font-medium text-primary">
                      {fg.letterGrade}
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mt-6 overflow-x-auto rounded-2xl border border-border bg-surface shadow-sm">
        <div className="border-b border-border px-6 py-3">
          <h2 className="text-sm font-semibold text-slate">Rapport d'évaluation par cours</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-border text-left text-xs font-medium uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3">Cours</th>
              <th className="px-6 py-3">Moyenne de classe</th>
              <th className="px-6 py-3">Min</th>
              <th className="px-6 py-3">Max</th>
              <th className="px-6 py-3">Élèves évalués</th>
            </tr>
          </thead>
          <tbody>
            {courseWiseReport.length === 0 && (
              <tr><td colSpan={5} className="px-6 py-6 text-center text-slate-soft">Aucune note pour cette sélection.</td></tr>
            )}
            {courseWiseReport.map((c) => (
              <tr key={c.courseName} className="border-b border-border last:border-0">
                <td className="px-6 py-3 font-medium text-slate">{c.courseName}</td>
                <td className="px-6 py-3 text-slate">{c.classAverage.toFixed(2)}/20</td>
                <td className="px-6 py-3 text-slate-soft">{c.minAverage.toFixed(2)}/20</td>
                <td className="px-6 py-3 text-slate-soft">{c.maxAverage.toFixed(2)}/20</td>
                <td className="px-6 py-3 text-slate-soft">{c.studentsEvaluated}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {isDirector && (
        <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
          <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
            <h2 className="text-base font-semibold text-slate">Groupes d'évaluation</h2>
            <div className="mt-4 flex flex-col gap-2">
              {groups.length === 0 && <p className="text-sm text-slate-soft">Aucun groupe créé.</p>}
              {groups.map((g) => (
                <div key={g.id} className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5">
                  <div>
                    <p className="text-sm font-medium text-slate">{g.name}</p>
                    <p className="text-xs text-slate-soft">{g.weightage}% · {g.academicTermName}</p>
                  </div>
                  <button type="button" onClick={() => handleDeleteGroup(g.id)} className="text-xs font-medium text-danger hover:text-danger">
                    Supprimer
                  </button>
                </div>
              ))}
            </div>
            <form onSubmit={handleCreateGroup} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-slate">Créer un groupe (trimestre sélectionné)</h3>
              <div className="grid grid-cols-2 gap-3">
                <input required placeholder="Nom (ex: Devoirs)" value={groupForm.name} onChange={(e) => setGroupForm({ ...groupForm, name: e.target.value })} className={inputClass} />
                <input required type="number" min="0" max="100" placeholder="Pondération %" value={groupForm.weightage} onChange={(e) => setGroupForm({ ...groupForm, weightage: e.target.value })} className={inputClass} />
              </div>
              <button type="submit" disabled={savingGroup} className="mt-1 w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                {savingGroup ? 'Création...' : 'Créer'}
              </button>
            </form>
          </div>

          <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
            <h2 className="text-base font-semibold text-slate">Barèmes de notation</h2>
            <div className="mt-4 flex flex-col gap-2">
              {scales.length === 0 && <p className="text-sm text-slate-soft">Aucun barème créé.</p>}
              {scales.map((s) => (
                <div key={s.id} className="rounded-xl border border-border px-3.5 py-2.5">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium text-slate">{s.name} {s.isDefault && <span className="ml-1 text-xs text-primary">(par défaut)</span>}</p>
                    <button type="button" onClick={() => handleDeleteScale(s.id)} className="text-xs font-medium text-danger hover:text-danger">
                      Supprimer
                    </button>
                  </div>
                  <p className="mt-1 text-xs text-slate-soft">
                    {s.intervals.map((i) => `${i.grade}: ${i.minScore}-${i.maxScore}`).join(' · ') || 'Aucune tranche'}
                  </p>
                </div>
              ))}
            </div>
            <form onSubmit={handleCreateScale} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-slate">Créer un barème</h3>
              <div className="flex gap-3">
                <input required placeholder="Nom" value={scaleForm.name} onChange={(e) => setScaleForm({ ...scaleForm, name: e.target.value })} className={`${inputClass} flex-1`} />
                <label className="flex items-center gap-2 text-sm text-slate-soft">
                  <input type="checkbox" checked={scaleForm.isDefault} onChange={(e) => setScaleForm({ ...scaleForm, isDefault: e.target.checked })} />
                  Par défaut
                </label>
              </div>
              <button type="submit" disabled={savingScale} className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                {savingScale ? 'Création...' : 'Créer'}
              </button>
            </form>

            <form onSubmit={handleAddInterval} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-slate">Ajouter une tranche</h3>
              <select required value={intervalForm.scaleId} onChange={(e) => setIntervalForm({ ...intervalForm, scaleId: e.target.value })} className={inputClass}>
                <option value="" disabled>Barème...</option>
                {scales.map((s) => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
              <div className="grid grid-cols-3 gap-3">
                <input required placeholder="Lettre" value={intervalForm.grade} onChange={(e) => setIntervalForm({ ...intervalForm, grade: e.target.value })} className={inputClass} />
                <input required type="number" step="0.01" placeholder="Min" value={intervalForm.minScore} onChange={(e) => setIntervalForm({ ...intervalForm, minScore: e.target.value })} className={inputClass} />
                <input required type="number" step="0.01" placeholder="Max" value={intervalForm.maxScore} onChange={(e) => setIntervalForm({ ...intervalForm, maxScore: e.target.value })} className={inputClass} />
              </div>
              <button type="submit" disabled={savingInterval} className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                {savingInterval ? 'Ajout...' : 'Ajouter'}
              </button>
            </form>
          </div>

          <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm lg:col-span-2">
            <h2 className="text-base font-semibold text-slate">Plans d'évaluation (classe sélectionnée)</h2>
            <div className="mt-4 flex flex-col gap-2">
              {plans.length === 0 && <p className="text-sm text-slate-soft">Aucun plan d'évaluation pour cette classe.</p>}
              {plans.map((plan) => (
                <div key={plan.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border px-3.5 py-2.5">
                  <div>
                    <p className="text-sm font-medium text-slate">{plan.name}</p>
                    <p className="text-xs text-slate-soft">
                      {plan.courseName} · {plan.assessmentGroupName} · {new Date(plan.plannedDate).toLocaleDateString('fr-FR')}
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <StatusBadge status={plan.status} />
                    <select
                      value={plan.status}
                      onChange={(e) => handleUpdatePlanStatus(plan.id, e.target.value as AssessmentPlanStatus)}
                      className={inputClass}
                    >
                      <option value="Draft">Brouillon</option>
                      <option value="Scheduled">Planifié</option>
                      <option value="Completed">Terminé</option>
                    </select>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm lg:col-span-2">
            <h2 className="text-base font-semibold text-slate">Planifier une évaluation (classe et trimestre sélectionnés)</h2>
            <form onSubmit={handleCreatePlan} className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
              <input required placeholder="Nom" value={planForm.name} onChange={(e) => setPlanForm({ ...planForm, name: e.target.value })} className={inputClass} />
              <select required value={planForm.courseId} onChange={(e) => setPlanForm({ ...planForm, courseId: e.target.value })} className={inputClass}>
                <option value="" disabled>Cours...</option>
                {courses.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
              <select required value={planForm.assessmentGroupId} onChange={(e) => setPlanForm({ ...planForm, assessmentGroupId: e.target.value })} className={inputClass}>
                <option value="" disabled>Groupe...</option>
                {groups.map((g) => (
                  <option key={g.id} value={g.id}>{g.name}</option>
                ))}
              </select>
              <input required type="date" value={planForm.plannedDate} onChange={(e) => setPlanForm({ ...planForm, plannedDate: e.target.value })} className={inputClass} />
              <button type="submit" disabled={savingPlan} className="col-span-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60 md:col-span-4">
                {savingPlan ? 'Création...' : "Planifier l'évaluation"}
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
