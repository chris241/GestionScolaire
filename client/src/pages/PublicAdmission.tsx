import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { GraduationCap } from 'lucide-react';
import { submitPublicApplication } from '../api/publicAdmissions';

const inputClass =
  'rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

const emptyForm = {
  firstName: '',
  lastName: '',
  dateOfBirth: '',
  gender: 'Masculin' as 'Masculin' | 'Feminin',
  email: '',
  phone: '',
  guardianName: '',
  guardianEmail: '',
  guardianPhone: '',
  levelAppliedFor: '',
};

export function PublicAdmission() {
  const [form, setForm] = useState(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await submitPublicApplication({
        firstName: form.firstName,
        lastName: form.lastName,
        dateOfBirth: form.dateOfBirth,
        gender: form.gender,
        email: form.email || null,
        phone: form.phone || null,
        guardianName: form.guardianName,
        guardianEmail: form.guardianEmail || null,
        guardianPhone: form.guardianPhone,
        levelAppliedFor: form.levelAppliedFor,
      });
      setSubmitted(true);
    } catch {
      setError("Impossible d'envoyer la candidature. Vérifiez les champs et réessayez dans quelques minutes.");
    } finally {
      setSubmitting(false);
    }
  }

  if (submitted) {
    return (
      <div className="flex min-h-svh items-center justify-center bg-bg px-4">
        <div className="w-full max-w-sm rounded-2xl border border-border bg-surface p-8 text-center shadow-sm">
          <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-success-soft text-success">
            <GraduationCap size={24} strokeWidth={2} />
          </span>
          <h1 className="mt-4 text-xl font-semibold text-slate">Candidature envoyée</h1>
          <p className="mt-2 text-sm text-slate-soft">
            Votre demande a bien été transmise à l'établissement. L'équipe vous recontactera prochainement.
          </p>
          <Link to="/login" className="mt-6 inline-block text-sm font-medium text-primary hover:text-primary-hover">
            Retour à la connexion
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-svh items-center justify-center bg-bg px-4 py-10">
      <div className="w-full max-w-lg rounded-2xl border border-border bg-surface p-8 shadow-sm">
        <div className="flex flex-col items-center gap-2">
          <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-soft text-primary">
            <GraduationCap size={24} strokeWidth={2} />
          </span>
          <h1 className="text-xl font-semibold text-slate">Candidature d'admission</h1>
          <p className="text-center text-sm text-slate-soft">
            Remplissez ce formulaire pour déposer une candidature. L'établissement vous contactera pour la suite du processus.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
          <div>
            <h2 className="text-sm font-semibold text-slate">Élève</h2>
            <div className="mt-3 grid grid-cols-2 gap-3">
              <input required placeholder="Prénom" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} className={inputClass} />
              <input required placeholder="Nom" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} className={inputClass} />
              <input required type="date" value={form.dateOfBirth} onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })} className={inputClass} />
              <select value={form.gender} onChange={(e) => setForm({ ...form, gender: e.target.value as 'Masculin' | 'Feminin' })} className={inputClass}>
                <option value="Masculin">Masculin</option>
                <option value="Feminin">Féminin</option>
              </select>
              <input required placeholder="Niveau demandé (ex: 6ème)" value={form.levelAppliedFor} onChange={(e) => setForm({ ...form, levelAppliedFor: e.target.value })} className={`${inputClass} col-span-2`} />
              <input type="email" placeholder="Email de l'élève (optionnel)" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className={`${inputClass} col-span-2`} />
              <input placeholder="Téléphone de l'élève (optionnel)" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} className={`${inputClass} col-span-2`} />
            </div>
          </div>

          <div className="border-t border-border pt-4">
            <h2 className="text-sm font-semibold text-slate">Tuteur / responsable légal</h2>
            <div className="mt-3 grid grid-cols-2 gap-3">
              <input required placeholder="Nom du tuteur" value={form.guardianName} onChange={(e) => setForm({ ...form, guardianName: e.target.value })} className={`${inputClass} col-span-2`} />
              <input required placeholder="Téléphone du tuteur" value={form.guardianPhone} onChange={(e) => setForm({ ...form, guardianPhone: e.target.value })} className={inputClass} />
              <input type="email" placeholder="Email du tuteur (optionnel)" value={form.guardianEmail} onChange={(e) => setForm({ ...form, guardianEmail: e.target.value })} className={inputClass} />
            </div>
          </div>

          {error && (
            <div className="rounded-xl border border-danger/20 bg-danger-soft px-3.5 py-2.5 text-sm text-danger">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="mt-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {submitting ? 'Envoi...' : 'Envoyer la candidature'}
          </button>

          <Link to="/login" className="text-center text-sm text-slate-soft hover:text-slate">
            Vous avez déjà un compte ? Se connecter
          </Link>
        </form>
      </div>
    </div>
  );
}
