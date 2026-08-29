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

export interface StudentCategory {
  id: string;
  name: string;
  description: string | null;
}

export interface StudentBatch {
  id: string;
  name: string;
  startDate: string;
  endDate: string | null;
  description: string | null;
  academicYearId: string;
  academicYearName: string;
}

export interface StudentGroup {
  id: string;
  name: string;
  groupType: string;
  maxSize: number | null;
  academicYearId: string;
  academicYearName: string;
  classId: string | null;
  className: string | null;
  memberCount: number;
}

export interface StudentGroupMember {
  id: string;
  studentId: string;
  studentFullName: string;
}

export interface StudentLog {
  id: string;
  studentId: string;
  logDate: string;
  logType: string;
  description: string;
}

export type AdmissionStatus = 'Submitted' | 'UnderReview' | 'Accepted' | 'Rejected' | 'Enrolled';

export interface StudentApplicant {
  id: string;
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
  academicYearName: string;
  appliedDate: string;
  status: AdmissionStatus;
  decisionDate: string | null;
  decisionNotes: string | null;
  convertedStudentId: string | null;
}

export interface Teacher {
  id: string;
  fullName: string;
  specialty: string;
}

export interface Program {
  id: string;
  name: string;
  code: string;
  description: string | null;
  isActive: boolean;
  classCount: number;
  courseCount: number;
}

export interface Room {
  id: string;
  name: string;
  capacity: number;
  building: string | null;
}

export interface Topic {
  id: string;
  name: string;
  description: string | null;
  order: number;
}

export interface Course {
  id: string;
  name: string;
  code: string | null;
  description: string | null;
  subjectId: string;
  subjectName: string;
  programId: string;
  programName: string;
  topics: Topic[];
}

export interface CourseSchedule {
  id: string;
  courseId: string;
  courseName: string;
  roomId: string;
  roomName: string;
  teacherId: string;
  teacherName: string;
  classId: string | null;
  className: string | null;
  academicTermId: string;
  academicTermName: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export type EnrollmentStatus = 'Active' | 'Completed' | 'Withdrawn';

export interface ProgramEnrollment {
  id: string;
  studentId: string;
  studentFullName: string;
  programId: string;
  programName: string;
  academicYearId: string;
  academicYearName: string;
  enrollmentDate: string;
  status: EnrollmentStatus;
}
