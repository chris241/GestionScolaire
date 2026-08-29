import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { GraduationCap } from 'lucide-react';
import { useAuth } from '../lib/AuthContext';

export function Login() {
  const { user, login, loading } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  if (user) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    try {
      await login(email, password);
      navigate('/', { replace: true });
    } catch {
      setError('Email ou mot de passe incorrect.');
    }
  }

  return (
    <div className="flex min-h-svh items-center justify-center bg-bg px-4">
      <div className="w-full max-w-sm rounded-2xl border border-border bg-surface p-8 shadow-sm">
        <div className="flex flex-col items-center gap-2">
          <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-soft text-primary">
            <GraduationCap size={24} strokeWidth={2} />
          </span>
          <h1 className="text-xl font-semibold text-slate">GestionScolaire</h1>
          <p className="text-sm text-slate-soft">Connectez-vous à votre espace</p>
        </div>

        <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="email" className="text-sm font-medium text-slate">Email</label>
            <input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="directeur@ecole.mg"
              className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="password" className="text-sm font-medium text-slate">Mot de passe</label>
            <input
              id="password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              className="rounded-xl border border-border bg-bg px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
            />
          </div>

          {error && (
            <div className="rounded-xl border border-danger/20 bg-danger-soft px-3.5 py-2.5 text-sm text-danger">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="mt-2 rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-primary-hover disabled:opacity-60"
          >
            {loading ? 'Connexion...' : 'Se connecter'}
          </button>

          <Link to="/candidature" className="text-center text-sm text-slate-soft hover:text-slate">
            Pas encore inscrit ? Déposer une candidature
          </Link>
        </form>
      </div>
    </div>
  );
}
