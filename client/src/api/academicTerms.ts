import { apiClient } from './client';
import type { AcademicTerm } from '../types';

export async function fetchAcademicTerms(academicYearId?: string): Promise<AcademicTerm[]> {
  const { data } = await apiClient.get<AcademicTerm[]>('/academicterms', {
    params: { academicYearId },
  });
  return data;
}

export async function createAcademicTerm(request: {
  name: string;
  order: number;
  startDate: string;
  endDate: string;
  academicYearId: string;
}): Promise<AcademicTerm> {
  const { data } = await apiClient.post<AcademicTerm>('/academicterms', request);
  return data;
}

export async function updateAcademicTerm(
  id: string,
  request: { name: string; order: number; startDate: string; endDate: string }
): Promise<AcademicTerm> {
  const { data } = await apiClient.put<AcademicTerm>(`/academicterms/${id}`, request);
  return data;
}

export async function deleteAcademicTerm(id: string): Promise<void> {
  await apiClient.delete(`/academicterms/${id}`);
}
