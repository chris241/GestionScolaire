export type PaymentStatus = 'EnAttente' | 'Paye' | 'EnRetard' | 'Annule';

export interface DashboardStats {
  enrolledStudents: number;
  teachers: number;
  recoveryRate: number;
  todayAbsences: number;
}

export interface RecentActivity {
  id: string;
  studentFullName: string;
  type: string;
  description: string;
  amount: number | null;
  status: PaymentStatus | string;
  date: string;
}

export interface Payment {
  id: string;
  studentId: string;
  studentFullName: string;
  description: string;
  amount: number;
  dueDate: string;
  paidAt: string | null;
  status: PaymentStatus;
}

export interface Student {
  id: string;
  enrollmentNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: 'Masculin' | 'Feminin';
  classId: string;
  className: string;
  isActive: boolean;
}

export interface Subject {
  id: string;
  name: string;
  coefficient: number;
}

export interface Grade {
  id: string;
  studentId: string;
  studentFullName: string;
  subjectId: string;
  subjectName: string;
  score: number;
  maxScore: number;
  coefficient: number;
  type: string;
  term: string;
  evaluatedAt: string;
  comment: string | null;
}

export interface StudentAverage {
  studentId: string;
  studentFullName: string;
  subjectName: string;
  average: number;
  gradeCount: number;
}

export interface StudentGeneralAverage {
  studentId: string;
  studentFullName: string;
  generalAverage: number;
  subjectAverages: StudentAverage[];
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Director' | 'Teacher' | 'Parent';
}

export interface AcademicYear {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
}

export interface AcademicTerm {
  id: string;
  name: string;
  order: number;
  startDate: string;
  endDate: string;
  academicYearId: string;
  academicYearName: string;
}

export interface EducationSettings {
  id: string;
  schoolName: string;
  address: string | null;
  currency: string;
  defaultMaxScore: number;
}
