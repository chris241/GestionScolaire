import { apiClient } from './client';
import type { Course, Topic } from '../types';

export async function fetchCourses(programId?: string): Promise<Course[]> {
  const { data } = await apiClient.get<Course[]>('/courses', { params: programId ? { programId } : undefined });
  return data;
}

export async function createCourse(request: {
  name: string;
  code: string | null;
  description: string | null;
  subjectId: string;
  programId: string;
}): Promise<Course> {
  const { data } = await apiClient.post<Course>('/courses', request);
  return data;
}

export async function deleteCourse(id: string): Promise<void> {
  await apiClient.delete(`/courses/${id}`);
}

export async function addTopic(
  courseId: string,
  request: { name: string; description: string | null; order: number }
): Promise<Topic> {
  const { data } = await apiClient.post<Topic>(`/courses/${courseId}/topics`, request);
  return data;
}

export async function deleteTopic(topicId: string): Promise<void> {
  await apiClient.delete(`/courses/topics/${topicId}`);
}
