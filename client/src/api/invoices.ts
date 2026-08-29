import { apiClient } from './client';
import type { Invoice } from '../types';

export async function fetchInvoices(take = 50): Promise<Invoice[]> {
  const { data } = await apiClient.get<Invoice[]>('/invoices', { params: { take } });
  return data;
}

export async function fetchStudentInvoices(studentId: string): Promise<Invoice[]> {
  const { data } = await apiClient.get<Invoice[]>(`/invoices/student/${studentId}`);
  return data;
}
