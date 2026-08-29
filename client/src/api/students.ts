import { apiClient } from './client';
import type { Sibling, Student } from '../types';

export async function fetchStudents(classId?: string): Promise<Student[]> {
  const { data } = await apiClient.get<Student[]>('/students', { params: { classId } });
  return data;
}

export async function fetchSiblings(studentId: string): Promise<Sibling[]> {
  const { data } = await apiClient.get<Sibling[]>(`/students/${studentId}/siblings`);
  return data;
}

export async function addSibling(studentId: string, siblingStudentId: string): Promise<void> {
  await apiClient.post(`/students/${studentId}/siblings/${siblingStudentId}`);
}

export async function removeSibling(studentId: string, siblingStudentId: string): Promise<void> {
  await apiClient.delete(`/students/${studentId}/siblings/${siblingStudentId}`);
}
