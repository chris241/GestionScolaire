import { apiClient } from './client';
import type { StudentApplicant, AdmissionStatus } from '../types';

// L'API attend la valeur numérique sous-jacente des enums Gender/AdmissionStatus (pas de JsonStringEnumConverter côté serveur).
const GENDER_VALUES: Record<'Masculin' | 'Feminin', number> = { Masculin: 1, Feminin: 2 };
const ADMISSION_STATUS_VALUES: Record<AdmissionStatus, number> = {
  Submitted: 1,
  UnderReview: 2,
  Accepted: 3,
  Rejected: 4,
  Enrolled: 5,
};

export async function fetchApplicants(status?: AdmissionStatus): Promise<StudentApplicant[]> {
  const { data } = await apiClient.get<StudentApplicant[]>('/studentapplicants', { params: { status } });
  return data;
}

export async function createApplicant(request: {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: 'Masculin' | 'Feminin';
  email: string | null;
  phone: string | null;
  guardianName: string | null;
  guardianEmail: string | null;
  guardianPhone: string | null;
  levelAppliedFor: string;
  academicYearId: string;
  programId: string | null;
  admissionCampaignId: string | null;
}): Promise<StudentApplicant> {
  const { data } = await apiClient.post<StudentApplicant>('/studentapplicants', {
    ...request,
    gender: GENDER_VALUES[request.gender],
  });
  return data;
}

export async function updateApplicantStatus(
  id: string,
  status: AdmissionStatus,
  decisionNotes: string | null
): Promise<StudentApplicant> {
  const { data } = await apiClient.put<StudentApplicant>(`/studentapplicants/${id}/status`, {
    status: ADMISSION_STATUS_VALUES[status],
    decisionNotes,
  });
  return data;
}

export async function acceptApplicant(id: string, classId: string): Promise<StudentApplicant> {
  const { data } = await apiClient.post<StudentApplicant>(`/studentapplicants/${id}/accept`, { classId, enrollmentNumber: null });
  return data;
}

export async function rejectApplicant(id: string, notes: string | null): Promise<StudentApplicant> {
  const { data } = await apiClient.post<StudentApplicant>(`/studentapplicants/${id}/reject`, notes);
  return data;
}
