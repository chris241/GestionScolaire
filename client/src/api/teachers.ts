import { apiClient } from './client';
import type { Teacher } from '../types';

export async function fetchTeachers(): Promise<Teacher[]> {
  const { data } = await apiClient.get<Teacher[]>('/teachers');
  return data;
}

export async function createTeacher(request: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  specialty: string;
  hireDate: string;
}): Promise<Teacher> {
  const { data } = await apiClient.post<Teacher>('/teachers', request);
  return data;
}

export async function linkTeacherToSchool(teacherId: string, schoolId: string): Promise<Teacher> {
  const { data } = await apiClient.post<Teacher>(`/teachers/${teacherId}/schools/${schoolId}`);
  return data;
}

export async function unlinkTeacherFromSchool(teacherId: string, schoolId: string): Promise<void> {
  await apiClient.delete(`/teachers/${teacherId}/schools/${schoolId}`);
}
