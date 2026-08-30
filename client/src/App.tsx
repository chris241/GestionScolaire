import type { ReactNode } from 'react';
import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { AuthProvider } from './lib/AuthContext';
import { ProtectedRoute } from './lib/ProtectedRoute';
import { AppLayout } from './components/AppLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Students } from './pages/Students';
import { Payments } from './pages/Payments';
import { Grades } from './pages/Grades';
import { Settings } from './pages/Settings';
import { Schools } from './pages/Schools';
import { StudentGroups } from './pages/StudentGroups';
import { Admissions } from './pages/Admissions';
import { Programs } from './pages/Programs';
import { Courses } from './pages/Courses';
import { Schedule } from './pages/Schedule';
import { Attendance } from './pages/Attendance';
import { FinalGrades } from './pages/FinalGrades';
import { Fees } from './pages/Fees';
import { Teachers } from './pages/Teachers';
import { PublicAdmission } from './pages/PublicAdmission';
import { useAuth } from './lib/AuthContext';

// Le tableau de bord est réservé au Directeur (les endpoints stats/paiements globaux lui sont restreints côté API).
function HomeRoute() {
  const { user } = useAuth();
  if (user?.role === 'Parent' || user?.role === 'Teacher' || user?.role === 'Student') {
    return <Navigate to="/notes" replace />;
  }
  return <Dashboard />;
}

// Un Directeur sans aucune école ne peut rien faire d'autre que d'en créer une.
function RequireSchoolGuard({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();

  if (user?.role === 'Director' && (user.availableSchools?.length ?? 0) === 0 && location.pathname !== '/ecoles') {
    return <Navigate to="/ecoles" replace />;
  }

  return <>{children}</>;
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/candidature" element={<PublicAdmission />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <RequireSchoolGuard>
                  <AppLayout>
                    <Routes>
                      <Route path="/" element={<HomeRoute />} />
                      <Route path="/eleves" element={<Students />} />
                      <Route path="/notes" element={<Grades />} />
                      <Route path="/paiements" element={<Payments />} />
                      <Route path="/parametres" element={<Settings />} />
                      <Route path="/ecoles" element={<Schools />} />
                      <Route path="/groupes" element={<StudentGroups />} />
                      <Route path="/admissions" element={<Admissions />} />
                      <Route path="/programmes" element={<Programs />} />
                      <Route path="/cours" element={<Courses />} />
                      <Route path="/emploi-du-temps" element={<Schedule />} />
                      <Route path="/presences" element={<Attendance />} />
                      <Route path="/resultats" element={<FinalGrades />} />
                      <Route path="/frais" element={<Fees />} />
                      <Route path="/enseignants" element={<Teachers />} />
                    </Routes>
                  </AppLayout>
                </RequireSchoolGuard>
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
