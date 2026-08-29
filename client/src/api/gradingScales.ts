import { apiClient } from './client';
import type { GradingScale } from '../types';

export async function fetchGradingScales(): Promise<GradingScale[]> {
  const { data } = await apiClient.get<GradingScale[]>('/gradingscales');
  return data;
}

export async function createGradingScale(request: { name: string; isDefault: boolean }): Promise<GradingScale> {
  const { data } = await apiClient.post<GradingScale>('/gradingscales', request);
  return data;
}

export async function addGradingScaleInterval(
  scaleId: string,
  request: { grade: string; minScore: number; maxScore: number }
): Promise<void> {
  await apiClient.post(`/gradingscales/${scaleId}/intervals`, request);
}

export async function deleteGradingScale(id: string): Promise<void> {
  await apiClient.delete(`/gradingscales/${id}`);
}
