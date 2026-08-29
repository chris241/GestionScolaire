import { apiClient } from './client';
import type { AcademicYear } from '../types';

export async function fetchAcademicYears(): Promise<AcademicYear[]> {
  const { data } = await apiClient.get<AcademicYear[]>('/academicyears');
  return data;
}

export async function fetchCurrentAcademicYear(): Promise<AcademicYear> {
  const { data } = await apiClient.get<AcademicYear>('/academicyears/current');
  return data;
}

export async function createAcademicYear(request: {
  name: string;
  startDate: string;
  endDate: string;
}): Promise<AcademicYear> {
  const { data } = await apiClient.post<AcademicYear>('/academicyears', request);
  return data;
}

export async function updateAcademicYear(
  id: string,
  request: { name: string; startDate: string; endDate: string }
): Promise<AcademicYear> {
  const { data } = await apiClient.put<AcademicYear>(`/academicyears/${id}`, request);
  return data;
}

export async function setCurrentAcademicYear(id: string): Promise<void> {
  await apiClient.post(`/academicyears/${id}/set-current`);
}
