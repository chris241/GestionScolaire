import { apiClient } from './client';
import type { ProgramEnrollment } from '../types';

export async function fetchProgramEnrollments(programId?: string): Promise<ProgramEnrollment[]> {
  const { data } = await apiClient.get<ProgramEnrollment[]>('/programenrollments', { params: programId ? { programId } : undefined });
  return data;
}

export async function bulkEnrollStudents(request: {
  studentIds: string[];
  programId: string;
  academicYearId: string;
}): Promise<ProgramEnrollment[]> {
  const { data } = await apiClient.post<ProgramEnrollment[]>('/programenrollments/bulk', request);
  return data;
}

export async function deleteProgramEnrollment(id: string): Promise<void> {
  await apiClient.delete(`/programenrollments/${id}`);
}
