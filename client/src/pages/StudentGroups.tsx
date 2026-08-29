import { useEffect, useState, type FormEvent } from 'react';
import {
  fetchStudentGroups,
  fetchStudentGroupMembers,
  createStudentGroup,
  updateStudentGroup,
  addStudentGroupMembers,
  removeStudentGroupMember,
  deleteStudentGroup,
} from '../api/studentGroups';
import { fetchStudentCategories, createStudentCategory, deleteStudentCategory } from '../api/studentCategories';
import { fetchAcademicYears } from '../api/academicYears';
import { fetchStudents } from '../api/students';
import { fetchTeachers } from '../api/teachers';
import type { StudentGroup, StudentGroupMember, StudentCategory, AcademicYear, Student, Teacher } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

export function StudentGroups() {
  const [groups, setGroups] = useState<StudentGroup[]>([]);
  const [categories, setCategories] = useState<StudentCategory[]>([]);
  const [years, setYears] = useState<AcademicYear[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [members, setMembers] = useState<StudentGroupMember[]>([]);
  const [studentToAdd, setStudentToAdd] = useState('');
  const [savingTeacher, setSavingTeacher] = useState(false);

  const [groupForm, setGroupForm] = useState({ name: '', groupType: '', maxSize: '', classId: '', teacherId: '' });
  const [categoryForm, setCategoryForm] = useState({ name: '', description: '' });
  const [savingGroup, setSavingGroup] = useState(false);
  const [savingCategory, setSavingCategory] = useState(false);

  const classOptions = Array.from(new Map(students.map((s) => [s.classId, s.className])).entries());

  useEffect(() => {
    Promise.all([fetchStudentGroups(), fetchStudentCategories(), fetchAcademicYears(), fetchStudents(), fetchTeachers()])
      .then(([groupsData, categoriesData, yearsData, studentsData, teachersData]) => {
        setGroups(groupsData);
        setCategories(categoriesData);
        setYears(yearsData);
        setStudents(studentsData);
        setTeachers(teachersData);
      })
      .catch(() => setError('Impossible de charger les données.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedGroupId) return;
    fetchStudentGroupMembers(selectedGroupId)
      .then(setMembers)
      .catch(() => setError('Impossible de charger les membres du groupe.'));
  }, [selectedGroupId]);

  async function handleCreateGroup(event: FormEvent) {
    event.preventDefault();
    const currentYear = years.find((y) => y.isCurrent) ?? years[0];
    if (!currentYear) return;

    setSavingGroup(true);
    setError(null);
    try {
      const created = await createStudentGroup({
        name: groupForm.name,
        groupType: groupForm.groupType,
        maxSize: groupForm.maxSize ? Number(groupForm.maxSize) : null,
        academicYearId: currentYear.id,
        classId: groupForm.classId || null,
        teacherId: groupForm.teacherId || null,
      });
      setGroups((prev) => [...prev, created]);
      setGroupForm({ name: '', groupType: '', maxSize: '', classId: '', teacherId: '' });
    } catch {
      setError('Impossible de créer le groupe.');
    } finally {
      setSavingGroup(false);
    }
  }

  async function handleChangeTeacher(teacherId: string) {
    if (!selectedGroup) return;
    setSavingTeacher(true);
    setError(null);
    try {
      const updated = await updateStudentGroup(selectedGroup.id, {
        name: selectedGroup.name,
        groupType: selectedGroup.groupType,
        maxSize: selectedGroup.maxSize,
        classId: selectedGroup.classId,
        teacherId: teacherId || null,
      });
      setGroups((prev) => prev.map((g) => (g.id === updated.id ? updated : g)));
    } catch {
      setError("Impossible de mettre à jour l'enseignant responsable.");
    } finally {
      setSavingTeacher(false);
    }
  }

  async function handleDeleteGroup(id: string) {
    setError(null);
    try {
      await deleteStudentGroup(id);
      setGroups((prev) => prev.filter((g) => g.id !== id));
      if (selectedGroupId === id) setSelectedGroupId(null);
    } catch {
      setError('Impossible de supprimer ce groupe.');
    }
  }

  async function handleAddMember(event: FormEvent) {
    event.preventDefault();
    if (!selectedGroupId || !studentToAdd) return;

    setError(null);
    try {
      const updated = await addStudentGroupMembers(selectedGroupId, [studentToAdd]);
      setMembers(updated);
      setStudentToAdd('');
      setGroups((prev) => prev.map((g) => (g.id === selectedGroupId ? { ...g, memberCount: updated.length } : g)));
    } catch {
      setError("Impossible d'ajouter cet élève au groupe.");
    }
  }

  async function handleRemoveMember(studentId: string) {
    if (!selectedGroupId) return;
    setError(null);
    try {
      await removeStudentGroupMember(selectedGroupId, studentId);
      setMembers((prev) => prev.filter((m) => m.studentId !== studentId));
      setGroups((prev) =>
        prev.map((g) => (g.id === selectedGroupId ? { ...g, memberCount: g.memberCount - 1 } : g))
      );
    } catch {
      setError('Impossible de retirer cet élève du groupe.');
    }
  }

  async function handleCreateCategory(event: FormEvent) {
    event.preventDefault();
    setSavingCategory(true);
    setError(null);
    try {
      const created = await createStudentCategory({
        name: categoryForm.name,
        description: categoryForm.description || null,
      });
      setCategories((prev) => [...prev, created]);
      setCategoryForm({ name: '', description: '' });
    } catch {
      setError('Impossible de créer la catégorie.');
    } finally {
      setSavingCategory(false);
    }
  }

  async function handleDeleteCategory(id: string) {
    setError(null);
    try {
      await deleteStudentCategory(id);
      setCategories((prev) => prev.filter((c) => c.id !== id));
    } catch {
      setError('Impossible de supprimer cette catégorie.');
    }
  }

  const selectedGroup = groups.find((g) => g.id === selectedGroupId);
  const availableStudents = students.filter((s) => !members.some((m) => m.studentId === s.id));

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Groupes & catégories d'élèves</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Regroupements ad hoc (clubs, niveaux) et catégorisation des élèves.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Groupes</h2>

          <div className="mt-4 flex flex-col gap-2">
            {groups.length === 0 && <p className="text-sm text-slate-soft">Aucun groupe créé.</p>}
            {groups.map((group) => (
              <button
                key={group.id}
                type="button"
                onClick={() => setSelectedGroupId(group.id)}
                className={`flex items-center justify-between rounded-xl border px-3.5 py-2.5 text-left transition-colors ${
                  selectedGroupId === group.id ? 'border-primary bg-primary-soft' : 'border-border hover:bg-bg'
                }`}
              >
                <div>
                  <p className="text-sm font-medium text-slate">{group.name}</p>
                  <p className="text-xs text-slate-soft">
                    {group.groupType} · {group.memberCount} membre(s)
                    {group.className ? ` · ${group.className}` : ''}
                    {group.teacherName ? ` · ${group.teacherName}` : ''}
                  </p>
                </div>
                <span
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDeleteGroup(group.id);
                  }}
                  className="text-xs font-medium text-danger hover:text-danger"
                >
                  Supprimer
                </span>
              </button>
            ))}
          </div>

          <form onSubmit={handleCreateGroup} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Créer un groupe</h3>
            <input
              required
              placeholder="Nom (ex: Club Sciences)"
              value={groupForm.name}
              onChange={(e) => setGroupForm({ ...groupForm, name: e.target.value })}
              className={inputClass}
            />
            <div className="grid grid-cols-2 gap-3">
              <input
                required
                placeholder="Type (ex: Club)"
                value={groupForm.groupType}
                onChange={(e) => setGroupForm({ ...groupForm, groupType: e.target.value })}
                className={inputClass}
              />
              <input
                type="number"
                min="1"
                placeholder="Effectif max"
                value={groupForm.maxSize}
                onChange={(e) => setGroupForm({ ...groupForm, maxSize: e.target.value })}
                className={inputClass}
              />
            </div>
            <select
              value={groupForm.classId}
              onChange={(e) => setGroupForm({ ...groupForm, classId: e.target.value })}
              className={inputClass}
            >
              <option value="">Toutes classes</option>
              {classOptions.map(([id, name]) => (
                <option key={id} value={id}>{name}</option>
              ))}
            </select>
            <select
              value={groupForm.teacherId}
              onChange={(e) => setGroupForm({ ...groupForm, teacherId: e.target.value })}
              className={inputClass}
            >
              <option value="">Aucun enseignant responsable</option>
              {teachers.map((t) => (
                <option key={t.id} value={t.id}>{t.fullName}</option>
              ))}
            </select>
            <button
              type="submit"
              disabled={savingGroup}
              className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {savingGroup ? 'Création...' : 'Créer'}
            </button>
          </form>

          {selectedGroup && (
            <div className="mt-4 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-slate">Membres de « {selectedGroup.name} »</h3>
              <div className="mt-3 flex items-center gap-2">
                <label className="text-xs font-medium text-slate-soft">Enseignant responsable</label>
                <select
                  value={selectedGroup.teacherId ?? ''}
                  onChange={(e) => handleChangeTeacher(e.target.value)}
                  disabled={savingTeacher}
                  className={`${inputClass} flex-1 py-2`}
                >
                  <option value="">Aucun</option>
                  {teachers.map((t) => (
                    <option key={t.id} value={t.id}>{t.fullName}</option>
                  ))}
                </select>
              </div>
              <div className="mt-3 flex flex-col gap-2">
                {members.length === 0 && <p className="text-sm text-slate-soft">Aucun membre.</p>}
                {members.map((member) => (
                  <div
                    key={member.id}
                    className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2"
                  >
                    <span className="text-sm text-slate">{member.studentFullName}</span>
                    <button
                      type="button"
                      onClick={() => handleRemoveMember(member.studentId)}
                      className="text-xs font-medium text-danger hover:text-danger"
                    >
                      Retirer
                    </button>
                  </div>
                ))}
              </div>

              <form onSubmit={handleAddMember} className="mt-3 flex gap-2">
                <select
                  value={studentToAdd}
                  onChange={(e) => setStudentToAdd(e.target.value)}
                  className={`${inputClass} flex-1`}
                >
                  <option value="" disabled>Ajouter un élève...</option>
                  {availableStudents.map((s) => (
                    <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>
                  ))}
                </select>
                <button
                  type="submit"
                  disabled={!studentToAdd}
                  className="rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
                >
                  Ajouter
                </button>
              </form>
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <h2 className="text-base font-semibold text-slate">Catégories d'élèves</h2>

          <div className="mt-4 flex flex-col gap-2">
            {categories.length === 0 && <p className="text-sm text-slate-soft">Aucune catégorie créée.</p>}
            {categories.map((category) => (
              <div
                key={category.id}
                className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5"
              >
                <div>
                  <p className="text-sm font-medium text-slate">{category.name}</p>
                  {category.description && <p className="text-xs text-slate-soft">{category.description}</p>}
                </div>
                <button
                  type="button"
                  onClick={() => handleDeleteCategory(category.id)}
                  className="text-xs font-medium text-danger hover:text-danger"
                >
                  Supprimer
                </button>
              </div>
            ))}
          </div>

          <form onSubmit={handleCreateCategory} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
            <h3 className="text-sm font-semibold text-slate">Créer une catégorie</h3>
            <input
              required
              placeholder="Nom (ex: Boursier)"
              value={categoryForm.name}
              onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
              className={inputClass}
            />
            <input
              placeholder="Description"
              value={categoryForm.description}
              onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })}
              className={inputClass}
            />
            <button
              type="submit"
              disabled={savingCategory}
              className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
            >
              {savingCategory ? 'Création...' : 'Créer'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
