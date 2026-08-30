import { apiClient } from './client';
import type { Invoice, OverdueInvoice, ProgramFeeCollection, StudentFeeCollection } from '../types';

export async function fetchInvoices(take = 50): Promise<Invoice[]> {
  const { data } = await apiClient.get<Invoice[]>('/invoices', { params: { take } });
  return data;
}

export async function fetchStudentInvoices(studentId: string): Promise<Invoice[]> {
  const { data } = await apiClient.get<Invoice[]>(`/invoices/student/${studentId}`);
  return data;
}

export async function fetchStudentCollectionReport(classId?: string): Promise<StudentFeeCollection[]> {
  const { data } = await apiClient.get<StudentFeeCollection[]>('/invoices/reports/student-collection', { params: { classId } });
  return data;
}

export async function fetchProgramCollectionReport(): Promise<ProgramFeeCollection[]> {
  const { data } = await apiClient.get<ProgramFeeCollection[]>('/invoices/reports/program-collection');
  return data;
}

export async function fetchOverdueInvoices(): Promise<OverdueInvoice[]> {
  const { data } = await apiClient.get<OverdueInvoice[]>('/invoices/reports/overdue');
  return data;
}
