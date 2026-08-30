import { apiClient } from './client';
import type { FeeStructure } from '../types';

export async function fetchFeeStructures(): Promise<FeeStructure[]> {
  const { data } = await apiClient.get<FeeStructure[]>('/feestructures');
  return data;
}

export async function createFeeStructure(request: {
  name: string;
  academicYearId: string;
  programId: string | null;
}): Promise<FeeStructure> {
  const { data } = await apiClient.post<FeeStructure>('/feestructures', request);
  return data;
}

export async function deleteFeeStructure(id: string): Promise<void> {
  await apiClient.delete(`/feestructures/${id}`);
}

export async function addFeeStructureItem(
  structureId: string,
  request: { feeCategoryId: string; amount: number }
): Promise<void> {
  await apiClient.post(`/feestructures/${structureId}/items`, request);
}

export async function deleteFeeStructureItem(itemId: string): Promise<void> {
  await apiClient.delete(`/feestructures/items/${itemId}`);
}

export async function addFeeSchedule(
  structureId: string,
  request: { academicTermId: string; dueDate: string }
): Promise<void> {
  await apiClient.post(`/feestructures/${structureId}/schedules`, request);
}

export async function deleteFeeSchedule(scheduleId: string): Promise<void> {
  await apiClient.delete(`/feestructures/schedules/${scheduleId}`);
}

export async function generateInvoices(scheduleId: string): Promise<{ created: number; alreadyExisted: number }> {
  const { data } = await apiClient.post<{ created: number; alreadyExisted: number }>(
    `/feestructures/schedules/${scheduleId}/generate-invoices`
  );
  return data;
}

export async function generateMonthlySchedules(
  structureId: string,
  request: { academicTermId: string; dueDayOfMonth: number }
): Promise<{ schedulesCreated: number; schedulesAlreadyExisted: number; invoicesCreated: number }> {
  const { data } = await apiClient.post<{ schedulesCreated: number; schedulesAlreadyExisted: number; invoicesCreated: number }>(
    `/feestructures/${structureId}/schedules/monthly`,
    request
  );
  return data;
}
