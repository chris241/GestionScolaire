import { apiClient } from './client';
import type { Guardian, StudentGuardianLink } from '../types';

export async function fetchGuardians(): Promise<Guardian[]> {
  const { data } = await apiClient.get<Guardian[]>('/guardians');
  return data;
}

export async function createGuardian(request: {
  firstName: string;
  lastName: string;
  phone: string;
  email: string | null;
  occupation: string | null;
}): Promise<Guardian> {
  const { data } = await apiClient.post<Guardian>('/guardians', request);
  return data;
}

export async function deleteGuardian(id: string): Promise<void> {
  await apiClient.delete(`/guardians/${id}`);
}

export async function fetchStudentGuardians(studentId: string): Promise<StudentGuardianLink[]> {
  const { data } = await apiClient.get<StudentGuardianLink[]>(`/guardians/student/${studentId}`);
  return data;
}

export async function linkGuardianToStudent(
  guardianId: string,
  studentId: string,
  request: { relationship: string; isPrimaryContact: boolean }
): Promise<StudentGuardianLink> {
  const { data } = await apiClient.post<StudentGuardianLink>(`/guardians/${guardianId}/students/${studentId}`, request);
  return data;
}

export async function unlinkGuardianFromStudent(guardianId: string, studentId: string): Promise<void> {
  await apiClient.delete(`/guardians/${guardianId}/students/${studentId}`);
}
