import { apiClient } from './client';
import type { Student } from '../types';

export async function fetchStudents(classId?: string): Promise<Student[]> {
  const { data } = await apiClient.get<Student[]>('/students', { params: { classId } });
  return data;
}
