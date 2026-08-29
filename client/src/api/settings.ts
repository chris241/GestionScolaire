import { apiClient } from './client';
import type { EducationSettings } from '../types';

export async function fetchEducationSettings(): Promise<EducationSettings> {
  const { data } = await apiClient.get<EducationSettings>('/educationsettings');
  return data;
}

export async function updateEducationSettings(request: {
  schoolName: string;
  address: string | null;
  currency: string;
  defaultMaxScore: number;
}): Promise<EducationSettings> {
  const { data } = await apiClient.put<EducationSettings>('/educationsettings', request);
  return data;
}
