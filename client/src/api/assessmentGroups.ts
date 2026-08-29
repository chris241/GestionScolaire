import { apiClient } from './client';
import type { AssessmentGroup } from '../types';

export async function fetchAssessmentGroups(academicTermId?: string): Promise<AssessmentGroup[]> {
  const { data } = await apiClient.get<AssessmentGroup[]>('/assessmentgroups', { params: academicTermId ? { academicTermId } : undefined });
  return data;
}

export async function createAssessmentGroup(request: {
  name: string;
  weightage: number;
  academicTermId: string;
}): Promise<AssessmentGroup> {
  const { data } = await apiClient.post<AssessmentGroup>('/assessmentgroups', request);
  return data;
}

export async function deleteAssessmentGroup(id: string): Promise<void> {
  await apiClient.delete(`/assessmentgroups/${id}`);
}
