import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  fetchAttendanceByClass,
  bulkMarkAttendance,
  fetchStudentAttendance,
  fetchAbsentReport,
  fetchMonthlyAttendance,
  fetchBatchAttendanceSummary,
} from '../api/attendance';
import {
  fetchLeaveApplications,
  fetchStudentLeaveApplications,
  createLeaveApplication,
  decideLeaveApplication,
} from '../api/leaveApplications';
import { fetchStudents } from '../api/students';
import { useAuth } from '../lib/AuthContext';
import type {
  AbsentStudent,
  AttendanceRecord,
  AttendanceStatus,
  BatchAttendanceSummary,
  LeaveApplication,
  MonthlyAttendanceRow,
  Student,
} from '../types';

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

const STATUS_LETTER: Record<AttendanceStatus, string> = {
  Present: 'P',
  Absent: 'A',
  Retard: 'R',
  Excuse: 'E',
};

const MONTH_NAMES = [
  'Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin',
  'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre',
];

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

function daysInMonth(year: number, month: number): number {
  return new Date(year, month, 0).getDate();
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

      <AttendanceReports classOptions={classOptions} isDirector={isDirector} />
    </div>
  );
}

function AttendanceReports({ classOptions, isDirector }: { classOptions: [string, string][]; isDirector: boolean }) {
  const [error, setError] = useState<string | null>(null);

  const [absentDate, setAbsentDate] = useState(today());
  const [absentClassId, setAbsentClassId] = useState('');
  const [absentStudents, setAbsentStudents] = useState<AbsentStudent[]>([]);
  const [loadingAbsent, setLoadingAbsent] = useState(false);

  const now = new Date();
  const [sheetClassId, setSheetClassId] = useState(classOptions[0]?.[0] ?? '');
  const [sheetYear, setSheetYear] = useState(now.getFullYear());
  const [sheetMonth, setSheetMonth] = useState(now.getMonth() + 1);
  const [monthlyRows, setMonthlyRows] = useState<MonthlyAttendanceRow[]>([]);
  const [loadingSheet, setLoadingSheet] = useState(false);

  const [rangeStart, setRangeStart] = useState(today());
  const [rangeEnd, setRangeEnd] = useState(today());
  const [batchSummaries, setBatchSummaries] = useState<BatchAttendanceSummary[]>([]);
  const [loadingBatches, setLoadingBatches] = useState(false);

  useEffect(() => {
    if (!sheetClassId && classOptions.length > 0) setSheetClassId(classOptions[0][0]);
  }, [classOptions, sheetClassId]);

  useEffect(() => {
    setLoadingAbsent(true);
    fetchAbsentReport(absentDate, absentClassId || undefined)
      .then(setAbsentStudents)
      .catch(() => setError('Impossible de charger le rapport des absences.'))
      .finally(() => setLoadingAbsent(false));
  }, [absentDate, absentClassId]);

  useEffect(() => {
    if (!sheetClassId) return;
    setLoadingSheet(true);
    fetchMonthlyAttendance(sheetClassId, sheetYear, sheetMonth)
      .then(setMonthlyRows)
      .catch(() => setError('Impossible de charger la feuille mensuelle.'))
      .finally(() => setLoadingSheet(false));
  }, [sheetClassId, sheetYear, sheetMonth]);

  useEffect(() => {
    if (!isDirector) return;
    setLoadingBatches(true);
    fetchBatchAttendanceSummary(rangeStart, rangeEnd)
      .then(setBatchSummaries)
      .catch(() => setError('Impossible de charger le résumé par lot.'))
      .finally(() => setLoadingBatches(false));
  }, [isDirector, rangeStart, rangeEnd]);

  const dayCount = daysInMonth(sheetYear, sheetMonth);
  const dayColumns = Array.from({ length: dayCount }, (_, i) => i + 1);

  return (
    <div className="mt-6 flex flex-col gap-6">
      {error && (
        <div className="rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-slate">Absents &amp; retards du jour</h2>
          <div className="flex flex-wrap items-center gap-3">
            <input type="date" value={absentDate} onChange={(e) => setAbsentDate(e.target.value)} className={inputClass} />
            <select value={absentClassId} onChange={(e) => setAbsentClassId(e.target.value)} className={inputClass}>
              <option value="">Toutes les classes</option>
              {classOptions.map(([id, name]) => (
                <option key={id} value={id}>{name}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="mt-4 flex flex-col gap-2">
          {!loadingAbsent && absentStudents.length === 0 && (
            <p className="text-sm text-slate-soft">Aucun absent ni retard ce jour-là.</p>
          )}
          {absentStudents.map((s) => (
            <div key={s.studentId} className="flex items-center justify-between rounded-xl border border-border px-3.5 py-2.5">
              <div>
                <p className="text-sm font-medium text-slate">{s.studentFullName}</p>
                <p className="text-xs text-slate-soft">{s.className}{s.comment ? ` · ${s.comment}` : ''}</p>
              </div>
              <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${STATUS_BADGE[s.status]}`}>
                {STATUS_LABELS[s.status]}
              </span>
            </div>
          ))}
        </div>
      </div>

      <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-slate">Feuille de présence mensuelle</h2>
          <div className="flex flex-wrap items-center gap-3">
            <select value={sheetClassId} onChange={(e) => setSheetClassId(e.target.value)} className={inputClass}>
              {classOptions.map(([id, name]) => (
                <option key={id} value={id}>{name}</option>
              ))}
            </select>
            <select value={sheetMonth} onChange={(e) => setSheetMonth(Number(e.target.value))} className={inputClass}>
              {MONTH_NAMES.map((name, i) => (
                <option key={name} value={i + 1}>{name}</option>
              ))}
            </select>
            <input
              type="number"
              value={sheetYear}
              onChange={(e) => setSheetYear(Number(e.target.value))}
              className={`${inputClass} w-24`}
            />
          </div>
        </div>

        {!loadingSheet && monthlyRows.length === 0 && (
          <p className="mt-4 text-sm text-slate-soft">Aucun élève dans cette classe.</p>
        )}

        {monthlyRows.length > 0 && (
          <div className="mt-4 overflow-x-auto">
            <table className="text-left text-xs">
              <thead>
                <tr>
                  <th className="sticky left-0 bg-surface px-2 py-2 font-medium text-slate-soft">Élève</th>
                  {dayColumns.map((day) => (
                    <th key={day} className="px-1.5 py-2 text-center font-medium text-slate-soft">{day}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {monthlyRows.map((row) => (
                  <tr key={row.studentId} className="border-t border-border">
                    <td className="sticky left-0 whitespace-nowrap bg-surface px-2 py-1.5 font-medium text-slate">
                      {row.studentFullName}
                    </td>
                    {dayColumns.map((day) => {
                      const status = row.dayStatuses[String(day)];
                      return (
                        <td key={day} className="px-1.5 py-1.5 text-center">
                          {status ? (
                            <span className={`inline-flex h-5 w-5 items-center justify-center rounded ${STATUS_BADGE[status]}`}>
                              {STATUS_LETTER[status]}
                            </span>
                          ) : (
                            <span className="text-slate-soft">·</span>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {isDirector && (
        <div className="rounded-2xl border border-border bg-surface p-6 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-base font-semibold text-slate">Résumé de présence par lot</h2>
            <div className="flex flex-wrap items-center gap-3">
              <input type="date" value={rangeStart} onChange={(e) => setRangeStart(e.target.value)} className={inputClass} />
              <span className="text-sm text-slate-soft">→</span>
              <input type="date" value={rangeEnd} onChange={(e) => setRangeEnd(e.target.value)} className={inputClass} />
            </div>
          </div>

          {!loadingBatches && batchSummaries.length === 0 && (
            <p className="mt-4 text-sm text-slate-soft">Aucune donnée pour cette période.</p>
          )}

          <div className="mt-4 flex flex-col gap-5">
            {batchSummaries.map((batch) => (
              <div key={batch.batchId}>
                <h3 className="text-sm font-semibold text-slate">{batch.batchName}</h3>
                <div className="mt-2 overflow-x-auto rounded-xl border border-border">
                  <table className="w-full text-left text-sm">
                    <thead>
                      <tr className="text-xs uppercase tracking-wide text-slate-soft">
                        <th className="px-4 py-2 font-medium">Élève</th>
                        <th className="px-4 py-2 font-medium">Présent</th>
                        <th className="px-4 py-2 font-medium">Absent</th>
                        <th className="px-4 py-2 font-medium">Retard</th>
                        <th className="px-4 py-2 font-medium">Excusé</th>
                      </tr>
                    </thead>
                    <tbody>
                      {batch.students.map((s) => (
                        <tr key={s.studentId} className="border-t border-border">
                          <td className="px-4 py-2 font-medium text-slate">{s.studentFullName}</td>
                          <td className="px-4 py-2 text-slate-soft">{s.presentCount}</td>
                          <td className="px-4 py-2 text-slate-soft">{s.absentCount}</td>
                          <td className="px-4 py-2 text-slate-soft">{s.retardCount}</td>
                          <td className="px-4 py-2 text-slate-soft">{s.excuseCount}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
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
