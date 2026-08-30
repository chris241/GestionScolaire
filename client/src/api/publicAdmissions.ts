import { apiClient } from './client';

const GENDER_VALUES: Record<'Masculin' | 'Feminin', number> = { Masculin: 1, Feminin: 2 };

export async function submitPublicApplication(request: {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: 'Masculin' | 'Feminin';
  email: string | null;
  phone: string | null;
  guardianName: string;
  guardianEmail: string | null;
  guardianPhone: string;
  levelAppliedFor: string;
  programId: string | null;
  admissionCampaignId: string | null;
}): Promise<{ id: string }> {
  const { data } = await apiClient.post('/studentapplicants/public', {
    ...request,
    gender: GENDER_VALUES[request.gender],
  });
  return data;
}
