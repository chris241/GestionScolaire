import { apiClient } from './client';
import type { TeacherLog } from '../types';

export async function fetchTeacherLogs(teacherId: string): Promise<TeacherLog[]> {
  const { data } = await apiClient.get<TeacherLog[]>(`/teacherlogs/teacher/${teacherId}`);
  return data;
}

export async function createTeacherLog(request: {
  teacherId: string;
  logDate: string;
  logType: string;
  description: string;
}): Promise<TeacherLog> {
  const { data } = await apiClient.post<TeacherLog>('/teacherlogs', request);
  return data;
}

export async function deleteTeacherLog(id: string): Promise<void> {
  await apiClient.delete(`/teacherlogs/${id}`);
}
