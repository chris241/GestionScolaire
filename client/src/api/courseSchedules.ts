import { apiClient } from './client';
import type { AutoPlanScheduleResult, CommitAutoPlanResult, CourseSchedule, ProposedScheduleSlot, ScheduleRequirement } from '../types';

export async function fetchCourseSchedules(params?: { classId?: string; academicTermId?: string }): Promise<CourseSchedule[]> {
  const { data } = await apiClient.get<CourseSchedule[]>('/courseschedules', { params });
  return data;
}

export async function fetchStudentCourseSchedules(studentId: string): Promise<CourseSchedule[]> {
  const { data } = await apiClient.get<CourseSchedule[]>(`/courseschedules/student/${studentId}`);
  return data;
}

export async function createCourseSchedule(request: {
  courseId: string;
  roomId: string;
  teacherId: string;
  classId: string | null;
  academicTermId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}): Promise<CourseSchedule> {
  const { data } = await apiClient.post<CourseSchedule>('/courseschedules', request);
  return data;
}

export async function deleteCourseSchedule(id: string): Promise<void> {
  await apiClient.delete(`/courseschedules/${id}`);
}

export async function autoPlanSchedule(request: {
  classId: string;
  academicTermId: string;
  days: number[];
  dailyStartTime: string;
  periodsPerDay: number;
  periodDurationMinutes: number;
  requirements: ScheduleRequirement[];
}): Promise<AutoPlanScheduleResult> {
  const { data } = await apiClient.post<AutoPlanScheduleResult>('/courseschedules/auto-plan', request);
  return data;
}

export async function commitAutoPlan(slots: ProposedScheduleSlot[]): Promise<CommitAutoPlanResult> {
  const { data } = await apiClient.post<CommitAutoPlanResult>('/courseschedules/auto-plan/commit', { slots });
  return data;
}
