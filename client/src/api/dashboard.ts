import { apiClient } from './client';
import type { DashboardStats, RecentActivity } from '../types';

export async function fetchDashboardStats(): Promise<DashboardStats> {
  const { data } = await apiClient.get<DashboardStats>('/dashboard/stats');
  return data;
}

export async function fetchRecentActivity(take = 8): Promise<RecentActivity[]> {
  const { data } = await apiClient.get<RecentActivity[]>('/dashboard/recent-activity', {
    params: { take },
  });
  return data;
}
