import { apiClient } from './client';
import type { AdmissionCampaign, OpenAdmissionCampaign } from '../types';

export async function fetchAdmissionCampaigns(): Promise<AdmissionCampaign[]> {
  const { data } = await apiClient.get<AdmissionCampaign[]>('/admissioncampaigns');
  return data;
}

export async function fetchOpenAdmissionCampaigns(): Promise<OpenAdmissionCampaign[]> {
  const { data } = await apiClient.get<OpenAdmissionCampaign[]>('/admissioncampaigns/open');
  return data;
}

export async function createAdmissionCampaign(request: {
  name: string;
  academicYearId: string;
  startDate: string;
  endDate: string;
}): Promise<AdmissionCampaign> {
  const { data } = await apiClient.post<AdmissionCampaign>('/admissioncampaigns', request);
  return data;
}

export async function deleteAdmissionCampaign(id: string): Promise<void> {
  await apiClient.delete(`/admissioncampaigns/${id}`);
}

export async function setAdmissionCampaignQuota(
  campaignId: string,
  request: { programId: string; quota: number }
): Promise<AdmissionCampaign> {
  const { data } = await apiClient.post<AdmissionCampaign>(`/admissioncampaigns/${campaignId}/quotas`, request);
  return data;
}

export async function deleteAdmissionCampaignQuota(quotaId: string): Promise<void> {
  await apiClient.delete(`/admissioncampaigns/quotas/${quotaId}`);
}
