import { apiClient } from './client';
import type { CourseWiseAssessment, FinalGrade } from '../types';

export async function fetchFinalGradesByClass(classId: string, term: string): Promise<FinalGrade[]> {
  const { data } = await apiClient.get<FinalGrade[]>(`/finalgrades/class/${classId}`, { params: { term } });
  return data;
}

export async function fetchFinalGradeByStudent(studentId: string, term: string): Promise<FinalGrade> {
  const { data } = await apiClient.get<FinalGrade>(`/finalgrades/student/${studentId}`, { params: { term } });
  return data;
}

export async function fetchCourseWiseAssessment(classId: string, term: string): Promise<CourseWiseAssessment[]> {
  const { data } = await apiClient.get<CourseWiseAssessment[]>(`/finalgrades/class/${classId}/by-course`, { params: { term } });
  return data;
}
