import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import {
  fetchStudents,
  fetchSiblings,
  addSibling,
  removeSibling,
  importStudents,
  fetchStudentFeeCategories,
  subscribeToFeeCategory,
  unsubscribeFromFeeCategory,
} from '../api/students';
import {
  fetchGuardians,
  createGuardian,
  fetchStudentGuardians,
  linkGuardianToStudent,
  unlinkGuardianFromStudent,
  updateGuardianInterests,
} from '../api/guardians';
import { fetchStudentInvoices } from '../api/invoices';
import type { Guardian, Invoice, Sibling, Student, StudentFeeCategory, StudentGuardianLink, StudentImportResult } from '../types';
import { useAuth } from '../lib/AuthContext';
import { StatusBadge } from '../components/StatusBadge';
import { formatAmount } from '../lib/format';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function Students() {
  const { user } = useAuth();
  const isParent = user?.role === 'Parent';
  const isTeacher = user?.role === 'Teacher';
  const isDirector = user?.role === 'Director';
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [selectedStudentId, setSelectedStudentId] = useState<string | null>(null);
  const [importResult, setImportResult] = useState<StudentImportResult | null>(null);
  const [importing, setImporting] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchStudents()
      .then((data) => !cancelled && setStudents(data))
      .catch(() => !cancelled && setError('Impossible de charger la liste des élèves.'))
      .finally(() => !cancelled && setLoading(false));

    return () => {
      cancelled = true;
    };
  }, []);

  const filtered = students.filter((s) =>
    `${s.firstName} ${s.lastName} ${s.enrollmentNumber} ${s.className}`
      .toLowerCase()
      .includes(search.toLowerCase())
  );

  const selectedStudent = students.find((s) => s.id === selectedStudentId) ?? null;

  async function handleImportFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;

    setImporting(true);
    setError(null);
    setImportResult(null);
    try {
      const result = await importStudents(file);
      setImportResult(result);
      if (result.successCount > 0) {
        setStudents(await fetchStudents());
      }
    } catch {
      setError("Impossible d'importer ce fichier.");
    } finally {
      setImporting(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }

  return (
    <div className="mx-auto max-w-7xl px-6 py-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate">
            {isParent ? 'Mes enfants' : isTeacher ? 'Mes élèves' : 'Élèves'}
          </h1>
          <p className="mt-1 text-sm text-slate-soft">
            {isParent
              ? `${students.length} enfant(s) rattaché(s)`
              : isTeacher
                ? `${students.length} élève(s) dans ma classe`
                : `${students.length} élève(s) inscrit(s)`}
          </p>
        </div>
        {!isParent && (
          <div className="flex items-center gap-3">
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Rechercher un élève..."
              className="w-64 rounded-xl border border-border bg-surface px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
            />
            {isDirector && (
              <>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv,text/csv"
                  onChange={handleImportFile}
                  className="hidden"
                  id="student-import-input"
                />
                <label
                  htmlFor="student-import-input"
                  className={`cursor-pointer rounded-xl border border-primary px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary-soft ${
                    importing ? 'pointer-events-none opacity-60' : ''
                  }`}
                >
                  {importing ? 'Import...' : 'Importer (CSV)'}
                </label>
              </>
            )}
          </div>
        )}
      </div>

      {isDirector && (
        <p className="mt-2 text-xs text-slate-soft">
          Colonnes attendues : FirstName,LastName,DateOfBirth (AAAA-MM-JJ),Gender (Masculin/Feminin),ClassName,EnrollmentNumber (optionnel).
        </p>
      )}

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      {importResult && (
        <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <div className="flex items-center justify-between">
            <h2 className="text-base font-semibold text-slate">Résultat de l'import</h2>
            <button type="button" onClick={() => setImportResult(null)} className="text-xs font-medium text-slate-soft hover:text-slate">
              Fermer
            </button>
          </div>
          <p className="mt-1 text-sm text-slate-soft">
            {importResult.totalRows} ligne(s) · {importResult.successCount} importée(s) · {importResult.errorCount} en échec
          </p>
          {importResult.errorCount > 0 && (
            <div className="mt-3 flex flex-col gap-1.5">
              {importResult.rows.filter((r) => !r.success).map((r) => (
                <p key={r.rowNumber} className="text-xs text-danger">
                  Ligne {r.rowNumber} ({r.firstName} {r.lastName}) : {r.errorMessage}
                </p>
              ))}
            </div>
          )}
        </div>
      )}

      <div className="mt-6 overflow-x-auto rounded-2xl border border-border bg-surface shadow-sm">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3 font-medium">Matricule</th>
              <th className="px-6 py-3 font-medium">Nom complet</th>
              <th className="px-6 py-3 font-medium">Classe</th>
              <th className="px-6 py-3 font-medium">Date de naissance</th>
              <th className="px-6 py-3 font-medium">Statut</th>
            </tr>
          </thead>
          <tbody>
            {!loading && filtered.length === 0 && (
              <tr>
                <td colSpan={5} className="px-6 py-8 text-center text-slate-soft">
                  Aucun élève trouvé.
                </td>
              </tr>
            )}
            {filtered.map((student) => (
              <tr
                key={student.id}
                onClick={() => setSelectedStudentId(student.id === selectedStudentId ? null : student.id)}
                className={`cursor-pointer border-t border-border transition-colors hover:bg-bg ${
                  student.id === selectedStudentId ? 'bg-primary-soft/40' : ''
                }`}
              >
                <td className="px-6 py-4 text-slate-soft">{student.enrollmentNumber}</td>
                <td className="px-6 py-4 font-medium text-slate">{student.firstName} {student.lastName}</td>
                <td className="px-6 py-4 text-slate-soft">{student.className}</td>
                <td className="px-6 py-4 text-slate-soft">{formatDate(student.dateOfBirth)}</td>
                <td className="px-6 py-4">
                  <span
                    className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ${
                      student.isActive ? 'bg-success-soft text-success' : 'bg-border text-slate-soft'
                    }`}
                  >
                    {student.isActive ? 'Actif' : 'Inactif'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {selectedStudent && (
        <StudentDetailPanel
          key={selectedStudent.id}
          student={selectedStudent}
          allStudents={students}
          isDirector={isDirector}
        />
      )}

      {selectedStudent && (isDirector || isParent) && (
        <StudentFeesPanel key={`fees-${selectedStudent.id}`} student={selectedStudent} isDirector={isDirector} />
      )}
    </div>
  );
}

function StudentDetailPanel({
  student,
  allStudents,
  isDirector,
}: {
  student: Student;
  allStudents: Student[];
  isDirector: boolean;
}) {
  const [links, setLinks] = useState<StudentGuardianLink[]>([]);
  const [siblings, setSiblings] = useState<Sibling[]>([]);
  const [guardians, setGuardians] = useState<Guardian[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [linkForm, setLinkForm] = useState({ guardianId: '', relationship: '', isPrimaryContact: false });
  const [newGuardianForm, setNewGuardianForm] = useState({ firstName: '', lastName: '', phone: '', email: '', occupation: '', areasOfInterest: '', relationship: '' });
  const [interestDrafts, setInterestDrafts] = useState<Record<string, string>>({});
  const [savingInterest, setSavingInterest] = useState<string | null>(null);
  const [savingLink, setSavingLink] = useState(false);
  const [savingNewGuardian, setSavingNewGuardian] = useState(false);

  const [siblingToAdd, setSiblingToAdd] = useState('');
  const [savingSibling, setSavingSibling] = useState(false);

  useEffect(() => {
    Promise.all([fetchStudentGuardians(student.id), fetchSiblings(student.id)])
      .then(([guardianLinks, siblingList]) => {
        setLinks(guardianLinks);
        setSiblings(siblingList);
      })
      .catch(() => setError("Impossible de charger les tuteurs ou la fratrie."));

    if (isDirector) {
      fetchGuardians().then(setGuardians).catch(() => setError('Impossible de charger la liste des tuteurs.'));
    }
  }, [student.id, isDirector]);

  async function refreshGuardianLinks() {
    setLinks(await fetchStudentGuardians(student.id));
  }

  async function handleLinkExisting(event: FormEvent) {
    event.preventDefault();
    if (!linkForm.guardianId) return;

    setSavingLink(true);
    setError(null);
    try {
      await linkGuardianToStudent(linkForm.guardianId, student.id, {
        relationship: linkForm.relationship,
        isPrimaryContact: linkForm.isPrimaryContact,
      });
      await refreshGuardianLinks();
      setLinkForm({ guardianId: '', relationship: '', isPrimaryContact: false });
    } catch {
      setError('Impossible de lier ce tuteur (peut-être déjà rattaché).');
    } finally {
      setSavingLink(false);
    }
  }

  async function handleCreateAndLink(event: FormEvent) {
    event.preventDefault();
    setSavingNewGuardian(true);
    setError(null);
    try {
      const created = await createGuardian({
        firstName: newGuardianForm.firstName,
        lastName: newGuardianForm.lastName,
        phone: newGuardianForm.phone,
        email: newGuardianForm.email || null,
        occupation: newGuardianForm.occupation || null,
        areasOfInterest: newGuardianForm.areasOfInterest || null,
      });
      await linkGuardianToStudent(created.id, student.id, {
        relationship: newGuardianForm.relationship,
        isPrimaryContact: links.length === 0,
      });
      setGuardians((prev) => [...prev, created]);
      await refreshGuardianLinks();
      setNewGuardianForm({ firstName: '', lastName: '', phone: '', email: '', occupation: '', areasOfInterest: '', relationship: '' });
    } catch {
      setError('Impossible de créer ce tuteur.');
    } finally {
      setSavingNewGuardian(false);
    }
  }

  async function handleSaveInterest(guardianId: string, currentValue: string | null) {
    const draft = interestDrafts[guardianId] ?? currentValue ?? '';
    if (draft === (currentValue ?? '')) return;

    setSavingInterest(guardianId);
    setError(null);
    try {
      await updateGuardianInterests(guardianId, draft || null);
      setLinks((prev) => prev.map((l) => (l.guardianId === guardianId ? { ...l, areasOfInterest: draft || null } : l)));
    } catch {
      setError("Impossible d'enregistrer les centres d'intérêt.");
    } finally {
      setSavingInterest(null);
    }
  }

  async function handleUnlinkGuardian(guardianId: string) {
    setError(null);
    try {
      await unlinkGuardianFromStudent(guardianId, student.id);
      await refreshGuardianLinks();
    } catch {
      setError('Impossible de retirer ce tuteur.');
    }
  }

  async function handleAddSibling(event: FormEvent) {
    event.preventDefault();
    if (!siblingToAdd) return;

    setSavingSibling(true);
    setError(null);
    try {
      await addSibling(student.id, siblingToAdd);
      setSiblings(await fetchSiblings(student.id));
      setSiblingToAdd('');
    } catch {
      setError('Impossible de lier cet élève comme frère/sœur (le lien existe peut-être déjà).');
    } finally {
      setSavingSibling(false);
    }
  }

  async function handleRemoveSibling(siblingStudentId: string) {
    setError(null);
    try {
      await removeSibling(student.id, siblingStudentId);
      setSiblings(await fetchSiblings(student.id));
    } catch {
      setError('Impossible de retirer ce lien de fratrie.');
    }
  }

  const linkedGuardianIds = new Set(links.map((l) => l.guardianId));
  const linkableGuardians = guardians.filter((g) => !linkedGuardianIds.has(g.id));
  const siblingIds = new Set(siblings.map((s) => s.studentId));
  const linkableStudents = allStudents.filter((s) => s.id !== student.id && !siblingIds.has(s.id));

  return (
    <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
      {error && (
        <div className="lg:col-span-2 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">
          Tuteurs — {student.firstName} {student.lastName}
        </h2>
        <div className="mt-4 flex flex-col gap-2">
          {links.length === 0 && <p className="text-sm text-slate-soft">Aucun tuteur enregistré.</p>}
          {links.map((link) => (
            <div key={link.id} className="rounded-xl border border-border px-3.5 py-2.5">
              <div className="flex items-center justify-between">
                <p className="text-sm font-medium text-slate">
                  {link.guardianFullName}
                  {link.isPrimaryContact && (
                    <span className="ml-2 rounded-full bg-primary-soft px-2 py-0.5 text-xs font-medium text-primary">Contact principal</span>
                  )}
                </p>
                {isDirector && (
                  <button type="button" onClick={() => handleUnlinkGuardian(link.guardianId)} className="text-xs font-medium text-danger hover:text-danger">
                    Retirer
                  </button>
                )}
              </div>
              <p className="mt-1 text-xs text-slate-soft">
                {link.relationship} · {link.phone}{link.email ? ` · ${link.email}` : ''}{link.occupation ? ` · ${link.occupation}` : ''}
              </p>
              {isDirector ? (
                <input
                  placeholder="Centres d'intérêt (bénévolat, comité...)"
                  value={interestDrafts[link.guardianId] ?? link.areasOfInterest ?? ''}
                  onChange={(e) => setInterestDrafts((prev) => ({ ...prev, [link.guardianId]: e.target.value }))}
                  onBlur={() => handleSaveInterest(link.guardianId, link.areasOfInterest)}
                  disabled={savingInterest === link.guardianId}
                  className={`${inputClass} mt-2 w-full py-1.5 text-xs`}
                />
              ) : (
                link.areasOfInterest && <p className="mt-1 text-xs text-slate-soft">Intérêts : {link.areasOfInterest}</p>
              )}
            </div>
          ))}
        </div>

        {isDirector && (
          <>
            {linkableGuardians.length > 0 && (
              <form onSubmit={handleLinkExisting} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
                <h3 className="text-sm font-semibold text-slate">Lier un tuteur existant</h3>
                <select value={linkForm.guardianId} onChange={(e) => setLinkForm({ ...linkForm, guardianId: e.target.value })} className={inputClass}>
                  <option value="">Tuteur...</option>
                  {linkableGuardians.map((g) => (
                    <option key={g.id} value={g.id}>{g.fullName}</option>
                  ))}
                </select>
                <div className="grid grid-cols-2 gap-3">
                  <input required placeholder="Relation (ex: Père)" value={linkForm.relationship} onChange={(e) => setLinkForm({ ...linkForm, relationship: e.target.value })} className={inputClass} />
                  <label className="flex items-center gap-2 text-sm text-slate-soft">
                    <input type="checkbox" checked={linkForm.isPrimaryContact} onChange={(e) => setLinkForm({ ...linkForm, isPrimaryContact: e.target.checked })} />
                    Contact principal
                  </label>
                </div>
                <button type="submit" disabled={savingLink} className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                  {savingLink ? 'Liaison...' : 'Lier'}
                </button>
              </form>
            )}

            <form onSubmit={handleCreateAndLink} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-slate">Créer un nouveau tuteur</h3>
              <div className="grid grid-cols-2 gap-3">
                <input required placeholder="Prénom" value={newGuardianForm.firstName} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, firstName: e.target.value })} className={inputClass} />
                <input required placeholder="Nom" value={newGuardianForm.lastName} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, lastName: e.target.value })} className={inputClass} />
                <input required placeholder="Téléphone" value={newGuardianForm.phone} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, phone: e.target.value })} className={inputClass} />
                <input required placeholder="Relation (ex: Mère)" value={newGuardianForm.relationship} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, relationship: e.target.value })} className={inputClass} />
                <input type="email" placeholder="Email (optionnel)" value={newGuardianForm.email} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, email: e.target.value })} className={inputClass} />
                <input placeholder="Profession (optionnel)" value={newGuardianForm.occupation} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, occupation: e.target.value })} className={inputClass} />
                <input placeholder="Centres d'intérêt (optionnel)" value={newGuardianForm.areasOfInterest} onChange={(e) => setNewGuardianForm({ ...newGuardianForm, areasOfInterest: e.target.value })} className={`${inputClass} col-span-2`} />
              </div>
              <button type="submit" disabled={savingNewGuardian} className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
                {savingNewGuardian ? 'Création...' : 'Créer et lier'}
              </button>
            </form>
          </>
        )}
      </div>

      <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">Fratrie</h2>
        <div className="mt-4 flex flex-col gap-2">
          {siblings.length === 0 && <p className="text-sm text-slate-soft">Aucun frère/sœur enregistré.</p>}
          {siblings.map((s) => (
            <div key={s.studentId} className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5">
              <div>
                <p className="text-sm font-medium text-slate">{s.studentFullName}</p>
                <p className="text-xs text-slate-soft">{s.enrollmentNumber} · {s.className}</p>
              </div>
              {isDirector && (
                <button type="button" onClick={() => handleRemoveSibling(s.studentId)} className="text-xs font-medium text-danger hover:text-danger">
                  Retirer
                </button>
              )}
            </div>
          ))}
        </div>

        {isDirector && linkableStudents.length > 0 && (
          <form onSubmit={handleAddSibling} className="mt-4 flex gap-2 border-t border-border pt-4">
            <select value={siblingToAdd} onChange={(e) => setSiblingToAdd(e.target.value)} className={`${inputClass} flex-1`}>
              <option value="">Élève...</option>
              {linkableStudents.map((s) => (
                <option key={s.id} value={s.id}>{s.firstName} {s.lastName} — {s.className}</option>
              ))}
            </select>
            <button type="submit" disabled={savingSibling} className="rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60">
              {savingSibling ? 'Ajout...' : 'Ajouter'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}

interface FeeMonthGroup {
  feeScheduleId: string;
  dueDate: string;
  structureName: string;
  termName: string;
  lines: Invoice[];
}

function StudentFeesPanel({ student, isDirector }: { student: Student; isDirector: boolean }) {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [categories, setCategories] = useState<StudentFeeCategory[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [togglingId, setTogglingId] = useState<string | null>(null);

  useEffect(() => {
    fetchStudentInvoices(student.id)
      .then(setInvoices)
      .catch(() => setError('Impossible de charger les factures de cet élève.'));

    if (isDirector) {
      fetchStudentFeeCategories(student.id)
        .then(setCategories)
        .catch(() => setError('Impossible de charger les catégories de frais.'));
    }
  }, [student.id, isDirector]);

  async function handleToggleCategory(categoryId: string, isSubscribed: boolean) {
    setTogglingId(categoryId);
    setError(null);
    try {
      if (isSubscribed) {
        await unsubscribeFromFeeCategory(student.id, categoryId);
      } else {
        await subscribeToFeeCategory(student.id, categoryId);
      }
      setCategories(await fetchStudentFeeCategories(student.id));
    } catch {
      setError("Impossible de modifier l'abonnement à cette catégorie.");
    } finally {
      setTogglingId(null);
    }
  }

  const optionalCategories = categories.filter((c) => !c.isMandatory);

  const groups = Object.values(
    invoices.reduce<Record<string, FeeMonthGroup>>((acc, invoice) => {
      const group = (acc[invoice.feeScheduleId] ??= {
        feeScheduleId: invoice.feeScheduleId,
        dueDate: invoice.dueDate,
        structureName: invoice.feeStructureName,
        termName: invoice.academicTermName,
        lines: [],
      });
      group.lines.push(invoice);
      return acc;
    }, {})
  ).sort((a, b) => new Date(b.dueDate).getTime() - new Date(a.dueDate).getTime());

  return (
    <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
      <h2 className="text-base font-semibold text-slate">
        Frais &amp; paiements — {student.firstName} {student.lastName}
      </h2>

      {error && (
        <div className="mt-3 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      {isDirector && optionalCategories.length > 0 && (
        <div className="mt-4 flex flex-wrap items-center gap-4 border-b border-border pb-4">
          <span className="text-xs font-medium text-slate-soft">Catégories facultatives :</span>
          {optionalCategories.map((c) => (
            <label key={c.feeCategoryId} className="flex items-center gap-2 text-sm text-slate">
              <input
                type="checkbox"
                checked={c.isSubscribed}
                disabled={togglingId === c.feeCategoryId}
                onChange={() => handleToggleCategory(c.feeCategoryId, c.isSubscribed)}
              />
              {c.feeCategoryName}
            </label>
          ))}
        </div>
      )}

      <div className="mt-4 flex flex-col gap-4">
        {groups.length === 0 && (
          <p className="text-sm text-slate-soft">Aucune facture générée pour cet élève pour l'instant.</p>
        )}
        {groups.map((group) => (
          <div key={group.feeScheduleId} className="rounded-xl border border-border">
            <div className="border-b border-border bg-bg px-4 py-2">
              <p className="text-sm font-medium text-slate">
                {group.structureName} · {group.termName} · échéance {formatDate(group.dueDate)}
              </p>
            </div>
            <table className="w-full text-left text-sm">
              <tbody>
                {group.lines.map((line) => (
                  <tr key={line.id} className="border-t border-border first:border-t-0">
                    <td className="px-4 py-2.5 text-slate">{line.feeCategoryName}</td>
                    <td className="px-4 py-2.5 text-slate-soft">{formatAmount(line.totalAmount)}</td>
                    <td className="px-4 py-2.5">
                      <StatusBadge status={line.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
      </div>
    </div>
  );
}
