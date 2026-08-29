import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './lib/AuthContext';
import { ProtectedRoute } from './lib/ProtectedRoute';
import { AppLayout } from './components/AppLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Students } from './pages/Students';
import { Payments } from './pages/Payments';
import { Grades } from './pages/Grades';
import { Settings } from './pages/Settings';
import { StudentGroups } from './pages/StudentGroups';
import { Admissions } from './pages/Admissions';
import { Programs } from './pages/Programs';
import { Courses } from './pages/Courses';
import { Schedule } from './pages/Schedule';
import { Attendance } from './pages/Attendance';
import { FinalGrades } from './pages/FinalGrades';
import { Fees } from './pages/Fees';
import { useAuth } from './lib/AuthContext';

// Le tableau de bord est réservé au Directeur (les endpoints stats/paiements globaux lui sont restreints côté API).
function HomeRoute() {
  const { user } = useAuth();
  if (user?.role === 'Parent' || user?.role === 'Teacher' || user?.role === 'Student') {
    return <Navigate to="/notes" replace />;
  }
  return <Dashboard />;
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <Routes>
                    <Route path="/" element={<HomeRoute />} />
                    <Route path="/eleves" element={<Students />} />
                    <Route path="/notes" element={<Grades />} />
                    <Route path="/paiements" element={<Payments />} />
                    <Route path="/parametres" element={<Settings />} />
                    <Route path="/groupes" element={<StudentGroups />} />
                    <Route path="/admissions" element={<Admissions />} />
                    <Route path="/programmes" element={<Programs />} />
                    <Route path="/cours" element={<Courses />} />
                    <Route path="/emploi-du-temps" element={<Schedule />} />
                    <Route path="/presences" element={<Attendance />} />
                    <Route path="/resultats" element={<FinalGrades />} />
                    <Route path="/frais" element={<Fees />} />
                  </Routes>
                </AppLayout>
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
