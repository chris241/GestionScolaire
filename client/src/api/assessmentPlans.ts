import { apiClient } from './client';
import type { AssessmentPlan } from '../types';

export async function fetchAssessmentPlans(params?: { classId?: string; academicTermId?: string }): Promise<AssessmentPlan[]> {
  const { data } = await apiClient.get<AssessmentPlan[]>('/assessmentplans', { params });
  return data;
}

export async function createAssessmentPlan(request: {
  name: string;
  maxScore: number;
  plannedDate: string;
  courseId: string;
  classId: string;
  academicTermId: string;
  assessmentGroupId: string;
  gradingScaleId: string | null;
}): Promise<AssessmentPlan> {
  const { data } = await apiClient.post<AssessmentPlan>('/assessmentplans', request);
  return data;
}

export async function deleteAssessmentPlan(id: string): Promise<void> {
  await apiClient.delete(`/assessmentplans/${id}`);
}

export async function addAssessmentCriteria(
  planId: string,
  request: { name: string; maxScore: number }
): Promise<void> {
  await apiClient.post(`/assessmentplans/${planId}/criteria`, request);
}
