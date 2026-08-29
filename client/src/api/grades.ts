import { apiClient } from './client';
import type { Grade, StudentGeneralAverage } from '../types';

export interface CreateGradeRequest {
  studentId: string;
  subjectId: string;
  classId: string;
  score: number;
  maxScore: number;
  coefficient: number;
  type: number;
  term: string;
  comment?: string;
}

export async function fetchStudentGrades(studentId: string): Promise<Grade[]> {
  const { data } = await apiClient.get<Grade[]>(`/grades/student/${studentId}`);
  return data;
}

export async function fetchStudentAverage(studentId: string): Promise<StudentGeneralAverage> {
  const { data } = await apiClient.get<StudentGeneralAverage>(`/grades/student/${studentId}/average`);
  return data;
}

export async function createGrade(request: CreateGradeRequest): Promise<Grade> {
  const { data } = await apiClient.post<Grade>('/grades', request);
  return data;
}

export async function downloadBulletin(studentId: string, term: string) {
  const response = await apiClient.get(`/bulletins/student/${studentId}`, {
    params: { term },
    responseType: 'blob',
  });

  const url = window.URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = `bulletin_${studentId}.pdf`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export async function downloadClassBulletins(classId: string, term: string) {
  const response = await apiClient.get(`/bulletins/class/${classId}`, {
    params: { term },
    responseType: 'blob',
  });

  const url = window.URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = `bulletins_${classId}.zip`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
