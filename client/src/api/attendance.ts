import { apiClient } from './client';
import type { AttendanceRecord, AttendanceStatus } from '../types';

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
