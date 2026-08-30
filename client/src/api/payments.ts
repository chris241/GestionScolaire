import { apiClient } from './client';
import type { Payment } from '../types';

export async function fetchPayments(take = 50, academicYear?: string): Promise<Payment[]> {
  const { data } = await apiClient.get<Payment[]>('/payments', { params: { take, academicYear } });
  return data;
}

export async function fetchPendingPayments(): Promise<Payment[]> {
  const { data } = await apiClient.get<Payment[]>('/payments/pending');
  return data;
}

export async function fetchPaymentsByStudent(studentId: string): Promise<Payment[]> {
  const { data } = await apiClient.get<Payment[]>(`/payments/student/${studentId}`);
  return data;
}

export async function createPayment(request: {
  studentId: string;
  description: string;
  amount: number;
  academicYear: string;
  term: string;
  method: string;
  invoiceId: string | null;
}): Promise<Payment> {
  const { data } = await apiClient.post<Payment>('/payments', request);
  return data;
}

export async function declarePayment(request: {
  studentId: string;
  description: string;
  amount: number;
  academicYear: string;
  term: string;
  method: string;
  invoiceId: string | null;
}): Promise<Payment> {
  const { data } = await apiClient.post<Payment>('/payments/declare', request);
  return data;
}

export async function validatePayment(id: string): Promise<Payment> {
  const { data } = await apiClient.put<Payment>(`/payments/${id}/validate`);
  return data;
}

export async function rejectPayment(id: string, decisionNotes: string | null): Promise<Payment> {
  const { data } = await apiClient.put<Payment>(`/payments/${id}/reject`, { decisionNotes });
  return data;
}
