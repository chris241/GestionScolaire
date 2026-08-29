import { apiClient } from './client';
import type { StudentCategory } from '../types';

export async function fetchStudentCategories(): Promise<StudentCategory[]> {
  const { data } = await apiClient.get<StudentCategory[]>('/studentcategories');
  return data;
}

export async function createStudentCategory(request: { name: string; description: string | null }): Promise<StudentCategory> {
  const { data } = await apiClient.post<StudentCategory>('/studentcategories', request);
  return data;
}

export async function deleteStudentCategory(id: string): Promise<void> {
  await apiClient.delete(`/studentcategories/${id}`);
}
