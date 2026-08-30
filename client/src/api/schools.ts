import { apiClient } from './client';
import type { School } from '../types';

export async function fetchSchools(): Promise<School[]> {
  const { data } = await apiClient.get<School[]>('/schools');
  return data;
}

export async function createSchool(request: {
  name: string;
  address: string | null;
  currency: string;
  defaultMaxScore: number;
}): Promise<School> {
  const { data } = await apiClient.post<School>('/schools', request);
  return data;
}

export async function updateSchool(id: string, request: {
  name: string;
  address: string | null;
  currency: string;
  defaultMaxScore: number;
}): Promise<School> {
  const { data } = await apiClient.put<School>(`/schools/${id}`, request);
  return data;
}
