import { apiClient } from './client';
import type { Teacher } from '../types';

export async function fetchTeachers(): Promise<Teacher[]> {
  const { data } = await apiClient.get<Teacher[]>('/teachers');
  return data;
}
