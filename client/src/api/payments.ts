import { apiClient } from './client';
import type { Payment } from '../types';

export async function fetchPayments(take = 50): Promise<Payment[]> {
  const { data } = await apiClient.get<Payment[]>('/payments', { params: { take } });
  return data;
}

export async function fetchPaymentsByStudent(studentId: string): Promise<Payment[]> {
  const { data } = await apiClient.get<Payment[]>(`/payments/student/${studentId}`);
  return data;
}
