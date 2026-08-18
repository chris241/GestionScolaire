import { useEffect, useState } from 'react';
import { fetchStudents } from '../api/students';
import type { Student } from '../types';
import { useAuth } from '../lib/AuthContext';

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function Students() {
  const { user } = useAuth();
  const isParent = user?.role === 'Parent';
  const isTeacher = user?.role === 'Teacher';
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');

  useEffect(() => {
    let cancelled = false;

    fetchStudents()
      .then((data) => !cancelled && setStudents(data))
      .catch(() => !cancelled && setError('Impossible de charger la liste des élèves.'))
      .finally(() => !cancelled && setLoading(false));

    return () => {
      cancelled = true;
    };
  }, []);

  const filtered = students.filter((s) =>
    `${s.firstName} ${s.lastName} ${s.enrollmentNumber} ${s.className}`
      .toLowerCase()
      .includes(search.toLowerCase())
  );

  return (
    <div className="mx-auto max-w-7xl px-6 py-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate">
            {isParent ? 'Mes enfants' : isTeacher ? 'Mes élèves' : 'Élèves'}
          </h1>
          <p className="mt-1 text-sm text-slate-soft">
            {isParent
              ? `${students.length} enfant(s) rattaché(s)`
              : isTeacher
                ? `${students.length} élève(s) dans ma classe`
                : `${students.length} élève(s) inscrit(s)`}
          </p>
        </div>
        {!isParent && (
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Rechercher un élève..."
            className="w-64 rounded-xl border border-border bg-surface px-3.5 py-2.5 text-sm text-slate outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
          />
        )}
      </div>

      {error && (
        <div className="mt-6 rounded-xl border border-danger/20 bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </div>
      )}

      <div className="mt-6 overflow-x-auto rounded-2xl border border-border bg-surface shadow-sm">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="text-xs uppercase tracking-wide text-slate-soft">
              <th className="px-6 py-3 font-medium">Matricule</th>
              <th className="px-6 py-3 font-medium">Nom complet</th>
              <th className="px-6 py-3 font-medium">Classe</th>
              <th className="px-6 py-3 font-medium">Date de naissance</th>
              <th className="px-6 py-3 font-medium">Statut</th>
            </tr>
          </thead>
          <tbody>
            {!loading && filtered.length === 0 && (
              <tr>
                <td colSpan={5} className="px-6 py-8 text-center text-slate-soft">
                  Aucun élève trouvé.
                </td>
              </tr>
            )}
            {filtered.map((student) => (
              <tr key={student.id} className="border-t border-border">
                <td className="px-6 py-4 text-slate-soft">{student.enrollmentNumber}</td>
                <td className="px-6 py-4 font-medium text-slate">{student.firstName} {student.lastName}</td>
                <td className="px-6 py-4 text-slate-soft">{student.className}</td>
                <td className="px-6 py-4 text-slate-soft">{formatDate(student.dateOfBirth)}</td>
                <td className="px-6 py-4">
                  <span
                    className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ${
                      student.isActive ? 'bg-success-soft text-success' : 'bg-border text-slate-soft'
                    }`}
                  >
                    {student.isActive ? 'Actif' : 'Inactif'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
