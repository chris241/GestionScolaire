import { Fragment, useEffect, useState, type FormEvent } from 'react';
import { fetchApplicants, createApplicant, updateApplicantStatus, acceptApplicant, rejectApplicant } from '../api/admissions';
import { fetchAdmissionCampaigns, createAdmissionCampaign, deleteAdmissionCampaign, setAdmissionCampaignQuota, deleteAdmissionCampaignQuota } from '../api/admissionCampaigns';
import { fetchAcademicYears } from '../api/academicYears';
import { fetchStudents } from '../api/students';
import { fetchPrograms } from '../api/programs';
import type { StudentApplicant, AcademicYear, Student, AdmissionCampaign, Program } from '../types';
import { StatusBadge } from '../components/StatusBadge';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

function formatDate(date: string | null) {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function Admissions() {
  const [applicants, setApplicants] = useState<StudentApplicant[]>([]);
  const [years, setYears] = useState<AcademicYear[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [programs, setPrograms] = useState<Program[]>([]);
  const [campaigns, setCampaigns] = useState<AdmissionCampaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
    firstName: '', lastName: '', dateOfBirth: '', gender: 'Masculin' as 'Masculin' | 'Feminin',
    guardianName: '', guardianPhone: '', levelAppliedFor: '', programId: '', admissionCampaignId: '',
  });
  const [submitting, setSubmitting] = useState(false);

  const [acceptingId, setAcceptingId] = useState<string | null>(null);
  const [classId, setClassId] = useState('');

  const [campaignForm, setCampaignForm] = useState({ name: '', startDate: '', endDate: '' });
  const [savingCampaign, setSavingCampaign] = useState(false);
  const [quotaForm, setQuotaForm] = useState({ campaignId: '', programId: '', quota: '10' });
  const [savingQuota, setSavingQuota] = useState(false);

  const classOptions = Array.from(new Map(students.map((s) => [s.classId, s.className])).entries());

  useEffect(() => {
    Promise.all([fetchApplicants(), fetchAcademicYears(), fetchStudents(), fetchPrograms(), fetchAdmissionCampaigns()])
      .then(([applicantsData, yearsData, studentsData, programsData, campaignsData]) => {
        setApplicants(applicantsData);
        setYears(yearsData);
        setStudents(studentsData);
        setPrograms(programsData);
        setCampaigns(campaignsData);
      })
      .catch(() => setError('Impossible de charger les candidatures.'))
      .finally(() => setLoading(false));
  }, []);

  async function handleCreateCampaign(event: FormEvent) {
    event.preventDefault();
    const currentYear = years.find((y) => y.isCurrent) ?? years[0];
    if (!currentYear) return;

    setSavingCampaign(true);
    setError(null);
    try {
      const created = await createAdmissionCampaign({
        name: campaignForm.name,
        academicYearId: currentYear.id,
        startDate: campaignForm.startDate,
        endDate: campaignForm.endDate,
      });
      setCampaigns((prev) => [created, ...prev]);
      setCampaignForm({ name: '', startDate: '', endDate: '' });
    } catch {
      setError('Impossible de créer la campagne (vérifiez que la date de fin est après la date de début).');
    } finally {
      setSavingCampaign(false);
    }
  }

  async function handleDeleteCampaign(id: string) {
    setError(null);
    try {
      await deleteAdmissionCampaign(id);
      setCampaigns((prev) => prev.filter((c) => c.id !== id));
    } catch {
      setError('Impossible de supprimer cette campagne (des candidatures y sont peut-être rattachées).');
    }
  }

  async function handleSetQuota(event: FormEvent) {
    event.preventDefault();
    if (!quotaForm.campaignId || !quotaForm.programId) return;

    setSavingQuota(true);
    setError(null);
    try {
      const updated = await setAdmissionCampaignQuota(quotaForm.campaignId, {
        programId: quotaForm.programId,
        quota: Number(quotaForm.quota) || 1,
      });
      setCampaigns((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
      setQuotaForm({ campaignId: quotaForm.campaignId, programId: '', quota: '10' });
    } catch {
      setError('Impossible de définir ce quota.');
    } finally {
      setSavingQuota(false);
    }
  }

  async function handleDeleteQuota(campaignId: string, quotaId: string) {
    setError(null);
    try {
      await deleteAdmissionCampaignQuota(quotaId);
      setCampaigns((prev) =>
        prev.map((c) => (c.id === campaignId ? { ...c, quotas: c.quotas.filter((q) => q.id !== quotaId) } : c))
      );
    } catch {
      setError('Impossible de supprimer ce quota.');
    }
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    const currentYear = years.find((y) => y.isCurrent) ?? years[0];
    if (!currentYear) return;

    setSubmitting(true);
    setError(null);
    try {
      const created = await createApplicant({
        firstName: form.firstName,
        lastName: form.lastName,
        dateOfBirth: form.dateOfBirth,
        gender: form.gender,
        email: null,
        phone: null,
        guardianName: form.guardianName || null,
        guardianEmail: null,
        guardianPhone: form.guardianPhone || null,
        levelAppliedFor: form.levelAppliedFor,
        academicYearId: currentYear.id,
        programId: form.programId || null,
        admissionCampaignId: form.admissionCampaignId || null,
      });
      setApplicants((prev) => [created, ...prev]);
      setForm({ firstName: '', lastName: '', dateOfBirth: '', gender: 'Masculin', guardianName: '', guardianPhone: '', levelAppliedFor: '', programId: '', admissionCampaignId: '' });
    } catch {
      setError("Impossible de créer la candidature (la campagne sélectionnée n'est peut-être pas ouverte).");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleMarkUnderReview(id: string) {
    setError(null);
    try {
      const updated = await updateApplicantStatus(id, 'UnderReview', null);
      setApplicants((prev) => prev.map((a) => (a.id === id ? updated : a)));
    } catch {
      setError('Impossible de mettre à jour le statut.');
    }
  }

  async function handleAccept(event: FormEvent, id: string) {
    event.preventDefault();
    if (!classId) return;

    setError(null);
    try {
      const updated = await acceptApplicant(id, classId);
      setApplicants((prev) => prev.map((a) => (a.id === id ? updated : a)));
      setAcceptingId(null);
      setClassId('');
      const refreshedStudents = await fetchStudents();
      setStudents(refreshedStudents);
    } catch {
      setError("Impossible d'accepter cette candidature.");
    }
  }

  async function handleReject(id: string) {
    setError(null);
    try {
      const updated = await rejectApplicant(id, null);
      setApplicants((prev) => prev.map((a) => (a.id === id ? updated : a)));
    } catch {
      setError('Impossible de refuser cette candidature.');
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-6 py-8">
      <h1 className="text-2xl font-semibold text-slate">Admissions</h1>
      <p className="mt-1 text-sm text-slate-soft">
        {loading ? 'Chargement...' : `${applicants.length} candidature(s)`}
      </p>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 rounded-2xl border border-border bg-surface shadow-sm">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3 font-medium">Candidat</th>
              <th className="px-6 py-3 font-medium">Niveau demandé</th>
              <th className="px-6 py-3 font-medium">Campagne / Programme</th>
              <th className="px-6 py-3 font-medium">Tuteur</th>
              <th className="px-6 py-3 font-medium">Déposée le</th>
              <th className="px-6 py-3 font-medium">Statut</th>
              <th className="px-6 py-3 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {!loading && applicants.length === 0 && (
              <tr>
                <td colSpan={7} className="px-6 py-8 text-center text-slate-soft">
                  Aucune candidature.
                </td>
              </tr>
            )}
            {applicants.map((applicant) => (
              <Fragment key={applicant.id}>
                <tr className="border-t border-border">
                  <td className="px-6 py-4 font-medium text-slate">{applicant.firstName} {applicant.lastName}</td>
                  <td className="px-6 py-4 text-slate-soft">{applicant.levelAppliedFor}</td>
                  <td className="px-6 py-4 text-slate-soft">
                    {applicant.admissionCampaignName ?? applicant.programName
                      ? [applicant.admissionCampaignName, applicant.programName].filter(Boolean).join(' · ')
                      : '—'}
                  </td>
                  <td className="px-6 py-4 text-slate-soft">{applicant.guardianName ?? '—'}</td>
                  <td className="px-6 py-4 text-slate-soft">{formatDate(applicant.appliedDate)}</td>
                  <td className="px-6 py-4"><StatusBadge status={applicant.status} /></td>
                  <td className="px-6 py-4">
                    {applicant.status === 'Submitted' && (
                      <button
                        type="button"
                        onClick={() => handleMarkUnderReview(applicant.id)}
                        className="mr-3 text-xs font-medium text-primary hover:text-primary-hover"
                      >
                        Mettre en examen
                      </button>
                    )}
                    {(applicant.status === 'Submitted' || applicant.status === 'UnderReview') && (
                      <>
                        <button
                          type="button"
                          onClick={() => setAcceptingId(acceptingId === applicant.id ? null : applicant.id)}
                          className="mr-3 text-xs font-medium text-success hover:text-success"
                        >
                          Accepter
                        </button>
                        <button
                          type="button"
                          onClick={() => handleReject(applicant.id)}
                          className="text-xs font-medium text-danger hover:text-danger"
                        >
                          Refuser
                        </button>
                      </>
                    )}
                  </td>
                </tr>
                {acceptingId === applicant.id && (
                  <tr className="border-t border-border bg-bg">
                    <td colSpan={7} className="px-6 py-3">
                      <form onSubmit={(e) => handleAccept(e, applicant.id)} className="flex items-center gap-3">
                        <span className="text-sm text-slate-soft">Inscrire dans la classe :</span>
                        <select value={classId} onChange={(e) => setClassId(e.target.value)} className={inputClass}>
                          <option value="" disabled>Choisir une classe</option>
                          {classOptions.map(([id, name]) => (
                            <option key={id} value={id}>{name}</option>
                          ))}
                        </select>
                        <button
                          type="submit"
                          disabled={!classId}
                          className="rounded-xl bg-primary px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
                        >
                          Confirmer l'inscription
                        </button>
                      </form>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">Nouvelle candidature</h2>
        <form onSubmit={handleCreate} className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <input required placeholder="Prénom" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} className={inputClass} />
          <input required placeholder="Nom" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} className={inputClass} />
          <input required type="date" value={form.dateOfBirth} onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })} className={inputClass} />
          <select value={form.gender} onChange={(e) => setForm({ ...form, gender: e.target.value as 'Masculin' | 'Feminin' })} className={inputClass}>
            <option value="Masculin">Masculin</option>
            <option value="Feminin">Féminin</option>
          </select>
          <input required placeholder="Niveau demandé (ex: 6ème)" value={form.levelAppliedFor} onChange={(e) => setForm({ ...form, levelAppliedFor: e.target.value })} className={inputClass} />
          <input placeholder="Nom du tuteur" value={form.guardianName} onChange={(e) => setForm({ ...form, guardianName: e.target.value })} className={inputClass} />
          <input placeholder="Téléphone du tuteur" value={form.guardianPhone} onChange={(e) => setForm({ ...form, guardianPhone: e.target.value })} className={inputClass} />
          <select value={form.admissionCampaignId} onChange={(e) => setForm({ ...form, admissionCampaignId: e.target.value })} className={inputClass}>
            <option value="">Aucune campagne</option>
            {campaigns.filter((c) => c.isOpen).map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
          <select value={form.programId} onChange={(e) => setForm({ ...form, programId: e.target.value })} className={inputClass}>
            <option value="">Aucun programme</option>
            {programs.map((p) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
          <button
            type="submit"
            disabled={submitting}
            className="sm:col-span-2 mt-1 w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {submitting ? 'Création...' : 'Créer la candidature'}
          </button>
        </form>
      </div>

      <div className="mt-6 rounded-2xl border border-border bg-surface p-6 shadow-sm">
        <h2 className="text-base font-semibold text-slate">Campagnes d'admission</h2>
        <p className="mt-1 text-sm text-slate-soft">Fenêtres de candidature avec dates d'ouverture/fermeture et quotas par programme.</p>

        <div className="mt-4 flex flex-col gap-3">
          {campaigns.length === 0 && <p className="text-sm text-slate-soft">Aucune campagne créée.</p>}
          {campaigns.map((campaign) => (
            <div key={campaign.id} className="rounded-xl border border-border px-3.5 py-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-slate">{campaign.name}</p>
                  <p className="text-xs text-slate-soft">
                    {formatDate(campaign.startDate)} → {formatDate(campaign.endDate)} · {campaign.applicantCount} candidature(s)
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span
                    className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium ${
                      campaign.isOpen ? 'bg-success-soft text-success' : 'bg-border text-slate-soft'
                    }`}
                  >
                    {campaign.isOpen ? 'Ouverte' : 'Fermée'}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleDeleteCampaign(campaign.id)}
                    className="text-xs font-medium text-danger hover:text-danger"
                  >
                    Supprimer
                  </button>
                </div>
              </div>

              {campaign.quotas.length > 0 && (
                <div className="mt-2 flex flex-col gap-1">
                  {campaign.quotas.map((q) => (
                    <div key={q.id} className="flex items-center justify-between text-xs text-slate-soft">
                      <span>{q.programName} — {q.used}/{q.quota} utilisé(s), {q.remaining} restant(s)</span>
                      <button
                        type="button"
                        onClick={() => handleDeleteQuota(campaign.id, q.id)}
                        className="font-medium text-danger hover:text-danger"
                      >
                        Retirer
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>

        <form onSubmit={handleCreateCampaign} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
          <h3 className="text-sm font-semibold text-slate">Créer une campagne (année en cours)</h3>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <input required placeholder="Nom (ex: Rentrée 2027)" value={campaignForm.name} onChange={(e) => setCampaignForm({ ...campaignForm, name: e.target.value })} className={inputClass} />
            <input required type="date" value={campaignForm.startDate} onChange={(e) => setCampaignForm({ ...campaignForm, startDate: e.target.value })} className={inputClass} />
            <input required type="date" value={campaignForm.endDate} onChange={(e) => setCampaignForm({ ...campaignForm, endDate: e.target.value })} className={inputClass} />
          </div>
          <button
            type="submit"
            disabled={savingCampaign}
            className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {savingCampaign ? 'Création...' : 'Créer la campagne'}
          </button>
        </form>

        <form onSubmit={handleSetQuota} className="mt-4 flex flex-col gap-3 border-t border-border pt-4">
          <h3 className="text-sm font-semibold text-slate">Définir un quota</h3>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <select required value={quotaForm.campaignId} onChange={(e) => setQuotaForm({ ...quotaForm, campaignId: e.target.value })} className={inputClass}>
              <option value="" disabled>Campagne...</option>
              {campaigns.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            <select required value={quotaForm.programId} onChange={(e) => setQuotaForm({ ...quotaForm, programId: e.target.value })} className={inputClass}>
              <option value="" disabled>Programme...</option>
              {programs.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
            <input required type="number" min="1" placeholder="Places" value={quotaForm.quota} onChange={(e) => setQuotaForm({ ...quotaForm, quota: e.target.value })} className={inputClass} />
          </div>
          <button
            type="submit"
            disabled={savingQuota}
            className="w-fit rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {savingQuota ? 'Enregistrement...' : 'Définir le quota'}
          </button>
        </form>
      </div>
    </div>
  );
}
