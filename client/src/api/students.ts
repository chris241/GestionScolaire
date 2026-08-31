import { apiClient } from './client';
import type { Sibling, Student, StudentFeeCategory, StudentImportResult } from '../types';

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

export async function fetchStudentFeeCategories(studentId: string): Promise<StudentFeeCategory[]> {
  const { data } = await apiClient.get<StudentFeeCategory[]>(`/students/${studentId}/fee-categories`);
  return data;
}

export async function subscribeToFeeCategory(studentId: string, categoryId: string): Promise<void> {
  await apiClient.post(`/students/${studentId}/fee-categories/${categoryId}`);
}

export async function unsubscribeFromFeeCategory(studentId: string, categoryId: string): Promise<void> {
  await apiClient.delete(`/students/${studentId}/fee-categories/${categoryId}`);
}

export async function importStudents(file: File): Promise<StudentImportResult> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<StudentImportResult>('/students/import', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}
