import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { fetchAttendanceByClass, bulkMarkAttendance, fetchStudentAttendance } from '../api/attendance';
import {
  fetchLeaveApplications,
  fetchStudentLeaveApplications,
  createLeaveApplication,
  decideLeaveApplication,
} from '../api/leaveApplications';
import { fetchStudents } from '../api/students';
import { useAuth } from '../lib/AuthContext';
import type { AttendanceRecord, AttendanceStatus, LeaveApplication, Student } from '../types';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

const STATUS_LABELS: Record<AttendanceStatus, string> = {
  Present: 'Présent',
  Absent: 'Absent',
  Retard: 'Retard',
  Excuse: 'Excusé',
};

const STATUS_BADGE: Record<AttendanceStatus, string> = {
  Present: 'bg-success-soft text-success',
  Absent: 'bg-danger-soft text-danger',
  Retard: 'bg-warning-soft text-warning',
  Excuse: 'bg-primary-soft text-primary',
};

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export function Attendance() {
  const { user } = useAuth();
  const canMark = user?.role === 'Director' || user?.role === 'Teacher';

  if (canMark) return <StaffAttendance />;
  return <ParentAttendance />;
}

function StaffAttendance() {
  const { user } = useAuth();
  const isDirector = user?.role === 'Director';

  const [classOptions, setClassOptions] = useState<[string, string][]>([]);
  const [selectedClassId, setSelectedClassId] = useState('');
  const [date, setDate] = useState(today());
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [leaveApplications, setLeaveApplications] = useState<LeaveApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchStudents()
      .then((students) => {
        const options = Array.from(new Map(students.map((s) => [s.classId, s.className])).entries());
        setClassOptions(options);
        if (options.length > 0) setSelectedClassId(options[0][0]);
      })
      .catch(() => setError('Impossible de charger les classes.'))
      .finally(() => setLoading(false));

    fetchLeaveApplications('Pending')
      .then(setLeaveApplications)
      .catch(() => setError('Impossible de charger les demandes de congé.'));
  }, []);

  useEffect(() => {
    if (!selectedClassId) return;
    fetchAttendanceByClass(selectedClassId, date)
      .then(setRecords)
      .catch(() => setError("Impossible de charger l'appel."));
  }, [selectedClassId, date]);

  function updateRecord(studentId: string, patch: Partial<AttendanceRecord>) {
    setRecords((prev) => prev.map((r) => (r.studentId === studentId ? { ...r, ...patch } : r)));
  }

  async function handleSave(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const updated = await bulkMarkAttendance({
        classId: selectedClassId,
        date,
        entries: records.map((r) => ({
          studentId: r.studentId,
          status: r.status ?? 'Present',
          comment: r.comment,
        })),
      });
      setRecords(updated);
    } catch {
      setError("Impossible d'enregistrer l'appel.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDecide(id: string, approve: boolean) {
    setError(null);
    try {
      await decideLeaveApplication(id, { approve, decisionNotes: null });
      setLeaveApplications((prev) => prev.filter((l) => l.id !== id));
    } catch {
      setError('Impossible de traiter cette demande.');
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Présences</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : "Appel journalier par classe."}
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
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} className={inputClass} />
      </div>

      <form onSubmit={handleSave} className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <div className="flex flex-col gap-2">
          {records.length === 0 && <p className="text-sm text-slate-soft">Aucun élève dans cette classe.</p>}
          {records.map((record) => (
            <div
              key={record.studentId}
              className="flex flex-wrap items-center gap-3 rounded-xl border border-border px-3.5 py-2.5"
            >
              <span className="min-w-40 flex-1 text-sm font-medium text-slate">{record.studentFullName}</span>
              <select
                value={record.status ?? 'Present'}
                onChange={(e) => updateRecord(record.studentId, { status: e.target.value as AttendanceStatus })}
                className={inputClass}
              >
                {(Object.keys(STATUS_LABELS) as AttendanceStatus[]).map((status) => (
                  <option key={status} value={status}>{STATUS_LABELS[status]}</option>
                ))}
              </select>
              <input
                placeholder="Commentaire (optionnel)"
                value={record.comment ?? ''}
                onChange={(e) => updateRecord(record.studentId, { comment: e.target.value || null })}
                className={`${inputClass} w-52`}
              />
            </div>
          ))}
        </div>

        <button
          type="submit"
          disabled={saving || records.length === 0}
          className="mt-4 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
        >
          {saving ? 'Enregistrement...' : "Enregistrer l'appel"}
        </button>
      </form>

      <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">Demandes de congé en attente</h2>
        <div className="mt-4 flex flex-col gap-2">
          {leaveApplications.length === 0 && <p className="text-sm text-slate-soft">Aucune demande en attente.</p>}
          {leaveApplications.map((leave) => (
            <div key={leave.id} className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5">
              <div>
                <p className="text-sm font-medium text-slate">{leave.studentFullName}</p>
                <p className="text-xs text-slate-soft">
                  {leave.startDate.slice(0, 10)} → {leave.endDate.slice(0, 10)} · {leave.reason}
                </p>
              </div>
              {isDirector && (
                <div className="flex gap-3">
                  <button
                    type="button"
                    onClick={() => handleDecide(leave.id, true)}
                    className="text-xs font-medium text-success hover:text-success"
                  >
                    Approuver
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDecide(leave.id, false)}
                    className="text-xs font-medium text-danger hover:text-danger"
                  >
                    Refuser
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function ParentAttendance() {
  const { user } = useAuth();
  const canRequestLeave = user?.role === 'Parent';
  const [students, setStudents] = useState<Student[]>([]);
  const [selectedStudentId, setSelectedStudentId] = useState('');
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [leaveApplications, setLeaveApplications] = useState<LeaveApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [requesting, setRequesting] = useState(false);
  const [leaveForm, setLeaveForm] = useState({ startDate: today(), endDate: today(), reason: '' });

  useEffect(() => {
    fetchStudents()
      .then((data) => {
        setStudents(data);
        if (data.length > 0) setSelectedStudentId(data[0].id);
      })
      .catch(() => setError('Impossible de charger vos enfants.'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedStudentId) return;
    Promise.all([fetchStudentAttendance(selectedStudentId), fetchStudentLeaveApplications(selectedStudentId)])
      .then(([attendanceData, leaveData]) => {
        setRecords(attendanceData);
        setLeaveApplications(leaveData);
      })
      .catch(() => setError('Impossible de charger les présences.'));
  }, [selectedStudentId]);

  async function handleRequestLeave(event: FormEvent) {
    event.preventDefault();
    if (!selectedStudentId) return;

    setRequesting(true);
    setError(null);
    try {
      const created = await createLeaveApplication({
        studentId: selectedStudentId,
        startDate: leaveForm.startDate,
        endDate: leaveForm.endDate,
        reason: leaveForm.reason,
      });
      setLeaveApplications((prev) => [created, ...prev]);
      setLeaveForm({ startDate: today(), endDate: today(), reason: '' });
    } catch {
      setError('Impossible de soumettre la demande de congé.');
    } finally {
      setRequesting(false);
    }
  }

  const selectedStudent = useMemo(() => students.find((s) => s.id === selectedStudentId), [students, selectedStudentId]);

  return (
    <div className="mx-auto max-w-4xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Présences</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : 'Historique de présence et demandes de congé.'}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      {students.length > 1 && (
        <select
          value={selectedStudentId}
          onChange={(e) => setSelectedStudentId(e.target.value)}
          className={`${inputClass} mt-6`}
        >
          {students.map((s) => (
            <option key={s.id} value={s.id}>{s.firstName} {s.lastName}</option>
          ))}
        </select>
      )}

      <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">
          Historique {selectedStudent ? `— ${selectedStudent.firstName}` : ''}
        </h2>
        <div className="mt-4 flex flex-col gap-2">
          {records.length === 0 && <p className="text-sm text-slate-soft">Aucun enregistrement.</p>}
          {records.map((record) => (
            <div key={record.id ?? record.date} className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5">
              <span className="text-sm text-slate">{record.date.slice(0, 10)}</span>
              {record.status && (
                <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${STATUS_BADGE[record.status]}`}>
                  {STATUS_LABELS[record.status]}
                </span>
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">Demandes de congé</h2>
        <div className="mt-4 flex flex-col gap-2">
          {leaveApplications.length === 0 && <p className="text-sm text-slate-soft">Aucune demande.</p>}
          {leaveApplications.map((leave) => (
            <div key={leave.id} className="rounded-xl border border-border px-3.5 py-2.5">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-slate">
                  {leave.startDate.slice(0, 10)} → {leave.endDate.slice(0, 10)}
                </span>
                <span className="text-xs font-medium text-slate-soft">{leave.status}</span>
              </div>
              <p className="mt-1 text-xs text-slate-soft">{leave.reason}</p>
            </div>
          ))}
        </div>

        {canRequestLeave && (
        <form onSubmit={handleRequestLeave} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
          <h3 className="text-sm font-semibold text-slate">Nouvelle demande</h3>
          <div className="grid grid-cols-2 gap-3">
            <input
              type="date"
              required
              value={leaveForm.startDate}
              onChange={(e) => setLeaveForm({ ...leaveForm, startDate: e.target.value })}
              className={inputClass}
            />
            <input
              type="date"
              required
              value={leaveForm.endDate}
              onChange={(e) => setLeaveForm({ ...leaveForm, endDate: e.target.value })}
              className={inputClass}
            />
          </div>
          <input
            required
            placeholder="Motif"
            value={leaveForm.reason}
            onChange={(e) => setLeaveForm({ ...leaveForm, reason: e.target.value })}
            className={inputClass}
          />
          <button
            type="submit"
            disabled={requesting}
            className="mt-1 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {requesting ? 'Envoi...' : 'Envoyer la demande'}
          </button>
        </form>
        )}
      </div>
    </div>
  );
}
