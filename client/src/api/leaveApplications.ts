import { apiClient } from './client';
import type { LeaveApplication } from '../types';

export async function fetchLeaveApplications(status?: string): Promise<LeaveApplication[]> {
  const { data } = await apiClient.get<LeaveApplication[]>('/leaveapplications', { params: status ? { status } : undefined });
  return data;
}

export async function fetchStudentLeaveApplications(studentId: string): Promise<LeaveApplication[]> {
  const { data } = await apiClient.get<LeaveApplication[]>(`/leaveapplications/student/${studentId}`);
  return data;
}

export async function createLeaveApplication(request: {
  studentId: string;
  startDate: string;
  endDate: string;
  reason: string;
}): Promise<LeaveApplication> {
  const { data } = await apiClient.post<LeaveApplication>('/leaveapplications', request);
  return data;
}

export async function decideLeaveApplication(
  id: string,
  request: { approve: boolean; decisionNotes: string | null }
): Promise<LeaveApplication> {
  const { data } = await apiClient.put<LeaveApplication>(`/leaveapplications/${id}/decide`, request);
  return data;
}
