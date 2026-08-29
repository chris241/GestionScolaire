import { apiClient } from './client';
import type { StudentLog } from '../types';

export async function fetchStudentLogs(studentId: string): Promise<StudentLog[]> {
  const { data } = await apiClient.get<StudentLog[]>(`/studentlogs/student/${studentId}`);
  return data;
}

export async function createStudentLog(request: {
  studentId: string;
  logDate: string;
  logType: string;
  description: string;
}): Promise<StudentLog> {
  const { data } = await apiClient.post<StudentLog>('/studentlogs', request);
  return data;
}

export async function deleteStudentLog(id: string): Promise<void> {
  await apiClient.delete(`/studentlogs/${id}`);
}
