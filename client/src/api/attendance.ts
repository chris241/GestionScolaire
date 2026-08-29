import { apiClient } from './client';
import type { AbsentStudent, AttendanceRecord, AttendanceStatus, BatchAttendanceSummary, MonthlyAttendanceRow } from '../types';

// L'API attend la valeur numérique sous-jacente de l'enum AttendanceStatus (pas de JsonStringEnumConverter côté serveur).
const STATUS_VALUES: Record<AttendanceStatus, number> = {
  Present: 1,
  Absent: 2,
  Retard: 3,
  Excuse: 4,
};

export async function fetchAttendanceByClass(classId: string, date: string): Promise<AttendanceRecord[]> {
  const { data } = await apiClient.get<AttendanceRecord[]>('/attendance', { params: { classId, date } });
  return data;
}

export async function fetchStudentAttendance(studentId: string): Promise<AttendanceRecord[]> {
  const { data } = await apiClient.get<AttendanceRecord[]>(`/attendance/student/${studentId}`);
  return data;
}

export async function bulkMarkAttendance(request: {
  classId: string;
  date: string;
  entries: { studentId: string; status: AttendanceStatus; comment: string | null }[];
}): Promise<AttendanceRecord[]> {
  const { data } = await apiClient.post<AttendanceRecord[]>('/attendance/bulk', {
    ...request,
    entries: request.entries.map((e) => ({ ...e, status: STATUS_VALUES[e.status] })),
  });
  return data;
}

export async function fetchAbsentReport(date: string, classId?: string): Promise<AbsentStudent[]> {
  const { data } = await apiClient.get<AbsentStudent[]>('/attendance/reports/absent', { params: { date, classId } });
  return data;
}

export async function fetchMonthlyAttendance(classId: string, year: number, month: number): Promise<MonthlyAttendanceRow[]> {
  const { data } = await apiClient.get<MonthlyAttendanceRow[]>('/attendance/reports/monthly', { params: { classId, year, month } });
  return data;
}

export async function fetchBatchAttendanceSummary(startDate: string, endDate: string): Promise<BatchAttendanceSummary[]> {
  const { data } = await apiClient.get<BatchAttendanceSummary[]>('/attendance/reports/batch-summary', { params: { startDate, endDate } });
  return data;
}
