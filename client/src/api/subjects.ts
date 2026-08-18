import { apiClient } from './client';
import type { Subject } from '../types';

export async function fetchSubjects(): Promise<Subject[]> {
  const { data } = await apiClient.get<Subject[]>('/subjects');
  return data;
}
