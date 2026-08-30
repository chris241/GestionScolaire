import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import type { UserProfile } from '../types';
import { login as loginRequest, switchSchool as switchSchoolRequest } from '../api/auth';

interface AuthContextValue {
  user: UserProfile | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  switchSchool: (schoolId: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(): UserProfile | null {
  const raw = localStorage.getItem('user');
  if (!raw) return null;
  try {
    return JSON.parse(raw) as UserProfile;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(() => readStoredUser());
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    function handleUnauthorized() {
      setUser(null);
    }
    window.addEventListener('auth:unauthorized', handleUnauthorized);
    return () => window.removeEventListener('auth:unauthorized', handleUnauthorized);
  }, []);

  async function login(email: string, password: string) {
    setLoading(true);
    try {
      const response = await loginRequest(email, password);
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.user));
      setUser(response.user);
    } finally {
      setLoading(false);
    }
  }

  function logout() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setUser(null);
  }

  async function switchSchool(schoolId: string) {
    setLoading(true);
    try {
      const response = await switchSchoolRequest(schoolId);
      localStorage.setItem('accessToken', response.accessToken);
      localStorage.setItem('refreshToken', response.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.user));
      setUser(response.user);
      // Recharge la page : toutes les données déjà affichées (effectifs, listes...) ont été
      // chargées sous l'ancienne école active et doivent être re-fetchées avec le nouveau contexte.
      window.location.reload();
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, switchSchool }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth doit être utilisé dans un AuthProvider');
  return context;
}
