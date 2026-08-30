import { apiClient } from './client';
import type { CourseEnrollment } from '../types';

export async function fetchCourseEnrollments(courseId?: string): Promise<CourseEnrollment[]> {
  const { data } = await apiClient.get<CourseEnrollment[]>('/courseenrollments', { params: courseId ? { courseId } : undefined });
  return data;
}

export async function fetchStudentCourseEnrollments(studentId: string): Promise<CourseEnrollment[]> {
  const { data } = await apiClient.get<CourseEnrollment[]>(`/courseenrollments/student/${studentId}`);
  return data;
}

export async function createCourseEnrollment(request: {
  studentId: string;
  courseId: string;
  academicYearId: string;
}): Promise<CourseEnrollment> {
  const { data } = await apiClient.post<CourseEnrollment>('/courseenrollments', request);
  return data;
}

export async function bulkEnrollInCourse(request: {
  studentIds: string[];
  courseId: string;
  academicYearId: string;
}): Promise<CourseEnrollment[]> {
  const { data } = await apiClient.post<CourseEnrollment[]>('/courseenrollments/bulk', request);
  return data;
}

export async function deleteCourseEnrollment(id: string): Promise<void> {
  await apiClient.delete(`/courseenrollments/${id}`);
}
