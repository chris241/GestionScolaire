import { apiClient } from './client';
import type { FeeCategory } from '../types';

export async function fetchFeeCategories(): Promise<FeeCategory[]> {
  const { data } = await apiClient.get<FeeCategory[]>('/feecategories');
  return data;
}

export async function createFeeCategory(request: { name: string; description: string | null }): Promise<FeeCategory> {
  const { data } = await apiClient.post<FeeCategory>('/feecategories', request);
  return data;
}

export async function deleteFeeCategory(id: string): Promise<void> {
  await apiClient.delete(`/feecategories/${id}`);
}
