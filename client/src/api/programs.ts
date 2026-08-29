import { apiClient } from './client';
import type { Program } from '../types';

export async function fetchPrograms(): Promise<Program[]> {
  const { data } = await apiClient.get<Program[]>('/programs');
  return data;
}

export async function createProgram(request: { name: string; code: string; description: string | null }): Promise<Program> {
  const { data } = await apiClient.post<Program>('/programs', request);
  return data;
}

export async function updateProgram(
  id: string,
  request: { name: string; code: string; description: string | null; isActive: boolean }
): Promise<Program> {
  const { data } = await apiClient.put<Program>(`/programs/${id}`, request);
  return data;
}

export async function deleteProgram(id: string): Promise<void> {
  await apiClient.delete(`/programs/${id}`);
}
