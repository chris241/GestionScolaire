import { apiClient } from './client';
import type { StudentGroup, StudentGroupMember } from '../types';

export async function fetchStudentGroups(): Promise<StudentGroup[]> {
  const { data } = await apiClient.get<StudentGroup[]>('/studentgroups');
  return data;
}

export async function fetchStudentGroupMembers(groupId: string): Promise<StudentGroupMember[]> {
  const { data } = await apiClient.get<StudentGroupMember[]>(`/studentgroups/${groupId}/members`);
  return data;
}

export async function createStudentGroup(request: {
  name: string;
  groupType: string;
  maxSize: number | null;
  academicYearId: string;
  classId: string | null;
}): Promise<StudentGroup> {
  const { data } = await apiClient.post<StudentGroup>('/studentgroups', request);
  return data;
}

export async function addStudentGroupMembers(groupId: string, studentIds: string[]): Promise<StudentGroupMember[]> {
  const { data } = await apiClient.post<StudentGroupMember[]>(`/studentgroups/${groupId}/members`, { studentIds });
  return data;
}

export async function removeStudentGroupMember(groupId: string, studentId: string): Promise<void> {
  await apiClient.delete(`/studentgroups/${groupId}/members/${studentId}`);
}

export async function deleteStudentGroup(id: string): Promise<void> {
  await apiClient.delete(`/studentgroups/${id}`);
}
