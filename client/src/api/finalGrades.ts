import { apiClient } from './client';
import type { FinalGrade } from '../types';

export async function fetchFinalGradesByClass(classId: string, term: string): Promise<FinalGrade[]> {
  const { data } = await apiClient.get<FinalGrade[]>(`/finalgrades/class/${classId}`, { params: { term } });
  return data;
}

export async function fetchFinalGradeByStudent(studentId: string, term: string): Promise<FinalGrade> {
  const { data } = await apiClient.get<FinalGrade>(`/finalgrades/student/${studentId}`, { params: { term } });
  return data;
}
