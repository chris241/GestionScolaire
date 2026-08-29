import { apiClient } from './client';
import type { StudentBatch } from '../types';

export async function fetchStudentBatches(): Promise<StudentBatch[]> {
  const { data } = await apiClient.get<StudentBatch[]>('/studentbatches');
  return data;
}

export async function createStudentBatch(request: {
  name: string;
  startDate: string;
  endDate: string | null;
  description: string | null;
  academicYearId: string;
}): Promise<StudentBatch> {
  const { data } = await apiClient.post<StudentBatch>('/studentbatches', request);
  return data;
}

export async function deleteStudentBatch(id: string): Promise<void> {
  await apiClient.delete(`/studentbatches/${id}`);
}
